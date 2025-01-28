# System.Security.Cryptography.Native

This folder contains C# bindings for native shim (libSystem.Security.Cryptography.Native.Openssl.so), shimming functionality provided by the OpenSSL library.

## Memory allocation hooks

One extra feature exposed by the native shim is tracking of memory used by
OpenSSL by hooking the memory allocation routines via
`CRYPTO_set_mem_functions`. This functionality is implemented in [native
code](../../../../../../native/libs/System.Security.Cryptography.Native/memory_debug.c)
because these memory callback can run in contexts where managed .NET code may
not run (such as thread destructors).

The functionality is enabled by setting
`DOTNET_SYSTEM_NET_SECURITY_OPENSSL_MEMORY_DEBUG` to 1. This environment
variable must be set before launching the program (calling
`Environment.SetEnvironmentVariable` at the start of the program is not
sufficient). The diagnostic API is not officially exposed and needs to be
accessed via private reflection on the `Interop.Crypto` type located in the
`System.Net.Security` assembly. On this type, you can use following static
methods:

- `int GetOpenSslAllocatedMemory()`
    - Gets the total amount of memory allocated by OpenSSL
- `int GetOpenSslAllocationCount()`
    - Gets the number of allocations made by OpenSSL
- `void EnableTracking()`/`void DisableTracking()`
    - toggles tracking of individual live allocations via internal data
      structures. I.e. will keep track of live memory allocated since the start of
      tracking.
- `void ForEachTrackedAllocation(Action<IntPtr, int, IntPtr, int> callback)`
    - Accepts an callback and calls it for each allocation performed since the
      last `EnableTracking` call. The order of reported information does not
      correspond to the order of allocation. This method holds an internal lock
      which prevents other threads from allocating any memory from OpenSSL.
    - Callback parameters are
        - IntPtr - The pointer to the allocated object
        - int - size of the allocation in bytes
        - IntPtr - Pointer to a null-terminated string (`const char*`) containing the name of the file from which the allocation was made.
        - int - line number within the file specified by the previous parameter where the allocation was called from.

The debug functionality brings some overhead (header for each allocation,
locks/synchronization during each allocation) and may cause performance penalty.

### Example usage

```cs
var ci = typeof(SslStream).Assembly.GetTypes().First(t => t.Name == "Crypto");
ci.InvokeMember("EnableTracking", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, null, null);

// do something which uses OpenSSL
HttpClient client = new HttpClient();
await client.GetAsync("https://www.google.com");

using var process = Process.GetCurrentProcess();
Console.WriteLine($"Bytes known to GC [{GC.GetTotalMemory(false)}], process working set [{process.WorkingSet64}]");
Console.WriteLine("OpenSSL memory {0}", ci.InvokeMember("GetOpenSslAllocatedMemory", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null));
Console.WriteLine("OpenSSL allocations {0}", ci.InvokeMember("GetOpenSslAllocationCount", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, null));

// tally the allocations by the file+line combination
Dictionary<(IntPtr file, int line), int> allAllocations = new Dictionary<(IntPtr file, int line), int>();
Action<IntPtr, int, IntPtr, int> callback = (ptr, size, namePtr, line) =>
{
    CollectionsMarshal.GetValueRefOrAddDefault(allAllocations, (namePtr, line), out _) += size;
};
ci.InvokeMember("ForEachTrackedAllocation", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, null, new object[] { callback });

// print the allocations by volume (descending)
System.Console.WriteLine("Total allocated OpenSSL memory by location:");
foreach (var ((filenameptr, line), total) in allAllocations.OrderByDescending(kvp => kvp.Value))
{
    string filename = Marshal.PtrToStringUTF8(filenameptr);
    Console.WriteLine($"{total:N0} B from {filename}:{line}");
}
```