# Building native AOT apps in containers

Containers are a useful tool to build any kind of code, including native AOT. There are a few patterns for doing that.

## Cloud native apps

Native AOT apps can be built in containers (with multi-stage build) just like regular (CoreCLR) apps. SDK `aot` images provide the additional native toolchain dependencies required to build native AOT apps.

Search for `aot` images at [dotnet/nightly/sdk](https://mcr.microsoft.com/en-us/product/dotnet/nightly/sdk/tags).

The [`releasesapi`](https://github.com/dotnet/dotnet-docker/blob/main/samples/releasesapi/README.md) sample demonstrates how to build native AOT apps in containers.

## Cross-compiling

For cloud native apps, build and runtime OS typically match, at least if you use multi-stage build. Once you step out of containers (for app delivery), it is more likely that you are delivering binaries that you want to work in more places (like on older Linux distros).

The .NET build has this exact same need. We produce several [container images to enable cross-building](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/linux-instructions.md#docker-images).

You can use these images to build native AOT apps which work on distros as old as Ubuntu 16.04. These build images are not supported, but are expected to work (since we use them to build .NET on daily basis).

### Containerized build

The following Dockerfiles demonstrate how to construct a working build environment that can be used for volume-mounted docker builds. They can be modified to use the other image flavors we provide, like Alpine.

- [Dockerfile.cross-build-x64-arm64](Dockerfile.cross-build-x64-arm64)
- [Dockerfile.cross-build-x64-x64](Dockerfile.cross-build-x64-arm64)

### x64

The following pattern demonstrates building an app x64.

Build a cross-build image for the x64 target.

```bash
$ docker build --pull -t cross-build-x64 -f Dockerfile.cross-build-x64-x64 .
```

Then navigate to the directory where your project is located. This example creates one via `dotnet new` for demonstration purposes.

```bash
$ mkdir cross-build-test
$ cd cross-build-test/
$ dotnet new console --aot
```

Build the app, with volume mounting

```bash
$ docker run --rm -it -v $(pwd):/source -w /source cross-build-x64 dotnet publish -o app -p:SysRoot=/crossrootfs/x64 -p:LinkerFlavor=lld 
$ ls -l app/
total 4004
-rwxr-xr-x 1 root root 1407576 Oct 19 10:25 cross-build-test
-rwxr-xr-x 1 root root 2689720 Oct 19 10:25 cross-build-test.dbg
$ ./app/cross-build-test
Hello, World!
```

The app can be tested in an old Linux container, again through volume mounting.

```bash
$ docker run --rm -v $(pwd)/app:/app -w /app ubuntu:16.04 ./cross-build-test
Hello, World!
```

### Arm64

The same pattern can be used for Arm64. The differences are demonstrated below, building the same console app.

Build a cross-build image for the Arm64 target.

```bash
$ docker build --pull -t cross-build-arm64 -f Dockerfile.cross-build-x64-arm64 .
```

Build the app, with volume mounting.

```bash
$ docker run --rm -it -v $(pwd):/source -w /source cross-build-arm64 dotnet publish -a arm64 -o app-arm64 -p:SysRoot=/crossrootfs/arm64 -p:LinkerFlavor=lld 
```

Notice all the places where `arm64` is used in that command.

The resulting executable won't run on an x64 machine and must be run on a Linux Arm64 machine (because that is what it targets). The binary can be inspected to validate its nature.

```bash
$ file app-arm64/cross-build-test
app-arm64/cross-build-test: ELF 64-bit LSB pie executable, ARM aarch64, version 1 (SYSV), dynamically linked, interpreter /lib/ld-linux-aarch64.so.1, for GNU/Linux 3.7.0, BuildID[sha1]=702dc1a1411a3fe5aed949b7e1536fd92475010c, stripped
$ ./app-arm64/cross-build-test
-bash: ./app-arm64/cross-build-test: cannot execute binary file: Exec format error
```
