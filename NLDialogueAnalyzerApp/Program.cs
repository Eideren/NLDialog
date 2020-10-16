namespace NLDialogueAnalyzerApp
{
	using System;
	using System.IO;
	using System.Threading;
	using NLDialogue;
	using System.Collections.Generic;
	using static System.Console;
	public static class Program
	{
		static Version LatestVersion;
		static FileSystemWatcher FileWatched;
		static SemaphoreSlim Semaphore = new SemaphoreSlim( 0 );



		class Version
		{
			public string Path;
		}



		public static void Main()
		{
			Version previousVersion = null;
			ASK_FOR_FILE:
			AskForFile();
			REDRAW:
			previousVersion = Volatile.Read( ref LatestVersion );
			Redraw(previousVersion.Path);
			while( true )
			{
				Semaphore.Wait( TimeSpan.FromMilliseconds( 200 ) );
				if( previousVersion != Volatile.Read( ref LatestVersion ) )
					goto REDRAW;
				if( KeyAvailable && ReadKey().Key == ConsoleKey.Escape )
					goto ASK_FOR_FILE;
			}
		}



		static void AskForFile()
		{
			Clear();
			string inputFile;
			do
			{
				WriteLine( "Please provide path to the file" );
				inputFile = ReadLine();
			} while( File.Exists( inputFile ) == false );
			SetFileToWatch( inputFile );
			Interlocked.Exchange( ref LatestVersion, new Version { Path = inputFile } );
		}



		static void Redraw( string path )
		{
			Clear();
			Parser nlParser;
			using( StreamReader sr = new StreamReader( path ) )
			{
				nlParser = new Parser( sr, new NullInterpreter() );
			}

			var tokens = nlParser.TokenAsLines();
			var issues = new List<(int line, Issue issue)>();
			foreach( Issue issue in nlParser.Issues )
			{
				issues.Add( (issue.SourceLine, issue) );
			}
			

			using( StreamReader sr = new StreamReader( path ) )
			{	
				string line;
				int currentLine = -1;
				while( (line = sr.ReadLine()) != null )
				{
					currentLine++;
					var token = tokens[ currentLine ];
					switch( token )
					{
						case Scope _: ForegroundColor = ConsoleColor.Blue; break;
						case null:
						case Line _: ForegroundColor = ConsoleColor.White; break;
						case Choice _: ForegroundColor = ConsoleColor.DarkCyan; break;
						case Comment _: ForegroundColor = ConsoleColor.DarkGreen; break;
						case Command _: ForegroundColor = ConsoleColor.DarkMagenta; break;
						case GoBack _:
						case GoTo _:
							ForegroundColor = ConsoleColor.DarkBlue;
							break;
						default: throw new NotImplementedException( token.GetType().ToString() );
					}
					Write(line);
					foreach( var issue in issues )
					{
						if (currentLine != issue.line)
							continue;
						
						ForegroundColor = issue.issue is Error ? ConsoleColor.Red : ConsoleColor.Yellow;
						Write('\t');
						Write(issue.issue.Text);
					}
					WriteLine();
				}
			}
		}



		static void SetFileToWatch( string path )
		{
			FileSystemWatcher fsw = new FileSystemWatcher
			{
				Path = Path.GetDirectoryName(path),
				Filter = Path.GetFileName(path),
				EnableRaisingEvents = true
			};
			fsw.Changed += ( s, e ) =>
			{
				Interlocked.Exchange( ref LatestVersion, new Version { Path = e.FullPath } );
				Semaphore.Release();
			};
			Interlocked.Exchange( ref FileWatched, fsw )?.Dispose();
		}
	}
}