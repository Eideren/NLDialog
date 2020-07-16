namespace NLDialog
{
	public abstract class Issue
	{
		public string Text{ get; private set; }
		public int SourceLine{ get; protected set; }
		public int SourceChar{ get; protected set; }
		protected Issue( int sourceLine, int sourceChar, string text )
		{
			Text = $"{GetType().Name}: {text}";
			SourceLine = sourceLine;
			SourceChar = sourceChar;
		}
	}



	public abstract class Error : Issue
	{

		protected Error( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar, text )
		{
		}
	}



	public class IndentationTooDeep : Error
	{
		public IndentationTooDeep( int sourceLine, int sourceChar ) : base( sourceLine, sourceChar, "Invalid indentation, the indentation is one or more level too deep, this might result in unexpected dialog flow" )
		{
			
		}
	}



	public class InvalidIndentation : Error
	{
		public InvalidIndentation( int sourceLine, int sourceChar ) : base( sourceLine, sourceChar, "Invalid indentation" )
		{
			
		}
	}
	
	
	
	public class TokenEmpty : Error
	{
		public TokenEmpty( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar, text )
		{
		}
	}
	
	
	
	public class TokenNonEmpty : Issue
	{
		public TokenNonEmpty( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar, text )
		{
		}
	}
	
	
	
	public class UnknownNode : Error
	{
		public UnknownNode( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar, $"Could not find node '{text}' in file" )
		{
		}
	}



	public abstract class InterpreterIssue : Issue
	{
		public object IssueObject{ get; private set; }
		
		protected InterpreterIssue( int sourceLine, int sourceChar, object issueObject ) : base( sourceLine, sourceChar, issueObject?.ToString() )
		{
			IssueObject = issueObject;
		}
	}



	public class FailedToInterpretCommand : InterpreterIssue
	{
		public FailedToInterpretCommand( int sourceLine, int sourceChar, object issueObject ) : base( sourceLine, sourceChar, issueObject )
		{
		}
	}



	public class FailedToInterpretConditional : InterpreterIssue
	{
		public FailedToInterpretConditional( int sourceLine, int sourceChar, object issueObject ) : base( sourceLine, sourceChar, issueObject )
		{
		}
	}
}