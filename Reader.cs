namespace NLDialog
{
	using System;
	using System.Collections.Generic;




	public class Reader
	{
		Stack<(TokenTree tree, int currentIndex)> _stack = new Stack<(TokenTree, int)>();
		List<Choice> _choices = new List<Choice>();



		public Reader( TokenTree root )
		{
			_stack.Push( ( root, 0 ) );
		}



		public bool MoveNext( IRunner runner )
		{
			if( _choices.Count > 0 )
			{
				throw new InvalidOperationException( $"Cannot call {nameof(MoveNext)} after a choice without calling {nameof(Choose)} beforehand" );
			}

			do
			{
				if( _stack.Count == 0 )
					return false;

				if( _stack.Peek().currentIndex >= _stack.Peek().tree.Children.Count )
				{
					if( _stack.Peek().tree is Node )
						return false;

					_stack.Pop();
					continue;
				}

				TokenData currentNode; 
				{
					(TokenTree tree, int currentIndex) = _stack.Peek();
					currentNode = tree.Children[ currentIndex ];
					if( currentNode is Node == false )
					{
						_stack.Pop();
						_stack.Push( ( tree, currentIndex + 1 ) );
					}
				}

				switch( currentNode )
				{
					case Line l: runner.NewLine( l.Text ); return true;
					case Command c:
					{
						bool condition = c.Children.Count > 0;
						bool canProcessChildren = runner.Command( c.Text, condition );
						if( canProcessChildren && condition )
							_stack.Push( ( c, 0 ) );
						continue;
					}
					case GoTo gt:
					{
						_stack.Push( ( gt.Destination, 0 ) );
						continue;
					}
					case GoBack _:
					{
						// Pop stack until we're just out of this node
						TokenTree n;
						do
						{
							if( _stack.Count == 0 )
								return false;

							n = _stack.Peek().tree;
							_stack.Pop();
						} while( n is Node == false );

						continue;
					}
					case Choice c:
					{
						_choices.Add( c );
						( TokenTree tree, int currentIndex ) = _stack.Peek();
						for( ; currentIndex < tree.Children.Count && tree.Children[ currentIndex ] is Choice otherC; currentIndex++ )
						{
							_choices.Add( otherC );
						}
						
						_stack.Pop();
						_stack.Push( ( tree, currentIndex ) );

						for( int i = _choices.Count - 1; i >= 0; i-- )
						{
							if( _choices[ i ] is ConditionalChoice cd && runner.Command( cd.Condition, true ) == false )
							{
								_choices.RemoveAt( i );
							}
						}
						
						if( _choices.Count == 0 )
							continue;

						runner.Choices( _choices.GetEnumerator(), this );
						return true;
					}
					case Node _: return false;
					case Comment _: continue;
				}
			} while( true );
		}



		public void Choose( int indexChosen )
		{
			_stack.Push( ( _choices[ indexChosen ], 0 ) );
			_choices.Clear();
		}
	}



	public interface IRunner
	{
		void NewLine( string line );
		bool Command( string command, bool isCondition );
		void Choices( List<Choice>.Enumerator choices, Reader reader );
	}
}