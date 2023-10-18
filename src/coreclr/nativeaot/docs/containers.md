# Building native AOT apps in containers

Containers are a useful tool to build any kind of code, including native AOT. There are a few patterns for doing that.

## Cloud native apps

Native AOT apps can be built in containers (with multi-stage build) just like regular (CoreCLR) apps. SDK `aot` images provide the additional native toolchain dependencies required to build native AOT apps.

Search for `aot` images at [dotnet/nightly/sdk](https://mcr.microsoft.com/en-us/product/dotnet/nightly/sdk/tags).

The [`releasesapi`](https://github.com/dotnet/dotnet-docker/blob/main/samples/releasesapi/README.md) sample demonstrates how to build native AOT apps in containers.

## Cross-compiling

For cloud native apps, build and runtime OS typically match, at least if you use multi-stage build. Once you step out of containers (for app delivery), it is more likely that you are delivering binaries that you want to work in more places (like on older Linux distros).

The .NET build has this exact same need. We produce several [container images to enable cross-building](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/linux-instructions.md).

You can use these images to build native AOT apps which work on distros as old as Ubuntu 16.04. These build images are not supported, but are expected to work (since we use them to build .NET on daily basis).

### Containerized build

The following Dockerfiles demonstrate how to construct a working build environment that could be use for volume-mounted docker builds. They can be modified to use other images we provide, like for Alpine.

- [Dockerfile.cross-build-x64-arm64](Dockerfile.cross-build-x64-arm64)
- [Dockerfile.cross-build-x64-x64](Dockerfile.cross-build-x64-arm64)

### x64

The following pattern demonstrates the approach for x64.

First build image.

```bash
$ docker build --pull -t cross-build -f Dockerfile.cross-build-x64-x64 .
```

Then build the app, with volume mounting, using the [releasesapi](https://github.com/dotnet/dotnet-docker/tree/main/samples/releasesapi) sample.

```bash
$ docker run --rm -it -v $(pwd):/source -w /source cross-build dotnet publish -o app -p:SysRoot=/crossrootfs/x64 -p:LinkerFlavor=lld releasesapi.csproj
$  ls app
appsettings.Development.json  nuget.config  releasesapi.dbg
appsettings.json              releasesapi
$ ./app/releasesapi
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /home/rich/git/dotnet-docker/samples/releasesapi
```

We can test the app in a container, again through volume mounting.

```bash
$ docker run --rm -d -v $(pwd)/app:/app -w /app -p 8000:80 mcr.microsoft.com/dotnet/runtime-deps:6.0 ./releasesapi
219e1df9e66906531ee609b8cb9b9fa8eccef566a8682b897798d80b3905aaf5
$ curl http://localhost:8000/healthz
Healthy
$ docker exec 219e1df9e66906531ee609b8cb9b9fa8eccef566a8682b897798d80b3905aaf5 cat /etc/os-release | head -n 1
PRETTY_NAME="Debian GNU/Linux 11 (bullseye)"
$ docker kill 219e1df9e66906531ee609b8cb9b9fa8eccef566a8682b897798d80b3905aaf5
219e1df9e66906531ee609b8cb9b9fa8eccef566a8682b897798d80b3905aaf5
```

### Arm64

Much the same pattern can be used for `x64-arm64` Dockerfile.


```bash
$ docker build --pull -t cross-build-arm64 -f Dockerfile.cross-build-x64-arm64 .
$ docker run --rm -it -v $(pwd):/source -w /source cross-build-arm64 dotnet publish -a arm64 -o app-arm64 -p:SysRoot=/crossrootfs/arm64 -p:LinkerFlavor=lld releasesapi.csproj
$ ./app-arm64/releasesapi
-bash: ./app-arm64/releasesapi: cannot execute binary file: Exec format error
$ file app-arm64/releasesapi
app-arm64/releasesapi: ELF 64-bit LSB pie executable, ARM aarch64, version 1 (SYSV), dynamically linked, interpreter /lib/ld-linux-aarch64.so.1, for GNU/Linux 3.7.0, BuildID[sha1]=72212bcbd040059f1c2e6d55f640d52f7cbe2faf, stripped
```

Note the use of `-a arm64` and `-p:SysRoot=/crossrootfs/arm64` in the `dotnet publish` command. Those are required to get the build to use and generate Arm64 compatible assets.

The app can then be copied to an Arm64 and it will work as demonstrated on an Apple M1 machine.

```bash
$ docker run --rm  -d -v $(pwd):/app -w /app -p 8000:8080 mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot ./releasesapi
67fd87ef2975cd441e71ff4a3cd5dc288c4d5c9b000f2219a5c456d35c13c76e
$ curl http://localhost:8000/healthz
Healthy
$ docker kill 67fd87ef2975cd441e71ff4a3cd5dc288c4d5c9b000f2219a5c456d35c13c76e
67fd87ef2975cd441e71ff4a3cd5dc288c4d5c9b000f2219a5c456d35c13c76e
$ uname -a
Darwin Richs-Air.phantomdomain 23.0.0 Darwin Kernel Version 23.0.0: Fri Sep 15 14:41:34 PDT 2023; root:xnu-10002.1.13~1/RELEASE_ARM64_T8103 arm64
```

### Complete example

The following Dockerfile provides a complete proof of concept for building an app and "deploying" it to Ubuntu 16.04.

- [Dockerfile.cross-build-ubuntu-1604](Dockerfile.cross-build-ubuntu-1604)

This Dockerfile is written as if it was run from [this directory](https://github.com/dotnet/dotnet-docker/tree/main/samples/releasesapi).

It can be built and run with the following:

```bash
$ docker build --pull -t app -f Dockerfile.cross-build-ubuntu-1604 .
$ docker run --rm -it --entrypoint bash app -c "cat /etc/os-release | head -n 2"
NAME="Ubuntu"
VERSION="16.04.7 LTS (Xenial Xerus)"
docker run --rm -d -p 8000:8080 app
e72f93de034db4a1a12d25ce756c96235e5f584da8b1d440e9d6d083f8b5418d
$ curl http://localhost:8000/healthz
Healthy
$ docker kill e72f93de034db4a1a12d25ce756c96235e5f584da8b1d440e9d6d083f8b5418d
e72f93de034db4a1a12d25ce756c96235e5f584da8b1d440e9d6d083f8b5418d
```

The app also has a `releases` endpoint.
