﻿# Natural Language Dialogue

A tool to build narrations for games, [ink](https://github.com/inkle/ink)'s more concise and friendly cousin. Similar to yarn spinner, twine and the likes.

This system provides a barebones syntax; the only logic it manages is the flow through branching and jumping, 
more complex logic is presented to your interpreter to be resolved.

### Interpreter

The interpreter is an object that you implement on your side to receive, parse and execute commands (``#`` lines) when they run, 
so those commands are your responsibility, you define them however you want to that way you can have game-specific behaviors, 
like defining the identify of a speaker, running events, changing variables, etc.

## The syntax
The syntax is not as permissive as other similar systems but it makes up for it by being more succinct and instinctive 
```
Normal Dialogue line
A second Dialogue line, presented after the first one
// Line entirely ignored by the engine, can be used as a comment to the person reading straight from the file

// Empty/whitespace lines like the one above are entirely skipped
# A command, this line is sent to your interpreter
* A Choice
	Dialogue line shown only if the user selected the choice above, must start with a tab character
	* A choice that only shows up if the query returned true    # query to the interpreter to ask if we can run this choice #
		Dialogue line unique to the choice above this line
		// The line bellow will continue reading from the line starting with '= Some scope' onward instead of continuing on the line below
		-> Some scope
* Some other choice presented to the player alongside 'A Choice'
	Dialogue line unique to that choice
Another normal line of Dialogue that runs after the choices have run their courses
Last line of this entire Dialogue chain as the next valid line is declaring a scope

= Some scope
// The above line defines a scope, we can jump to it by starting a line with ``->`` followed by the name of the scope, 
// leading and trailing whitespaces are ignored so ``= Some scope`` and ``=    Some scope   `` are the same to the engine.
// The line below specifies that the reader should continue from wherever it came from before, if nothing came up before, the Dialogue ends
<- 
```

Creating an instance of the parser will parse the provided stream, you can inspect the issues property to find out if there are any. 

The system won't throw if the issue is error worthy, it'll still do its best to run the content you gave it, 
so you're responsible for throwing if you deem some of those issues to warrant it. 

Here are the issues reported by the parser:
- Error ``UnexpectedIndentation``, the context around that line does not expect such a level of indentation, the parser could not predict its intent.
- Error ``TokenEmpty`` when ``#``, ``*``, ``->`` and ``=`` content is empty or made up of whitespace
- Error ``UnknownScope`` when ``->`` target was not found
- Issue ``InterpreterIssue`` when the content of ``#`` was reported by the interpreter as invalid
- Issue ``TokenNonEmpty`` when ``<-`` contains any other non-whitespace characters