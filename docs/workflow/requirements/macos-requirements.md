# Requirements to Set Up the Build Environment on macOS

- [Xcode Developer Tools](#xcode-developer-tools)
- [Toolchain Additional Dependencies](#toolchain-additional-dependencies)

To build the runtime repo on *macOS*, you will need to install the *Xcode* developer tools and a few other dependencies, described in the sections below.

## Xcode Developer Tools

- Install *Apple Xcode* developer tools from the [Mac App Store](https://apps.apple.com/app/xcode/id497799835).
- Configure the *Xcode* command line tools. You can do this via one of these two methods:
  - Run Xcode, open Preferences, and on the Locations tab, change `Command Line Tools` to point to this installation of _Xcode.app_. This usually comes already done by default, but it's always good to ensure.
  - Alternately, you can run `sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer` in a terminal. This command assumes your Xcode app is named `Xcode.app` as it comes by default. If you've renamed it to something else, adjust the path accordingly, then run the command.

## Toolchain Additional Dependencies

To build the runtime repo, you will also need to install the following dependencies:

- `CMake` 3.20 or newer
- `icu4c`
- `openssl@1.1` or `openssl@3`
- `pkg-config`
- `python3`
- `ninja` (This one is optional. It is an alternative tool to `make` for building native code)

You can install them separately, or you can alternatively opt to install *[Homebrew](https://brew.sh/)* and use the `install-dependencies.sh` script provided by the repo, which takes care of everything for you. If you go by this route, once you have *Homebrew* up and running on your machine, run the following command from the root of the repo to download and install all the necessary dependencies at once:

```bash
./eng/common/native/install-dependencies.sh
```
