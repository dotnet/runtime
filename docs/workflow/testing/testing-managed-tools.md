# Testing managed tools

There are managed unit and functional tests for a number of tools including the
compiler for NativeAOT (`ILCompiler`), and the trimmer (`illink`).

## Adding new testsuites

To add a new test suite, create a new `.csproj` with a name that ends in `Tests`, such as:
`MyTool.Tests.csproj`.  The property `IsTestProject` will be set by the `Directories.Build.props` in
the repository root.  The property will, in turn, add references to the xunit package and the
apropriate test runner.

Now add a `ProjectToBuild` item in `eng/Substes.props` to one of the existing subsets, such as
`clr.toolstests`, or a new subset.

## Adding new testsuites to CI

To run the tests in CI, add a new pipeline or add to an exsiting pipeline such as `CLR_Tools_Tests`
in `eng/pipelines/runtime.yml`.  Update the trigger condition, perhaps by adding a new set of paths
to `eng/pipelines/common/evaluate-default-paths.yml` in order to run the tests when the tool source
or the test sources change.

## Running tests locally

Build and run the tests locally either with

```console
./build.[sh|cmd] -s clr.toolstests -c [Release|Debug] -build -test
```

or

```console
./dotnet.[sh|cmd] test .../MyTool.Tests.csproj -c [Release|Debug]
```

The `dotnet-test` xunit filter mechanisms work to run a single test or a subset of the tests

```console
./dotnet.[sh|cmd] test .../MyTool.Tests.csproj -c [Release|Debug] --filter "FullyQualifiedName~MyTest"
```

The above command runs all tests whose fully-qualified name contains the substring `MyTest`.  See
[dotnet test - Run selective unit tests](https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests?pivots=mstest#syntax)
for the full syntax.
