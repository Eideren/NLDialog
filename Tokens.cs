namespace NLDialogue
{
	using System.Collections.Generic;



	public class Line : TokenData
	{
		public string Text{ get; private set; }
		public Line( int sourceLine, int sourceChar, int length, string text ) : base( sourceLine, sourceChar, length )
		{
			Text = text;
		}
	}
	
	public class Command : TokenTree
	{
		public string Text{ get; private set; }
		public Command( int sourceLine, int sourceChar, int length, string text ) : base( sourceLine, sourceChar, length )
		{
			Text = text;
		}
	}
	
	public class Comment : TokenData
	{
		public string Text{ get; private set; }
		public Comment( int sourceLine, int sourceChar, int length, string text ) : base( sourceLine, sourceChar, length )
		{
			Text = text;
		}
	}
	
	public class GoTo : TokenData
	{
		public Scope Destination{ get; internal set; }
		public GoTo( int sourceLine, int sourceChar, int length, Scope destination ) : base( sourceLine, sourceChar, length )
		{
			Destination = destination;
		}
	}

	public class GoBack : TokenData
	{
		public GoBack( int sourceLine, int sourceChar, int length ) : base( sourceLine, sourceChar, length )
		{
			
		}
	}
	
	public class Choice : TokenTree
	{
		public string Text{ get; private set; }
		public Choice( int sourceLine, int sourceChar, int length, string text ) : base( sourceLine, sourceChar, length )
		{
			Text = text;
		}
	}
	
	public class ConditionalChoice : Choice
	{
		public string Condition{ get; private set; }
		public ConditionalChoice( int sourceLine, int sourceChar, int length, string condition, string text ) : base( sourceLine, sourceChar, length, text )
		{
			Condition = condition;
		}
	}
	
	public class Scope : TokenTree
	{
		public string Key{ get; protected set; }
		public Scope( int sourceLine, int sourceChar, int length, string key ) : base( sourceLine, sourceChar, length )
		{
			Key = key;
		}
	}
	
	public abstract class TokenTree : TokenData
	{
		public List<TokenData> Children{ get; private set; } = new List<TokenData>();
		public int RangeEnd { get; protected set; } = -1;
		protected TokenTree( int sourceLine, int sourceChar, int length ) : base( sourceLine, sourceChar, length )
		{
			
		}

		internal void SetRangeEnd( int end )
		{
			Length = end - SourceChar;
		}
	}



	public abstract class TokenData
	{
		/// <summary> The line where this token was found </summary>
		public int SourceLine{ get; protected set; }
		/// <summary> The character index this token sits at in the source </summary>
		public int SourceChar{ get; protected set; }
		/// <summary> Amount of character this token is made up of </summary>
		public int Length { get; protected set;  }



		protected TokenData( int sourceLine, int sourceChar, int length )
		{
			SourceLine = sourceLine;
			SourceChar = sourceChar;
			Length = length;
		}
	}
}