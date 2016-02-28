Building CoreFX on FreeBSD, Linux and OS X
==========================================

Building CoreFX is pretty straightforward. Clone the repo and run the build script.

```sh
git clone https://github.com/dotnet/corefx.git
cd corefx
./build.sh
```

>These instructions have been validated on:
* Ubuntu 15.04, 14.04, and 12.04
* Fedora 22
* MacOS 10.10 (Yosemite)

# Known Issues
If you see errors along the lines of `SendFailure (Error writing headers)` you may need to import trusted root certificates:

```sh
mozroots --import --sync
```

System.Diagnostics.Debug.Tests does not build on Unix. https://github.com/dotnet/corefx/issues/1609
