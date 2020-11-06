# Running Tests using Mono Runtime

Before running any tests, build mono in the way that you would like to test with.
See the instructions for [Building Mono](../../building/mono/README.md)

## Running Runtime Tests

See the instructions for [Running runtime tests with Mono](../coreclr/testing-mono.md) Plus, there are additional mono runtime tests in mono/mono, but they
have not been moved over yet. 

## Running Library Tests
Running library tests against Mono is straightforward regardless of configuration.  Simply run the following commands:

1. cd into the test library of your choice (`cd src/libraries/<library>/tests`)

2. Run the tests

```
dotnet build /t:Test /p:RuntimeFlavor=mono /p:Configuration=<Release/Debug>
```

# Test with sample program
There is a HelloWorld sample program lives at

```
$(REPO_ROOT)/src/mono/netcore/sample/HelloWorld
```

This is a good way to write simple test programs and get a glimpse of how mono will work with the dotnet tooling.

To run that program, you could simply cd to that directory and execute

```
make run
```

Note that, it is configured with run with Release and LLVM mode by default. If you would like to work with other modes, 
you could edit the Makefile from that folder.
