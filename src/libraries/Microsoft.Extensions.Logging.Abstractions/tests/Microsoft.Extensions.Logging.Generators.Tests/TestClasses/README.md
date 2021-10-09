The source files in this directory serve two purposes:

1. They are used to trigger the source generator during compilation of the test suite itself. The resulting generated code
is then tested by LoggerMessageGeneratedCodeTests.cs. This ensures the generated code works reliably.

2.They are loaded as a file from `LoggerMessageGeneratorEmitterTests.cs`, and then fed manually to the parser and then the generator
This is used strictly to calculate code coverage attained by the first case above.
