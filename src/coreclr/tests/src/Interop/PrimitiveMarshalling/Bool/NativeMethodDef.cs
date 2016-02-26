using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;

public class NativeMethods
{

    public const string NativeSharedBinaryName = "BoolNative";

    [DllImport(NativeSharedBinaryName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool Marshal_In([In]bool boolValue);

    [DllImport(NativeSharedBinaryName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool Marshal_InOut([In, Out]bool boolValue);

    [DllImport(NativeSharedBinaryName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool Marshal_Out([Out]bool boolValue);

    [DllImport(NativeSharedBinaryName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalPointer_In([In]ref bool pboolValue);

    [DllImport(NativeSharedBinaryName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalPointer_InOut(ref bool pboolValue);

    [DllImport(NativeSharedBinaryName, CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalPointer_Out(out bool pboolValue);

    [DllImport(NativeSharedBinaryName, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_As_In(
      [In, MarshalAs(UnmanagedType.U1)]bool boolValue);

    [DllImport(NativeSharedBinaryName, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_As_InOut(
      [In, Out, MarshalAs(UnmanagedType.U1)]bool boolValue);

    [DllImport(NativeSharedBinaryName, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Marshal_As_Out(
      [Out, MarshalAs(UnmanagedType.U1)]bool boolValue);
}
