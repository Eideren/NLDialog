namespace NLDialogueRunnerApp
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using NLDialogue;
	using static System.Console;
	
	public static class Program
	{
		static Version? LatestVersion;
		static FileSystemWatcher? FileWatched;
		static SemaphoreSlim Semaphore = new SemaphoreSlim( 0 );



		class Version
		{
			public string Path;
			public Version( string path ) => Path = path;
		}



		public static void Main()
		{
			Runner runner = new Runner();
			ASK_FOR_FILE:
			AskForFile();
			
			REDRAW:
			var previousVersion = Volatile.Read( ref LatestVersion );
			var reader = Restart(previousVersion!.Path);

			do
			{
				// gameloop
				Semaphore.Wait( TimeSpan.FromMilliseconds( 50 ) );
				if( previousVersion != Volatile.Read( ref LatestVersion ) )
					goto REDRAW;

				if( reader.MoveNext( runner ) == false )
				{
					WriteLine( "Press escape to change file, press enter to restart" );
					switch( ReadKey().Key )
					{
						case ConsoleKey.Escape:
							goto ASK_FOR_FILE;
						case ConsoleKey.Enter:
							goto REDRAW;
					}
				}
			} while( true );
		}



		public class Runner : IRunner
		{
			public void NewLine( string line )
			{
				WriteLine( line );
				while( ReadKey( true ).Key != ConsoleKey.Enter )
				{
					
				}
			}



			public bool Command( string command, bool isCondition )
			{
				return true;
			}



			public void Choices( List<Choice>.Enumerator choices, Reader reader )
			{
				int max = 0;
				while(choices.MoveNext())
				{
					WriteLine( $"{max}: {choices.Current!.Text}" );
					max++;
				}

				int val;
				while( int.TryParse( ReadKey( true ).KeyChar.ToString(), out val ) == false || val >= max )
				{
					WriteLine( $"Write a digit from 0 to {max-1}" );
				}
				
				reader.Choose( val );
			}
		}



		static void AskForFile()
		{
			string inputFile;
			do
			{
				WriteLine( "Please provide path to the file" );
				inputFile = ReadLine() ?? "";
			} while( File.Exists( inputFile ) == false );
			inputFile = Path.GetFullPath( inputFile );
			SetFileToWatch( inputFile );
			Interlocked.Exchange( ref LatestVersion, new Version(inputFile) );
		}



		static Reader Restart( string path )
		{
			Clear();
			Parser nlParser;
			using( StreamReader sr = new StreamReader( path ) )
			{
				nlParser = new Parser( sr, new NullInterpreter() );
			}

			foreach( var issue in nlParser.Issues )
			{
				ForegroundColor = issue is Error ? ConsoleColor.Red : ConsoleColor.Yellow;
				WriteLine( $"@{issue.SourceLine},{issue.SourceChar}: {issue.Text}" );
			}

			if( nlParser.Issues.Count > 0 )
			{
				WriteLine();
				ResetColor();
			}

			return new Reader( nlParser.Root );
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
				Interlocked.Exchange( ref LatestVersion, new Version(e.FullPath) );
				Semaphore.Release();
			};
			Interlocked.Exchange( ref FileWatched, fsw )?.Dispose();
		}
	}
}