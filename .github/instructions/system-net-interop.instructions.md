---
applyTo: "src/libraries/Common/src/Interop/Windows/HttpApi/**,src/libraries/Common/src/Interop/Windows/IpHlpApi/**,src/libraries/Common/src/Interop/Windows/WinHttp/**,src/libraries/Common/src/Interop/Windows/WinSock/**,src/libraries/Common/src/Interop/Unix/System.Native/**,src/libraries/Common/src/Interop/Unix/System.Net.Security.Native/**,src/native/libs/System.Native/pal_network*"
---

# System.Net Interop — Folder-Specific Guidance

## P/Invoke Declarations

- Use `LibraryImport` (source-generated) for all new P/Invoke declarations — do not use `DllImport`
- Mark string parameters with `StringMarshalling.Utf16` (Windows) or `StringMarshalling.Utf8` (Unix) explicitly
- Use `[return: MarshalAs(UnmanagedType.Bool)]` for native functions returning BOOL to avoid misinterpretation of non-zero values
- Verify that `SetLastError = true` is set only when the native function documents setting the thread error code

## SafeHandle Usage

- Every native handle must be wrapped in a SafeHandle-derived type — never store raw IntPtr in managed fields
- Override `ReleaseHandle` to call the correct native close/free function — never throw from ReleaseHandle
- Use `DangerousAddRef`/`DangerousRelease` only when passing handles to native code that may outlive the managed call — prefer CriticalHandle patterns where possible
- Ensure SafeHandle instances are not disposed while a P/Invoke call using that handle is in flight

## Error Code Mapping

- Map Windows error codes (HRESULT, Win32, WSA*) to specific .NET exception types — do not use generic Exception
- Map Unix errno values to SocketException with the correct SocketError value
- Use `Marshal.GetLastPInvokeError()` (not `Marshal.GetLastWin32Error()`) to retrieve error codes after LibraryImport calls
- Preserve the native error code in the exception for diagnostic purposes (e.g., as NativeErrorCode or inner exception)

## Native Memory Management

- Allocate native memory with `NativeMemory.Alloc` or `Marshal.AllocHGlobal` — always free in a finally block or via SafeHandle
- Pin managed arrays with `GCHandle.Alloc(array, GCHandleType.Pinned)` before passing to native code — release in finally
- For variable-length native structures, allocate sufficient buffer space and handle ERROR_INSUFFICIENT_BUFFER / ERANGE retry patterns
- Never access native memory after the owning SafeHandle is disposed

## Struct Layout and Marshaling

- Use `[StructLayout(LayoutKind.Sequential)]` for all interop structs — verify field order matches the native definition
- Ensure struct sizes and field offsets are correct for both 32-bit and 64-bit architectures
- Use fixed-size buffers (`fixed byte[N]`) for inline arrays in native structs rather than MarshalAs.ByValArray
- Test struct marshaling on all target platforms — padding and alignment rules differ between Windows and Unix

## Native PAL Code (pal_network*)

- C PAL functions must return error codes, not throw — the managed caller is responsible for mapping errors
- Use portable POSIX APIs where possible; isolate platform-specific code behind #ifdef guards
- Ensure all native allocations are freed on both success and error paths
- Do not include system headers that are not available on all supported Unix platforms — check build on Linux, macOS, and FreeBSD
