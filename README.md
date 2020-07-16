# Natural Language Dialog

A tool to build narrations for games, similar to yarn spinner, twine and the likes.

This system provides a barebones syntax; the only logic it manages is branching and jumping, more complex logic is presented to the interpreter to be resolved.

### Interpreter

The interpreter is an object that receives commands (``#`` lines) for you to interpret and run game-specific behaviors, could be used to identify the speaker talking, run events, change variables, etc.

## The syntax

```
Normal dialog line
A second dialog line, presented after the first one
// Line entirely ignored by the engine, can be used as a comment to the person reading straight from the file
# A command, this line is sent to your interpreter
* A Choice
	Dialog line shown only if the user selected the choice above, must start with a tab character
	* A choice that only shows up if the query returned true    # query to the interpreter to ask if we can run this choice #
		Dialog line unique to the choice above this line
		// The line bellow will continue reading from the line starting with '= Some node' onward instead of continuing on the line below
		-> Some node
* Some other choice presented to the player alongside 'A Choice'
	Dialog line unique to that choice
Another normal line of dialog that runs after the choices have run their courses
Last line of this entire dialog chain as the next valid line is declaring a node

= Some node
// The above line defines a node, we can go to it by starting a line with '->' followed by the name of the node, leading and trailing whitespaces are ignored so '= Some node' and '=    Some node   ' are the same to the engine.
// The line below specifies that the reader should continue from wherever it came from before, if nothing came up before, the dialog ends
<- 
```

The dialog engine must warn when:
	``=`` content is whitespace
	``->`` goes to invalid nodes
	``#`` is whitespace or the interpreted returned that it was an invalid operation
	``<-`` lines contains any other non-whitespace characters, warn that they are ignored
	when there are leading spaces (not tabs) before ``*``/``#``/``->`` but still accept them as valid normal dialog lines just in case writers want to use those in certain contexts.