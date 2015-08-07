# Running XUnit tests cross platform

Unlike Windows, where we run tests as part of the build, we have a seperate
explicit testing step on Linux and OSX.  Over time, this special step will go
away in favor of a similar "run tests during the build" model.

`run-test.sh` is the shell script used by Jenkins in order to run all the XUnit
tests cross platform. It combines the cross platform CoreCLR and CoreFX builds
together into a test layout and then runs each test project from CoreFX.

In order to run tests, you need to build a bunch of different projects.  The
instructions assume you are building for Linux, but are easily modifiable for OSX.

1. Release or debug CoreCLR.  In Jenkins we use a release CoreCLR build instead
   of debug CoreCLR since it is much faster at actually running XUnit, but debug
   will work if you have the time.

   From the root of your CoreCLR enlistment on Linux, run `./build.sh Release` in
   order to build.
2. A coresponding version of mscorlib.dll, built on Windows but targeting
   Linux.  This can be produced by running `build.cmd linuxmscorlib Release` from
   a CoreCLR enlistment on Windows.  Remember that the runtime and mscorlib are
   tightly coupled with respect to object sizes and layout so you need to ensure
   you have either a release coreclr and release mscorlib or debug coreclr and
   debug mscorlib.
3. A Linux build of CoreFX.  On Windows, run `build.cmd /p:OSGroup=Linux`.  It
   is okay to build a Debug version of CoreFX and run it on top of a release
   CoreCLR (which is exactly what we do in Jenkins).
4. A Linux build of the native CoreFX components.  On Linux, run ./build.sh from
   src/Native in your CoreFX enlistment.

After building all the projects, we need to copy the files we built on Windows
over to our Linux machine.  The easiest way to do this is to mount a windows
share on linux.  For example, I do:

```
# sudo mount.cifs "//MATELL3/D\$" ~/mnt/matell3/d -o user=matell
```

The copy the CoreFX binaries over to your local machine (I have my enlistements
on Linux under ~/git).

```
# rsync -v -r --exclude 'obj' --exclude 'packages' ~/mnt/matell3/d/git/corefx/bin/ ~/git/corefx/bin/
# rsync -v -r  ~/mnt/matell3/d/git/coreclr/bin/Product/ ~/git/coreclr/bin/Product/
```

Then, run the tests.  run-test.sh defaults to wanting to use Windows tests (for
historical reasons), so we need to pass an explict path to the tests, as well as
a path to the location of CoreCLR and mscorlib.dll.

```
# ./run-test.sh --coreclr-bins ~/git/coreclr/bin/Product/Linux.x64.Release \
--mscorlib-bins ~/git/coreclr/bin/Product/Linux.x64.Release \
--corefx-tests ~/git/corefx/bin/tests/Linux.AnyCPU.Debug
```

run-test.sh should now invoke all the managed tests.
