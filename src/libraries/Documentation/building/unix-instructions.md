Building CoreFX on FreeBSD, Linux and OS X
==========================================
###Building
Calling the script `build.sh` builds both the native and managed code.
Only use it when the parameters that you are passing to the script apply for both components. Otherwise, use the scripts `build-native.sh` and `build-managed.sh` respectively.

For more information about the different options when building, run `build.sh -?` and look at examples in the [developer-guide](../project-docs/developer-guide.md).

### Prerequisites (native build)

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

On OS X, all the needed libraries are provided by XCode, with the exception of
openssl. To install openssl, we recommend that you use
[Homebrew](http://brew.sh) and do the following:

```
brew install openssl
brew link --force openssl
```

Once installed, the native components can be built by running:

```bash
./build-native.sh
```

from the root of the repository.

### Prerequisites (managed build)

For Ubuntu 14.04, install the following packages:

* libunwind8
* libicu52
* curl

`sudo apt-get install libunwind8 libicu52 curl`

In addition to the above packages, the runtime versions of the packages listed
in the native section should also be installed (this happens automatically on
most systems when you install the development packages).

On OS X, we also require that openssl has been installed via [Homebrew](http://brew.sh).

Once installed, the managed components can be built by running:

```bash
./build-managed.sh
```

### Known Issues
If you see errors along the lines of `SendFailure (Error writing headers)` you may need to import trusted root certificates:

```sh
mozroots --import --sync
```

## Steps to build on Ubuntu 14.04 LTS


1. Install the prerequisites
 * `sudo apt-get install git clang-3.5 cmake make libc6-dev libssl-dev
   libkrb5-dev libcurl4-openssl-dev zlib1g-dev libunwind8 libicu52 curl`
2. Clone the corefx repo `git clone https://github.com/dotnet/corefx.git`
3. Navigate to the `corefx` directory
4. Run the build script `./build.sh`
