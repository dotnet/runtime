# System.Security.Cryptography.Native

This folder contains C# bindings for native shim (libSystem.Security.Cryptography.Native.OpenSsl.so), shimming functionality provided by the OpenSSL library.

## Memory allocation hooks

One extra feature exposed by the native shim is tracking of memory used by
OpenSSL by hooking the memory allocation routines via
`CRYPTO_set_mem_functions`.

The functionality is enabled by setting
`DOTNET_OPENSSL_MEMORY_DEBUG` to 1. This environment
variable must be set before launching the program (calling
`Environment.SetEnvironmentVariable` at the start of the program is not
sufficient). The diagnostic API is not officially exposed and needs to be
accessed via private reflection on the `Interop.Crypto` type located in the
`System.Security.Cryptography` assembly. On this type, you can use following static
methods:

- `int GetOpenSslAllocatedMemory()`
    - Gets the total amount of memory allocated by OpenSSL
- `int GetOpenSslAllocationCount()`
    - Gets the number of allocations made by OpenSSL
- `void EnableMemoryTracking()`/`void DisableMemoryTracking()`
    - toggles tracking of individual live allocations via internal data
      structures. I.e. will keep track of live memory allocated since the start of
      tracking.
- `void ForEachTrackedAllocation(Action<IntPtr, ulong, IntPtr, int> callback)`
    - Accepts an callback and calls it for each allocation performed since the
      last `EnableMemoryTracking` call. The order of reported information does not
      correspond to the order of allocation. This method holds an internal lock
      which prevents other threads from allocating any memory from OpenSSL.
    - Callback parameters are
        - IntPtr - The pointer to the allocated object
        - ulong - size of the allocation in bytes
        - IntPtr - Pointer to a null-terminated string (`const char*`) containing the name of the file from which the allocation was made.
        - int - line number within the file specified by the previous parameter where the allocation was called from.

The debug functionality brings some overhead (header for each allocation,
locks/synchronization during each allocation) and may cause performance penalty.

### Example usage

```cs
// all above mentioned APIs are accessible via "private reflection"
BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static;
var cryptoInterop = typeof(RandomNumberGenerator).Assembly.GetTypes().First(t => t.Name == "Crypto");

// enable tracking, this clears up any previously tracked allocations
cryptoInterop.InvokeMember("EnableMemoryTracking", flags, null, null, null);

// do some work that includes OpenSSL
HttpClient client = new HttpClient();
await client.GetAsync("https://www.microsoft.com");

// stop tracking (this step is optional)
cryptoInterop.InvokeMember("DisableMemoryTracking", flags, null, null, null);

using var process = Process.GetCurrentProcess();
Console.WriteLine($"Bytes known to GC [{GC.GetTotalMemory(false)}], process working set [{process.WorkingSet64}]");
Console.WriteLine("OpenSSL - currently allocated memory: {0} B", cryptoInterop.InvokeMember("GetOpenSslAllocatedMemory", flags, null, null, null));
Console.WriteLine("OpenSSL - total allocations since start: {0}", cryptoInterop.InvokeMember("GetOpenSslAllocationCount", flags, null, null, null));

Dictionary<(IntPtr file, int line), ulong> allAllocations = new();
Action<IntPtr, ulong, IntPtr, int> callback = (ptr, size, namePtr, line) =>
{
    CollectionsMarshal.GetValueRefOrAddDefault(allAllocations, (namePtr, line), out _) += size;
};
cryptoInterop.InvokeMember("ForEachTrackedAllocation", flags, null, null, [callback]);

Console.WriteLine("Total allocated OpenSSL memory by location");
foreach (var ((filenameptr, line), total) in allAllocations.OrderByDescending(kvp => kvp.Value).Take(10))
{
    string filename = Marshal.PtrToStringUTF8(filenameptr);
    Console.WriteLine($"{total:N0} B from {filename}:{line}");
}
```