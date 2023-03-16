# Building

This depends on `emsdk` to be installed.

## emsdk on macOS

* You can run `make provision-wasm`, which will install it to `$reporoot/src/mono/wasm/emsdk` .
Note: Irrespective of `$(EMSDK_PATH)`'s value, `provision-wasm` will always install into `$reporoot/src/mono/wasm/emsdk`.

`EMSDK_PATH` is set to `$reporoot/src/mono/wasm/emsdk` by default, by the Makefile.

Note: `EMSDK_PATH` is set by default in `src/mono/wasm/Makefile`, so building targets from that will have it set. But you might need to set it manually if
you are directly using the `dotnet build`, or `build.sh`.

* Alternatively you can install **correct version** yourself from the [Emscripten SDK guide](https://emscripten.org/docs/getting_started/downloads.html).
Do not install `latest` but rather specific version e.g. `./emsdk install 2.0.23`. See [emscripten-version.txt](./emscripten-version.txt)

Make sure to set `EMSDK_PATH` variable, whenever building, or running tests for wasm.

## Building on macOS

* To build the whole thing, with libraries:

`make build-all`

* To build just the runtime (useful when doing incremental builds with only runtime changes):

`make runtime`

**Note:** Additional msbuild arguments can be passed with: `make build-all MSBUILD_ARGS="/p:a=b"`

## emsdk on Windows

Windows build [requirements](https://github.com/dotnet/runtime/blob/main/docs/workflow/requirements/windows-requirements.md)

If `EMSDK_PATH` is not set, the `emsdk` should be provisioned automatically during the build.

**Note:** The EMSDK has an implicit dependency on Python for it to be initialized. A consequence of this is that if the system doesn't have Python installed prior to attempting a build, the automatic provisioning will fail and be in an invalid state. Therefore, if Python needs to be installed after a build attempt the `$reporoot/src/mono/wasm/emsdk` directory should be manually deleted and then a rebuild attempted.

## Building on Windows

* To build everything

`build.cmd -os browser -subset mono+libs` in the repo top level directory.

# Running tests

## Installation of JavaScript engines

The latest engines can be installed with jsvu (JavaScript engine Version Updater https://github.com/GoogleChromeLabs/jsvu)

### macOS

* Install npm with brew:

`brew install npm`

* Install jsvu with npm:

`npm install jsvu -g`

* Run jsvu and install `v8`, `SpiderMonkey`, or `JavaScriptCore` engines:

`jsvu`

Add `~/.jsvu` to your `PATH`:

`export PATH="${HOME}/.jsvu:${PATH}"`

### Windows

* Install node/npm from https://nodejs.org/en/ and add its npm and nodejs directories to the `PATH` environment variable

* Install jsvu with npm:

`npm install jsvu -g`

* Run jsvu and install `v8`, `SpiderMonkey`, or `JavaScriptCore` engines:

`jsvu`

* Add `~/.jsvu` to the `PATH` environment variable

## Libraries tests

Library tests can be run with js engines: `v8`, `SpiderMonkey`,or `JavaScriptCore`:

### macOS

* `v8`: `make run-tests-v8-$(lib_name)`
* SpiderMonkey: `make run-tests-sm-$(lib_name)`
* JavaScriptCore: `make run-tests-jsc-$(lib_name)`
* Or default: `make run-tests-$(lib_name)`. This runs the tests with `v8`.

For example, for `System.Collections.Concurrent`: `make run-tests-v8-System.Collections.Concurrent`

### Windows

Library tests on windows can be run as described in [testing-libraries](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing.md#testing-libraries) documentation. Without setting additional properties, it will run tests for all libraries on `v8` engine:

`.\build.cmd libs.tests -test -os browser`

* `JSEngine` property can be used to specify which engine to use. Right now `v8` and `SpiderMonkey` engines can be used.

Examples of running tests for individual libraries:

`.\dotnet.cmd build /t:Test /p:TargetOS=browser src\libraries\System.Collections.Concurrent\tests`
`.\dotnet.cmd build /t:Test /p:TargetOS=browser /p:JSEngine="SpiderMonkey" src\libraries\System.Text.Json\tests`

### Browser tests on macOS

Or they can be run with a browser (Chrome):

`make run-browser-tests-$(lib_name)`

Note: this needs `chromedriver`, and `Google Chrome` to be installed.

For example, for `System.Collections.Concurrent`: `make run-browser-tests-System.Collections.Concurrent`

These tests are run with `xharness wasm test-browser`, for running on the browser. And `xharness wasm test` for others.
The wrapper script used to actually run these tests, accepts:

`$XHARNESS_COMMAND`, which defaults to `test`.
`$XHARNESS_CLI_PATH` (see next section)

### Using a local build of xharness

* set `XHARNESS_CLI_PATH=/path/to/xharness/artifacts/bin/Microsoft.DotNet.XHarness.CLI/Debug/net7.0/Microsoft.DotNet.XHarness.CLI.dll`

**Note:** Additional msbuild arguments can be passed with: `make ..  MSBUILD_ARGS="/p:a=b"`

### Symbolicating traces

Exceptions thrown after the runtime starts get symbolicating from js itself. Exceptions before that, like asserts containing native traces get symbolicated by xharness using `src/mono/wasm/symbolicator`.

If you need to symbolicate some traces manually, then you need the corresponding `dotnet.js.symbols` file. Then:

```console
src/mono/wasm/symbolicator$ dotnet run /path/to/dotnet.js.symbols /path/to/file/with/traces
```

When not relinking, or not building with AOT, you can find `dotnet.js.symbols` in the runtime pack.

## Debugger tests on macOS

Debugger tests need `Google Chrome` to be installed.

`make run-debugger-tests`

To run a test with `FooBar` in the name:

`make run-debugger-tests TEST_FILTER=FooBar`

(See https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit for filter options)

Additional arguments for `dotnet test` can be passed via `MSBUILD_ARGS` or `TEST_ARGS`. For example `MSBUILD_ARGS="/p:WasmDebugLevel=5"`. Though only one of `TEST_ARGS`, or `TEST_FILTER` can be used at a time.

Chrome can be installed for testing by setting `InstallChromeForDebuggerTests=true` when building the tests.

## Run samples

The samples in `src/mono/sample/wasm` can be build and run like this:

* console Hello world sample

`dotnet build /t:RunSample console-v8/Wasm.Console.V8.Sample.csproj`

* browser TestMeaning sample

`dotnet build /t:RunSample browser/Wasm.Browser.Sample.csproj`

To build and run the samples with AOT, add `/p:RunAOTCompilation=true` to the above command lines.

* bench sample

Also check [bench](../sample/wasm/browser-bench/README.md) sample to measure mono/wasm runtime performance.

## Wasm App Host

[Use dotnet run to run wasm applications](host/README.md)

## Templates

The wasm templates, located in the `templates` directory, are templates for `dotnet new`, VS and VS for Mac. They are packaged and distributed as part of the `wasm-experimental` workload. We have 2 templates, `wasmbrowser` and `wasmconsole`, for browser and console WebAssembly applications.

For details about using `dotnet new` see the dotnet tool [documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-new).

To test changes in the templates, use `dotnet new install --force src/mono/wasm/templates/templates/browser`.

Example use of the `wasmconsole` template:

```console
> dotnet new wasmconsole
> dotnet publish
> cd bin/Debug/net7.0/browser-wasm/AppBundle
> node main.mjs
Hello World!
Args:
```

## Analyzing binary wasm files

We have few tools to analyze binary wasm files. The [wa-info](https://github.com/radekdoulik/wa-info#wa-info) and [wa-diff](https://github.com/radekdoulik/wa-info#wa-info) to analyze `dotnet.wasm` in the `AppBundle` directory, once you build your app. These can be easily [installed](https://github.com/radekdoulik/wa-info#installation) as dotnet tools.

They are handy to quickly disassemble functions and inspect webassembly module sections. The wa-diff is able to compare 2 wasm files, so you can for example check the effect of changes in your source code. You can see changes in the functions code as well as changes in sizes of sections and of code.

There is also the [wa-edit](https://github.com/radekdoulik/wa-info#wa-edit) tool, which is now used to prototype improved warm startup.

## Upgrading Emscripten

Bumping Emscripten version involves these steps:

* update https://github.com/dotnet/runtime/blob/main/src/mono/wasm/emscripten-version.txt
* bump emscripten versions in docker images in https://github.com/dotnet/dotnet-buildtools-prereqs-docker
* bump emscripten in https://github.com/dotnet/emsdk
* bump docker images in https://github.com/dotnet/icu, update emscripten files in eng/patches/
* update version number in docs
* update `Microsoft.NET.Runtime.Emscripten.<emscripten version>.Node.win-x64` package name, version and sha hash in https://github.com/dotnet/runtime/blob/main/eng/Version.Details.xml and in https://github.com/dotnet/runtime/blob/main/eng/Versions.props. the sha is the commit hash in https://github.com/dotnet/emsdk and the package version can be found at https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet6
* update packages in the workload manifest https://github.com/dotnet/runtime/blob/main/src/mono/nuget/Microsoft.NET.Workload.Mono.Toolchain.Current.Manifest/WorkloadManifest.json.in

## Upgrading NPM packages

Two things to keep in mind:

1. We use the Azure DevOps NPM registry (configured in `src/mono/wasm/runtime/.npmrc`).  When
   updating `package.json`, you will need to be logged in (see instructions for Windows and
   mac/Linux, below) in order for the registry to populate with the correct package versions.
   Otherwise, CI builds will fail.

2. Currently the Emscripten SDK uses NPM version 6 which creates `package-lock.json` files in the
  "v1" format.  When updating NPM packages, it is important to use this older version of NPM (for
  example by using the `emsdk_env.sh` script to set the right environment variables) or by using the
  `--lockfile-format=1` option with more recent versions of NPM.

### Windows

The steps below will download the `vsts-npm-auth` tool from https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-public-npm/connect/npm

In folder `src\mono\wasm\runtime\`

```sh
rm -rf node_modules
rm package-lock.json
npm install -g vsts-npm-auth`
vsts-npm-auth -config .npmrc
npm cache clean --force
npm outdated
npm update --lockfile-version=1
```

### mac/Linux

Go to https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-public-npm/connect/npm and log in and click on the "Other" tab.
Follow the instructions to set up your `~/.npmrc` with a personal authentication token.

In folder `src/mono/wasm/runtime/`

```sh
rm -rf node_modules
rm package-lock.json
npm cache clean --force
npm outdated
npm update --lockfile-version=1
```

## Code style

* Is enforced via [eslint](https://eslint.org/) and rules are in `./.eslintrc.js`
* You could check the style by running `npm run lint` in `src/mono/wasm/runtime` directory
* You can install [plugin into your VS Code](https://marketplace.visualstudio.com/items?itemName=dbaeumer.vscode-eslint) to show you the errors as you type

## Builds on CI

* For PRs, tests are generally triggered based on path changes. But if you have a change which would not trigger the relevant builds, then you can run `runtime-wasm` pipeline manually to run all of them. Comment `/azp run runtime-wasm` on the PR.

### How do I know which jobs run on CI, and when?

## PR:

* `only-pc` means `only on relevant path changes`

### Run by default

* `runtime` runs jobs only when relevant paths change. And for `AOT`, only smoke tests are run.

| .                 | runtime                       |
| ----------------- | --------------------          |
| libtests          | linux+windows: all,   only-pc |
| libtests eat      | linux+windows: smoke, only-pc |
| libtests aot      | linux+windows: smoke, only-pc |
| high resource aot | none                          |
| Wasm.Build.Tests  | linux+windows:        only-pc |
| Debugger tests    | linux+windows:        only-pc |
| Runtime tests     | linux+windows:        only-pc |

### Run manually with `/azp run ..`

* `runtime-wasm*` pipelines are triggered manually, and they only run the jobs that would not run on any default pipelines based on path changes.
* The `AOT` jobs run only smoke tests on `runtime`, and on `runtime-wasm*` pipelines all the `AOT` tests are run.

| .                 | runtime-wasm               | runtime-wasm-libtests | runtime-wasm-non-libtests |
| ----------------- | -------------------------- | --------------------  | --------------------      |
| libtests          | linux+windows: all         | linux+windows: all    | none                      |
| libtests eat      | linux:         all         | linux:         all    | none                      |
| libtests aot      | linux+windows: all         | linux+windows: all    | none                      |
| high resource aot | linux+windows: all         | linux+windows: all    | none                      |
| Wasm.Build.Tests  | linux+windows              | none                  | linux+windows             |
| Debugger tests    | linux+windows              | none                  | linux+windows             |
| Runtime tests     | linux                      | none                  | linux                     |
| Perftrace         | linux: all tests           | linux: all tests      | none                      |
| Multi-thread      | linux: all tests           | linux: all tests      | none                      |

* `runtime-extra-platforms` does not run any wasm jobs on PRs
* `high resource aot` runs a few specific library tests with AOT, that require more memory to AOT.

* `runtime-wasm-dbgtests` runs all the debugger test jobs

## Rolling build (twice a day):

* `runtime` runs all the wasm jobs, but `AOT` still only runs smoke tests.
* `runtime-extra-platforms` also runs by default. And it runs only the cases not covered by `runtime`.

| .                 | runtime                    | runtime-extra-platforms (always run) |
| ----------------- | -------------              | ------------------------------------ |
| libtests          | linux+windows: all         | none                                 |
| libtests eat      | linux: all                 | none                                 |
| libtests aot      | linux+windows: smoke       | linux+windows: all                   |
| high resource aot | none                       | linux+windows: all                   |
|                   |                            |                                      |
| Wasm.Build.Tests  | linux+windows              | none                                 |
| Debugger tests    | linux+windows              | none                                 |
| Runtime tests     | linux                      | none                                 |
| Perftrace         | linux: build only          | none                                 |
| Multi-thread      | linux: build only          | none                                 |

* `high resource aot` runs a few specific library tests with AOT, that require more memory to AOT.
