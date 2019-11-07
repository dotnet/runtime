# Running unit tests within Visual Studio

Sometimes it is convenient to run individual unit tests within the Visual Studio
IDE. First, build the repo from the command line to create artifacts and set up
the test environment. Then, use VS Test Explorer to run and debug tests.

## Steps

1. `build.cmd -test`
2. Open the solution file in the root of the repo.
3. Open the test explorer window within the Visual Studio IDE.
4. Select tests and run and/or debug.

## Limitations

* The managed projects load and build, but native and setup projects are not
  present in the solution and there's no way to trigger a build from inside VS.
* Rebuilding the native assets alone won't make them used during tests. The
  tests rely on the setup projects to assemble the native bits into a usable
  form, and they have to be rebuilt.
  * With a deep enough understanding of the test layout, you can work around
    this by copying native build outputs directly into the test layout.
