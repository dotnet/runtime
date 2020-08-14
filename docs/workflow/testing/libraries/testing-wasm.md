# Testing Libraries on WebAssembly

In order to be able to run tests, the following JavaScript engines should be installed:
- V8
- JavaScriptCore
- SpiderMonkey

They can be installed as a part of [jsvu](https://github.com/GoogleChromeLabs/jsvu).

Please make sure that a JavaScript engine binary is available via command line,  
e.g. for V8:
```bash
$ v8
V8 version 8.5.62
```

If you use `jsvu`, first add its location to PATH variable  
e.g. for V8

```bash
PATH=/Users/<your_user>/.jsvu/:$PATH V8
```

## Building Libs and Tests for WebAssembly

Now we're ready to build everything for WebAssembly (for more details, please read [this document](../../building/libraries/webassembly-instructions.md#building-everything)):
```bash
./build.sh --arch wasm --os Browser -c release
```
and even run tests one by one for each library:
```
./build.sh --subset libs.tests -t --arch wasm --os Browser -c release
```

### Running individual test suites
The following shows how to run tests for a specific library
```
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=Release
```

### Running tests using different JavaScript engines
It's possible to set a JavaScript engine explicitly by adding `/p:JSEngine` property: 

```
./dotnet.sh build /t:Test src/libraries/System.AppContext/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=Release /p:JSEngine=SpiderMonkey
```

At the moment supported values are:
- `V8`
- `JavaScriptCore`
- `SpiderMonkey`

By default, `V8` engine is used.

### Test App Design
TBD

### Obtaining the logs
TBD

### Existing Limitations
TBD
