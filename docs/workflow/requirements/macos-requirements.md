Requirements to build dotnet/runtime on macOS
=====================

This guide will walk you through the requirements needed to build dotnet/runtime on macOS. We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions were validated on macOS 10.15 (Catalina).

Xcode
-----

Install Apple Xcode developer tools from the Mac App Store ([link](https://apps.apple.com/us/app/xcode/id497799835)).

Configure the Xcode command line tools:
Run Xcode, open Preferences, and on the Locations tab, change "Command Line Tools" to point to this installation of Xcode.app.
Alternately, run `sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer` in a terminal.
(Adjust the path if you renamed Xcode.app)

Toolchain Setup
---------------

Building dotnet/runtime depends on several tools to be installed. You can download them individually or use [Homebrew](https://brew.sh) for easier toolchain setup.

Install the following packages:

- cmake 3.15.5 or newer
- icu4c
- openssl@1.1 or openssl@3
- pkg-config
- python3
- ninja (optional, enables building native code with ninja instead of make)

You can install all the required packages above using Homebrew by running this command in the repository root:

```
brew bundle --no-lock --file eng/Brewfile
```
