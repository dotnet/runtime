# Running ARM32 tests on modern hardware

One of our supported targets is 32-bit ARM. It can be quite challenging to construct a realistic ARM32 environment where you can build or run tests in a reasonable amount of time. Thankfully, it's possible to take some shortcuts, and do your build in a different environment, which reduces your need to 'somewhere I can run ARM32 binaries'. This is a rough explanation of one way to do that - building your runtime+tests in a cross-build container, and then running them in an ARM64 linux environment.

## Setting up a cross-build environment targeting ARM32

https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/cross-building.md#linux-cross-building provides detailed instructions on how to cross-build, but if you try to cross-build for ARM32 in particular, you're likely to encounter issues due to latent problems in our build scripts and dependencies. As such, here's a set of steps that worked for me:

1. Pull the `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-cross-arm-net9.0` docker image onto an x64 host.
2. Spawn a persistent container from the image. regular `docker run` won't work right, so you want to spawn a shell in the container and leave it running, i.e. `docker run -dit mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-cross-arm-net9.0 bash`
3. Attach to the now-running container. You can find it with `docker container list` and then `docker attach <container-hash>`. You are now in a cross-build ready environment that has an ARM32 cross-build root at `/crossrootfs/arm`.
4. `git clone` the runtime and check out the branch you wish to build.
5. Perform a cross build of the runtime with the right flags set to be able to also cross-build tests. The details are important here; if you miss one of these switches the test build will probably go looking for x64 linux binaries or things like that.

```bash
$ export ROOTFS_DIR=/crossrootfs/arm

$ ./build.sh clr+libs+host+packs -rc Release -lc Release -c Debug -arch arm -os linux -cross
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:07:59.69

$ ./src/tests/build.sh release arm -cross /p:LibrariesConfiguration=Release
Test build successful.
Test binaries are available at /home/runtime/artifacts/tests/coreclr/linux.arm.Release
To run all the tests use:
    src/tests/run.sh Release
To run a single test use:
    bash /home/runtime/artifacts/tests/coreclr/linux.arm.Release/__TEST_PATH__/__TEST_NAME__.sh -coreroot=
```

Make sure to match the `LibrariesConfiguration` for the tests build to the `-lc` configuration from the runtime build.

Now that you've successfully built the runtime and tests, you're going to want to grab the entire artifacts directory, and migrate it from this build environment to your execution environment. First, archive the whole artifacts directory from inside the container, in the existing shell you opened:

```bash
$ tar -acf /home/artifacts.tar.gz ./artifacts
```

This will take a while, the artifacts folder is big. Now outside of the container, grab the artifacts tarball from its filesystem using `docker cp`, and migrate it to the ARM64 environment where you will be running tests - i.e.:
```bash
$ docker cp <container-hash>:/home/artifacts.tar.gz ./artifacts.tar.gz

$ scp -i ~/.ssh/<identity>.pem ./artifacts.tar.gz <username>@<arm64 system>:/home/<username>/artifacts.tar.gz
```

This will take a while, the tarball is probably a few gigabytes. Once it's in the right place, you can migrate over to the ARM64 environment and get set up, i.e.:

```bash
$ git clone https://github.com/dotnet/runtime.git

$ cd runtime

$ tar -xzvf ~/artifacts.tar.gz
```

This will also take a while since cloning the runtime repo isn't fast and neither is unpacking a 4GB tarball, but once it's done, you're almost ready to run tests!

## Configuring your ARM64 environment to run ARM32 binaries

By default your ARM64 Linux install probably doesn't have support for ARM32 binaries enabled, which will cause running the binaries to fail with a cryptic error. So you'll need to add the architecture to dpkg and install some core userspace libraries that CoreCLR will need to actually run your tests, i.e.:

```bash
$ sudo dpkg --add-architecture armhf

$ sudo apt-get update
Reading package lists... Done

$ sudo apt-get install libc6:armhf libstdc++6:armhf libicu74:armhf
The following additional packages will be installed:
```

Note that when installing a package for another architecture, you need to suffix the package name with the architecture name. For me, the three packages above were sufficient to run an ARM32 JIT test.

## Running an ARM32 test in your ARM64 environment

We're finally ready to go, probably. Export an environment variable to point to your ARM32 core root, and then run your test, i.e.:

```bash
$ export CORE_ROOT=/home/kg/runtime/artifacts/tests/coreclr/linux.arm.Release/Tests/Core_Root/

$ bash artifacts/tests/coreclr/linux.arm.Release/JIT/Directed/StructABI/StructABI/StructABI.sh
BEGIN EXECUTION
/home/kg/runtime/artifacts/tests/coreclr/linux.arm.Release/Tests/Core_Root//corerun -p System.Reflection.Metadata.MetadataUpdater.IsSupported=false -p System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization=true StructABI.dll ''
Issue80393_HFA failed. Retval: f1=1 f3=3
```
