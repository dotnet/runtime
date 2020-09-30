# Running Tests using Mono Runtime

## Running Runtime Tests
We currently only support running tests against coreclr.  There are additional mono runtime tests in mono/mono, but they
have not been moved over yet. Simply run the following command:

```
dotnet build /t:RunCoreClrTests $(REPO_ROOT)/src/mono/mono.proj
```

If you want to run individual tests, execute this command:

```
dotnet build /t:RunCoreClrTest /p:CoreClrTest="<TestName>" $(REPO_ROOT)/src/mono/mono.proj
```

## Running Library Tests
Running library tests against Mono is straightforward regardless of configuration.  Simply run the following commands:

1. Build and set the RuntimeFlavor

```bash
./build.sh /p:RuntimeFlavor=mono
```
or on Windows
```bat
build.cmd /p:RuntimeFlavor=mono
```

2. cd into the test library of your choice (`cd src/libraries/<library>/tests`)

3. Run the tests

```
dotnet build /t:Test /p:RuntimeFlavor=mono
```

# Patching Local dotnet (.dotnet-mono)
Another way to test mono out is by 'patching' a local dotnet with our runtime bits.  This is a good way to write simple
test programs and get a glimpse of how mono will work with the dotnet tooling.

To generate a local .dotnet-mono, execute this command:

```
dotnet build /t:PatchLocalMonoDotnet $(REPO_ROOT)/src/mono/mono.proj
```

You can then, for example, run our HelloWorld sample via:

```
dotnet build -c Release $(REPO_ROOT)/src/mono/netcore/sample/HelloWorld
MONO_ENV_OPTIONS="" COMPlus_DebugWriteToStdErr=1 \
$(REPO_ROOT)/.dotnet-mono/dotnet $(REPO_ROOT)/src/mono/netcore/sample/HelloWorld/bin/HelloWorld.dll
```
