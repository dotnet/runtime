// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

if (!OperatingSystem.IsWindows())
    return 100;

foreach (string name in new[]
{
    "System.StubHelpers.AsAnyMarshaler",
    "System.StubHelpers.CSTRMarshaler",
    "System.StubHelpers.MngdRefCustomMarshaler",
    "System.StubHelpers.StructureMarshaler`1",
})
{
    if (FindType(name, typeof(object).Assembly) is not null)
    {
        Console.Error.WriteLine($"Runtime marshalling helper was not trimmed: {name}");
        return -1;
    }
}

Type comObjectType = FindType("System.__ComObject", typeof(object).Assembly);
if (comObjectType is not null &&
    (comObjectType.GetField("m_ObjectToDataMap", BindingFlags.Instance | BindingFlags.NonPublic) is not null ||
     comObjectType.GetMethod("ReleaseAllData", BindingFlags.Static | BindingFlags.NonPublic) is not null))
    return -14;

Type stringBuilderType = FindType("System.Text.StringBuilder", typeof(object).Assembly);
if (stringBuilderType is null ||
    stringBuilderType.GetMethod("ReplaceBufferInternal", BindingFlags.Instance | BindingFlags.NonPublic) is not null ||
    stringBuilderType.GetMethod("ReplaceBufferAnsiInternal", BindingFlags.Instance | BindingFlags.NonPublic) is not null)
    return -15;

Type exceptionType = FindType("System.Exception", typeof(object).Assembly);
if (exceptionType is null ||
    exceptionType.GetMethod("GetDescriptionBstr", BindingFlags.Static | BindingFlags.NonPublic) is not null ||
    exceptionType.GetMethod("GetSourceBstr", BindingFlags.Static | BindingFlags.NonPublic) is not null ||
    exceptionType.GetMethod("GetHelpContextBstr", BindingFlags.Static | BindingFlags.NonPublic) is not null)
    return -23;

if (FindType("System.Runtime.InteropServices.Marshalling.ComImportInteropInterfaceDetailsStrategy", typeof(ComObject).Assembly) is not null)
    return -10;

_ = GetTickCount64();
if (typeof(object).IsCOMObject || Marshal.IsComObject(new object()))
    return -12;

_ = typeof(object).GetHashCode();

nint kernel32 = NativeMethods.GetModuleHandle("kernel32.dll");
if (kernel32 == 0)
    return -5;

nint calliTarget = NativeLibrary.GetExport(kernel32, "IsProcessorFeaturePresent");
if (ConditionalCalli(calliTarget, false) != 0 ||
    ConditionalStructCalli(calliTarget, false) != 0 ||
    ConditionalStructReturnCalli(calliTarget, false))
    return -29;

try
{
    _ = ConditionalCalli(calliTarget, true);
    return -30;
}
catch (MarshalDirectiveException)
{
}

try
{
    _ = ConditionalStructCalli(calliTarget, true);
    return -31;
}
catch (MarshalDirectiveException)
{
}

try
{
    _ = ConditionalStructReturnCalli(calliTarget, true);
    return -32;
}
catch (MarshalDirectiveException)
{
}

try
{
    _ = GetModuleHandle("kernel32.dll");
    return -2;
}
catch (MarshalDirectiveException)
{
}

try
{
    _ = GetModuleHandleForBuilder(new StringBuilder("kernel32.dll"));
    return -16;
}
catch (MarshalDirectiveException)
{
}

#pragma warning disable CS0618
try
{
    _ = Marshal.ReadByte((object)"runtime", 0);
    return -26;
}
catch (MarshalDirectiveException)
{
}

bool[] values = [true];
try
{
    Marshal.WriteInt32((object)values, 0, 0);
    return -27;
}
catch (MarshalDirectiveException)
{
}
#pragma warning restore CS0618

if (!values[0])
    return -28;

unsafe
{
    int native = 0;
    Marshal.StructureToPtr(new Blittable { Value = 42 }, (nint)(&native), false);
    if (native != 42 || Marshal.PtrToStructure<Blittable>((nint)(&native)).Value != 42)
        return -6;
    Marshal.DestroyStructure<Blittable>((nint)(&native));

    Marshal.StructureToPtr(new NestedBlittable { Value = new Blittable { Value = 84 } }, (nint)(&native), false);
    if (native != 84 || Marshal.PtrToStructure<NestedBlittable>((nint)(&native)).Value.Value != 84)
        return -17;

    try
    {
        Marshal.StructureToPtr(new NonBlittable { Value = true }, (nint)(&native), false);
        return -7;
    }
    catch (MarshalDirectiveException)
    {
    }

    try
    {
        _ = GetModuleHandleForDateTime(default);
        return -18;
    }
    catch (MarshalDirectiveException)
    {
    }

    Callback callback = static value => value + 1;
    var pointer = (delegate* unmanaged[Cdecl]<int, int>)Marshal.GetFunctionPointerForDelegate(callback);
    if (pointer(41) != 42)
        return -3;
    GC.KeepAlive(callback);

    MarshallingCallback marshallingCallback = static value => value;
    try
    {
        _ = Marshal.GetFunctionPointerForDelegate(marshallingCallback);
        return -11;
    }
    catch (MarshalDirectiveException)
    {
    }
}

try
{
    _ = Marshal.SizeOf<NonBlittable>();
    return -8;
}
catch (MarshalDirectiveException)
{
}

try
{
    _ = Marshal.GetIUnknownForObject(new object());
    return -9;
}
catch (NotSupportedException)
{
}

if (ErrorInfoTest.SetStaleErrorInfo(out nint staleErrorInfo) != 0)
    return -19;

try
{
    var exception = new InvalidOperationException();
    if (Marshal.GetHRForException(exception) != exception.HResult || Marshal.GetHRForException(null) != 0)
        return -20;

    int errorInfoResult = GetErrorInfo(0, out nint errorInfo);
    if (errorInfo != 0)
        Marshal.Release(errorInfo);
    if (errorInfoResult != 1 || errorInfo != 0)
        return -21;
}
finally
{
    Marshal.Release(staleErrorInfo);
}

if (ErrorInfoTest.SetStaleErrorInfo(out staleErrorInfo) != 0)
    return -24;

try
{
    if (RuntimeHostTest.ExecuteManagedException() != RuntimeHostTest.InvalidOperationHResult)
        return -24;

    int errorInfoResult = GetErrorInfo(0, out nint errorInfo);
    if (errorInfo != 0)
        Marshal.Release(errorInfo);
    if (errorInfoResult != 1 || errorInfo != 0)
        return -25;
}
finally
{
    Marshal.Release(staleErrorInfo);
}

// Exercise an unrelated intrinsic after the VM-only structure marshaler has been removed.
if (RuntimeHelpers.IsReferenceOrContainsReferences<int>())
    return -4;

var comWrappers = new StrategyBasedComWrappers();
var nativeObject = comWrappers.GetOrCreateComInterfaceForObject(new ComClass(), CreateComInterfaceFlags.None);
try
{
    var wrapper = (IComInterface)comWrappers.GetOrCreateObjectForComInstance(nativeObject, CreateObjectFlags.None);
    var weakReference = new WeakReference<IComInterface>(wrapper);
    if (!weakReference.TryGetTarget(out IComInterface target) || !ReferenceEquals(wrapper, target))
        return -13;

    try
    {
        wrapper.Throw();
        return -22;
    }
    catch (InvalidOperationException)
    {
    }

    return wrapper.Method();
}
finally
{
    Marshal.Release(nativeObject);
}

[MethodImpl(MethodImplOptions.NoInlining)]
static Type FindType(string name, Assembly assembly) => assembly.GetType(name, throwOnError: false);

[MethodImpl(MethodImplOptions.NoInlining)]
static unsafe int ConditionalCalli(nint target, bool invoke) =>
    invoke ? ((delegate* unmanaged[Stdcall]<bool, int>)target)(true) : 0;

[MethodImpl(MethodImplOptions.NoInlining)]
static unsafe int ConditionalStructCalli(nint target, bool invoke) =>
    invoke ? ((delegate* unmanaged[Stdcall]<NonBlittable, int>)target)(new NonBlittable { Value = true }) : 0;

[MethodImpl(MethodImplOptions.NoInlining)]
static unsafe bool ConditionalStructReturnCalli(nint target, bool invoke) =>
    invoke && ((delegate* unmanaged[Stdcall]<uint, NonBlittable>)target)(0).Value;

[DllImport("kernel32.dll", ExactSpelling = true)]
static extern ulong GetTickCount64();

[DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
static extern nint GetModuleHandle(string name);

[DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
static extern nint GetModuleHandleForBuilder(StringBuilder name);

[DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
static extern nint GetModuleHandleForDateTime(DateTime value);

[DllImport("oleaut32.dll", ExactSpelling = true)]
static extern int GetErrorInfo(uint reserved, out nint errorInfo);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int Callback(int value);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
delegate bool MarshallingCallback([MarshalAs(UnmanagedType.U1)] bool value);

partial class NativeMethods
{
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleA", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint GetModuleHandle(string name);
}

[GeneratedComInterface]
[Guid("86bda32b-74d6-4bdb-b7a5-05f70ab114c1")]
partial interface IComInterface
{
    int Method();
    void Throw();
}

[GeneratedComClass]
partial class ComClass : IComInterface
{
    public int Method() => 100;
    public void Throw() => throw new InvalidOperationException();
}

struct Blittable
{
    public int Value;
}

struct NestedBlittable
{
    public Blittable Value;
}

struct NonBlittable
{
    public bool Value;
}
