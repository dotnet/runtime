# Running XUnit tests cross platform

Unlike Windows, where we run tests as part of the build, we have a separate
explicit testing step on Linux and OSX. Over time, this special step will go
away in favor of a similar "run tests during the build" model.

`run-test.sh` is the shell script used by Jenkins in order to run all the XUnit
tests cross platform. It combines the cross platform CoreCLR and CoreFX builds
together into a test layout and then runs each test project from CoreFX.

In order to run tests, you need to build a bunch of different projects. The
instructions assume you are building for Linux, but are easily modifiable for OSX.

1. Release or debug CoreCLR. In Jenkins we use a release CoreCLR build instead
   of debug CoreCLR since it is much faster at actually running XUnit, but debug
   will work if you have the time.

   From the root of your CoreCLR repo on Linux, run `./build.sh Release` in
   order to build.
2. A corresponding version of System.Private.Corelib.dll. Depending on your platform, this may
   be produced when you run  `build.sh`. Otherwise, this can be produced by
   running `build.cmd linuxmscorlib Release` (it's `mscorlib` for historical reasons) from a CoreCLR repo on
   Windows. Remember that the runtime and System.Private.Corelib are tightly coupled with
   respect to object sizes and layout so you need to ensure you have either a
   release coreclr and release System.Private.Corelib or debug coreclr and debug System.Private.Corelib.
3. A Linux build of CoreFX. We currently have experimental support for building
   CoreFX on Linux via `build.sh`.
   The other option is:

   * Build the managed parts of CoreFX on Windows. To do so run `build.cmd /p:BuildNative=false -os=Linux`. It is okay to build a Debug version of CoreFX and run it
   on top of a release CoreCLR (which is exactly what we do in Jenkins).

   * Build the native parts of CoreFX on Linux. To do so run `./src/Native/build-native.sh` from the root of your CoreFX repo.

4. The packages folder which contains all the packages restored from NuGet and
   MyGet when building CoreFX.


After building all the projects, we need to copy any of the files we built on Windows
over to our Linux machine. The easiest way to do this is to mount a windows
share on linux. For example, I do:

```
# sudo mount.cifs "//MATELL3/D\$" ~/mnt/matell3/d -o user=matell
```

If needed, copy CoreFX:

```
# rsync -v -r ~/mnt/matell3/d/git/corefx/bin/tests ~/git/corefx/bin/tests
```

If needed, copy the packages folder:

```
# rsync -v -f ~/mnt/matell3/d/git/corefx/packages ~/git/corefx/packages
```

If needed, copy System.Private.Corelib:
```
# rsync -v -r  ~/mnt/matell3/d/git/coreclr/bin/Product/ ~/git/coreclr/bin/Product/
```

Then, run the tests. We need to pass an explicit path to the location of CoreCLR.

```
# ./run-test.sh --coreclr-bins ~/git/coreclr/bin/Product/Linux.x64.Release
```

run-test.sh should now invoke all the managed tests.
