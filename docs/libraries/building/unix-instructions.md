Building CoreFX on FreeBSD, Linux and OS X
==========================================
## Building

1. Install the prerequisites ([Linux](#user-content-linux), [macOS](#user-content-macos))
2. Clone the corefx repo `git clone https://github.com/dotnet/corefx.git`
3. Navigate to the `corefx` directory
4. Run the build script `./build.sh`

Calling the script `build.sh` builds both the native and managed code.

For more information about the different options when building, run `build.sh --help` and look at examples in the [developer-guide](../project-docs/developer-guide.md).

## Minimum Hardware Requirements
- 2GB RAM

## Prerequisites (native build)

### Linux

First, the package lists might need to be updated

`sudo apt-get update`

#### Native build

For Ubuntu 14.04, the following packages should be installed to build the native
components

* git
* clang-3.9
* cmake
* make
* libc6-dev
* libssl-dev
* libkrb5-dev
* libcurl4-openssl-dev
* zlib1g-dev

`sudo apt-get install git clang-3.9 cmake make libc6-dev libssl-dev libkrb5-dev
libcurl4-openssl-dev zlib1g-dev`

#### Managed build

For Ubuntu 14.04, install the following packages:

* libunwind8
* libicu52
* curl

`sudo apt-get install libunwind8 libicu52 curl`

For Ubuntu 16.04 LTS / Bash on Ubuntu on Windows you may need to replace libicu52 with libicu55.
Ubuntu 16.10 and Ubuntu 17.04 will require libicu57.

`sudo apt-get install libunwind8 libicu55 curl`

For Ubuntu 18.04, you will also need to replace libicu52 with libicu60 and install libssl1.0-dev_1.0.2n-1ubuntu5.1_amd64.deb with dpkg-deb.

```sh
sudo apt-get install libunwind8 libicu60 curl
apt-get download libssl1.0-dev
sudo dpkg-deb -X libssl1.0-dev_1.0.2n-1ubuntu5.1_amd64.deb /
```

In addition to the above packages, the runtime versions of the packages listed
in the native section should also be installed (this happens automatically on
most systems when you install the development packages).

### macOS

macOS 10.12 or higher is needed to build corefx 2.x.

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
is presented here to facilitate simplifying runtime requirements and compile-time requirements (for build systems using
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

Alternatively, to avoid modifying /usr/local/ you can invoke cmake with the `OPENSSL_ROOT_DIR` env var set. The value to be passed in the directory where openssl is installed. Use `brew info openssl` to determine it. For example:

```none
$ brew info openssl
openssl: stable 1.0.2p (bottled) [keg-only]
SSL/TLS cryptography library
https://openssl.org/
/usr/local/Cellar/openssl/1.0.2l (1,709 files, 12.3MB)
  Poured from bottle on 2017-10-10 at 21:30:10
/usr/local/Cellar/openssl/1.0.2m (1,792 files, 12.3MB)
  Poured from bottle on 2017-11-06 at 17:45:21
/usr/local/Cellar/openssl/1.0.2n (1,792 files, 12.3MB)
  Poured from bottle on 2018-01-25 at 20:22:45
/usr/local/Cellar/openssl/1.0.2o_2 (1,792 files, 12.3MB)
  Poured from bottle on 2018-07-15 at 16:59:46
/usr/local/Cellar/openssl/1.0.2p (1,793 files, 12.3MB)
  Poured from bottle on 2018-08-31 at 19:37:10
```

With the above example, use the latest version path for `1.0.2p`:

```none
$ OPENSSL_ROOT_DIR=/usr/local/Cellar/openssl/1.0.2p ./build.sh
```

### Known Issues
If you see errors along the lines of `SendFailure (Error writing headers)` you may need to import trusted root certificates:

```sh
mozroots --import --sync
```

Bash on Ubuntu on Windows issues are tracked by: [#11057](https://github.com/dotnet/corefx/issues/11057)
