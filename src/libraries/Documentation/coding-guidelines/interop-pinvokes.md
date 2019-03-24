P/Invokes
=========

This document extends the [Interop Guidelines](interop-guidelines.md) to provide more specific guidelines, notes, and resources for defining P/Invokes.

Attributes
----------

**Implicit attributes applied to parameter and return values:**

|                  | Implicit Attribute |
|------------------|--------------------|
| parameter        | `[In]`             |
| `out` parameter  | `[Out]`            |
| `ref` parameter  | `[In],[Out]`       |
| return value     | `[Out]`            |

**`[DllImport()]` [1] attribute settings:**

| Setting | Recommendation | Details |
|---------|----------------|---------|
| [`PreserveSig`][2]   | keep default       | When this is explicitly set to false (the default is true), failed HRESULT return values will be turned into Exceptions (and the return value in the definition becomes null as a result).|
| [`SetLastError`][3]  | as per API         | Set this to true (default is false) if the API uses GetLastError and use Marshal.GetLastWin32Error to get the value. If the API sets a condition that says it has an error, get the error before making other calls to avoid inadvertently having it overwritten.|
| [`ExactSpelling`][4] | `true`             | Set this to true (default is false) and gain a slight perf benefit as the framework will avoid looking for an "A" or "W" version. (See NDirectMethodDesc::FindEntryPoint).|
| [`CharSet`][5]       | Explicitly  use `CharSet.Unicode` or `CharSet.Ansi` when strings are present in the definition | This specifies marshalling behavior of strings and what `ExactSpelling` does when `false`. Be explicit with this one as the documented default is `CharSet.Ansi`. Note that `CharSet.Ansi` is actually UTF8 on Unix (`CharSet.Utf8` is coming). _Most_ of the time Windows uses Unicode while Unix uses UTF8. |

[1]: https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.dllimportattribute.aspx
[2]: https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.dllimportattribute.preservesig.aspx
[3]: https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.dllimportattribute.setlasterror.aspx
[4]: https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.dllimportattribute.exactspelling.aspx
[5]: https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.dllimportattribute.charset.aspx

Strings
-------

When the CharSet is Unicode or the argument is explicitly marked as `[MarshalAs(UnmanagedType.LPWSTR)]` _and_ the string is passed by value (not `ref` or `out`) the string will be pinned and used directly by native code (rather than copied).

Remember to mark the `[DllImport]` as `Charset.Unicode` unless you explicitly want ANSI treatment of your strings.

**[AVOID]** `StringBuilder` marshalling *always* creates a native buffer copy (see `ILWSTRBufferMarshaler`). As such it can be extremely inefficient. Take the typical
scenario of calling a Windows API that takes a string:

1. Create a SB of the desired capacity (allocates managed capacity) **{1}**
2. Invoke
   1. Allocates a native buffer **{2}**  
   2. Copies the contents if `[In]` _(the default for a `StringBuilder` parameter)_  
   3. Copies the native buffer into a newly allocated managed array if `[Out]` **{3}** _(also the default for `StringBuilder`)_  
3. `ToString()` allocates yet another managed array **{4}**

That is *{4}* allocations to get a string out of native code. The best you can do to limit this is to reuse the `StringBuilder`
in another call but this still only saves *1* allocation. It is much better to use and cache a character buffer from `ArrayPool`- you can then get down to just the allocation for the `ToString()` on subsequent calls. 

The other issue with `StringBuilder` is that it always copies the return buffer back up to the first null. If the passed back string isn't terminated or is a double-null-terminated string your P/Invoke is incorrect at best.

If you *do* use `StringBuilder` one last gotcha is that the capacity does **not** include a hidden null which is always accounted for in interop. It is pretty common for people to get this wrong as most APIs want the size of the buffer *including* the null. This can result in wasted/unnecessary allocations.

**[USE]** Char arrays from `ArrayPool` or `StringBuffer`.

[Default Marshalling for Strings](https://msdn.microsoft.com/en-us/library/s9ts558h.aspx)

> ### Windows Specific

> For `[Out]` strings the CLR will use `CoTaskMemFree` by default to free strings or `SysStringFree` for strings that are marked
as `UnmanagedType.BSTR`.

> **For most APIs with an output string buffer:**

> The passed in character count must include the null. If the returned value is less than the passed in character count the call has succeeded and the value is the number of characters *without* the trailing null. Otherwise the count is the required size of the buffer *including* the null character.

> - Pass in 5, get 4: The string is 4 characters long with a trailing null.
> - Pass in 5, get 6: The string is 5 characters long, need a 6 character buffer to hold the null.

> [Windows Data Types for Strings](http://msdn.microsoft.com/en-us/library/dd374131.aspx)

Booleans
--------

Booleans are easy to mess up. The default marshalling for P/Invoke is as the Windows type `BOOL`, where it is a 4 byte value. `BOOLEAN`, however, is a *single* byte. This can lead to hard to track down bugs as half the return value will be discarded, which will only *potentially* change the result. For `BOOLEAN` attributing `bool` with either `[MarshalAs(UnmanagedType.U1)]` or `[MarshalAs(UnmanagedType.I1)]` will work as `TRUE` is defined as `1` and `FALSE` is defined as `0`. `U1` is technically more correct as `BOOLEAN` is defined as an `unsigned char`.

`bool` is not a blittable type (see blitting below). As such, when defining structs it is recommended to use `Interop.BOOL.cs` for `BOOL` to get the best performance.

[Default Marshalling for Boolean Types](https://msdn.microsoft.com/en-us/library/t2t3725f.aspx)  

Guids
-----

Guids are usable directly in signatures. When passed by ref they can either be passed by `ref` or with the `[MarshalAs(UnmanagedType.LPStruct)]` attribute.

| Guid | By ref Guid |
|------|-------------|
| `KNOWNFOLDERID` | `REFKNOWNFOLDERID` |

`[MarshalAs(UnmanagedType.LPStruct)]` should _only_ be used for by ref Guids.

Common Data Types
-----------------

The following types are the same size on 32-bit and 64-bit Windows, despite their names. (In contrast
to Unix, where some of these types are wider on 64-bit, for example native `long` becomes 64-bit.)

| Width | Windows          | C                  | C#       | Alternative                          |
|:------|:-----------------|:-------------------|:---------|:-------------------------------------|
| 32    | `BOOL`           | `int`              | `int`    | `bool`                               |
| 8     | `BOOLEAN`        | `unsigned char`    | `byte`   | `[MarshalAs(UnmanagedType.U1)] bool` |
| 8     | `BYTE`           | `unsigned char`    | `byte`   |                                      |
| 8     | `CHAR`           | `char`             | `sbyte`  |                                      |
| 8     | `UCHAR`          | `unsigned char`    | `byte`   |                                      |
| 16    | `SHORT`          | `short`            | `short`  |                                      |
| 16    | `CSHORT`         | `short`            | `short`  |                                      |
| 16    | `USHORT`         | `unsigned short`   | `ushort` |                                      |
| 16    | `WORD`           | `unsigned short`   | `ushort` |                                      |
| 16    | `ATOM`           | `unsigned short`   | `ushort` |                                      |
| 32    | `INT`            | `int`              | `int`    |                                      |
| 32    | `LONG`           | `long`             | `int`    |                                      |
| 32    | `ULONG`          | `unsigned long`    | `uint`   |                                      |
| 32    | `DWORD`          | `unsigned long`    | `uint`   |                                      |
| 64    | `QWORD`          | `__int64`          | `long`   |                                      |
| 64    | `LARGE_INTEGER`  | `__int64`          | `long`   |                                      |
| 64    | `LONGLONG`       | `__int64`          | `long`   |                                      |
| 64    | `ULONGLONG`      | `unsigned __int64` | `ulong`  |                                      |
| 64    | `ULARGE_INTEGER` | `unsigned __int64` | `ulong`  |                                      |
| 32    | `HRESULT`        | `long`             | `int`    |                                      |
| 32    | `NTSTATUS`       | `long`             | `int`    |                                      |


The following types, being pointers, do follow the width of the platform. Use `IntPtr`/`UIntPtr` for these.

| Signed Pointer Types (use `IntPtr`) | Unsigned Pointer Types (use `UIntPtr`) |
|:------------------------------------|:---------------------------------------|
| `HANDLE`                            | `WPARAM`                               |
| `HWND`                              | `UINT_PTR`                             |
| `HINSTANCE`                         | `ULONG_PTR`                            |
| `LPARAM`                            | `SIZE_T`                               |
| `LRESULT`                           |                                        |
| `LONG_PTR`                          |                                        |
| `INT_PTR`                           |                                        |

A Windows `PVOID` which is a C `void*` can be marshaled as either `IntPtr` or `UIntPtr` but we would prefer `void*`.

[Windows Data Types](https://docs.microsoft.com/en-us/windows/desktop/WinProg/windows-data-types)

[Data Type Ranges](https://docs.microsoft.com/en-us/cpp/cpp/data-type-ranges?view=vs-2017)

Blittable Types
---------------
Blittable types are types that have the same representation for native code. As such they do not need to be converted to another format to be marshalled to and from native code, and as this improves performance they should be preferred.

**Blittable types:**

- `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `single`, `double`
- non-nested one dimensional arrays of blittable types (e.g. `int[]`)
- structs and classes with fixed layout that only have blittable types for instance fields
  - fixed layout requires `[StructLayout(LayoutKind.Sequential)]` or `[StructLayout(LayoutKind.Explicit)]`
  - structs are `LayoutKind.Sequential` by default, classes are `LayoutKind.Auto`

**NOT blittable:**

- `bool`

**SOMETIMES blittable:**

- `char`, `string`

When blittable types are passed by reference they are simply pinned by the marshaller instead of being copied to an intermediate buffer. (Classes are inherently passed by reference, structs are passed by reference when used with `ref` or `out`.)

`char` is blittable in a one dimensional array **or** if it is part of a type that contains it is explicitly marked with `[StructLayout]` with `CharSet = CharSet.Unicode`.

```C#
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct UnicodeCharStruct
{
    public char c;
}
```

`string` is blittable if it isn't contained in another type and it's being passed as an argument that is marked with `[MarshalAs(UnmanagedType.LPWStr)]` or the `[DllImport]` has `CharSet = CharSet.Unicode` set.

You can see if a type is blittable by attempting to create a pinned `GCHandle`. If the type is not a string or considered blittable `GCHandle.Alloc` will throw an `ArgumentException`.


[Blittable and Non-Blittable Types](https://msdn.microsoft.com/en-us/library/75dwhxf7.aspx)  
[Default Marshalling for Value Types](https://msdn.microsoft.com/en-us/library/0t2cwe11.aspx)

Keeping Managed Objects Alive
-----------------------------
`GC.KeepAlive()` will ensure an object stays in scope until the KeepAlive method is hit.

[`HandleRef`][6] allows the marshaller to keep an object alive for the duration of a P/Invoke. It can be used instead of `IntPtr` in method signatures. `SafeHandle` effectively replaces this class and should be used instead.

[6]: https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.handleref.aspx

[`GCHandle`][7] allows pinning a managed object and getting the native pointer to it. Basic pattern is:  

``` C#
GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
IntPtr ptr = handle.AddrOfPinnedObject();
handle.Free();
```

[7]: https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.gchandle.aspx

Pinning is not the default for `GCHandle`. The other major pattern is for passing a reference to a managed object through native code back to managed code (via a callback, typically). Here is the pattern:

``` C#
GCHandle handle = GCHandle.Alloc(obj);
SomeNativeEnumerator(callbackDelegate, GCHandle.ToIntPtr(handle));

// In the callback
GCHandle handle = GCHandle.FromIntPtr(param);
object managedObject = handle.Target;

// After the last callback
handle.Free();
```

Don't forget that `GCHandle` needs to be explicitly freed to avoid memory leaks. 

Structs
-------

Managed structs are created on the stack and aren't removed until the method returns. By definition then, they are "pinned" (it won't get moved by the GC). You can also simply take the address in unsafe code blocks if native code won't use the pointer past the end of the current method.

Blittable structs are much more performant as they can simply be used directly by the marshalling layer. Try to make structs blittable (for example, avoid `bool`). See the "Blittable Types" section above for more details.

*If* the struct is blittable use `sizeof()` instead of `Marshal.SizeOf<MyStruct>()` for better performance. As mentioned above, you can validate that the type is blittable by attempting to create a pinned `GCHandle`. If the type is not a string or considered blittable `GCHandle.Alloc` will throw an `ArgumentException`.

Pointers to structs in definitions must either be passed by `ref` or use `unsafe` and `*`.

We always prefer to match the managed struct as closely as possible to the shape and names that are used in the official platform documentation or header.

An array like `INT_PTR Reserved1[2]` has to be marshaled to two `IntPtr` fields, `Reserved1a` and `Reserved1b`. When the native array is a primitive type, we can use the `fixed` keyword to write it a little more cleanly. For example, `SYSTEM_PROCESS_INFORMATION` looks like this in the native header:

```c
typedef struct _SYSTEM_PROCESS_INFORMATION {
    ULONG NextEntryOffset;
    ULONG NumberOfThreads;
    BYTE Reserved1[48];
    UNICODE_STRING ImageName;
...
} SYSTEM_PROCESS_INFORMATION
```

In C#, we can write it like this:

```c#
    internal unsafe struct SYSTEM_PROCESS_INFORMATION
    {
        internal uint NextEntryOffset;
        internal uint NumberOfThreads;
        private fixed byte Reserved1[48];
        internal Interop.UNICODE_STRING ImageName;
        ...
    }
```

Other References
----------------

[MarshalAs Attribute](http://msdn.microsoft.com/en-us/library/system.runtime.interopservices.marshalasattribute.aspx)  
[GetLastError and managed code](http://blogs.msdn.com/b/adam_nathan/archive/2003/04/25/56643.aspx)  
[Copying and Pinning](https://msdn.microsoft.com/en-us/library/23acw07k.aspx)  
[Marshalling between Managed and Unmanaged Code (MSDN Magazine January 2008)](http://download.microsoft.com/download/3/A/7/3A7FA450-1F33-41F7-9E6D-3AA95B5A6AEA/MSDNMagazineJanuary2008en-us.chm) *This is a .chm download*  
