# Building native AOT apps in containers

Containers are a useful tool to build any kind of code, including native AOT. There are a few patterns for doing that.

## Cloud native apps

Native AOT apps can be built in containers (with multi-stage build) just like regular (CoreCLR) apps. SDK `aot` images provide the additional native toolchain dependencies required to build native AOT apps.

The [`releasesapi`](https://github.com/dotnet/dotnet-docker/blob/main/samples/releasesapi/README.md) sample demonstrates how to build native AOT apps in containers.

## Cross-compiling

For cloud native apps, build and runtime OS likely match, at least if you use multi-stage build. Once you step out of containers (for app delivery), it is more likely that you are delivering binaries that you want to work in more places (like on older Linux distros).

The .NET build has this exact same need. We produce several [container images to enable cross-building](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/linux-instructions.md).

You can use these images to build native AOT apps which work on distros as old as Ubuntu 16.04. The images are not supported, but are expected to work (since we use them to build .NET on daily basis).

The following Dockerfiles demonstrate how to construct a working build environment that could be use for volume-mounted docker builds.

- [Dockerfile.cross-build-x64-arm64](Dockerfile.cross-build-x64-arm64)
- [Dockerfile.cross-build-x64-x64](Dockerfile.cross-build-x64-arm64)


The following Dockerfile provides a complete proof of concept for building an app and "deploying" it to Ubuntu 16.04.

- [Dockerfile.cross-build-ubuntu-1604](Dockerfile.cross-build-ubuntu-1604)

This Dockerfile is written as if it was run from [this directory](https://github.com/dotnet/dotnet-docker/tree/main/samples/releasesapi).
