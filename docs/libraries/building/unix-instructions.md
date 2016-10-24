Building CoreFX on FreeBSD, Linux and OS X
==========================================
##Building

1. Install the prerequisites ([Linux](#user-content-linux), [macOS](#user-content-macos))
2. Clone the corefx repo `git clone https://github.com/dotnet/corefx.git`
3. Navigate to the `corefx` directory
4. Run the build script `./build.sh`

Calling the script `build.sh` builds both the native and managed code.
Only use it when the parameters that you are passing to the script apply for both components. Otherwise, use the scripts `build-native.sh` and `build-managed.sh` respectively.

For more information about the different options when building, run `build.sh -?` and look at examples in the [developer-guide](../project-docs/developer-guide.md).

## Prerequisites (native build)

### Linux

#### Native build

For Ubuntu 14.04, the following packages should be installed to build the native
components

* git
* clang-3.5
* cmake
* make
* libc6-dev
* libssl-dev
* libkrb5-dev
* libcurl4-openssl-dev
* zlib1g-dev

`sudo apt-get install git clang-3.5 cmake make libc6-dev libssl-dev libkrb5-dev
libcurl4-openssl-dev zlib1g-dev`

#### Managed build

For Ubuntu 14.04, install the following packages:

* libunwind8
* libicu52
* curl

`sudo apt-get install libunwind8 libicu52 curl`

For Ubuntu 16.x you may need to replace libicu52 with libicu57.

In addition to the above packages, the runtime versions of the packages listed
in the native section should also be installed (this happens automatically on
most systems when you install the development packages).

### macOS

On macOS a few components are needed which are not provided by a default developer setup:
* CMake
* pkgconfig
* OpenSSL 1.0.1 or 1.0.2

One way of obtaining these components is via [Homebrew](http://brew.sh):
```sh
$ brew install cmake pkgconfig openssl
```

As of El Capitan (OS X 10.11), Apple still has the libraries for OpenSSL 0.9.8 in `/usr/lib`,
but the headers are no longer available since that library version is out of support.
Some compilers get upset over new headers being in `/usr/local/include` with the old library being present at
`/usr/lib/libcrypto.dylib` (the tools have no issue with the versioned files, e.g. `/usr/lib/libcrypto.0.9.8.dylib`),
and so Homebrew does not allow the OpenSSL package to be installed into system default paths. A minimal installation
is presented here to facilitiate simplifying runtime requirements and compile-time requirements (for build systems using
CMake's `find_package`, like ours):
```sh
# We need to make the runtime libraries discoverable, as well as make
# pkg-config be able to find the headers and current ABI version.
#
# Ensure the paths we will need exist
mkdir -p /usr/local/lib/pkgconfig

# The rest of these instructions assume a default Homebrew path of
# /usr/local/opt/<module>, with /usr/local being the answer to
# `brew --prefix`.
#
# Runtime dependencies
ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/
ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/

# Compile-time dependencies (for pkg-config)
ln -s /usr/local/opt/openssl/lib/pkgconfig/libcrypto.pc /usr/local/lib/pkgconfig/
ln -s /usr/local/opt/openssl/lib/pkgconfig/libssl.pc /usr/local/lib/pkgconfig/
ln -s /usr/local/opt/openssl/lib/pkgconfig/openssl.pc /usr/local/lib/pkgconfig/
```

### Known Issues
If you see errors along the lines of `SendFailure (Error writing headers)` you may need to import trusted root certificates:

```sh
mozroots --import --sync
```

