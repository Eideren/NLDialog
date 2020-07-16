namespace NLDialog
{
	using System.Collections.Generic;



	public class Line : TokenData
	{
		public string Text{ get; private set; }
		public Line( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar )
		{
			Text = text;
		}
	}
	
	public class Command : TokenTree
	{
		public string Text{ get; private set; }
		public Command( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar )
		{
			Text = text;
		}
	}
	
	public class Comment : TokenData
	{
		public string Text{ get; private set; }
		public Comment( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar )
		{
			Text = text;
		}
	}
	
	public class GoTo : TokenData
	{
		public Node Destination{ get; internal set; }
		public GoTo( int sourceLine, int sourceChar, Node destination ) : base( sourceLine, sourceChar )
		{
			Destination = destination;
		}
	}

	public class GoBack : TokenData
	{
		public GoBack( int sourceLine, int sourceChar ) : base( sourceLine, sourceChar )
		{
			
		}
	}
	
	public class Choice : TokenTree
	{
		public string Text{ get; private set; }
		public Choice( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar )
		{
			Text = text;
		}
	}
	
	public class ConditionalChoice : Choice
	{
		public string Condition{ get; private set; }
		public ConditionalChoice( int sourceLine, int sourceChar, string condition, string text ) : base( sourceLine, sourceChar, text )
		{
			Condition = condition;
		}
	}
	
	public class Node : TokenTree
	{
		public string key{ get; private set; }
		public Node( int sourceLine, int sourceChar, string key ) : base( sourceLine, sourceChar )
		{
			
		}
	}
	
	public abstract class TokenTree : TokenData
	{
		public List<TokenData> Children{ get; private set; } = new List<TokenData>();
		protected TokenTree( int sourceLine, int sourceChar ) : base( sourceLine, sourceChar )
		{
			
		}
	}



	public abstract class TokenData
	{
		public int SourceLine{ get; protected set; }
		public int SourceChar{ get; protected set; }



		protected TokenData( int sourceLine, int sourceChar)
		{
			SourceLine = sourceLine;
			SourceChar = sourceChar;
		}
	}
}