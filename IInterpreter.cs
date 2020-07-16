namespace NLDialogue
{
	public interface IInterpreter
	{
		bool CanInterpretCommand( string command, out object warning );
		bool CanInterpretConditionalChoice( string commandForChoice, out object warning );
	}



	/// <summary>
	/// An interpreter returning all commands as being interpretable
	/// </summary>
	public class NullInterpreter : IInterpreter
	{
		public bool CanInterpretCommand( string command, out object warning )
		{
			warning = null; 
			return true;
		}
		public bool CanInterpretConditionalChoice( string commandForChoice, out object warning )
		{
			warning = null; 
			return true;
		}
	}
}