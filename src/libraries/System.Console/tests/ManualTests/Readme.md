# System.Console manual tests

For verifying console functionality that cannot be run as fully automated.
To run the suite, follow these steps:

1. Build the CLR and libraries.
2. Using a terminal, navigate to the current folder.
3. Enable manual testing by defining the `MANUAL_TESTS` environment variable (e.g. on bash `export MANUAL_TESTS=true`).
4. Run `dotnet test` and follow the instructions in the command prompt.
