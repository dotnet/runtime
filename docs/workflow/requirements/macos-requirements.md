# Requirements to build dotnet/runtime on macOS

* [Environment](#environment)
  * [Xcode](#xcode)
  * [Toolchain Setup](#toolchain-setup)

This guide will walk you through the requirements needed to build _dotnet/runtime_ on macOS. We'll start by showing how to set up your environment from scratch.

## Environment

Here are the components you will need to install and setup to work with the repo.

### Xcode

* Install Apple Xcode developer tools from the [Mac App Store](https://apps.apple.com/us/app/xcode/id497799835).
* Configure the Xcode command line tools:
  * Run Xcode, open Preferences, and on the Locations tab, change "Command Line Tools" to point to this installation of _Xcode.app_. This usually comes already done by default, but it's always good to ensure.
  * Alternately, you can run `sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer` in a terminal (Adjust the path if you renamed _Xcode.app_).

### Toolchain Setup

Building _dotnet/runtime_ depends on several tools to be installed. You can download them individually or use [Homebrew](https://brew.sh) for easier toolchain setup.

Install the following packages:

* CMake 3.15.5 or newer
* icu4c
* openssl@1.1 or openssl@3
* pkg-config
* python3
* ninja (optional, enables building native code with ninja instead of make)

You can install all the required packages above using _Homebrew_ by running this command in the repository root:

```bash
brew bundle --no-lock --file eng/Brewfile
```
