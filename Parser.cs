namespace NLDialogue
{
	using System.Collections.Generic;
	using System.IO;



	public class Parser
	{
		public TokenTree Root{ get; private set; }
		public int TotalLines{ get; private set; }
		public List<Issue> Issues{ get; private set; } = new List<Issue>();
		public bool ContainsErrors{ get; private set; }



		public Parser( StreamReader content, IInterpreter interpreter )
		{
			StartParsing( content, interpreter );
		}



		public TokenData[] TokenAsLines()
		{
			TokenData[] output = new TokenData[ TotalLines ];
			RecursiveAssignTokenToArray( Root, output );
			return output;
		}



		void RecursiveAssignTokenToArray( TokenTree tree, TokenData[] output )
		{
			foreach( var data in tree.Children )
			{
				output[ data.SourceLine ] = data;
				if( data is TokenTree subTree )
				{
					RecursiveAssignTokenToArray( subTree, output );
				}
			}
		}



		void RecursiveCheckCommands( TokenTree tree, IInterpreter interpreter )
		{
			var children = tree.Children;
			for (int i = children.Count - 1; i >= 0; i--)
			{
				var data = children[i];
				if (data is Command cmd && interpreter.CanInterpretCommand(cmd.Text, out var warningObj) == false)
				{
					Issues.Add(new FailedToInterpretCommand(cmd.SourceLine, cmd.SourceChar, warningObj));
					children.RemoveAt(i);
				}

				if (data is TokenTree subTree)
				{
					RecursiveCheckCommands(subTree, interpreter);
				}
			}
		}



		void StartParsing( StreamReader sr, IInterpreter interpreter )
		{
			try
			{
				Root = new Scope( 0, 0, 0, null );
				
				var scopes = new Dictionary<string, Scope>();
				var goTos = new List<(int line, int charS, GoTo goTo, string key)>();

				{
					Stack<TokenTree> stack = new Stack<TokenTree>();
					stack.Push( Root );
					
					string line;
					int lineIndex = -1;
					int nextCharCount = 0;
					while( ( line = sr.ReadLine() ) != null )
					{
						TotalLines = ++lineIndex + 1;
						int charIndex = nextCharCount;
						nextCharCount += line.Length + 1/*The new line char*/;
						if( string.IsNullOrWhiteSpace( line ) )
							continue;
						
						int tabs = 1;
						int i = 0;
						for( ; i < line.Length; i++ )
						{
							if( line[ i ] == '\t' )
								tabs++;
							else if( char.IsWhiteSpace( line[ i ] ) == false )
								break;
						}

						charIndex += i;
						
						if( tabs > stack.Count )
						{
							Issues.Add( new UnexpectedIndentation( lineIndex, charIndex ) );
							continue;
						}

						// Manage scope
						{
							var topOfStack = stack.Peek();
							if( topOfStack is Choice || topOfStack is Command )
							{
								while( stack.Count > tabs )
								{
									topOfStack = stack.Peek();
									if( ( topOfStack is Choice || topOfStack is Command ) == false )
									{
										Issues.Add( new UnexpectedIndentation( lineIndex, charIndex ) );
										return;
									}
									stack.Pop().SetRangeEnd( charIndex );
								}
							}
						}

						char? nextChar = line.Length > i + 1 ? line[ i + 1 ] : (char?)null;
						switch(line[i], nextChar)
						{
							case ('=', _):
							{
								if( tabs != 1 )
									Issues.Add( new UnexpectedIndentation( lineIndex, charIndex ) );
							
								var start = i + 1;
								if( TrimWhitespace( line, ref start, out string text ) )
								{
									// Empty stack, from now on stack starts from this scope
									while( stack.Count != 0 )
										stack.Pop().SetRangeEnd( charIndex );

									var scope = new Scope( lineIndex, charIndex, line.Length - i, text );
									scopes.Add( text, scope );
									stack.Push( scope );
									// Push this scope on the base tree
									Root.Children.Add( scope );
								}
								else
								{
									Issues.Add( new TokenEmpty( lineIndex, charIndex, $"Looks like a {nameof(Scope)}, you should append a name to it" ) );
								}
								continue;
							}
							case ('#', _):
							{
								var start = i + 1;
								if( TrimWhitespace( line, ref start, out string text ) == false )
								{
									Issues.Add( new TokenEmpty( lineIndex, charIndex, $"Looks like you want to create a {nameof(Command)} here but you didn't provide the actual command" ) );
									continue;
								}
								
								Command token;
								var siblings = stack.Peek().Children;
								// Merge commands on the next lines into a single command
								if( siblings.Count > 0 && siblings[ siblings.Count - 1 ] is Command cmd )
								{
									siblings.RemoveAt( siblings.Count - 1 );
									token = new Command( cmd.SourceLine, cmd.SourceChar, ( charIndex + line.Length - i ) - cmd.SourceChar, $"{cmd.Text}\n{text}" );
								}
								else
								{
									token = new Command( lineIndex, charIndex, line.Length - i, text );
								}
								
								siblings.Add( token );
								stack.Push( token );
								continue;
							}
							case ('*', _):
							{
								var start = i + 1;
								if( TrimWhitespace( line, ref start, out string cleanLine ) == false )
								{
									Issues.Add( new TokenEmpty( lineIndex, charIndex, $"Looks like a {nameof(Choice)}, you must append a line of text to this choice" ) );
									continue;
								}
								
								var firstTokenId = cleanLine.IndexOf( '#' );
								var lastTokenId = cleanLine.Length - 1;

								Choice token;
								if( // Ends with a token character
									cleanLine[ lastTokenId ] == '#'
								    // and contains another one somewhere else in the line
								    && firstTokenId != lastTokenId )
								{
									firstTokenId += 1;
									if( TrimWhitespace( cleanLine, ref firstTokenId, out var conditionalText ) == false )
									{
										Issues.Add( new TokenEmpty( lineIndex, cleanLine.IndexOf( '#' ), $"Looks like a {nameof(ConditionalChoice)}, you must provide a command between its '#'" ) );
										continue;
									}

									// Clip last '#'
									conditionalText = conditionalText.Substring( 0, conditionalText.Length - 1 );
									
									if( interpreter.CanInterpretConditionalChoice( conditionalText, out var warningObj ) == false )
									{
										Issues.Add( new FailedToInterpretConditional( lineIndex, firstTokenId, warningObj ) );
										continue;
									}

									var text = cleanLine.Substring(0, cleanLine.IndexOf('#')).Trim();
									token = new ConditionalChoice( lineIndex, charIndex, line.Length - i, conditionalText, text );
								}
								else
								{
									token = new Choice( lineIndex, charIndex, line.Length - i, cleanLine );
								}

								
								stack.Peek().Children.Add( token );
								stack.Push( token );
								continue;
							}
							case ('/', '/'):
							{
								var start = i + 2;
								Comment comment;
								if( TrimWhitespace( line, ref start, out string text ) )
									comment = new Comment( lineIndex, charIndex, line.Length - i, text );
								else
									comment = new Comment( lineIndex, charIndex, 2, "" );
								stack.Peek().Children.Add( comment );
								continue;
							}
							case ('-', '>'):
							{
								var start = i + 2;
								if( TrimWhitespace( line, ref start, out string text ) )
								{
									var goTo = new GoTo( lineIndex, charIndex, line.Length - i, null );
									goTos.Add( (lineIndex, charIndex, goTo, text) );
									stack.Peek().Children.Add( goTo );
								}
								else
								{
									Issues.Add( new TokenEmpty( lineIndex, charIndex, $"This looks like a {nameof(GoTo)}, {nameof(GoTo)} requires a {nameof(Scope)} name to its right" ) );
								}
								continue;
							}
							case ('<', '-'):
							{
								var start = i + 2;
								if( TrimWhitespace( line, ref start, out string text ) )
									Issues.Add( new TokenNonEmpty( lineIndex, charIndex, text ) );
								
								stack.Peek().Children.Add( new GoBack( lineIndex, charIndex, 2 ) );
								continue;
							}
							default:
							{
								var start = i;
								if( TrimWhitespace( line, ref start, out string text ) )
									stack.Peek().Children.Add( new Line( lineIndex, charIndex, line.Length - i, text ) );
								continue;
							}
						} // switch
					} // while
					
					// Pop any remaining tokens from the stack and set their ranges
					while( stack.Count > 0 )
						stack.Pop().SetRangeEnd( nextCharCount - 1 /* exclude last new line */ );
					
				} // scope
				
				// Validate gotos
				foreach( (int line, int charS, GoTo goTo, string key) in goTos )
				{
					if( scopes.TryGetValue( key, out var val ) )
						goTo.Destination = val;
					else
						Issues.Add( new UnknownScope( line, charS, key ) );
				}
				
				// Validate commands
				RecursiveCheckCommands( Root, interpreter );
			}
			catch
			{
				ContainsErrors = true;
				throw;
			}
			finally
			{
				if( ContainsErrors == false )
				{
					foreach( var issue in Issues )
					{
						if( issue is Error )
						{
							ContainsErrors = true;
							break;
						}
					}
				}
				
				while( sr.ReadLine() != null )
					TotalLines++;
			}
		}
		
		static bool TrimWhitespace( string str, ref int start, out string output )
		{
			while( start < str.Length )
			{
				if( char.IsWhiteSpace( str[ start ] ) )
					start++;
				else
					break;
			}

			int end = str.Length - 1;
			while( end > start )
			{
				if( char.IsWhiteSpace( str[ end ] ) )
					end--;
				else
					break;
			}

			int length = end - start+1;
			if( length > 0 )
			{
				output = str.Substring( start, length );
				return true;
			}

			output = null;
			return false;
		}
	}
}