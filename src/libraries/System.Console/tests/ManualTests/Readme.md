# System.Console manual tests

For verifying console functionality that cannot be run as fully automated.
To run the suite, follow these steps:

1. Build the CLR and libraries.
2. Using a terminal, navigate to the current folder.
3. Enable manual testing by defining the `MANUAL_TESTS` environment variable (e.g. on bash `export MANUAL_TESTS=true`).
4. Run `dotnet test` and follow the instructions in the command prompt.

## Instructions for Windows testers

VsTest on Windows redirects console input, so in order to properly execute the manual tests, 
`xunit-console` must be invoked directly. To do this first run

```
> dotnet build -t:Test
```

And then copy and execute the commands logged under the `To repro directly:` section of the output logs.

## Instructions for MacOS testers

By default, Alt-Key does not work on the MacOS terminal.
Before running the tests, navigate to `Terminal > Preferences > Settings > Keyboard`
and check "Use option as meta key" at the bottom.
