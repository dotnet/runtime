# Running Tests using Mono Runtime

## Running Runtime Tests
The runtime tests will be available at a later date.

## Running Library Tests
Running library tests against Mono is straightforward regardless of configuration.  Simply run the following commands:

1. Build and set the TestRuntimeFlavor

```bash
./build.sh /p:TestRuntimeFlavor=mono
```
or on Windows
```bat
build.cmd /p:TestRuntimeFlavor=mono
```

2. cd into the test library of your choice (`cd src/libraries/<library>/tests`)

3. Run the tests

```
dotnet msbuild /t:BuildAndTest /p:TestRuntimeFlavor=mono
```
