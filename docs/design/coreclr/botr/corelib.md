`System.Private.CoreLib` and calling into the runtime
===

# Introduction

`System.Private.CoreLib.dll` is the assembly for defining the core parts of the type system, and a good portion of the Base Class Library in .NET Framework. It was originally named `mscorlib` in .NET Core, though many places in the code and documentation still refer to it as `mscorlib`. This document will endeavour to stick to using `System.Private.CoreLib` or CoreLib. Base data types live in this assembly, and it has a tight coupling with the CLR. Here you will learn exactly how and why CoreLib is special and the basics about calling into the CLR from managed code via QCall and FCall methods. It also discusses calling from within the CLR into managed code.

## Dependencies

Since CoreLib defines base data types like `Object`, `Int32`, and `String`, CoreLib cannot depend on other managed assemblies. However, there is a strong dependency between CoreLib and the CLR. Many of the types in CoreLib need to be accessed from native code, so the layout of many managed types is defined both in managed code and in native code inside the CLR. Additionally, some fields may be defined only in Debug, Checked, or Release builds, so typically CoreLib must be compiled separately for each type of build.

`System.Private.CoreLib.dll` builds separately for 64 bit and 32 bit, and some public constants it exposes differ by bitness. By using these constants, such as `IntPtr.Size`, most libraries above CoreLib should not need to build separately for 32 bit vs. 64 bit.

## What makes `System.Private.CoreLib` special?

CoreLib has several unique properties, many of which are due to its tight coupling to the CLR.

- CoreLib defines the core types necessary to implement the CLR's Virtual Object System, such as the base data types (`Object`, `Int32`, `String`, etc).
- The CLR must load CoreLib on startup to load certain system types.
- Can only have one CoreLib loaded in the process at a time, due to layout issues. Loading multiple CoreLibs would require formalizing a contract of behavior, FCall methods, and datatype layout between CLR and CoreLib, and keeping that contract relatively stable across versions.
- CoreLib's types are used heavily for native interop and managed exceptions should map correctly to native error codes/formats.
- The CLR's multiple JIT compilers may special case a small group of certain methods in CoreLib for performance reasons, both in terms of optimizing away the method (such as `Math.Cos(double)`), or calling a method in peculiar ways (such as `Array.Length`, or some implementation details on `StringBuilder` for getting the current thread).
- CoreLib will need to call into native code, via P/Invoke where appropriate, primarily into the underlying operating system or occasionally a platform adaptation layer.
- CoreLib will require calling into the CLR to expose some CLR-specific functionality, such as triggering a garbage collection, to load classes, or to interact with the type system in a non-trivial way. This requires a bridge between managed code and native, "manually managed" code within the CLR.
- The CLR will need to call into managed code to call managed methods, and to get at certain functionality that is only implemented in managed code.

# Interface between managed and CLR code

To reiterate, the needs of managed code in CoreLib include:

- The ability to access fields of some managed data structures in both managed code and "manually managed" code within the CLR.
- Managed code must be able to call into the CLR.
- The CLR must be able to call managed code.

To implement these, we need a way for the CLR to specify and optionally verify the layout of a managed object in native code, a managed mechanism for calling into native code, and a native mechanism for calling into managed code.

The managed mechanism for calling into native code must also support the special managed calling convention used by `String`'s constructors, where the constructor allocates the memory used by the object (instead of the typical convention where the constructor is called after the GC allocates memory).

The CLR provides a [`mscorlib` binder](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/binder.cpp) internally, providing a mapping between unmanaged types and fields to managed types and fields. The binder will look up and load classes and allows the calling of managed methods. It also performs simple verification to ensure the correctness of any layout information specified in both managed and native code. The binder ensures that the managed class attempting to load exists in mscorlib, has been loaded, and the field offsets are correct. It also needs the ability to differentiate between method overloads with different signatures.

# Calling from managed to native code

Two techniques exist for calling into the CLR from managed code. FCall allows you to call directly into the CLR code, and provides a lot of flexibility in terms of manipulating objects, though it is easy to cause GC holes by not tracking object references correctly. QCall also allows you to call into the CLR via the P/Invoke, but is much harder to accidentally mis-use. FCalls are identified in managed code as extern methods with the [`MethodImplOptions.InternalCall`](https://docs.microsoft.com/dotnet/api/system.runtime.compilerservices.methodimploptions) bit set. QCalls are marked `static extern` methods similar to regular P/Invokes, but are directed toward a library called `"QCall"`.

There is a small variant of FCall called HCall (for Helper call) for implementing JIT helpers. The HCall is intended for doing things like accessing multi-dimensional array elements, range checks, etc. The only difference between HCall and FCall is that HCall methods won't show up in an exception stack trace.

### Choosing between FCall, QCall, P/Invoke, and writing in managed code

First, remember that you should be writing as much as possible in managed code. You avoid a raft of potential GC hole issues, you get a better debugging experience, and the code is often simpler.

Reasons to write FCalls in the past generally fell into three camps: missing language features, better performance, or implementing unique interactions with the runtime. C# now has almost every useful language feature that you could get from C++, including unsafe code and stack-allocated buffers, and this eliminates the first two reasons for FCalls. We have ported some parts of the CLR that were heavily reliant on FCalls to managed code in the past (such as Reflection, some Encoding, and String operations) and we intend to continue this momentum.

If the only reason you're defining a FCall method is to call a native method, you should be using P/Invoke to call the method directly. [P/Invoke](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute) is the public native method interface and should be doing everything you need in a correct manner.

If you still need to implement a feature inside the runtime, consider if there is a way to reduce the frequency of transitioning to native code. Can you write the common case in managed and only call into native for some rare corner cases? You're usually best off keeping as much as possible in managed code.

QCalls are the preferred mechanism going forward. You should only use FCalls when you are "forced" to. This happens when there is common "short path" through the code that is important to optimize. This short path should not be more than a few hundred instructions, cannot allocate GC memory, take locks or throw exceptions (`GC_NOTRIGGER`, `NOTHROWS`). In all other circumstances (and especially when you enter a FCall and then simply erect HelperMethodFrame), you should be using QCall.

FCalls were specifically designed for short paths of code that must be optimized. They allowed explicit control over when erecting a frame was done. However, it is error prone and not worth the complexity for many APIs. QCalls are essentially P/Invokes into the CLR. In the event the performance of an FCall is required consider creating a QCall and marking it with [`SuppressGCTransitionAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.suppressgctransitionattribute).

As a result, QCalls give you some advantageous marshaling for `SafeHandle`s automatically – your native method just takes a `HANDLE` type, and can be used without worrying whether someone will free the handle while in that method body. The resulting FCall method would need to use a `SafeHandleHolder` and may need to protect the `SafeHandle`, etc. Leveraging the P/Invoke marshaler can avoid this additional plumbing code.

## QCall functional behavior

QCalls are very much like a normal P/Invoke from CoreLib to CLR. Unlike FCalls, QCalls will marshal all arguments as unmanaged types like a normal P/Invoke. QCall also switch to preemptive GC mode like a normal P/Invoke. These two features should make QCalls easier to write reliably compared to FCalls. QCalls are not prone to GC holes and GC starvation bugs that are common with FCalls.

QCalls perform better than FCalls that erect a `HelperMethodFrame`. The overhead is about 1.4x less compared to FCall w/ `HelperMethodFrame` overhead on x86 and x64.

The preferred types for QCall arguments are primitive types that are efficiently handled by the P/Invoke marshaler (`INT32`, `LPCWSTR`, `BOOL`). Notice that `BOOL` is the correct boolean flavor for QCall arguments. On the other hand, `CLR_BOOL` is the correct boolean flavor for FCall arguments.

The pointers to common unmanaged EE structures should be wrapped into handle types. This is to make the managed implementation type safe and avoid falling into unsafe C# everywhere. See AssemblyHandle in [vm\qcall.h][qcall] for an example.

[qcall]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/qcall.h

Passing object references in and out of QCalls is done by wrapping a pointer to a local variable in a handle. It is intentionally cumbersome and should be avoided if reasonably possible. See the `StringHandleOnStack` in the example below. Returning objects, especially strings, from QCalls is the only common pattern where passing the raw objects is widely acceptable. (For reasoning on why this set of restrictions helps make QCalls less prone to GC holes, read the ["GC Holes, FCall, and QCall"](#gcholes) section below.)

### QCall example - managed

Do not replicate the comments into your actual QCall implementation. This is for illustrative purposes.

```CSharp
class Foo
{
    // All QCalls should have the following DllImport attribute
    [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]

    // QCalls should always be static extern.
    private static extern bool BarInternal(int flags, string inString, StringHandleOnStack retString);

    // Many QCalls have a thin managed wrapper around them to perform
    // as much work prior to the transition as possible. An example would be
    // argument validation which is easier in managed than native code.
    public string Bar(int flags)
    {
        if (flags != 0)
            throw new ArgumentException("Invalid flags");

        string retString = null;
        // The strings are returned from QCalls by taking address
        // of a local variable using StringHandleOnStack
        if (!BarInternal(flags, this.Id, new StringHandleOnStack(ref retString)))
            FatalError();

        return retString;
    }
}
```

### QCall example - unmanaged

Do not replicate the comments into your actual QCall implementation.

The QCall entrypoint has to be registered in tables in [vm\ecalllist.h][ecalllist] using `QCFuncEntry` macro. See ["Registering your QCall or FCall Method"](#register) below.

[ecalllist]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/ecalllist.h

```C++
class FooNative
{
public:
    // All QCalls should be static and tagged with QCALLTYPE
    static
    BOOL QCALLTYPE BarInternal(int flags, LPCWSTR wszString, QCall::StringHandleOnStack retString);
};

BOOL QCALLTYPE FooNative::BarInternal(int flags, LPCWSTR wszString, QCall::StringHandleOnStack retString)
{
    // All QCalls should have QCALL_CONTRACT.
    // It is alias for THROWS; GC_TRIGGERS; MODE_PREEMPTIVE.
    QCALL_CONTRACT;

    // Optionally, use QCALL_CHECK instead and the expanded form of the contract
    // if you want to specify preconditions:
    // CONTRACTL {
    //     QCALL_CHECK;
    //     PRECONDITION(wszString != NULL);
    // } CONTRACTL_END;

    // The only line between QCALL_CONTRACT and BEGIN_QCALL
    // should be the return value declaration if there is one.
    BOOL retVal = FALSE;

    // The body has to be enclosed in BEGIN_QCALL/END_QCALL macro.
    // It is necessary for exception handling.
    BEGIN_QCALL;

    // Argument validation would ideally be in managed, but in some cases
    // needs to be done in native. If argument validation is done in
    // managed asserting in native is warranted.
    _ASSERTE(flags != 0);

    // No need to worry about GC moving strings passed into QCall.
    // Marshalling pins them for us.
    printf("%S\n", wszString);

    // This is the most efficient way to return strings back
    // to managed code. No need to use StringBuilder.
    retString.Set(L"Hello");

    // You can not return from inside of BEGIN_QCALL/END_QCALL.
    // The return value has to be passed out in helper variable.
    retVal = TRUE;

    END_QCALL;

    return retVal;
}
```

## FCall functional behavior

FCalls allow more flexibility in terms of passing object references around, but with higher code complexity and more opportunities to make mistakes. Additionally, FCall methods must either erect a helper method frame along their common code paths, or for any FCall of non-trivial length, explicitly poll for whether a garbage collection must occur. Failing to do so will lead to starvation issues if managed code repeatedly calls the FCall method in a tight loop, because FCalls execute while the thread only allows the GC to run in a cooperative manner.

FCalls require a lot of boilerplate code, too much to describe here. Refer to [fcall.h][fcall] for details.

[fcall]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/fcall.h

### <a name="gcholes"></a> GC holes, FCall, and QCall

A more complete discussion on GC holes can be found in the [CLR Code Guide](../../../coding-guidelines/clr-code-guide.md). Look for ["Is your code GC-safe?"](../../../coding-guidelines/clr-code-guide.md#2.1). This tailored discussion motivates some of the reasons why FCall and QCall have some of their strange conventions.

Object references passed as parameters to FCall methods are not GC-protected, meaning that if a GC occurs, those references will point to the old location in memory of an object, not the new location. For this reason, FCalls usually follow the discipline of accepting something like `StringObject*` as their parameter type, then explicitly converting that to a `STRINGREF` before doing operations that may trigger a GC. If you expect to use an object reference later, you must GC protect object references before triggering a GC.

All GC heap allocations within an FCall method must happen within a helper method frame. If you allocate memory on the GC heap, the GC may collect dead objects and move objects around in unpredictable ways, with some low probability. For this reason, you must manually report any object references in your method to the GC, so that if a garbage collection occurs, your object reference will be updated to refer to the new location in memory. Any pointers into managed objects (like arrays or Strings) within your code will not be updated automatically, and must be re-fetched after any operation that may allocate memory and before your first usage. Reporting a reference can be done via the `GCPROTECT_*` macros or as parameters when erecting a helper method frame.

Failing to properly report an `OBJECTREF` or to update an interior pointer is commonly referred to as a "GC hole", because the `OBJECTREF` class will do some validation that it points to a valid object every time you dereference it in Debug and Checked builds. When an `OBJECTREF` pointing to an invalid object is dereferenced, an assert will trigger saying something like "Detected an invalid object reference. Possible GC hole?". This assert is unfortunately easy to hit when writing "manually managed" code.

Note that QCall's programming model is restrictive to sidestep GC holes by forcing you to pass in the address of an object reference on the stack. This guarantees that the object reference is GC protected by the JIT's reporting logic, and that the actual object reference will not move because it is not allocated in the GC heap. QCall is our recommended approach, precisely because it makes GC holes harder to write.

### FCall epilog walker for x86

The managed stack walker needs to be able to find its way from FCalls. It is relative easy on newer platforms that define conventions for stack unwinding as part of the ABI. The stack unwinding conventions are not defined by an ABI for x86. The runtime works around this by implementing an epilog walker. The epilog walker computes the FCall return address and callee save registers by simulating the FCall execution. This imposes limits on what constructs are allowed in the FCall implementation.

Complex constructs like stack allocated objects with destructors or exception handling in the FCall implementation may confuse the epilog walker. This can lead to GC holes or crashes during stack walking. There is no comprehensive list of what constructs should be avoided to prevent this class of bugs. An FCall implementation that is fine one day may break with the next C++ compiler update. We depend on stress runs and code coverage to find bugs in this area.

Setting a breakpoint inside an FCall implementation may confuse the epilog walker. It leads to an "Invalid breakpoint in a helpermethod frame epilog" assert inside [vm\i386\gmsx86.cpp](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/i386/gmsx86.cpp).

### FCall example – managed

Here's a real-world example from the `String` class:

```CSharp
public partial sealed class String
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    private extern string? IsInterned();

    public static string? IsInterned(string str)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        return str.IsInterned();
    }
}
```

### FCall example – unmanaged

The FCall entrypoint has to be registered in tables in [vm\ecalllist.h][ecalllist] using `FCFuncEntry` macro. See ["Registering your QCall or FCall Method"](#register).

This method is an instance method in managed code, with the "this" parameter passed as the first argument. We use `StringObject*` as the argument type, then copy it into a `STRINGREF` so we get some error checking when we use it.

```C++
FCIMPL1(Object*, AppDomainNative::IsStringInterned, StringObject* pStringUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF       refString   = ObjectToSTRINGREF(pStringUNSAFE);
    STRINGREF*      prefRetVal  = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refString);

    if (refString == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));

    prefRetVal = GetAppDomain()->IsStringInterned(&refString);

    HELPER_METHOD_FRAME_END();

    if (prefRetVal == NULL)
        return NULL;

    return OBJECTREFToObject(*prefRetVal);
}
FCIMPLEND
```

## <a name="register"></a> Registering your QCall or FCall method

The CLR must know the name of your QCall and FCall methods, both in terms of the managed class and method names, as well as which native methods to call. That is done in [ecalllist.h][ecalllist], with two arrays. The first array maps namespace and class names to an array of function elements. That array of function elements then maps individual method names and signatures to function pointers.

Say we defined an FCall method for `String.IsInterned()`, in the example above. First, we need to ensure that we have an array of function elements for the String class.

``` C++
// Note these have to remain sorted by name:namespace pair
    ...
    FCClassElement("String", "System", gStringFuncs)
    ...
```

Second, we must then ensure that `gStringFuncs` contains a proper entry for `IsInterned`. Note that if a method name has multiple overloads then we can specify a signature:

```C++
FCFuncStart(gStringFuncs)
    ...
    FCFuncElement("IsInterned", AppDomainNative::IsStringInterned)
    ...
FCFuncEnd()
```

There is a parallel `QCFuncElement` macro.

## Naming convention

FCalls and QCalls should not be publicly exposed. Instead wrap the actual FCall or QCall and provide a API approved name.

The internal FCall or QCall should use the "Internal" suffix to disambiguate the name of the FCall or QCall from public entry point (e.g. the public entry point does error checking and then calls shared worker function with exactly same signature). This is no different from how you would deal with this situation in pure managed code in BCL.

# Types with a managed/unmanaged duality

Certain managed types must have a representation available in both managed and native code. You could ask whether the canonical definition of a type is in managed code or native code within the CLR, but the answer doesn't matter – the key thing is they must both be identical. This will allow the CLR's native code to access fields within a managed object in a fast and efficient manner. There is a more complex way of using essentially the CLR's equivalent of Reflection over `MethodTable`s and `FieldDesc`s to retrieve field values, but this doesn't perform as well as desired and isn't very usable. For commonly used types, it makes sense to declare a data structure in native code and keep the two in sync.

The CLR provides a binder for this purpose. After you define your managed and native classes, you should provide some clues to the binder to help ensure that the field offsets remain the same to quickly spot when someone accidentally adds a field to only one definition of a type.

In [mscorlib.h][mscorlib.h], use macros ending in "_U" to describe a type, the name of fields in managed code, and the name of fields in a corresponding native data structure. Additionally, you can specify a list of methods, and reference them by name when you attempt to call them later.

[mscorlib.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/mscorlib.h

``` C++
DEFINE_CLASS_U(SAFE_HANDLE,         Interop,                SafeHandle,         SafeHandle)
DEFINE_FIELD(SAFE_HANDLE,           HANDLE,                 handle)
DEFINE_FIELD_U(SAFE_HANDLE,         STATE,                  _state,                     SafeHandle,            m_state)
DEFINE_FIELD_U(SAFE_HANDLE,         OWNS_HANDLE,            _ownsHandle,                SafeHandle,            m_ownsHandle)
DEFINE_FIELD_U(SAFE_HANDLE,         INITIALIZED,            _fullyInitialized,          SafeHandle,            m_fullyInitialized)
DEFINE_METHOD(SAFE_HANDLE,          GET_IS_INVALID,         get_IsInvalid,              IM_RetBool)
DEFINE_METHOD(SAFE_HANDLE,          RELEASE_HANDLE,         ReleaseHandle,              IM_RetBool)
DEFINE_METHOD(SAFE_HANDLE,          DISPOSE,                Dispose,                    IM_RetVoid)
DEFINE_METHOD(SAFE_HANDLE,          DISPOSE_BOOL,           Dispose,                    IM_Bool_RetVoid)
```

Then, you can use the `REF<T>` template to create a type name like `SAFEHANDLEREF`. All the error checking from `OBJECTREF` is built into the `REF<T>` template, and you can freely dereference this `SAFEHANDLEREF` and use fields off of it in native code. You still must GC protect these references.

# Calling into managed code from unmanaged code

Clearly there are places where the CLR must call into managed code from native. For this purpose, we have added a `MethodDescCallSite` class to handle a lot of plumbing for you. Conceptually, all you need to do is find the `MethodDesc*` for the method you want to call, find a managed object for the "this" pointer (if you're calling an instance method), pass in an array of arguments, and deal with the return value. Internally, you'll need to potentially toggle your thread's state to allow the GC to run in preemptive mode, etc.

Here's a simplified example. Note how this instance uses the binder described in the previous section to call `SafeHandle`'s virtual `ReleaseHandle` method.

```C++
void SafeHandle::RunReleaseMethod(SafeHandle* psh)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    SAFEHANDLEREF sh(psh);

    GCPROTECT_BEGIN(sh);

    MethodDescCallSite releaseHandle(s_pReleaseHandleMethod, METHOD__SAFE_HANDLE__RELEASE_HANDLE, (OBJECTREF*)&sh, TypeHandle(), TRUE);

    ARG_SLOT releaseArgs[] = { ObjToArgSlot(sh) };
    if (!(BOOL)releaseHandle.Call_RetBool(releaseArgs)) {
        MDA_TRIGGER_ASSISTANT(ReleaseHandleFailed, ReportViolation)(sh->GetTypeHandle(), sh->m_handle);
    }

    GCPROTECT_END();
}
```

# Interactions with other subsystems

## Debugger

One limitation of FCalls today is that you cannot easily debug both managed code and FCalls easily in Visual Studio's Interop (or mixed mode) debugging. Setting a breakpoint today in an FCall and debugging with Interop debugging just doesn't work. This most likely won't be fixed.

# Physical architecture

When the CLR starts up, CoreLib is loaded by a method called `SystemDomain::LoadBaseSystemClasses()`. Here, the base data types and other similar classes (like `Exception`) are loaded, and appropriate global pointers are set up to refer to CoreLib's types.

For FCalls, look in [fcall.h][fcall] for infrastructure, and [ecalllist.h][ecalllist] to properly inform the runtime about your FCall method.

For QCalls, look in [qcall.h][qcall] for associated infrastructure, and [ecalllist.h][ecalllist] to properly inform the runtime about your QCall method.

More general infrastructure and some native type definitions can be found in [object.h][object.h]. The binder uses `mscorlib.h` to associate managed and native classes.

[object.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/object.h
