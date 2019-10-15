*This blog post originally appeared on David Broman's blog on 6/20/2007*


_Here are the full details I received from Grant Richins and Fei Chen when I asked how the JIT decides whether to employ the tail call optimization.  Note that these statements apply to the JITs as they were when Grant and Fei looked through the code base, and are prone to change at whim.  **You must not take dependencies on this behavior**. Use this information for your own personal entertainment only._

_First, Grant talked about the 64-bit JITs (one for x64, one for ia64):_

For the 64-bit JIT, we tail call whenever we’re allowed to. Here’s what prevents us from tail calling (in no particular order):

- We inline the call instead (we never inline recursive calls to the same method, but we will tail call them)
- The call/callvirt/calli is followed by something other than nop or ret IL instructions. 
- The caller or callee return a value type. 
- The caller and callee return different types. 
- The caller is synchronized (MethodImplOptions.Synchronized). 
- The caller is a shared generic method. 
- The caller has imperative security (a call to Assert, Demand, Deny, etc.). 
- The caller has declarative security (custom attributes). 
- The caller is varargs
- The callee is varargs. 
- The runtime forbids the JIT to tail call.   (_There are various reasons the runtime may disallow tail calling, such as caller / callee being in different assemblies, the call going to the application's entrypoint, any conflicts with usage of security features, and other esoteric cases._)
- The il did not have the tail. prefix and we are not optimizing (the profiler and debugger control this) 
- The il did not have the tail. prefix and the caller had a localloc instruction (think alloca or dynamic stack allocation) 
- The caller is getting some GS security cookie checks 
- The il did not have the tail. prefix and a local or parameter has had its address taken (ldarga, or ldloca) 
- The caller is the same as the callee and the runtime disallows inlining
- The callee is invoked via stub dispatch (_i.e., via intermediate code that's generated at runtime to optimize certain types of calls_).
- For x64 we have these additional restrictions: 

  - The callee has one or more parameters that are valuetypes of size 3,5,6,7 or \>8 bytes 
  - The callee has more than 4 arguments (don’t forget to count the this pointer, generics, etc.) and more than the caller 
  - For all of the parameters passed on the stack the GC-ness must match between the caller and callee.  (_"GC-ness" means the state of being a pointer to the beginning of an object managed by the GC, or a pointer to the interior of an object managed by the GC (e.g., a byref field), or neither (e.g., an integer or struct)._)
- For ia64 we have this additional restriction: 

  - Any of the callee arguments do not get passed in a register.

If all of those conditions are satisfied, we will perform a tail call. Also note that for verifiability, if the code uses a “tail.” prefix, the subsequent call opcode must be immediately followed by a ret opcode (no intermediate nops or prefixs are allowed, although there might be additional prefixes between the “tail.” prefix and the actual call opcode).

_Fei has this to add about the 32-bit JIT:_

I looked at the code briefly and here are the cases I saw where tailcall is disallowed:

- tail. prefix does not exist in the IL stream (note that tail. prefix is ignored in the inlinee).
- Synchronized method or method with varargs.
- P/Invoke to unmanaged method.
- Return types don’t match between the current method and the method it attempts to tailcall into.
- The runtime forbids the JIT to tail call.
- Callee has valuetype return.
- Many more restrictions that mirror those Grant mentioned above

