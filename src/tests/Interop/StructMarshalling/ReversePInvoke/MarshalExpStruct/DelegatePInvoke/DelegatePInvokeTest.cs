// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using Xunit;

public class Test_DelegatePInvokeTest
{
    const int iNative = 11;//the value passed from Native side to Managed side
    const int iManaged = 10;//The value passed from Managed side to Native sid

    enum StructID
    {
        INNER2Id,
        InnerExplicitId,
        InnerArrayExplicitId,
        OUTER3Id,
        UId,
        ByteStructPack2ExplicitId,
        ShortStructPack4ExplicitId,
        IntStructPack8ExplicitId,
        LongStructPack16ExplicitId
    }

    #region Methods implementation

    #region Delegate,PInvoke,PassByRef,Cdecl
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByRef_INNER2([In, Out]ref INNER2 inner);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByRef_INNER2 Get_MarshalStructAsParam_AsExpByRefINNER2_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByRef_InnerExplicit([In, Out]ref InnerExplicit ie);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByRef_InnerExplicit Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByRef_InnerArrayExplicit([In, Out]ref InnerArrayExplicit iae);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByRef_InnerArrayExplicit Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByRef_OUTER3([In, Out]ref OUTER3 outer);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByRef_OUTER3 Get_MarshalStructAsParam_AsExpByRefOUTER3_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByRef_U([In, Out]ref U u);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByRef_U Get_MarshalStructAsParam_AsExpByRefU_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByRef_ByteStructPack2Explicit([In, Out]ref ByteStructPack2Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByRef_ByteStructPack2Explicit Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByRef_ShortStructPack4Explicit([In, Out]ref ShortStructPack4Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByRef_ShortStructPack4Explicit Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByRef_IntStructPack8Explicit([In, Out]ref IntStructPack8Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByRef_IntStructPack8Explicit Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByRef_LongStructPack16Explicit([In, Out]ref LongStructPack16Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByRef_LongStructPack16Explicit Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Cdecl_FuncPtr();

    private static void TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID structid)
    {
        Console.WriteLine("Delegate,Pinvoke,By Ref,Cdecl");
        switch (structid)
        {
            case StructID.INNER2Id:
                INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                INNER2 changeINNER2 = Helper.NewINNER2(77, 77.0F, "changed string");
                DelegateCdeclByRef_INNER2 caller_INNER2 = Get_MarshalStructAsParam_AsExpByRefINNER2_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefINNER2_Cdecl_FuncPtr...");
                Assert.True(caller_INNER2(ref sourceINNER2));
                Assert.True(Helper.ValidateINNER2(sourceINNER2, changeINNER2, "Get_MarshalStructAsParam_AsExpByRefINNER2_Cdecl_FuncPtr"));
                break;

            case StructID.InnerExplicitId:
                InnerExplicit sourceInnerExplicit = new InnerExplicit();
                sourceInnerExplicit.f1 = 1;
                sourceInnerExplicit.f3 = "some string";
                InnerExplicit changeInnerExplicit = new InnerExplicit();
                changeInnerExplicit.f1 = 77;
                changeInnerExplicit.f3 = "changed string";
                DelegateCdeclByRef_InnerExplicit caller_InnerExplicit = Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Cdecl_FuncPtr...");
                Assert.True(caller_InnerExplicit(ref sourceInnerExplicit));
                Assert.True(Helper.ValidateInnerExplicit(sourceInnerExplicit, changeInnerExplicit, "Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Cdecl_FuncPtr"));
                break;

            case StructID.InnerArrayExplicitId:
                InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                InnerArrayExplicit changeInnerArrayExplicit = Helper.NewInnerArrayExplicit(77, 77.0F, "change string1", "change string2");
                DelegateCdeclByRef_InnerArrayExplicit caller_InnerArrayExplicit = Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Cdecl_FuncPtr...");
                Assert.True(caller_InnerArrayExplicit(ref sourceInnerArrayExplicit));
                Assert.True(Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, changeInnerArrayExplicit, "Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Cdecl_FuncPtr"));
                break;

            case StructID.OUTER3Id:
                OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                OUTER3 changeOUTER3 = Helper.NewOUTER3(77, 77.0F, "changed string", "changed string");
                DelegateCdeclByRef_OUTER3 caller_OUTER3 = Get_MarshalStructAsParam_AsExpByRefOUTER3_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefOUTER3_Cdecl_FuncPtr...");
                Assert.True(caller_OUTER3(ref sourceOUTER3));
                Assert.True(Helper.ValidateOUTER3(sourceOUTER3, changeOUTER3, "Get_MarshalStructAsParam_AsExpByRefOUTER3_Cdecl_FuncPtr"));
                break;

            case StructID.UId:
                U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue,
                    byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                U changeU = Helper.NewU(Int32.MaxValue, UInt32.MinValue, new IntPtr(-64), new UIntPtr(64), short.MaxValue, ushort.MinValue,
                    byte.MaxValue, sbyte.MinValue, long.MaxValue, ulong.MinValue, 64.0F, 6.4);
                DelegateCdeclByRef_U caller_U = Get_MarshalStructAsParam_AsExpByRefU_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefU_Cdecl_FuncPtr...");
                Assert.True(caller_U(ref sourceU));
                Assert.True(Helper.ValidateU(sourceU, changeU, "Get_MarshalStructAsParam_AsExpByRefU_Cdecl_FuncPtr"));
                break;

            case StructID.ByteStructPack2ExplicitId:
                ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                ByteStructPack2Explicit change_bspe = Helper.NewByteStructPack2Explicit(64, 64);
                DelegateCdeclByRef_ByteStructPack2Explicit caller_ByteStructPack2Explicit = Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Cdecl_FuncPtr...");
                Assert.True(caller_ByteStructPack2Explicit(ref source_bspe));
                Assert.True(Helper.ValidateByteStructPack2Explicit(source_bspe, change_bspe, "Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Cdecl_FuncPtr"));
                break;

            case StructID.ShortStructPack4ExplicitId:
                ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                ShortStructPack4Explicit change_sspe = Helper.NewShortStructPack4Explicit(64, 64);
                DelegateCdeclByRef_ShortStructPack4Explicit caller_ShortStructPack4Explicit = Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Cdecl_FuncPtr...");
                Assert.True(caller_ShortStructPack4Explicit(ref source_sspe));
                Assert.True(Helper.ValidateShortStructPack4Explicit(source_sspe, change_sspe, "Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Cdecl_FuncPtr"));
                break;

            case StructID.IntStructPack8ExplicitId:
                IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                IntStructPack8Explicit change_ispe = Helper.NewIntStructPack8Explicit(64, 64);
                DelegateCdeclByRef_IntStructPack8Explicit caller_IntStructPack8Explicit = Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Cdecl_FuncPtr...");
                Assert.True(caller_IntStructPack8Explicit(ref source_ispe));
                Assert.True(Helper.ValidateIntStructPack8Explicit(source_ispe, change_ispe, "Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Cdecl_FuncPtr"));
                break;

            case StructID.LongStructPack16ExplicitId:
                LongStructPack16Explicit source_lspe = Helper.NewLongStructPack16Explicit(32, 32);
                LongStructPack16Explicit change_lspe = Helper.NewLongStructPack16Explicit(64, 64);
                DelegateCdeclByRef_LongStructPack16Explicit caller_LongStructPack16Explicit = Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Cdecl_FuncPtr...");
                Assert.True(caller_LongStructPack16Explicit(ref source_lspe));
                Assert.True(Helper.ValidateLongStructPack16Explicit(source_lspe, change_lspe, "Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Cdecl_FuncPtr"));
                break;

            default:
                Assert.Fail("TestMethod_DelegatePInvoke_MarshalByRef_Cdecl:The structid (Managed Side) is wrong");
                break;
        }
    }

    #endregion

    #region Delegate,PInvoke,PassByRef,Stdcall

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByRef_INNER2([In, Out]ref INNER2 inner);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByRef_INNER2 Get_MarshalStructAsParam_AsExpByRefINNER2_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByRef_InnerExplicit([In, Out]ref InnerExplicit ie);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByRef_InnerExplicit Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByRef_InnerArrayExplicit([In, Out]ref InnerArrayExplicit iae);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByRef_InnerArrayExplicit Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByRef_OUTER3([In, Out]ref OUTER3 outer);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByRef_OUTER3 Get_MarshalStructAsParam_AsExpByRefOUTER3_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByRef_U([In, Out]ref U u);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByRef_U Get_MarshalStructAsParam_AsExpByRefU_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByRef_ByteStructPack2Explicit([In, Out]ref ByteStructPack2Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByRef_ByteStructPack2Explicit Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByRef_ShortStructPack4Explicit([In, Out]ref ShortStructPack4Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByRef_ShortStructPack4Explicit Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByRef_IntStructPack8Explicit([In, Out]ref IntStructPack8Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByRef_IntStructPack8Explicit Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByRef_LongStructPack16Explicit([In, Out]ref LongStructPack16Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByRef_LongStructPack16Explicit Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Stdcall_FuncPtr();

    private static void TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID structid)
    {
        Console.WriteLine("Delegate,Pinvoke,By Ref,Stdcall");
        switch (structid)
        {
            case StructID.INNER2Id:
                INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                INNER2 changeINNER2 = Helper.NewINNER2(77, 77.0F, "changed string");
                DelegateStdcallByRef_INNER2 caller_INNER2 = Get_MarshalStructAsParam_AsExpByRefINNER2_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefINNER2_Stdcall_FuncPtr...");
                Assert.True(caller_INNER2(ref sourceINNER2));
                Assert.True(Helper.ValidateINNER2(sourceINNER2, changeINNER2, "Get_MarshalStructAsParam_AsExpByRefINNER2_Stdcall_FuncPtr"));
                break;

            case StructID.InnerExplicitId:
                InnerExplicit sourceInnerExplicit = new InnerExplicit();
                sourceInnerExplicit.f1 = 1;
                sourceInnerExplicit.f3 = "some string";
                InnerExplicit changeInnerExplicit = new InnerExplicit();
                changeInnerExplicit.f1 = 77;
                changeInnerExplicit.f3 = "changed string";
                DelegateStdcallByRef_InnerExplicit caller_InnerExplicit = Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Stdcall_FuncPtr...");
                Assert.True(caller_InnerExplicit(ref sourceInnerExplicit));
                Assert.True(Helper.ValidateInnerExplicit(sourceInnerExplicit, changeInnerExplicit, "Get_MarshalStructAsParam_AsExpByRefInnerExplicit_Stdcall_FuncPtr"));
                break;

            case StructID.InnerArrayExplicitId:
                InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                InnerArrayExplicit changeInnerArrayExplicit = Helper.NewInnerArrayExplicit(77, 77.0F, "change string1", "change string2");
                DelegateStdcallByRef_InnerArrayExplicit caller_InnerArrayExplicit = Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Stdcall_FuncPtr...");
                Assert.True(caller_InnerArrayExplicit(ref sourceInnerArrayExplicit));
                Assert.True(Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, changeInnerArrayExplicit, "Get_MarshalStructAsParam_AsExpByRefInnerArrayExplicit_Stdcall_FuncPtr"));
                break;

            case StructID.OUTER3Id:
                OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                OUTER3 changeOUTER3 = Helper.NewOUTER3(77, 77.0F, "changed string", "changed string");
                DelegateStdcallByRef_OUTER3 caller_OUTER3 = Get_MarshalStructAsParam_AsExpByRefOUTER3_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefOUTER3_Stdcall_FuncPtr...");
                Assert.True(caller_OUTER3(ref sourceOUTER3));
                Assert.True(Helper.ValidateOUTER3(sourceOUTER3, changeOUTER3, "Get_MarshalStructAsParam_AsExpByRefOUTER3_Stdcall_FuncPtr"));
                break;

            case StructID.UId:
                U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue,
                    byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                U changeU = Helper.NewU(Int32.MaxValue, UInt32.MinValue, new IntPtr(-64), new UIntPtr(64), short.MaxValue, ushort.MinValue,
                    byte.MaxValue, sbyte.MinValue, long.MaxValue, ulong.MinValue, 64.0F, 6.4);
                DelegateStdcallByRef_U caller_U = Get_MarshalStructAsParam_AsExpByRefU_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefU_Stdcall_FuncPtr...");
                Assert.True(caller_U(ref sourceU));
                Assert.True(Helper.ValidateU(sourceU, changeU, "Get_MarshalStructAsParam_AsExpByRefU_Stdcall_FuncPtr"));
                break;

            case StructID.ByteStructPack2ExplicitId:
                ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                ByteStructPack2Explicit change_bspe = Helper.NewByteStructPack2Explicit(64, 64);
                DelegateStdcallByRef_ByteStructPack2Explicit caller_ByteStructPack2Explicit = Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Stdcall_FuncPtr...");
                Assert.True(caller_ByteStructPack2Explicit(ref source_bspe));
                Assert.True(Helper.ValidateByteStructPack2Explicit(source_bspe, change_bspe, "Get_MarshalStructAsParam_AsExpByRefByteStructPack2Explicit_Stdcall_FuncPtr"));
                break;

            case StructID.ShortStructPack4ExplicitId:
                ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                ShortStructPack4Explicit change_sspe = Helper.NewShortStructPack4Explicit(64, 64);
                DelegateStdcallByRef_ShortStructPack4Explicit caller_ShortStructPack4Explicit = Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Stdcall_FuncPtr...");
                Assert.True(caller_ShortStructPack4Explicit(ref source_sspe));
                Assert.True(Helper.ValidateShortStructPack4Explicit(source_sspe, change_sspe, "Get_MarshalStructAsParam_AsExpByRefShortStructPack4Explicit_Stdcall_FuncPtr"));
                break;

            case StructID.IntStructPack8ExplicitId:
                IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                IntStructPack8Explicit change_ispe = Helper.NewIntStructPack8Explicit(64, 64);
                DelegateStdcallByRef_IntStructPack8Explicit caller_IntStructPack8Explicit = Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Stdcall_FuncPtr...");
                Assert.True(caller_IntStructPack8Explicit(ref source_ispe));
                Assert.True(Helper.ValidateIntStructPack8Explicit(source_ispe, change_ispe, "Get_MarshalStructAsParam_AsExpByRefIntStructPack8Explicit_Stdcall_FuncPtr"));
                break;

            case StructID.LongStructPack16ExplicitId:
                LongStructPack16Explicit source_lspe = Helper.NewLongStructPack16Explicit(32, 32);
                LongStructPack16Explicit change_lspe = Helper.NewLongStructPack16Explicit(64, 64);
                DelegateStdcallByRef_LongStructPack16Explicit caller_LongStructPack16Explicit = Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Stdcall_FuncPtr...");
                Assert.True(caller_LongStructPack16Explicit(ref source_lspe));
                Assert.True(Helper.ValidateLongStructPack16Explicit(source_lspe, change_lspe, "Get_MarshalStructAsParam_AsExpByRefLongStructPack16Explicit_Stdcall_FuncPtr"));
                break;

            default:
                Assert.Fail("TestMethod_DelegatePInvoke_MarshalByRef_Stdcall:The structid (Managed Side) is wrong");
                break;
        }
    }

    #endregion

    #region Delegate,PInvoke,PassByVal,Cdecl

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByVal_INNER2([In, Out] INNER2 inner);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByVal_INNER2 Get_MarshalStructAsParam_AsExpByValINNER2_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByVal_InnerExplicit([In, Out] InnerExplicit ie);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByVal_InnerExplicit Get_MarshalStructAsParam_AsExpByValInnerExplicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByVal_InnerArrayExplicit([In, Out] InnerArrayExplicit iae);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByVal_InnerArrayExplicit Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByVal_OUTER3([In, Out] OUTER3 outer);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByVal_OUTER3 Get_MarshalStructAsParam_AsExpByValOUTER3_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByVal_U([In, Out] U u);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByVal_U Get_MarshalStructAsParam_AsExpByValU_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByVal_ByteStructPack2Explicit([In, Out] ByteStructPack2Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByVal_ByteStructPack2Explicit Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByVal_ShortStructPack4Explicit([In, Out] ShortStructPack4Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByVal_ShortStructPack4Explicit Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByVal_IntStructPack8Explicit([In, Out] IntStructPack8Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByVal_IntStructPack8Explicit Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Cdecl_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegateCdeclByVal_LongStructPack16Explicit([In, Out] LongStructPack16Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateCdeclByVal_LongStructPack16Explicit Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Cdecl_FuncPtr();

    private static void TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID structid)
    {
        Console.WriteLine("Delegate,Pinvoke,By Val,Cdecl");
        switch (structid)
        {
            case StructID.INNER2Id:
                INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                INNER2 cloneINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                DelegateCdeclByVal_INNER2 caller_INNER2 = Get_MarshalStructAsParam_AsExpByValINNER2_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValINNER2_Cdecl_FuncPtr...");
                Assert.True(caller_INNER2(sourceINNER2));
                Assert.True(Helper.ValidateINNER2(sourceINNER2, cloneINNER2, "Get_MarshalStructAsParam_AsExpByValINNER2_Cdecl_FuncPtr"));
                break;

            case StructID.InnerExplicitId:
                InnerExplicit sourceInnerExplicit = new InnerExplicit();
                sourceInnerExplicit.f1 = 1;
                sourceInnerExplicit.f3 = "some string";
                InnerExplicit cloneInnerExplicit = new InnerExplicit();
                cloneInnerExplicit.f1 = 1;
                cloneInnerExplicit.f3 = "some string";
                DelegateCdeclByVal_InnerExplicit caller_InnerExplicit = Get_MarshalStructAsParam_AsExpByValInnerExplicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValInnerExplicit_Cdecl_FuncPtr...");
                Assert.True(caller_InnerExplicit(sourceInnerExplicit));
                Assert.True(Helper.ValidateInnerExplicit(sourceInnerExplicit, cloneInnerExplicit, "Get_MarshalStructAsParam_AsExpByValInnerExplicit_Cdecl_FuncPtr"));
                break;

            case StructID.InnerArrayExplicitId:
                InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                InnerArrayExplicit cloneInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                DelegateCdeclByVal_InnerArrayExplicit caller_InnerArrayExplicit = Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Cdecl_FuncPtr...");
                Assert.True(caller_InnerArrayExplicit(sourceInnerArrayExplicit));
                Assert.True(Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, cloneInnerArrayExplicit, "Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Cdecl_FuncPtr"));
                break;

            case StructID.OUTER3Id:
                OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                OUTER3 cloneOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                DelegateCdeclByVal_OUTER3 caller_OUTER3 = Get_MarshalStructAsParam_AsExpByValOUTER3_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValOUTER3_Cdecl_FuncPtr...");
                Assert.True(caller_OUTER3(sourceOUTER3));
                Assert.True(Helper.ValidateOUTER3(sourceOUTER3, cloneOUTER3, "Get_MarshalStructAsParam_AsExpByValOUTER3_Cdecl_FuncPtr"));
                break;

            case StructID.UId:
                U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue,
                    sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                U cloneU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue,
                    sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                DelegateCdeclByVal_U caller_U = Get_MarshalStructAsParam_AsExpByValU_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValU_Cdecl_FuncPtr...");
                Assert.True(caller_U(sourceU));
                Assert.True(Helper.ValidateU(sourceU, cloneU, "Get_MarshalStructAsParam_AsExpByValU_Cdecl_FuncPtr"));
                break;

            case StructID.ByteStructPack2ExplicitId:
                ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                ByteStructPack2Explicit clone_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                DelegateCdeclByVal_ByteStructPack2Explicit caller_ByteStructPack2Explicit = Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Cdecl_FuncPtr...");
                Assert.True(caller_ByteStructPack2Explicit(source_bspe));
                Assert.True(Helper.ValidateByteStructPack2Explicit(source_bspe, clone_bspe, "Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Cdecl_FuncPtr"));
                break;

            case StructID.ShortStructPack4ExplicitId:
                ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                ShortStructPack4Explicit clone_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                DelegateCdeclByVal_ShortStructPack4Explicit caller_ShortStructPack4Explicit = Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Cdecl_FuncPtr...");
                Assert.True(caller_ShortStructPack4Explicit(source_sspe));
                Assert.True(Helper.ValidateShortStructPack4Explicit(source_sspe, clone_sspe, "Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Cdecl_FuncPtr"));
                break;

            case StructID.IntStructPack8ExplicitId:
                IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                IntStructPack8Explicit clone_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                DelegateCdeclByVal_IntStructPack8Explicit caller_IntStructPack8Explicit = Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Cdecl_FuncPtr...");
                Assert.True(caller_IntStructPack8Explicit(source_ispe));
                Assert.True(Helper.ValidateIntStructPack8Explicit(source_ispe, clone_ispe, "Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Cdecl_FuncPtr"));
                break;

            case StructID.LongStructPack16ExplicitId:
                LongStructPack16Explicit source_lspe = Helper.NewLongStructPack16Explicit(32, 32);
                LongStructPack16Explicit clone_lspe = Helper.NewLongStructPack16Explicit(32, 32);
                DelegateCdeclByVal_LongStructPack16Explicit caller_LongStructPack16Explicit = Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Cdecl_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Cdecl_FuncPtr...");
                Assert.True(caller_LongStructPack16Explicit(source_lspe));
                Assert.True(Helper.ValidateLongStructPack16Explicit(source_lspe, clone_lspe, "Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Cdecl_FuncPtr"));
                break;

            default:
                Assert.Fail("TestMethod_DelegatePInvoke_MarshalByRef_Cdecl:The structid (Managed Side) is wrong");
                break;
        }
    }

    #endregion

    #region Delegate,PInvoke,PassByVal,Stdcall

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByVal_INNER2([In, Out] INNER2 inner);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByVal_INNER2 Get_MarshalStructAsParam_AsExpByValINNER2_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByVal_InnerExplicit([In, Out] InnerExplicit ie);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByVal_InnerExplicit Get_MarshalStructAsParam_AsExpByValInnerExplicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByVal_InnerArrayExplicit([In, Out] InnerArrayExplicit iae);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByVal_InnerArrayExplicit Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByVal_OUTER3([In, Out] OUTER3 outer);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByVal_OUTER3 Get_MarshalStructAsParam_AsExpByValOUTER3_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByVal_U([In, Out] U u);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByVal_U Get_MarshalStructAsParam_AsExpByValU_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByVal_ByteStructPack2Explicit([In, Out] ByteStructPack2Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByVal_ByteStructPack2Explicit Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByVal_ShortStructPack4Explicit([In, Out] ShortStructPack4Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByVal_ShortStructPack4Explicit Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByVal_IntStructPack8Explicit([In, Out] IntStructPack8Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByVal_IntStructPack8Explicit Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Stdcall_FuncPtr();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegateStdcallByVal_LongStructPack16Explicit([In, Out] LongStructPack16Explicit bspe);

    [DllImport("ReversePInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegateStdcallByVal_LongStructPack16Explicit Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Stdcall_FuncPtr();

    private static void TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID structid)
    {
        Console.WriteLine("Delegate,Pinvoke,By Val,Stdcall");
        switch (structid)
        {
            case StructID.INNER2Id:
                INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                INNER2 cloneINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                DelegateStdcallByVal_INNER2 caller_INNER2 = Get_MarshalStructAsParam_AsExpByValINNER2_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValINNER2_Stdcall_FuncPtr...");
                Assert.True(caller_INNER2(sourceINNER2));
                Assert.True(Helper.ValidateINNER2(sourceINNER2, cloneINNER2, "Get_MarshalStructAsParam_AsExpByValINNER2_Stdcall_FuncPtr"));
                break;

            case StructID.InnerExplicitId:
                InnerExplicit sourceInnerExplicit = new InnerExplicit();
                sourceInnerExplicit.f1 = 1;
                sourceInnerExplicit.f3 = "some string";
                InnerExplicit cloneInnerExplicit = new InnerExplicit();
                cloneInnerExplicit.f1 = 1;
                cloneInnerExplicit.f3 = "some string";
                DelegateStdcallByVal_InnerExplicit caller_InnerExplicit = Get_MarshalStructAsParam_AsExpByValInnerExplicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValInnerExplicit_Stdcall_FuncPtr...");
                Assert.True(caller_InnerExplicit(sourceInnerExplicit));
                Assert.True(Helper.ValidateInnerExplicit(sourceInnerExplicit, cloneInnerExplicit, "Get_MarshalStructAsParam_AsExpByValInnerExplicit_Stdcall_FuncPtr"));
                break;

            case StructID.InnerArrayExplicitId:
                InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                InnerArrayExplicit cloneInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                DelegateStdcallByVal_InnerArrayExplicit caller_InnerArrayExplicit = Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Stdcall_FuncPtr...");
                Assert.True(caller_InnerArrayExplicit(sourceInnerArrayExplicit));
                Assert.True(Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, cloneInnerArrayExplicit, "Get_MarshalStructAsParam_AsExpByValInnerArrayExplicit_Stdcall_FuncPtr"));
                break;

            case StructID.OUTER3Id:
                OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                OUTER3 cloneOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                DelegateStdcallByVal_OUTER3 caller_OUTER3 = Get_MarshalStructAsParam_AsExpByValOUTER3_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValOUTER3_Stdcall_FuncPtr...");
                Assert.True(caller_OUTER3(sourceOUTER3));
                Assert.True(Helper.ValidateOUTER3(sourceOUTER3, cloneOUTER3, "Get_MarshalStructAsParam_AsExpByValOUTER3_Stdcall_FuncPtr"));
                break;

            case StructID.UId:
                U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue,
                    byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                U cloneU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue,
                    byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                DelegateStdcallByVal_U caller_U = Get_MarshalStructAsParam_AsExpByValU_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValU_Stdcall_FuncPtr...");
                Assert.True(caller_U(sourceU));
                Assert.True(Helper.ValidateU(sourceU, cloneU, "Get_MarshalStructAsParam_AsExpByValU_Stdcall_FuncPtr"));
                break;

            case StructID.ByteStructPack2ExplicitId:
                ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                ByteStructPack2Explicit clone_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                DelegateStdcallByVal_ByteStructPack2Explicit caller_ByteStructPack2Explicit = Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Stdcall_FuncPtr...");
                Assert.True(caller_ByteStructPack2Explicit(source_bspe));
                Assert.True(Helper.ValidateByteStructPack2Explicit(source_bspe, clone_bspe, "Get_MarshalStructAsParam_AsExpByValByteStructPack2Explicit_Stdcall_FuncPtr"));
                break;

            case StructID.ShortStructPack4ExplicitId:
                ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                ShortStructPack4Explicit clone_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                DelegateStdcallByVal_ShortStructPack4Explicit caller_ShortStructPack4Explicit = Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Stdcall_FuncPtr...");
                Assert.True(caller_ShortStructPack4Explicit(source_sspe));
                Assert.True(Helper.ValidateShortStructPack4Explicit(source_sspe, clone_sspe, "Get_MarshalStructAsParam_AsExpByValShortStructPack4Explicit_Stdcall_FuncPtr"));
                break;

            case StructID.IntStructPack8ExplicitId:
                IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                IntStructPack8Explicit clone_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                DelegateStdcallByVal_IntStructPack8Explicit caller_IntStructPack8Explicit = Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Stdcall_FuncPtr...");
                Assert.True(caller_IntStructPack8Explicit(source_ispe));
                Assert.True(Helper.ValidateIntStructPack8Explicit(source_ispe, clone_ispe, "Get_MarshalStructAsParam_AsExpByValIntStructPack8Explicit_Stdcall_FuncPtr"));
                break;

            case StructID.LongStructPack16ExplicitId:
                LongStructPack16Explicit source_lspe = Helper.NewLongStructPack16Explicit(32, 32);
                LongStructPack16Explicit clone_lspe = Helper.NewLongStructPack16Explicit(32, 32);
                DelegateStdcallByVal_LongStructPack16Explicit caller_LongStructPack16Explicit = Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Stdcall_FuncPtr();
                Console.WriteLine("Calling Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Stdcall_FuncPtr...");
                Assert.True(caller_LongStructPack16Explicit(source_lspe));
                Assert.True(Helper.ValidateLongStructPack16Explicit(source_lspe, clone_lspe, "Get_MarshalStructAsParam_AsExpByValLongStructPack16Explicit_Stdcall_FuncPtr"));
                break;

            default:
                Assert.Fail("TestMethod_DelegatePInvoke_MarshalByRef_Stdcall:The structid (Managed Side) is wrong");
                break;
        }
    }

    #endregion

    #endregion

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try
        {
            #region calling method

            ////Delegate PInvoke,ByRef,Cdecl
            TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.INNER2Id);
            TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.InnerExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.InnerArrayExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.OUTER3Id);
            TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.UId);
            TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.ByteStructPack2ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.ShortStructPack4ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.IntStructPack8ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.LongStructPack16ExplicitId);

            ////Delegate PInvoke,ByRef,StdCall
            TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID.INNER2Id);
            TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID.InnerExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID.InnerArrayExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID.OUTER3Id);
            TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID.UId);
            TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID.ByteStructPack2ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID.ShortStructPack4ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID.IntStructPack8ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByRef_Stdcall(StructID.LongStructPack16ExplicitId);

            ////Delegate PInvoke,ByVal,Cdecl
            TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.INNER2Id);
            TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.InnerExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.InnerArrayExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.OUTER3Id);
            TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.UId);
            TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.ByteStructPack2ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.ShortStructPack4ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.IntStructPack8ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.LongStructPack16ExplicitId);

            ////Delegate PInvoke,ByVal,Stdcall
            TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID.INNER2Id);
            TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID.InnerExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID.InnerArrayExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID.OUTER3Id);
            TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID.UId);
            TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID.ByteStructPack2ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID.ShortStructPack4ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID.IntStructPack8ExplicitId);
            TestMethod_DelegatePInvoke_MarshalByVal_Stdcall(StructID.LongStructPack16ExplicitId);

            #endregion

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
