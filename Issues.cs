namespace NLDialogue
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



	public class UnexpectedIndentation : Error
	{
		public UnexpectedIndentation( int sourceLine, int sourceChar ) : base( sourceLine, sourceChar, "Invalid indentation; the indentation is one or more level too deep, this might result in unexpected Dialogue flow" )
		{
			
		}
	}
	
	
	
	public class TokenEmpty : Error
	{
		public TokenEmpty( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar, text )
		{
		}
	}
	
	
	
	public class UnknownScope : Error
	{
		public UnknownScope( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar, $"Could not find scope '{text}' in file" )
		{
		}
	}
	
	
	
	public class TokenNonEmpty : Issue
	{
		public TokenNonEmpty( int sourceLine, int sourceChar, string text ) : base( sourceLine, sourceChar, text )
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