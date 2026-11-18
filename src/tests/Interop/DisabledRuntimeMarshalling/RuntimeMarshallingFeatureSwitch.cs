// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Xunit;

public unsafe class RuntimeMarshallingFeatureSwitch
{
    private const string SwitchName = "System.Runtime.InteropServices.RuntimeMarshalling.IsEnabled";
    private const string BuiltInComSwitchName = "System.Runtime.InteropServices.BuiltInComInterop.IsSupported";
    private const string NativeLibraryName = "DisabledRuntimeMarshallingNative";

    private static bool IsEnabled => !AppContext.TryGetSwitch(SwitchName, out bool enabled) || enabled;
    private static bool IsBuiltInComEnabled => IsEnabled && (!AppContext.TryGetSwitch(BuiltInComSwitchName, out bool enabled) || enabled);

    [DllImport(NativeLibraryName, EntryPoint = "PassThrough", CallingConvention = CallingConvention.Cdecl)]
    private static extern byte PassThrough(byte value);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetCalliBooleanTarget();

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetCalliStructureTarget();

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetCalliStructureReturnTarget();

    [DllImport(NativeLibraryName, EntryPoint = "PassThrough", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool PassThroughBoolean([MarshalAs(UnmanagedType.U1)] bool value);

    [DllImport(NativeLibraryName, EntryPoint = "Invalid", CallingConvention = CallingConvention.Cdecl)]
    private static extern void MarshalString([MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(NativeLibraryName, EntryPoint = "Invalid", CallingConvention = CallingConvention.Cdecl)]
    private static extern void MarshalStringBuilder(StringBuilder value);

    [DllImport(NativeLibraryName, EntryPoint = "Invalid", CallingConvention = CallingConvention.Cdecl)]
    private static extern void MarshalCustom([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CustomMarshaler))] object value);

    [DllImport(NativeLibraryName, EntryPoint = "Invalid", CallingConvention = CallingConvention.Cdecl)]
    private static extern void MarshalArray([In] int[] value, int length);

    [DllImport(NativeLibraryName, EntryPoint = "Invalid", CallingConvention = CallingConvention.Cdecl, PreserveSig = false)]
    private static extern void MarshalHResult();

    [DllImport(NativeLibraryName, EntryPoint = "Invalid", CallingConvention = CallingConvention.Cdecl)]
    [LCIDConversion(0)]
    private static extern void MarshalLcid();

    [DllImport(NativeLibraryName, EntryPoint = "PassThrough", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern byte PassThroughWithLastError(byte value);

    [DllImport(NativeLibraryName, EntryPoint = "DecimalLow64", CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong DecimalLow64(decimal value);

    [DllImport(NativeLibraryName, EntryPoint = "DecimalLow64", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern ulong DecimalLow64WithLastError(decimal value);

    [DllImport(NativeLibraryName, EntryPoint = "Invalid", CallingConvention = CallingConvention.Cdecl)]
    private static extern void MarshalDateTime(DateTime value);

    [DllImport("oleaut32.dll", ExactSpelling = true)]
    private static extern int GetErrorInfo(uint reserved, out nint errorInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte BlittableDelegate(byte value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private delegate bool MarshallingDelegate([MarshalAs(UnmanagedType.U1)] bool value);

    [StructLayout(LayoutKind.Sequential)]
    private struct Blittable
    {
        public int Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NonBlittable
    {
        public bool Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private ref struct BlittableByRefLike
    {
        public int Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private class BlittableLayoutBase
    {
        public int First;
    }

    [StructLayout(LayoutKind.Sequential)]
    private sealed class BlittableLayout : BlittableLayoutBase
    {
        public int Second;
    }

    [StructLayout(LayoutKind.Auto)]
    private sealed class AutoLayout
    {
        public int Value;
    }

    private sealed class CustomMarshaler : ICustomMarshaler
    {
        public static ICustomMarshaler GetInstance(string cookie) => throw new InvalidOperationException("The custom marshaler must not be instantiated.");
        public void CleanUpManagedData(object value) => throw new InvalidOperationException();
        public void CleanUpNativeData(IntPtr value) => throw new InvalidOperationException();
        public int GetNativeDataSize() => throw new InvalidOperationException();
        public IntPtr MarshalManagedToNative(object value) => throw new InvalidOperationException();
        public object MarshalNativeToManaged(IntPtr value) => throw new InvalidOperationException();
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void PInvokes()
    {
        Assert.Equal(42, PassThrough(42));
        Assert.Equal(42, PassThroughWithLastError(42));
        Assert.Equal(123456789012345UL, DecimalLow64(123456789012345m));
        Assert.Equal(123456789012345UL, DecimalLow64WithLastError(123456789012345m));
        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(
            new DisabledRuntimeMarshallingNative.StructWithShortAndBool(42, true), 42, true));

        if (IsEnabled)
        {
            Assert.True(PassThroughBoolean(true));
            Assert.False(PassThroughBoolean(false));
            MarshalString("runtime marshalling");
            MarshalStringBuilder(new StringBuilder("runtime marshalling"));
        }
        else
        {
            Assert.Throws<MarshalDirectiveException>(() => PassThroughBoolean(true));
            Assert.Throws<MarshalDirectiveException>(() => MarshalString("runtime marshalling"));
            Assert.Throws<MarshalDirectiveException>(() => MarshalStringBuilder(new StringBuilder("runtime marshalling")));
            Assert.Throws<MarshalDirectiveException>(() => MarshalDateTime(default));
            Assert.Throws<MarshalDirectiveException>(() => MarshalCustom(new object()));
            Assert.Throws<MarshalDirectiveException>(() => MarshalArray([42], 1));
            Assert.Throws<MarshalDirectiveException>(() => MarshalHResult());
            Assert.Throws<MarshalDirectiveException>(() => MarshalLcid());
        }
    }

    [Fact]
    public static void LegacyObjectMarshalling()
    {
        bool[] values = [true];

#pragma warning disable CS0618
        Assert.Throws<AccessViolationException>(() => Marshal.ReadByte((object)null!, 0));
        Assert.Throws<AccessViolationException>(() => Marshal.WriteInt32((object)null!, 0, 0));

        if (IsEnabled)
        {
            Assert.Equal((byte)'r', Marshal.ReadByte((object)"runtime", 0));
            Assert.Equal(1, Marshal.ReadInt32((object)values, 0));
            Marshal.WriteInt32((object)values, 0, 0);
            Assert.False(values[0]);
        }
        else
        {
            Assert.Throws<MarshalDirectiveException>(() => Marshal.ReadByte((object)"runtime", 0));
            Assert.Throws<MarshalDirectiveException>(() => Marshal.ReadByte((object)new byte[] { 42 }, 0));
            Assert.Throws<MarshalDirectiveException>(() => Marshal.WriteInt32((object)values, 0, 0));
            Assert.True(values[0]);
        }
#pragma warning restore CS0618
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void UnmanagedCalliMarshalling()
    {
        nint boolean = GetCalliBooleanTarget();
        nint structure = GetCalliStructureTarget();
        nint structureReturn = GetCalliStructureReturnTarget();

        Assert.Equal(0, CallBoolean(boolean, false));
        Assert.Equal(0, CallStructure(structure, false));
        Assert.False(CallStructureReturn(structureReturn, false));

        if (IsEnabled)
        {
            Assert.Equal(1, CallBoolean(boolean, true));
            Assert.Equal(1, CallStructure(structure, true));
            Assert.True(CallStructureReturn(structureReturn, true));
        }
        else
        {
            Assert.Throws<MarshalDirectiveException>(() => CallBoolean(boolean, true));
            Assert.Throws<MarshalDirectiveException>(() => CallStructure(structure, true));
            Assert.Throws<MarshalDirectiveException>(() => CallStructureReturn(structureReturn, true));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallBoolean(nint address, bool invoke) =>
        invoke ? ((delegate* unmanaged[Cdecl]<bool, int>)address)(true) : 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallStructure(nint address, bool invoke) =>
        invoke ? ((delegate* unmanaged[Cdecl]<NonBlittable, int>)address)(new NonBlittable { Value = true }) : 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CallStructureReturn(nint address, bool invoke) =>
        invoke && ((delegate* unmanaged[Cdecl]<NonBlittable>)address)().Value;

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void Delegates()
    {
        nint library = NativeLibrary.Load(NativeLibraryName, typeof(RuntimeMarshallingFeatureSwitch).Assembly, null);
        try
        {
            nint address = NativeLibrary.GetExport(library, "PassThrough");
            BlittableDelegate callback = Marshal.GetDelegateForFunctionPointer<BlittableDelegate>(address);
            Assert.Equal(42, callback(42));
            Assert.Equal(42, ((delegate* unmanaged[Cdecl]<byte, byte>)Marshal.GetFunctionPointerForDelegate(callback))(42));

            BlittableDelegate managedCallback = static value => value;
            nint reverseAddress = Marshal.GetFunctionPointerForDelegate(managedCallback);
            Assert.Equal(42, ((delegate* unmanaged[Cdecl]<byte, byte>)reverseAddress)(42));
            GC.KeepAlive(managedCallback);

            if (IsEnabled)
            {
                Assert.True(Marshal.GetDelegateForFunctionPointer<MarshallingDelegate>(address)(true));
            }
            else
            {
                Assert.Throws<MarshalDirectiveException>(() => Marshal.GetDelegateForFunctionPointer<MarshallingDelegate>(address)(true));
            }
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsExceptionInteropSupported))]
    public static void ReverseDelegateRequiringMarshalling()
    {
        MarshallingDelegate callback = static value => value;
        nint address = Marshal.GetFunctionPointerForDelegate(callback);
        if (IsEnabled)
        {
            Assert.Equal((byte)1, ((delegate* unmanaged[Cdecl]<byte, byte>)address)(1));
        }
        else
        {
            Assert.Throws<MarshalDirectiveException>(() => ((delegate* unmanaged[Cdecl]<byte, byte>)address)(1));
        }
        GC.KeepAlive(callback);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void DelegatesFromAssemblyWithDisabledMarshalling()
    {
        const short value = 42;
        var structure = new DisabledRuntimeMarshallingNative.StructWithShortAndBool(value, true);
        var callback = Marshal.GetDelegateForFunctionPointer<DisabledRuntimeMarshallingNative.CheckStructWithShortAndBoolCallback>(
            DisabledRuntimeMarshallingNative.GetStructWithShortAndBoolCallback());
        Assert.True(callback(structure, value, true));

        DisabledRuntimeMarshallingNative.CheckStructWithShortAndBoolCallback managedCallback =
            static (item, expected, flag) => item.s == expected && item.b == flag;
        nint address = Marshal.GetFunctionPointerForDelegate(managedCallback);
        Assert.True(DisabledRuntimeMarshallingNative.CallCheckStructWithShortAndBoolCallback(
            (delegate* unmanaged<DisabledRuntimeMarshallingNative.StructWithShortAndBool, short, bool, bool>)address,
            structure, value, true));
        GC.KeepAlive(managedCallback);
    }

    [Fact]
    public static void Structures()
    {
        int native = 0;
        Blittable value = new() { Value = 42 };
        Marshal.StructureToPtr(value, (nint)(&native), false);
        Assert.Equal(42, native);
        Assert.Equal(42, Marshal.PtrToStructure<Blittable>((nint)(&native)).Value);
        Marshal.DestroyStructure<Blittable>((nint)(&native));
        Assert.Equal(sizeof(int), Marshal.SizeOf<Blittable>());
        Assert.Equal(0, (int)Marshal.OffsetOf<Blittable>(nameof(Blittable.Value)));

        nint address = (nint)(&native);
        if (IsEnabled)
        {
            Marshal.StructureToPtr(new NonBlittable { Value = true }, address, false);
            Assert.Equal(1, native);
            Assert.True(Marshal.PtrToStructure<NonBlittable>(address).Value);
            Marshal.DestroyStructure<NonBlittable>(address);
        }
        else
        {
            Assert.Throws<MarshalDirectiveException>(() => Marshal.StructureToPtr(new NonBlittable(), address, false));
            Assert.Throws<MarshalDirectiveException>(() => Marshal.PtrToStructure<NonBlittable>(address));
            Assert.Throws<MarshalDirectiveException>(() => Marshal.DestroyStructure<NonBlittable>(address));
            Assert.Throws<MarshalDirectiveException>(() => Marshal.SizeOf<NonBlittable>());
            Assert.Throws<MarshalDirectiveException>(() => Marshal.OffsetOf<NonBlittable>(nameof(NonBlittable.Value)));
        }
    }

    [Fact]
    public static void BlittableLayoutClass()
    {
        int* native = stackalloc int[2];
        nint address = (nint)native;
        var value = new BlittableLayout { First = 42, Second = 84 };

        Assert.Equal(2 * sizeof(int), Marshal.SizeOf<BlittableLayout>());
        Marshal.StructureToPtr(value, address, true);
        Assert.Equal(42, native[0]);
        Assert.Equal(84, native[1]);

        var result = Marshal.PtrToStructure<BlittableLayout>(address);
        Assert.Equal(42, result.First);
        Assert.Equal(84, result.Second);
        native[0] = 21;
        native[1] = 63;
        Marshal.PtrToStructure(address, value);
        Assert.Equal(21, value.First);
        Assert.Equal(63, value.Second);
        Marshal.DestroyStructure<BlittableLayout>(address);
    }

    [Fact]
    public static void PointerTypes()
    {
        Assert.Equal(IntPtr.Size, Marshal.SizeOf(typeof(int*)));
        Assert.Equal(IntPtr.Size, Marshal.SizeOf(typeof(delegate*<void>)));

        nint value = 0;
        nint address = (nint)(&value);
        Assert.Throws<ArgumentException>(() => Marshal.DestroyStructure(address, typeof(int*)));
        Assert.Throws<ArgumentException>(() => Marshal.DestroyStructure(address, typeof(delegate*<void>)));
        Assert.Throws<ArgumentException>(() => Marshal.DestroyStructure(address, typeof(BlittableByRefLike)));
    }

    [Fact]
    public static void InvalidStructureTypes()
    {
        Assert.Throws<ArgumentException>(() => Marshal.SizeOf(typeof(int).MakeByRefType()));
        Assert.Throws<ArgumentException>(() => Marshal.SizeOf(typeof(string[])));
        Assert.Throws<ArgumentException>(() => Marshal.OffsetOf<AutoLayout>(nameof(AutoLayout.Value)));
    }

    [Fact]
    public static void ManagedComFeatureUsesRuntimeConfiguration()
    {
        bool enabled = IsEnabled;
        bool builtInComEnabled = OperatingSystem.IsWindows() && IsBuiltInComEnabled;
        object? originalData = AppContext.GetData(SwitchName);
        AppContext.SetSwitch(SwitchName, !enabled);
        AppContext.SetData(SwitchName, enabled ? "false" : "true");
        try
        {
            PropertyInfo property = typeof(ComObject).GetProperty("BuiltInComSupported", BindingFlags.Static | BindingFlags.NonPublic)!;
            Assert.Equal(builtInComEnabled, (bool)property.GetValue(null)!);
        }
        finally
        {
            AppContext.SetData(SwitchName, originalData);
            AppContext.SetSwitch(SwitchName, enabled);
        }
    }

    [Fact]
    public static void BuiltInCom()
    {
        if (!IsBuiltInComEnabled && OperatingSystem.IsWindows())
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetIUnknownForObject(new object()));
        }
        else if (OperatingSystem.IsWindows())
        {
            Marshal.Release(Marshal.GetIUnknownForObject(new object()));
        }
    }

    [Fact]
    public static void ExceptionHResultAndErrorInfo()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.Equal(0, ErrorInfoTest.SetStaleErrorInfo(out nint staleErrorInfo));
        try
        {
            Assert.Equal(0, Marshal.GetHRForException(null));
            Assert.Equal(0, GetErrorInfo(0, out nint nullExceptionErrorInfo));
            try
            {
                Assert.Equal(staleErrorInfo, nullExceptionErrorInfo);
            }
            finally
            {
                if (nullExceptionErrorInfo != 0)
                {
                    Marshal.Release(nullExceptionErrorInfo);
                }
            }
        }
        finally
        {
            Marshal.Release(staleErrorInfo);
        }

        Assert.Equal(0, ErrorInfoTest.SetStaleErrorInfo(out staleErrorInfo));
        try
        {
            var exception = new InvalidOperationException();
            Assert.Equal(exception.HResult, Marshal.GetHRForException(exception));
            Assert.Equal(0, Marshal.GetHRForException(null));

            int result = GetErrorInfo(0, out nint errorInfo);
            try
            {
                Assert.Equal(IsEnabled ? 0 : 1, result);
                if (IsEnabled)
                {
                    Assert.NotEqual(0, errorInfo);
                    Assert.NotEqual(staleErrorInfo, errorInfo);
                }
                else
                {
                    Assert.Equal(0, errorInfo);
                }
            }
            finally
            {
                if (errorInfo != 0)
                {
                    Marshal.Release(errorInfo);
                }
            }
        }
        finally
        {
            Marshal.Release(staleErrorInfo);
        }
    }

    [Fact]
    public static void HostExceptionHResultAndErrorInfo()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.Equal(0, ErrorInfoTest.SetStaleErrorInfo(out nint staleErrorInfo));
        try
        {
            Assert.Equal(RuntimeHostTest.InvalidOperationHResult, RuntimeHostTest.ExecuteManagedException());

            int result = GetErrorInfo(0, out nint errorInfo);
            try
            {
                Assert.Equal(IsEnabled ? 0 : 1, result);
                Assert.Equal(IsEnabled, errorInfo != 0);
                if (IsEnabled)
                {
                    Assert.NotEqual(staleErrorInfo, errorInfo);
                }
            }
            finally
            {
                if (errorInfo != 0)
                {
                    Marshal.Release(errorInfo);
                }
            }
        }
        finally
        {
            Marshal.Release(staleErrorInfo);
        }
    }
}
