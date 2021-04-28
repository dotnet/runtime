
WASM runtime debugging
======================

- Disable symbol stripping by setting the `WasmNativeStrip` msbuild property to `false`.

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
This will cause the app to allways execute the same way, i.e. using the same memory
addresses, random numbers, etc. This can be used to make random crashes happen reliably.
Sometimes, hovewer, turning this on will make the problem disappear. In this case, it
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
