/*
Here's how dialog are written:

Normal dialog line
/ Line entirely ignored by the engine, can be used as a comment to the person reading straight from the file
# Line interpreted by the engine, could be used to identify the speaker talking, trigger stuff, apply changes to variables, etc.
* A Choice
	Dialog line shown only when selecting this choice
	* A choice that only shows up if the query returned true        # query to the engine to ask if we can run this choice #
		Dialog line unique to the choice above this line
		// The line bellow will force the engine to continue from the line starting with '= Some node' onward instead of continuing on the line below
		-> Some node
* Some other choice presented to the player alongside 'A Choice'
	Dialog line unique to that choice
Another normal line of dialog that runs after the choices have run their courses
Last line of this entire dialog chain as the next valid line is declaring a node

= Some node
// The above line defines a node, we can go to it by starting a line with '->' followed by the name of the node, leading and trailing whitespaces are ignored so '= Some node' and '=    Some node   ' are the same to the engine.
// The line below specifies that the reader should continue from wherever it came from before, if nothing came up before the dialog ends
<- 



The dialog engine must warn when:
	'=' content is whitespace
	'->' goes to invalid nodes.
	'#' is whitespace or maps to invalid operations.
	'<-' lines contains other non-whitespace characters, that they are ignored
	when there are leading spaces (not tabs) before '*'/'#'/'->' but still accept them as valid normal dialog lines just in case writers want to use those in certain contexts

 */

namespace NLDialog
{
	using System.Collections.Generic;
	using System.IO;
	using Project.Collection;



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



		void StartParsing( StreamReader sr, IInterpreter interpreter )
		{
			try
			{
				Root = new Node( 0, 0, null );
				Stack<TokenTree> stack = new Stack<TokenTree>();
				stack.Push( Root );
				
				var nodes = new Dictionary<string, Node>();
				var goTos = new List<(int line, int charS, GoTo goTo, string key)>();

				{
					string line;
					int currentLineIndex = -1;
					while( ( line = sr.ReadLine() ) != null )
					{
						TotalLines = ++currentLineIndex + 1;
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
						
						if( tabs > stack.Count )
						{
							Issues.Add( new IndentationTooDeep( currentLineIndex, i ) );
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
										Issues.Add( new InvalidIndentation( currentLineIndex, i ) );
										return;
									}

									stack.Pop();
								}
							}
						}

						char? nextChar = line.Length > i + 1 ? line[ i + 1 ] : (char?)null;
						switch(line[i], nextChar)
						{
							case ('=', _):
							{
								if( tabs != 1 )
									Issues.Add( new IndentationTooDeep( currentLineIndex, i ) );
							
								var start = i + 1;
								if( TrimWhitespace( line, ref start, out string text ) )
								{
									// Empty stack, from now on stack starts from this node
									while( stack.Count != 0 )
										stack.Pop();
									var node = new Node( currentLineIndex, start, text );
									nodes.Add( text, node );
									stack.Push( node );
									// Push this node on the base tree
									Root.Children.Add( node );
								}
								else
								{
									Issues.Add( new TokenEmpty( currentLineIndex, i, $"Looks like a {nameof(Node)}, you should append a name to it" ) );
								}
								continue;
							}
							case ('#', _):
							{
								var start = i + 1;
								if( TrimWhitespace( line, ref start, out string text ) == false )
								{
									Issues.Add( new TokenEmpty( currentLineIndex, i, $"Looks like you want to create a {nameof(Command)} here but you didn't provide the actual command" ) );
									continue;
								}
								
								if( interpreter.CanInterpretCommand( text, out var warningObj ) == false )
								{
									Issues.Add( new FailedToInterpretCommand( currentLineIndex, start, warningObj ) );
									continue;
								}
								
								var token = new Command( currentLineIndex, start, text );
								stack.Peek().Children.Add( token );
								stack.Push( token );
								continue;
							}
							case ('*', _):
							{
								var start = i + 1;
								if( TrimWhitespace( line, ref start, out string cleanLine ) == false )
								{
									Issues.Add( new TokenEmpty( currentLineIndex, i, $"Looks like a {nameof(Choice)}, you must append a line of text to this choice" ) );
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
										Issues.Add( new TokenEmpty( currentLineIndex, cleanLine.IndexOf( '#' ), $"Looks like a {nameof(ConditionalChoice)}, you must provide a command between its '#'" ) );
										continue;
									}

									// Clip last '#'
									conditionalText = conditionalText.Substring( 0, conditionalText.Length - 1 );
									
									if( interpreter.CanInterpretConditionalChoice( conditionalText, out var warningObj ) == false )
									{
										Issues.Add( new FailedToInterpretConditional( currentLineIndex, firstTokenId, warningObj ) );
										continue;
									}

									token = new ConditionalChoice( currentLineIndex, firstTokenId, conditionalText, cleanLine.Substring( 0, cleanLine.IndexOf( '#' ) ).Trim() );
								}
								else
								{
									token = new Choice( currentLineIndex, start, cleanLine );
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
									comment = new Comment( currentLineIndex, start, text );
								else
									comment = new Comment( currentLineIndex, i, "" );
								stack.Peek().Children.Add( comment );
								continue;
							}
							case ('-', '>'):
							{
								var start = i + 2;
								if( TrimWhitespace( line, ref start, out string text ) )
								{
									var goTo = new GoTo( currentLineIndex, start, null );
									goTos.Add( (currentLineIndex, i + 1, goTo, text) );
									stack.Peek().Children.Add( goTo );
								}
								else
								{
									Issues.Add( new TokenEmpty( currentLineIndex, i, $"Looks like a {nameof(GoTo)}, you must append the name of a node as a destination" ) );
								}
								continue;
							}
							case ('<', '-'):
							{
								var start = i + 2;
								if( TrimWhitespace( line, ref start, out string text ) )
									Issues.Add( new TokenNonEmpty( currentLineIndex, start, text ) );
								
								stack.Peek().Children.Add( new GoBack( currentLineIndex, i ) );
								continue;
							}
							default:
							{
								var start = i;
								if( TrimWhitespace( line, ref start, out string text ) )
									stack.Peek().Children.Add( new Line( currentLineIndex, start, text ) );
								continue;
							}
						} // switch
					} // while
				} // scope
				
				// Validate gotos
				foreach( (int line, int charS, GoTo goTo, string key) in goTos )
				{
					if( nodes.TryGetValue( key, out var val ) )
						goTo.Destination = val;
					else
						Issues.Add( new UnknownNode( line, charS, key ) );
				}
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