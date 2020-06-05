# Testing Libraries on WebAssembly

In order to be able to run tests, the following JavaScript engines should be installed:
- V8
- JavaScriptCore
- SpiderMonkey

They can be installed as a part of [jsvu](https://github.com/GoogleChromeLabs/jsvu).

Please make sure that a JavaScript engine binary is available via command line.  
In case of JSVU, just update PATH variable:
```bash
PATH=/Users/<your_user>/.jsvu/:$PATH 
```

## Building Libs and Tests for WebAssembly

Now we're ready to build everything for WebAssembly:
```bash
./build.sh --arch wasm --subset mono -c Release
```
and even run tests one by one for each library:
```
./build.sh --subset libs.tests -t --arch wasm --os Browser -c release
```

### Running individual test suites
The following shows how to run tests for a specific library
```
./dotnet.sh build /t:Test src/Common/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=release
```

### Running tests using different JavaScript engines
It's possible to set a JavaScript engine explicitly by adding `/p:JSEngine` property: 

```
./dotnet.sh build /t:Test src/Common/tests /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=release /p:JSEngine=SpiderMonkey
```

By default, `V8` engine is used.

### Test App Design
TBD

### Obtaining the logs
TBD

### Existing Limitations
TBD