
WASM runtime debugging
======================

- Disable symbol stripping by setting the `WasmNativeStrip` msbuild property to `false`.  See also, [collecting stack traces with symbols in Blazor](#collecting-stack-traces-with-symbols-in-blazor)

- Emscripten generates dwarf debug info and Chrome 80 and later can use it.

- To break in the JS debugger from runtime code, do:
```
#include <emscripten.h>
EM_ASM(debugger;);
```

- To print a stack trace from runtime code, do:
```
#ifdef HOST_WASM
#include <emscripten.h>
EM_ASM(
	var err = new Error();
	console.log ("Stacktrace: \n");
	console.log (err.stack);
	);
#endif
```
There is a mono_wasm_print_stack_trace () function that does the same:
```
#ifdef HOST_WASM
mono_wasm_print_stack_trace ();
#endif
```
The ifdef is needed to avoid compilation errors when compiling the cross compiler.

- The runtime-tests.js test runner supports various options useful for debugging:
   - Runtime command line options can be passed using the --runtime-arg=<arg> option.
      In particular --trace can be used to enable executing tracing when using the interpreter.
  - Environment variables can be set using --setenv=<var>=<value>
     In particular MONO_LOG_LEVEL/MONO_LOG_MASK can be set.

- The --stack-trace-limit=1000 option to V8 can be used to avoid V8 truncating stack traces.

- Emscripten supports clang's -fsanitize=address option, it can also decompile
  wasm images at runtime to create readable stacktraces for C code.

- The numbers in stack traces such as:
```
WebAssembly.instantiate:wasm-function[8003]:0x12b564
```
mean wasm function index/offset inside the wasm binary.
The `wasm-objdump` tool from `https://github.com/WebAssembly/wabt` can be used to find the
corresponding wasm code:
```
12b551 func[8003] <mono_wasm_load_runtime>:
```

- The `wasm-dis` tool from `https://github.com/WebAssembly/binaryen` can be used to
disassemble wasm executables (.wasm files).

# Deterministic execution

Wasm execution can be made deterministic by passing the -s DETERMINISTIC=1 option to emcc.
This will cause the app to always execute the same way, i.e. using the same memory
addresses, random numbers, etc. This can be used to make random crashes happen reliably.
Sometimes, however, turning this on will make the problem disappear. In this case, it
might be useful to add some controlled indeterminism. For example, to make the
random number generator mostly deterministic, change `$getRandomDevice` in
`upstream/emscripten/src/library.js` to:
```
	var randomBuffer2 = new Uint8Array(1);
	crypto.getRandomValues(randomBuffer2);

	FS.seed2 = randomBuffer2 [0];
	console.log('SEED: ' + FS.seed2);
	return function() {
		FS.seed2 = FS.seed2 * 16807 % 2147483647;
		return FS.seed2;
	};
```
Then run the app until the failure occurs. Note the seed value printed at the beginning,
and change the
`FS.seed2 = randomBuffer...` line to:
`FS.seed2 = <seed value>`.
This will hopefully cause the failure to happen reliably.

There is another random number generator in `upstream/emscripten/src/deterministic.js`
which needs the same treatment.

Running `make patch-deterministic` in `src/mono/wasm` will patch the
emscripten installation in `src/mono/browser/emsdk` with these changes.

# Debugging signature mismatch errors

When v8 fails with `RuntimeError: function signature mismatch`, it means a function call was
made to a function pointer with an incompatible signature, or to a NULL pointer.
This branch of v8 contains some modifications to print out the actual function pointer
value when this kind of fault happens: https://github.com/vargaz/v8/tree/sig-mismatch.
The value is an index into the function table inside the wasm executable.

The following script can be used to print out the table:
```
#!/usr/bin/env python3

#
# print-table.py: Print the function table for a webassembly .wast file
#

import sys

prefix=" (elem (i32.const 1) "

if len(sys.argv) < 2:
    print ("Usage: python print-table.py <path to mono.wast>")
    sys.exit (1)

f = open (sys.argv [1])
table_line = None
for line in f:
     if prefix in line:
         table_line = line[len(prefix):]
         break

for (index, v) in enumerate (table_line.split (" ")):
    print ("" + str(index) + ": " + v)
    index += 1
```
The input to the script is the textual assembly created by the wasm-dis tool.

These kinds of faults usually happen because the mono runtime has some helper functions which are
never meant to be reached, i.e. `no_gsharedvt_in_wrapper` or `no_llvmonly_interp_method_pointer`.
These functions are used as placeholders for function pointers with different signatures, so
if they do end up being called due to a bug, a signature mismatch error happens.

# Collecting stack traces with symbols in Blazor

When debugging a native crash in a .NET 6 Blazor app or another WebAssembly
framework that uses our default `dotnet.wasm`, the native stack frames will not
have C symbol names, but will instead look like `$func1234`.

For example this Razor page will crash when a user clicks on the `Crash` button

```csharp
<button class="btn btn-warning" @onclick="Crash">Crash</button>

@code {
    private void Crash ()
    {
        IntPtr p = (IntPtr)0x01;
        Console.WriteLine ("About to crash");
        System.Runtime.InteropServices.Marshal.FreeHGlobal(p);
    }
}
```

Clicking on the `Crash` button will produce the following output in the console (the function indices may be different):

```console
dotnet.wasm:0x1d8355 Uncaught (in promise) RuntimeError: memory access out of bounds
    at _framework/dotnet.wasm
    at _framework/dotnet.wasm
    at _framework/dotnet.wasm
    at _framework/dotnet.wasm
    at _framework/dotnet.wasm
    at _framework/dotnet.wasm
    at _framework/dotnet.wasm
    at _framework/dotnet.wasm
    at _framework/dotnet.wasm
    at _framework/dotnet.wasm
$free @ dotnet.wasm:0x1d8355
$func4027 @ dotnet.wasm:0xead6a
$func219 @ dotnet.wasm:0x1a03a
$func167 @ dotnet.wasm:0xcaf7
$func166 @ dotnet.wasm:0xba0a
$func2810 @ dotnet.wasm:0xabacf
$func1615 @ dotnet.wasm:0x6f8eb
$func1613 @ dotnet.wasm:0x6f85d
$func966 @ dotnet.wasm:0x502dc
$func219 @ dotnet.wasm:0x1a0e2
$func167 @ dotnet.wasm:0xcaf7
$func166 @ dotnet.wasm:0xba0a
$func2810 @ dotnet.wasm:0xabacf
$func1615 @ dotnet.wasm:0x6f8eb
$func1619 @ dotnet.wasm:0x6ff58
$mono_wasm_invoke_method @ dotnet.wasm:0x96c9
Module._mono_wasm_invoke_method @ dotnet.6.0.1.hopd7ipo8x.js:1
managed__Microsoft_AspNetCore_Components_WebAssembly__Microsoft_AspNetCore_Components_WebAssembly_Services_DefaultWebAssemblyJSRuntime_BeginInvokeDotNet @ managed__Microsoft_AspNetCore_Components_WebAssembly__Microsoft_AspNetCore_Components_WebAssembly_Services_DefaultWebAssemblyJSRuntime_BeginInvokeDotNet:19
beginInvokeDotNetFromJS @ blazor.webassembly.js:1
b @ blazor.webassembly.js:1
invokeMethodAsync @ blazor.webassembly.js:1
(anonymous) @ blazor.webassembly.js:1
invokeWhenHeapUnlocked @ blazor.webassembly.js:1
S @ blazor.webassembly.js:1
C @ blazor.webassembly.js:1
dispatchGlobalEventToAllElements @ blazor.webassembly.js:1
onGlobalEvent @ blazor.webassembly.js:1
```

In order to get symbols, the user should:

1. Install the `wasm-tools` workload using `dotnet workload install wasm-tools`
2. Set these additional properties in their `.csproj` file:

   ```xml
     <!-- Builds a dotnet.wasm with debug symbols preserved -->
     <PropertyGroup>
       <WasmBuildNative>true</WasmBuildNative>
       <WasmNativeStrip>false</WasmNativeStrip>
     </PropertyGroup>
   ```

3. Delete the `bin` and `obj` folders, re-build the project and run it again.

Now clicking on the `Crash` button will produce a stack trace with symbols:

```console
dotnet.wasm:0x224878 Uncaught (in promise) RuntimeError: memory access out of bounds
    at dlfree (dotnet.wasm:0x224878)
    at SystemNative_Free (dotnet.wasm:0x20f0e2)
    at do_icall (dotnet.wasm:0x190f9)
    at do_icall_wrapper (dotnet.wasm:0x18429)
    at interp_exec_method (dotnet.wasm:0xa56c)
    at interp_runtime_invoke (dotnet.wasm:0x943a)
    at mono_jit_runtime_invoke (dotnet.wasm:0x1dec32)
    at do_runtime_invoke (dotnet.wasm:0x95fca)
    at mono_runtime_invoke_checked (dotnet.wasm:0x95f57)
    at mono_runtime_try_invoke_array (dotnet.wasm:0x9a87e)
$dlfree @ dotnet.wasm:0x224878
$SystemNative_Free @ dotnet.wasm:0x20f0e2
$do_icall @ dotnet.wasm:0x190f9
$do_icall_wrapper @ dotnet.wasm:0x18429
$interp_exec_method @ dotnet.wasm:0xa56c
$interp_runtime_invoke @ dotnet.wasm:0x943a
$mono_jit_runtime_invoke @ dotnet.wasm:0x1dec32
$do_runtime_invoke @ dotnet.wasm:0x95fca
$mono_runtime_invoke_checked @ dotnet.wasm:0x95f57
$mono_runtime_try_invoke_array @ dotnet.wasm:0x9a87e
$mono_runtime_invoke_array_checked @ dotnet.wasm:0x9af17
$ves_icall_InternalInvoke @ dotnet.wasm:0x702ed
$ves_icall_InternalInvoke_raw @ dotnet.wasm:0x7777f
$do_icall @ dotnet.wasm:0x191c5
$do_icall_wrapper @ dotnet.wasm:0x18429
$interp_exec_method @ dotnet.wasm:0xa56c
$interp_runtime_invoke @ dotnet.wasm:0x943a
$mono_jit_runtime_invoke @ dotnet.wasm:0x1dec32
$do_runtime_invoke @ dotnet.wasm:0x95fca
$mono_runtime_try_invoke @ dotnet.wasm:0x966fe
$mono_runtime_invoke @ dotnet.wasm:0x98982
$mono_wasm_invoke_method @ dotnet.wasm:0x227de2
Module._mono_wasm_invoke_method @ dotnet..y6ggkhlo8e.js:9927
managed__Microsoft_AspNetCore_Components_WebAssembly__Microsoft_AspNetCore_Components_WebAssembly_Services_DefaultWebAssemblyJSRuntime_BeginInvokeDotNet @ managed__Microsoft_AspNetCore_Components_WebAssembly__Microsoft_AspNetCore_Components_WebAssembly_Services_DefaultWebAssemblyJSRuntime_BeginInvokeDotNet:19
beginInvokeDotNetFromJS @ blazor.webassembly.js:1
b @ blazor.webassembly.js:1
invokeMethodAsync @ blazor.webassembly.js:1
(anonymous) @ blazor.webassembly.js:1
invokeWhenHeapUnlocked @ blazor.webassembly.js:1
S @ blazor.webassembly.js:1
C @ blazor.webassembly.js:1
dispatchGlobalEventToAllElements @ blazor.webassembly.js:1
onGlobalEvent @ blazor.webassembly.js:1
```

# Enabling additional logging in Blazor

In .NET 8+, Blazor startup can be controlled by setting the `autostart="false"` attribute on the
`<script>` tag that loads the blazor webassembly framework.  After that, a call to the
`globalThis.Blazor.start()` JavaScript function can be passed additional configuration options,
including setting mono environment variables, or additional command line arguments.

The name of the script and the location of the `<script>` tag depends on whether the project was a
Blazor WebAssembly project (template `blazorwasm`) or a Blazor project (template `blazor`).

See the runtime `DotnetHostBuilder` interface in
[dotnet.d.ts](../../../../src/mono/wasm/runtime/dotnet.d.ts) for additional configuration functions.

## Blazor WebAssembly

In a `blazorwasm` project, the script is `_framework/blazor.webassembly.js` and it is loaded in `wwwroot/index.html`:

```html
<body>
    <div id="app">
      ...
  </div>

  <div id="blazor-error-ui">
    ...
  </div>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
```

Replace it with this:

```html
<body>
    <div id="app">
        ...
    </div>

    <div id="blazor-error-ui">
        ...
    </div>
    <script src="_framework/blazor.webassembly.js" autostart="false"></script>

    <script>
        Blazor.start({
            configureRuntime: dotnet => {
                dotnet.withEnvironmentVariable("MONO_LOG_LEVEL", "debug");
                dotnet.withEnvironmentVariable("MONO_LOG_MASK", "all");
            }
        });
    </script></body>
```

## Blazor

In a `blazor` project, the script is `_framework/blazor.web.js` and it is loaded by `Components/App.razor` in the server-side project:

```html
<body>
    <Routes />
    <script src="_framework/blazor.web.js"></script>
</body>
```

Replace it with this (note that for a `blazor` project, `Blazor.start` needs an extra dictionary with a `webAssembly` key):

```html
<body>
    <Routes />
    <script src="_framework/blazor.web.js" autostart="false"></script>
    <script>
        Blazor.start({
            webAssembly: {
                configureRuntime: dotnet => {
                    console.log("in configureRuntime");
                    dotnet.withEnvironmentVariable("MONO_LOG_LEVEL", "debug");
                    dotnet.withEnvironmentVariable("MONO_LOG_MASK", "all");
                }
            }
        });
    </script>
</body>
```
