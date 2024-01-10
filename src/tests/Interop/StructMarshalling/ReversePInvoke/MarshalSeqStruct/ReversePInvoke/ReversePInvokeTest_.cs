// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using Xunit;

public class MarshalStructTest
{
    const int iNative = 11;//the value passed from Native side to Managed side
    const int iManaged = 10;//The value passed from Managed side to Native side

    private static string strOne;
    private static string strTwo;

    enum StructID
    {
        InnerSequentialId,
        InnerArraySequentialId,
        CharSetAnsiSequentialId,
        CharSetUnicodeSequentialId,
        NumberSequentialId,
        S3Id,
        S5Id,
        StringStructSequentialAnsiId,
        StringStructSequentialUnicodeId,
        S8Id,
        S9Id,
        IncludeOuterIntegerStructSequentialId,
        S11Id,
        ComplexStructId,
        ByteStruct3Byte
    }


    private static void testMethod(S9 s9)
    {
        Console.WriteLine("The first field of s9 is:", s9.i32);
    }

    #region Methods for the struct InnerSequential declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InnerSequentialByRefCdeclcaller([In, Out]ref InnerSequential argStr);
    private static bool TestMethodForStructInnerSequential_ReversePInvokeByRef_Cdecl(ref InnerSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        InnerSequential change_is = Helper.NewInnerSequential(77, 77.0F, "changed string");
        //Check the input
        Assert.True(Helper.ValidateInnerSequential(argstr, change_is, "TestMethodForStructInnerSequential_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.f1 = 1;
        argstr.f2 = 1.0F;
        argstr.f3 = "some string";
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool InnerSequentialByRefStdCallcaller([In, Out]ref InnerSequential argStr);
    private static bool TestMethodForStructInnerSequential_ReversePInvokeByRef_StdCall(ref InnerSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        InnerSequential change_is = Helper.NewInnerSequential(77, 77.0F, "changed string");
        //Check the input
        Assert.True(Helper.ValidateInnerSequential(argstr, change_is, "TestMethodForStructInnerSequential_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        argstr.f1 = 1;
        argstr.f2 = 1.0F;
        argstr.f3 = "some string";
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructInnerSequentialByRef_Cdecl(InnerSequentialByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructInnerSequentialByRef_StdCall(InnerSequentialByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InnerSequentialByValCdeclcaller([In, Out] InnerSequential argStr);
    private static bool TestMethodForStructInnerSequential_ReversePInvokeByVal_Cdecl(InnerSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        InnerSequential change_is = Helper.NewInnerSequential(77, 77.0F, "changed string");
        //Check the input
        Assert.True(Helper.ValidateInnerSequential(argstr, change_is, "TestMethodForStructInnerSequential_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.f1 = 1;
        argstr.f2 = 1.0F;
        argstr.f3 = "some string";
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool InnerSequentialByValStdCallcaller([In, Out] InnerSequential argStr);
    private static bool TestMethodForStructInnerSequential_ReversePInvokeByVal_StdCall(InnerSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        InnerSequential change_is = Helper.NewInnerSequential(77, 77.0F, "changed string");
        //Check the input
        Assert.True(Helper.ValidateInnerSequential(argstr, change_is, "TestMethodForStructInnerSequential_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.f1 = 1;
        argstr.f2 = 1.0F;
        argstr.f3 = "some string";
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructInnerSequentialByVal_Cdecl(InnerSequentialByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructInnerSequentialByVal_StdCall(InnerSequentialByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct InnerArraySequential declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InnerArraySequentialByRefCdeclcaller([In, Out]ref InnerArraySequential argStr);
    private static bool TestMethodForStructInnerArraySequential_ReversePInvokeByRef_Cdecl(ref InnerArraySequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        InnerArraySequential change_is = Helper.NewInnerArraySequential(77, 77.0F, "changed string");
        //Check the input
        Assert.True(Helper.ValidateInnerArraySequential(argstr, change_is, "TestMethodForStructInnerArraySequential_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            argstr.arr[i].f1 = 1;
            argstr.arr[i].f2 = 1.0F;
            argstr.arr[i].f3 = "some string";
        }
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool InnerArraySequentialByRefStdCallcaller([In, Out]ref InnerArraySequential argStr);
    private static bool TestMethodForStructInnerArraySequential_ReversePInvokeByRef_StdCall(ref InnerArraySequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        InnerArraySequential change_is = Helper.NewInnerArraySequential(77, 77.0F, "changed string");
        //Check the input
        Assert.True(Helper.ValidateInnerArraySequential(argstr, change_is, "TestMethodForStructInnerArraySequential_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            argstr.arr[i].f1 = 1;
            argstr.arr[i].f2 = 1.0F;
            argstr.arr[i].f3 = "some string";
        }
        return true;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructInnerArraySequentialByRef_Cdecl(InnerArraySequentialByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructInnerArraySequentialByRef_StdCall(InnerArraySequentialByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InnerArraySequentialByValCdeclcaller([In, Out] InnerArraySequential argStr);
    private static bool TestMethodForStructInnerArraySequential_ReversePInvokeByVal_Cdecl(InnerArraySequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        InnerArraySequential change_is = Helper.NewInnerArraySequential(77, 77.0F, "changed string");
        //Check the input
        Assert.True(Helper.ValidateInnerArraySequential(argstr, change_is, "TestMethodForStructInnerArraySequential_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            argstr.arr[i].f1 = 1;
            argstr.arr[i].f2 = 1.0F;
            argstr.arr[i].f3 = "some string";
        }
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool InnerArraySequentialByValStdCallcaller([In, Out] InnerArraySequential argStr);
    private static bool TestMethodForStructInnerArraySequential_ReversePInvokeByVal_StdCall(InnerArraySequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        InnerArraySequential change_is = Helper.NewInnerArraySequential(77, 77.0F, "changed string");
        //Check the input
        Assert.True(Helper.ValidateInnerArraySequential(argstr, change_is, "TestMethodForStructInnerArraySequential_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            argstr.arr[i].f1 = 1;
            argstr.arr[i].f2 = 1.0F;
            argstr.arr[i].f3 = "some string";
        }
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructInnerArraySequentialByVal_Cdecl(InnerArraySequentialByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructInnerArraySequentialByVal_StdCall(InnerArraySequentialByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct CharSetAnsiSequential declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CharSetAnsiSequentialByRefCdeclcaller([In, Out]ref CharSetAnsiSequential argStr);
    private static bool TestMethodForStructCharSetAnsiSequential_ReversePInvokeByRef_Cdecl(ref CharSetAnsiSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        CharSetAnsiSequential change_is = Helper.NewCharSetAnsiSequential("change string", 'n');
        //Check the input
        Assert.True(Helper.ValidateCharSetAnsiSequential(argstr, change_is, "TestMethodForStructCharSetAnsiSequential_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.f1 = "some string";
        argstr.f2 = 'c';
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool CharSetAnsiSequentialByRefStdCallcaller([In, Out]ref CharSetAnsiSequential argStr);
    private static bool TestMethodForStructCharSetAnsiSequential_ReversePInvokeByRef_StdCall(ref CharSetAnsiSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        CharSetAnsiSequential change_is = Helper.NewCharSetAnsiSequential("change string", 'n');
        //Check the input
        Assert.True(Helper.ValidateCharSetAnsiSequential(argstr, change_is, "TestMethodForStructCharSetAnsiSequential_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        argstr.f1 = "some string";
        argstr.f2 = 'c';
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructCharSetAnsiSequentialByRef_Cdecl(CharSetAnsiSequentialByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructCharSetAnsiSequentialByRef_StdCall(CharSetAnsiSequentialByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CharSetAnsiSequentialByValCdeclcaller([In, Out] CharSetAnsiSequential argStr);
    private static bool TestMethodForStructCharSetAnsiSequential_ReversePInvokeByVal_Cdecl(CharSetAnsiSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        CharSetAnsiSequential change_is = Helper.NewCharSetAnsiSequential("change string", 'n');
        //Check the input
        Assert.True(Helper.ValidateCharSetAnsiSequential(argstr, change_is, "TestMethodForStructCharSetAnsiSequential_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.f1 = "some string";
        argstr.f2 = 'c';
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool CharSetAnsiSequentialByValStdCallcaller([In, Out] CharSetAnsiSequential argStr);
    private static bool TestMethodForStructCharSetAnsiSequential_ReversePInvokeByVal_StdCall(CharSetAnsiSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        CharSetAnsiSequential change_is = Helper.NewCharSetAnsiSequential("change string", 'n');
        //Check the input
        Assert.True(Helper.ValidateCharSetAnsiSequential(argstr, change_is, "TestMethodForStructCharSetAnsiSequential_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.f1 = "some string";
        argstr.f2 = 'c';
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructCharSetAnsiSequentialByVal_Cdecl(CharSetAnsiSequentialByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructCharSetAnsiSequentialByVal_StdCall(CharSetAnsiSequentialByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct CharSetUnicodeSequential declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CharSetUnicodeSequentialByRefCdeclcaller([In, Out]ref CharSetUnicodeSequential argStr);
    private static bool TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByRef_Cdecl(ref CharSetUnicodeSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        CharSetUnicodeSequential change_is = Helper.NewCharSetUnicodeSequential("change string", 'n');
        //Check the input
        Assert.True(Helper.ValidateCharSetUnicodeSequential(argstr, change_is, "TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.f1 = "some string";
        argstr.f2 = 'c';
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool CharSetUnicodeSequentialByRefStdCallcaller([In, Out]ref CharSetUnicodeSequential argStr);
    private static bool TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByRef_StdCall(ref CharSetUnicodeSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        CharSetUnicodeSequential change_is = Helper.NewCharSetUnicodeSequential("change string", 'n');
        //Check the input
        Assert.True(Helper.ValidateCharSetUnicodeSequential(argstr, change_is, "TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        argstr.f1 = "some string";
        argstr.f2 = 'c';
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_Cdecl(CharSetUnicodeSequentialByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_StdCall(CharSetUnicodeSequentialByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CharSetUnicodeSequentialByValCdeclcaller([In, Out] CharSetUnicodeSequential argStr);
    private static bool TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByVal_Cdecl(CharSetUnicodeSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        CharSetUnicodeSequential change_is = Helper.NewCharSetUnicodeSequential("change string", 'n');
        //Check the input
        Assert.True(Helper.ValidateCharSetUnicodeSequential(argstr, change_is, "TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.f1 = "some string";
        argstr.f2 = 'c';
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool CharSetUnicodeSequentialByValStdCallcaller([In, Out] CharSetUnicodeSequential argStr);
    private static bool TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByVal_StdCall(CharSetUnicodeSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        CharSetUnicodeSequential change_is = Helper.NewCharSetUnicodeSequential("change string", 'n');
        //Check the input
        Assert.True(Helper.ValidateCharSetUnicodeSequential(argstr, change_is, "TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.f1 = "some string";
        argstr.f2 = 'c';
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_Cdecl(CharSetUnicodeSequentialByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_StdCall(CharSetUnicodeSequentialByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct NumberSequential declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool NumberSequentialByRefCdeclcaller([In, Out]ref NumberSequential argStr);
    private static bool TestMethodForStructNumberSequential_ReversePInvokeByRef_Cdecl(ref NumberSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        NumberSequential change_is = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);
        //Check the input
        Assert.True(Helper.ValidateNumberSequential(argstr, change_is, "TestMethodForStructNumberSequential_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.i32 = Int32.MinValue;
        argstr.ui32 = UInt32.MaxValue;
        argstr.s1 = short.MinValue;
        argstr.us1 = ushort.MaxValue;
        argstr.b = byte.MinValue;
        argstr.sb = sbyte.MaxValue;
        argstr.i16 = Int16.MinValue;
        argstr.ui16 = UInt16.MaxValue;
        argstr.i64 = -1234567890;
        argstr.ui64 = 1234567890;
        argstr.sgl = 32.0F;
        argstr.d = 3.2;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool NumberSequentialByRefStdCallcaller([In, Out]ref NumberSequential argStr);
    private static bool TestMethodForStructNumberSequential_ReversePInvokeByRef_StdCall(ref NumberSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        NumberSequential change_is = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);
        //Check the input
        Assert.True(Helper.ValidateNumberSequential(argstr, change_is, "TestMethodForStructNumberSequential_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        argstr.i32 = Int32.MinValue;
        argstr.ui32 = UInt32.MaxValue;
        argstr.s1 = short.MinValue;
        argstr.us1 = ushort.MaxValue;
        argstr.b = byte.MinValue;
        argstr.sb = sbyte.MaxValue;
        argstr.i16 = Int16.MinValue;
        argstr.ui16 = UInt16.MaxValue;
        argstr.i64 = -1234567890;
        argstr.ui64 = 1234567890;
        argstr.sgl = 32.0F;
        argstr.d = 3.2;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructNumberSequentialByRef_Cdecl(NumberSequentialByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructNumberSequentialByRef_StdCall(NumberSequentialByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NumberSequential NumberSequentialByValCdeclcaller([In, Out] NumberSequential argStr);
    private static NumberSequential TestMethodForStructNumberSequential_ReversePInvokeByVal_Cdecl(NumberSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        NumberSequential change_is = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);
        //Check the input
        Assert.True(Helper.ValidateNumberSequential(argstr, change_is, "TestMethodForStructNumberSequential_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.i32 = Int32.MinValue;
        argstr.ui32 = UInt32.MaxValue;
        argstr.s1 = short.MinValue;
        argstr.us1 = ushort.MaxValue;
        argstr.b = byte.MinValue;
        argstr.sb = sbyte.MaxValue;
        argstr.i16 = Int16.MinValue;
        argstr.ui16 = UInt16.MaxValue;
        argstr.i64 = -1234567890;
        argstr.ui64 = 1234567890;
        argstr.sgl = 32.0F;
        argstr.d = 3.2;
        return argstr;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate NumberSequential NumberSequentialByValStdCallcaller([In, Out] NumberSequential argStr);
    private static NumberSequential TestMethodForStructNumberSequential_ReversePInvokeByVal_StdCall(NumberSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        NumberSequential change_is = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);
        //Check the input
        Assert.True(Helper.ValidateNumberSequential(argstr, change_is, "TestMethodForStructNumberSequential_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.i32 = Int32.MinValue;
        argstr.ui32 = UInt32.MaxValue;
        argstr.s1 = short.MinValue;
        argstr.us1 = ushort.MaxValue;
        argstr.b = byte.MinValue;
        argstr.sb = sbyte.MaxValue;
        argstr.i16 = Int16.MinValue;
        argstr.ui16 = UInt16.MaxValue;
        argstr.i64 = -1234567890;
        argstr.ui64 = 1234567890;
        argstr.sgl = 32.0F;
        argstr.d = 3.2;
        return argstr;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructNumberSequentialByVal_Cdecl(NumberSequentialByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructNumberSequentialByVal_StdCall(NumberSequentialByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct S3 declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S3ByRefCdeclcaller([In, Out]ref S3 argStr);
    private static bool TestMethodForStructS3_ReversePInvokeByRef_Cdecl(ref S3 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        int[] iarr = new int[256];
        int[] icarr = new int[256];
        Helper.InitialArray(iarr, icarr);
        S3 changeS3 = Helper.NewS3(false, "change string", icarr);
        //Check the input
        Assert.True(Helper.ValidateS3(argstr, changeS3, "TestMethodForStructS3_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.flag = true;
        argstr.str = "some string";
        argstr.vals = iarr;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S3ByRefStdCallcaller([In, Out]ref S3 argStr);
    private static bool TestMethodForStructS3_ReversePInvokeByRef_StdCall(ref S3 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        int[] iarr = new int[256];
        int[] icarr = new int[256];
        Helper.InitialArray(iarr, icarr);
        S3 changeS3 = Helper.NewS3(false, "change string", icarr);
        //Check the input
        Assert.True(Helper.ValidateS3(argstr, changeS3, "TestMethodForStructS3_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.flag = true;
        argstr.str = "some string";
        argstr.vals = iarr;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS3ByRef_Cdecl(S3ByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS3ByRef_StdCall(S3ByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S3ByValCdeclcaller([In, Out] S3 argStr);
    private static bool TestMethodForStructS3_ReversePInvokeByVal_Cdecl(S3 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        int[] iarr = new int[256];
        int[] icarr = new int[256];
        Helper.InitialArray(iarr, icarr);
        S3 changeS3 = Helper.NewS3(false, "change string", icarr);
        //Check the input
        Assert.True(Helper.ValidateS3(argstr, changeS3, "TestMethodForStructS3_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.flag = true;
        argstr.str = "some string";
        argstr.vals = iarr;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S3ByValStdCallcaller([In, Out] S3 argStr);
    private static bool TestMethodForStructS3_ReversePInvokeByVal_StdCall(S3 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        int[] iarr = new int[256];
        int[] icarr = new int[256];
        Helper.InitialArray(iarr, icarr);
        S3 changeS3 = Helper.NewS3(false, "change string", icarr);
        //Check the input
        Assert.True(Helper.ValidateS3(argstr, changeS3, "TestMethodForStructS3_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.flag = true;
        argstr.str = "some string";
        argstr.vals = iarr;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS3ByVal_Cdecl(S3ByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS3ByVal_StdCall(S3ByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct S5 declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S5ByRefCdeclcaller([In, Out]ref S5 argStr);
    private static bool TestMethodForStructS5_ReversePInvokeByRef_Cdecl(ref S5 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        Enum1 enums = Enum1.e1;
        Enum1 enumch = Enum1.e2;
        S4 s4 = new S4();
        s4.age = 32;
        s4.name = "some string";
        S5 changeS5 = Helper.NewS5(64, "change string", enumch);
        //Check the input
        Assert.True(Helper.ValidateS5(argstr, changeS5, "TestMethodForStructS5_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.s4 = s4;
        argstr.ef = enums;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S5ByRefStdCallcaller([In, Out]ref S5 argStr);
    private static bool TestMethodForStructS5_ReversePInvokeByRef_StdCall(ref S5 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        Enum1 enums = Enum1.e1;
        Enum1 enumch = Enum1.e2;
        S4 s4 = new S4();
        s4.age = 32;
        s4.name = "some string";
        S5 changeS5 = Helper.NewS5(64, "change string", enumch);
        //Check the input
        Assert.True(Helper.ValidateS5(argstr, changeS5, "TestMethodForStructS5_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        argstr.s4 = s4;
        argstr.ef = enums;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS5ByRef_Cdecl(S5ByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS5ByRef_StdCall(S5ByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S5ByValCdeclcaller([In, Out] S5 argStr);
    private static bool TestMethodForStructS5_ReversePInvokeByVal_Cdecl(S5 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        Enum1 enums = Enum1.e1;
        Enum1 enumch = Enum1.e2;
        S4 s4 = new S4();
        s4.age = 32;
        s4.name = "some string";
        S5 changeS5 = Helper.NewS5(64, "change string", enumch);
        //Check the input
        Assert.True(Helper.ValidateS5(argstr, changeS5, "TestMethodForStructS5_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.s4 = s4;
        argstr.ef = enums;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S5ByValStdCallcaller([In, Out] S5 argStr);
    private static bool TestMethodForStructS5_ReversePInvokeByVal_StdCall(S5 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        Enum1 enums = Enum1.e1;
        Enum1 enumch = Enum1.e2;
        S4 s4 = new S4();
        s4.age = 32;
        s4.name = "some string";
        S5 changeS5 = Helper.NewS5(64, "change string", enumch);
        //Check the input
        Assert.True(Helper.ValidateS5(argstr, changeS5, "TestMethodForStructS5_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.s4 = s4;
        argstr.ef = enums;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS5ByVal_Cdecl(S5ByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS5ByVal_StdCall(S5ByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct StringStructSequentialAnsi declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool StringStructSequentialAnsiByRefCdeclcaller([In, Out]ref StringStructSequentialAnsi argStr);
    private static bool TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByRef_Cdecl(ref StringStructSequentialAnsi argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        strOne = new String('a', 512);
        strTwo = new String('b', 512);
        StringStructSequentialAnsi change_sssa = Helper.NewStringStructSequentialAnsi(strTwo, strOne);
        //Check the input
        Assert.True(Helper.ValidateStringStructSequentialAnsi(argstr, change_sssa, "TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.first = strOne;
        argstr.last = strTwo;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool StringStructSequentialAnsiByRefStdCallcaller([In, Out]ref StringStructSequentialAnsi argStr);
    private static bool TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByRef_StdCall(ref StringStructSequentialAnsi argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        strOne = new String('a', 512);
        strTwo = new String('b', 512);
        StringStructSequentialAnsi change_sssa = Helper.NewStringStructSequentialAnsi(strTwo, strOne);
        //Check the input
        Assert.True(Helper.ValidateStringStructSequentialAnsi(argstr, change_sssa, "TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        argstr.first = strOne;
        argstr.last = strTwo;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructStringStructSequentialAnsiByRef_Cdecl(StringStructSequentialAnsiByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructStringStructSequentialAnsiByRef_StdCall(StringStructSequentialAnsiByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool StringStructSequentialAnsiByValCdeclcaller([In, Out] StringStructSequentialAnsi argStr);
    private static bool TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByVal_Cdecl(StringStructSequentialAnsi argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        strOne = new String('a', 512);
        strTwo = new String('b', 512);
        StringStructSequentialAnsi change_sssa = Helper.NewStringStructSequentialAnsi(strTwo, strOne);
        //Check the input
        Assert.True(Helper.ValidateStringStructSequentialAnsi(argstr, change_sssa, "TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.first = strOne;
        argstr.last = strTwo;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool StringStructSequentialAnsiByValStdCallcaller([In, Out] StringStructSequentialAnsi argStr);
    private static bool TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByVal_StdCall(StringStructSequentialAnsi argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        strOne = new String('a', 512);
        strTwo = new String('b', 512);
        StringStructSequentialAnsi change_sssa = Helper.NewStringStructSequentialAnsi(strTwo, strOne);
        //Check the input
        Assert.True(Helper.ValidateStringStructSequentialAnsi(argstr, change_sssa, "TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.first = strOne;
        argstr.last = strTwo;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructStringStructSequentialAnsiByVal_Cdecl(StringStructSequentialAnsiByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructStringStructSequentialAnsiByVal_StdCall(StringStructSequentialAnsiByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct StringStructSequentialUnicode declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool StringStructSequentialUnicodeByRefCdeclcaller([In, Out]ref StringStructSequentialUnicode argStr);
    private static bool TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByRef_Cdecl(ref StringStructSequentialUnicode argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        strOne = new String('a', 256);
        strTwo = new String('b', 256);
        StringStructSequentialUnicode change_sssa = Helper.NewStringStructSequentialUnicode(strTwo, strOne);
        //Check the input
        Assert.True(Helper.ValidateStringStructSequentialUnicode(argstr, change_sssa, "TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.first = strOne;
        argstr.last = strTwo;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool StringStructSequentialUnicodeByRefStdCallcaller([In, Out]ref StringStructSequentialUnicode argStr);
    private static bool TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByRef_StdCall(ref StringStructSequentialUnicode argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        strOne = new String('a', 256);
        strTwo = new String('b', 256);
        StringStructSequentialUnicode change_sssa = Helper.NewStringStructSequentialUnicode(strTwo, strOne);
        //Check the input
        Assert.True(Helper.ValidateStringStructSequentialUnicode(argstr, change_sssa, "TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        argstr.first = strOne;
        argstr.last = strTwo;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_Cdecl(StringStructSequentialUnicodeByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_StdCall(StringStructSequentialUnicodeByRefStdCallcaller caller);

    #endregion

    #region PassByValue
    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool StringStructSequentialUnicodeByValCdeclcaller([In, Out] StringStructSequentialUnicode argStr);
    private static bool TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByVal_Cdecl(StringStructSequentialUnicode argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        strOne = new String('a', 256);
        strTwo = new String('b', 256);
        StringStructSequentialUnicode change_sssa = Helper.NewStringStructSequentialUnicode(strTwo, strOne);
        //Check the input
        Assert.True(Helper.ValidateStringStructSequentialUnicode(argstr, change_sssa, "TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.first = strOne;
        argstr.last = strTwo;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool StringStructSequentialUnicodeByValStdCallcaller([In, Out] StringStructSequentialUnicode argStr);
    private static bool TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByVal_StdCall(StringStructSequentialUnicode argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        strOne = new String('a', 256);
        strTwo = new String('b', 256);
        StringStructSequentialUnicode change_sssa = Helper.NewStringStructSequentialUnicode(strTwo, strOne);
        //Check the input
        Assert.True(Helper.ValidateStringStructSequentialUnicode(argstr, change_sssa, "TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.first = strOne;
        argstr.last = strTwo;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructStringStructSequentialUnicodeByVal_Cdecl(StringStructSequentialUnicodeByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructStringStructSequentialUnicodeByVal_StdCall(StringStructSequentialUnicodeByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct S8 declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S8ByRefCdeclcaller([In, Out]ref S8 argStr);
    private static bool TestMethodForStructS8_ReversePInvokeByRef_Cdecl(ref S8 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        S8 changeS8 = Helper.NewS8("world", false, 1, 256, 256, 64);
        //Check the input
        Assert.True(Helper.ValidateS8(argstr, changeS8, "TestMethodForStructS8_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.name = "hello";
        argstr.gender = true;
        argstr.jobNum = 10;
        argstr.i32 = 128;
        argstr.ui32 = 128;
        argstr.mySByte = 32;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S8ByRefStdCallcaller([In, Out]ref S8 argStr);
    private static bool TestMethodForStructS8_ReversePInvokeByRef_StdCall(ref S8 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        S8 changeS8 = Helper.NewS8("world", false, 1, 256, 256, 64);
        //Check the input
        Assert.True(Helper.ValidateS8(argstr, changeS8, "TestMethodForStructS8_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.name = "hello";
        argstr.gender = true;
        argstr.jobNum = 10;
        argstr.i32 = 128;
        argstr.ui32 = 128;
        argstr.mySByte = 32;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS8ByRef_Cdecl(S8ByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS8ByRef_StdCall(S8ByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S8ByValCdeclcaller([In, Out] S8 argStr);
    private static bool TestMethodForStructS8_ReversePInvokeByVal_Cdecl(S8 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        S8 changeS8 = Helper.NewS8("world", false, 1, 256, 256, 64);
        //Check the input
        Assert.True(Helper.ValidateS8(argstr, changeS8, "TestMethodForStructS8_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.name = "hello";
        argstr.gender = true;
        argstr.jobNum = 10;
        argstr.i32 = 128;
        argstr.ui32 = 128;
        argstr.mySByte = 32;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S8ByValStdCallcaller([In, Out] S8 argStr);
    private static bool TestMethodForStructS8_ReversePInvokeByVal_StdCall(S8 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        S8 changeS8 = Helper.NewS8("world", false, 1, 256, 256, 64);
        //Check the input
        Assert.True(Helper.ValidateS8(argstr, changeS8, "TestMethodForStructS8_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.name = "hello";
        argstr.gender = true;
        argstr.jobNum = 10;
        argstr.i32 = 128;
        argstr.ui32 = 128;
        argstr.mySByte = 32;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS8ByVal_Cdecl(S8ByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS8ByVal_StdCall(S8ByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct S9 declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S9ByRefCdeclcaller([In, Out]ref S9 argStr);
    private static bool TestMethodForStructS9_ReversePInvokeByRef_Cdecl(ref S9 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        S9 changeS9 = Helper.NewS9(256, null);
        //Check the input
        Assert.True(Helper.ValidateS9(argstr, changeS9, "TestMethodForStructS9_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.i32 = 128;
        argstr.myDelegate1 = new TestDelegate1(testMethod);
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S9ByRefStdCallcaller([In, Out]ref S9 argStr);
    private static bool TestMethodForStructS9_ReversePInvokeByRef_StdCall(ref S9 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        S9 changeS9 = Helper.NewS9(256, null);
        //Check the input
        Assert.True(Helper.ValidateS9(argstr, changeS9, "TestMethodForStructS9_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        argstr.i32 = 128;
        argstr.myDelegate1 = new TestDelegate1(testMethod);
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS9ByRef_Cdecl(S9ByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS9ByRef_StdCall(S9ByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S9ByValCdeclcaller([In, Out] S9 argStr);
    private static bool TestMethodForStructS9_ReversePInvokeByVal_Cdecl(S9 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        S9 changeS9 = Helper.NewS9(256, null);
        //Check the input
        Assert.True(Helper.ValidateS9(argstr, changeS9, "TestMethodForStructS9_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.i32 = 128;
        argstr.myDelegate1 = new TestDelegate1(testMethod);
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S9ByValStdCallcaller([In, Out] S9 argStr);
    private static bool TestMethodForStructS9_ReversePInvokeByVal_StdCall(S9 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        S9 changeS9 = Helper.NewS9(256, null);
        //Check the input
        Assert.True(Helper.ValidateS9(argstr, changeS9, "TestMethodForStructS9_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.i32 = 128;
        argstr.myDelegate1 = new TestDelegate1(testMethod);
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS9ByVal_Cdecl(S9ByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS9ByVal_StdCall(S9ByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct IncludeOuterIntegerStructSequential declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool IncludeOuterIntegerStructSequentialByRefCdeclcaller([In, Out]ref IncludeOuterIntegerStructSequential argStr);
    private static bool TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByRef_Cdecl(ref IncludeOuterIntegerStructSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        IncludeOuterIntegerStructSequential changeIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(64, 64);
        //Check the input
        Assert.True(Helper.ValidateIncludeOuterIntegerStructSequential(argstr,
            changeIncludeOuterIntegerStructSequential, "TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.s.s_int.i = 32;
        argstr.s.i = 32;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool IncludeOuterIntegerStructSequentialByRefStdCallcaller([In, Out]ref IncludeOuterIntegerStructSequential argStr);
    private static bool TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByRef_StdCall(ref IncludeOuterIntegerStructSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        IncludeOuterIntegerStructSequential changeIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(64, 64);
        //Check the input
        Assert.True(Helper.ValidateIncludeOuterIntegerStructSequential(argstr,
            changeIncludeOuterIntegerStructSequential, "TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.s.s_int.i = 32;
        argstr.s.i = 32;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl(IncludeOuterIntegerStructSequentialByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall(IncludeOuterIntegerStructSequentialByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IncludeOuterIntegerStructSequential IncludeOuterIntegerStructSequentialByValCdeclcaller([In, Out] IncludeOuterIntegerStructSequential argStr);
    private static IncludeOuterIntegerStructSequential TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByVal_Cdecl(IncludeOuterIntegerStructSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        IncludeOuterIntegerStructSequential changeIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(64, 64);
        //Check the input
        Assert.True(Helper.ValidateIncludeOuterIntegerStructSequential(argstr,
            changeIncludeOuterIntegerStructSequential, "TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.s.s_int.i = 32;
        argstr.s.i = 32;
        return argstr;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IncludeOuterIntegerStructSequential IncludeOuterIntegerStructSequentialByValStdCallcaller([In, Out] IncludeOuterIntegerStructSequential argStr);
    private static IncludeOuterIntegerStructSequential TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByVal_StdCall(IncludeOuterIntegerStructSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        IncludeOuterIntegerStructSequential changeIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(64, 64);
        //Check the input
        Assert.True(Helper.ValidateIncludeOuterIntegerStructSequential(argstr,
            changeIncludeOuterIntegerStructSequential, "TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.s.s_int.i = 32;
        argstr.s.i = 32;
        return argstr;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl(IncludeOuterIntegerStructSequentialByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall(IncludeOuterIntegerStructSequentialByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct S11 declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S11ByRefCdeclcaller([In, Out]ref S11 argStr);
    unsafe private static bool TestMethodForStructS11_ReversePInvokeByRef_Cdecl(ref S11 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
        S11 changeS11 = Helper.NewS11((int*)(64), 64);
        //Check the input
        Assert.True(Helper.ValidateS11(argstr, changeS11, "TestMethodForStructS11_ReversePInvokeByRef_Cdecl"));
        //Chanage the value
        argstr.i32 = (int*)(32);
        argstr.i = 32;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S11ByRefStdCallcaller([In, Out]ref S11 argStr);
    unsafe private static bool TestMethodForStructS11_ReversePInvokeByRef_StdCall(ref S11 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        S11 changeS11 = Helper.NewS11((int*)(64), 64);
        //Check the input
        Assert.True(Helper.ValidateS11(argstr, changeS11, "TestMethodForStructS11_ReversePInvokeByRef_StdCall"));
        //Chanage the value
        argstr.i32 = (int*)(32);
        argstr.i = 32;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS11ByRef_Cdecl(S11ByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS11ByRef_StdCall(S11ByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S11ByValCdeclcaller([In, Out] S11 argStr);
    unsafe private static bool TestMethodForStructS11_ReversePInvokeByVal_Cdecl(S11 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        S11 changeS11 = Helper.NewS11((int*)(64), 64);
        //Check the input
        Assert.True(Helper.ValidateS11(argstr, changeS11, "TestMethodForStructS11_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.i32 = (int*)(32);
        argstr.i = 32;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S11ByValStdCallcaller([In, Out] S11 argStr);
    unsafe private static bool TestMethodForStructS11_ReversePInvokeByVal_StdCall(S11 argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        S11 changeS11 = Helper.NewS11((int*)(64), 64);
        //Check the input
        Assert.True(Helper.ValidateS11(argstr, changeS11, "TestMethodForStructS11_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.i32 = (int*)(32);
        argstr.i = 32;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructS11ByVal_Cdecl(S11ByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructS11ByVal_StdCall(S11ByValStdCallcaller caller);

    #endregion

    #endregion

    #region Methods for the struct ComplexStruct declaration

    #region PassByRef

    //For Reverse Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool ComplexStructByRefCdeclcaller([In, Out]ref ComplexStruct argStr);
    private static bool TestMethodForStructComplexStruct_ReversePInvokeByRef_Cdecl(ref ComplexStruct cs)
    {
        Console.WriteLine("ReversePinvoke,By Ref,Cdecl");
            //Check the input
            Assert.Equal(9999, cs.i);
            Assert.False(cs.b);
            Assert.Equal("Native", cs.str);
            Assert.Equal(-1, cs.type.idata);
            Assert.Equal(3.14159, cs.type.ddata);
            //Chanage the value
            cs.i = 321;
            cs.b = true;
            cs.str = "Managed";
            cs.type.idata = 123;
            cs.type.ptrdata = (IntPtr)0x120000;
            return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool ComplexStructByRefStdCallcaller([In, Out]ref ComplexStruct argStr);
    private static bool TestMethodForStructComplexStruct_ReversePInvokeByRef_StdCall(ref ComplexStruct cs)
    {
        Console.WriteLine("ReversePinvoke,By Ref,StdCall");
        //Check the input
        Assert.Equal(9999, cs.i);
        Assert.False(cs.b);
        Assert.Equal("Native", cs.str);
        Assert.Equal(-1, cs.type.idata);
        Assert.Equal(3.14159, cs.type.ddata);
        //Chanage the value
        cs.i = 321;
        cs.b = true;
        cs.str = "Managed";
        cs.type.idata = 123;
        cs.type.ptrdata = (IntPtr)0x120000;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructComplexStructByRef_Cdecl(ComplexStructByRefCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructComplexStructByRef_StdCall(ComplexStructByRefStdCallcaller caller);

    #endregion

    #region PassByValue

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool ComplexStructByValCdeclcaller([In, Out] ComplexStruct argStr);
    private static bool TestMethodForStructComplexStruct_ReversePInvokeByVal_Cdecl(ComplexStruct cs)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        //Check the input
        Assert.Equal(9999, cs.i);
        Assert.False(cs.b);
        Assert.Equal("Native", cs.str);
        Assert.Equal(-1, cs.type.idata);
        Assert.Equal(3.14159, cs.type.ddata);
        //Try to Chanage the value
        cs.i = 321;
        cs.b = true;
        cs.str = "Managed";
        cs.type.idata = 123;
        cs.type.ptrdata = (IntPtr)0x120000;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool ComplexStructByValStdCallcaller([In, Out] ComplexStruct argStr);
    private static bool TestMethodForStructComplexStruct_ReversePInvokeByVal_StdCall(ComplexStruct cs)
    {
        Console.WriteLine("Reverse Pinvoke,By Value,StdCall");
        //Check the input
        Assert.Equal(9999, cs.i);
        Assert.False(cs.b);
        Assert.Equal("Native", cs.str);
        Assert.Equal(-1, cs.type.idata);
        Assert.Equal(3.14159, cs.type.ddata);
        //Try to Chanage the value
        cs.i = 321;
        cs.b = true;
        cs.str = "Managed";
        cs.type.idata = 123;
        cs.type.ptrdata = (IntPtr)0x120000;
        return true;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructComplexStructByVal_Cdecl(ComplexStructByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructComplexStructByVal_StdCall(ComplexStructByValStdCallcaller caller);

    #endregion

    #endregion
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate ByteStruct3Byte ByValCdeclcaller_ByteStruct3Byte(ByteStruct3Byte bspe, [MarshalAs(UnmanagedType.Bool)] out bool success);

    public static ByteStruct3Byte TestMethod_DoCallBack_MarshalStructByVal_ByteStruct3Byte_Cdecl(ByteStruct3Byte bspe, out bool success)
    {
        Console.WriteLine("Reverse,Pinvoke,By Val,Cdecl");
        ByteStruct3Byte change_bspe = Helper.NewByteStruct3Byte(1, 42, 90);
        Assert.True(Helper.ValidateByteStruct3Byte(change_bspe, bspe, "TestMethod_DoCallBack_MarshalStructByVal_ByteStruct3Byte_Cdecl"));
        //changed the value
        bspe.b1 = 7;
        bspe.b2 = 12;
        bspe.b3 = 18;
        success = true;
        return bspe;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate ByteStruct3Byte ByValStdCallcaller_ByteStruct3Byte(ByteStruct3Byte bspe, [MarshalAs(UnmanagedType.Bool)] out bool success);

    public static ByteStruct3Byte TestMethod_DoCallBack_MarshalStructByVal_ByteStruct3Byte_StdCall(ByteStruct3Byte bspe, out bool success)
    {
        Console.WriteLine("Reverse,Pinvoke,By Val,StdCall");
        ByteStruct3Byte change_bspe = Helper.NewByteStruct3Byte(1, 42, 90);
        Assert.True(Helper.ValidateByteStruct3Byte(change_bspe, bspe, "TestMethod_DoCallBack_MarshalStructByVal_ByteStruct3Byte_StdCall"));
        //changed the value
        bspe.b1 = 7;
        bspe.b2 = 12;
        bspe.b3 = 18;
        success = true;
        return bspe;
    }


    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructByVal_StdCall_ByteStruct3Byte(ByValStdCallcaller_ByteStruct3Byte caller);

    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructByVal_Cdecl_ByteStruct3Byte(ByValCdeclcaller_ByteStruct3Byte caller);

    //For Reverse Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntegerStructSequential IntegerStructSequentialByValCdeclcaller([In, Out] IntegerStructSequential argStr);
    private static IntegerStructSequential TestMethodForStructIntegerStructSequential_ReversePInvokeByVal_Cdecl(IntegerStructSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,Cdecl");
        IntegerStructSequential changeIntegerStructSequential = Helper.NewIntegerStructSequential(64);
        //Check the input
        Assert.True(Helper.ValidateIntegerStructSequential(argstr,
            changeIntegerStructSequential, "TestMethodForStructIntegerStructSequential_ReversePInvokeByVal_Cdecl"));
        //Chanage the value
        argstr.i = 32;
        return argstr;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntegerStructSequential IntegerStructSequentialByValStdCallcaller([In, Out] IntegerStructSequential argStr);
    private static IntegerStructSequential TestMethodForStructIntegerStructSequential_ReversePInvokeByVal_StdCall(IntegerStructSequential argstr)
    {
        Console.WriteLine("ReversePinvoke,By Value,StdCall");
        IntegerStructSequential changeIntegerStructSequential = Helper.NewIntegerStructSequential(64);
        //Check the input
        Assert.True(Helper.ValidateIntegerStructSequential(argstr,
            changeIntegerStructSequential, "TestMethodForStructIntegerStructSequential_ReversePInvokeByVal_StdCall"));
        //Chanage the value
        argstr.i = 32;
        return argstr;
    }

    //Reverse Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalStructIntegerStructSequentialByVal_Cdecl(IntegerStructSequentialByValCdeclcaller caller);
    //Reverse Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalStructIntegerStructSequentialByVal_StdCall(IntegerStructSequentialByValStdCallcaller caller);

    #region Methods implementation

    //Reverse P/Invoke By Ref
    private static void TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID structid)
    {
        switch (structid)
        {
            case StructID.ComplexStructId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructComplexStructByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructComplexStructByRef_Cdecl(new ComplexStructByRefCdeclcaller(TestMethodForStructComplexStruct_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.InnerSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructInnerSequentialByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructInnerSequentialByRef_Cdecl(new InnerSequentialByRefCdeclcaller(TestMethodForStructInnerSequential_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.InnerArraySequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructInnerArraySequentialByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructInnerArraySequentialByRef_Cdecl(
                    new InnerArraySequentialByRefCdeclcaller(TestMethodForStructInnerArraySequential_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.CharSetAnsiSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructCharSetAnsiSequentialByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructCharSetAnsiSequentialByRef_Cdecl(
                    new CharSetAnsiSequentialByRefCdeclcaller(TestMethodForStructCharSetAnsiSequential_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.CharSetUnicodeSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructCharSetUnicodeSequentialByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_Cdecl(
                    new CharSetUnicodeSequentialByRefCdeclcaller(TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.NumberSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructNumberSequentialByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructNumberSequentialByRef_Cdecl(new NumberSequentialByRefCdeclcaller(TestMethodForStructNumberSequential_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.S3Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS3ByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS3ByRef_Cdecl(new S3ByRefCdeclcaller(TestMethodForStructS3_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.S5Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS5ByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS5ByRef_Cdecl(new S5ByRefCdeclcaller(TestMethodForStructS5_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.StringStructSequentialAnsiId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructStringStructSequentialAnsiByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructStringStructSequentialAnsiByRef_Cdecl(
                    new StringStructSequentialAnsiByRefCdeclcaller(TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.StringStructSequentialUnicodeId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructStringStructSequentialUnicodeByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_Cdecl(
                    new StringStructSequentialUnicodeByRefCdeclcaller(TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.S8Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS8ByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS8ByRef_Cdecl(new S8ByRefCdeclcaller(TestMethodForStructS8_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.S9Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS9ByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS9ByRef_Cdecl(new S9ByRefCdeclcaller(TestMethodForStructS9_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.IncludeOuterIntegerStructSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl(
                    new IncludeOuterIntegerStructSequentialByRefCdeclcaller(TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByRef_Cdecl)));
                break;
            case StructID.S11Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS11ByRef_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS11ByRef_Cdecl(new S11ByRefCdeclcaller(TestMethodForStructS11_ReversePInvokeByRef_Cdecl)));
                break;
            default:
                Assert.Fail("DoCallBack_MarshalStructByRef_Cdecl:The structid (Managed Side) is wrong");
                break;
        }
    }

    private static void TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID structid)
    {
        switch (structid)
        {
            case StructID.ComplexStructId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructComplexStructByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructComplexStructByRef_StdCall(new ComplexStructByRefStdCallcaller(TestMethodForStructComplexStruct_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.InnerSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructInnerSequentialByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructInnerSequentialByRef_StdCall(new InnerSequentialByRefStdCallcaller(TestMethodForStructInnerSequential_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.InnerArraySequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructInnerArraySequentialByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructInnerArraySequentialByRef_StdCall(
                    new InnerArraySequentialByRefStdCallcaller(TestMethodForStructInnerArraySequential_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.CharSetAnsiSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructCharSetAnsiSequentialByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructCharSetAnsiSequentialByRef_StdCall(
                    new CharSetAnsiSequentialByRefStdCallcaller(TestMethodForStructCharSetAnsiSequential_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.CharSetUnicodeSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructCharSetUnicodeSequentialByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructCharSetUnicodeSequentialByRef_StdCall(
                    new CharSetUnicodeSequentialByRefStdCallcaller(TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.NumberSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructNumberSequentialByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructNumberSequentialByRef_StdCall(new NumberSequentialByRefStdCallcaller(TestMethodForStructNumberSequential_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.S3Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS3ByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructS3ByRef_StdCall(new S3ByRefStdCallcaller(TestMethodForStructS3_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.S5Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS5ByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructS5ByRef_StdCall(new S5ByRefStdCallcaller(TestMethodForStructS5_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.StringStructSequentialAnsiId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructStringStructSequentialAnsiByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructStringStructSequentialAnsiByRef_StdCall(
                    new StringStructSequentialAnsiByRefStdCallcaller(TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.StringStructSequentialUnicodeId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructStringStructSequentialUnicodeByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructStringStructSequentialUnicodeByRef_StdCall(
                    new StringStructSequentialUnicodeByRefStdCallcaller(TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.S8Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS8ByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructS8ByRef_StdCall(new S8ByRefStdCallcaller(TestMethodForStructS8_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.S9Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS9ByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructS9ByRef_StdCall(new S9ByRefStdCallcaller(TestMethodForStructS9_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.IncludeOuterIntegerStructSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall(
                    new IncludeOuterIntegerStructSequentialByRefStdCallcaller(TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByRef_StdCall)));
                break;
            case StructID.S11Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS11ByRef_StdCall...");
                Assert.True(DoCallBack_MarshalStructS11ByRef_StdCall(new S11ByRefStdCallcaller(TestMethodForStructS11_ReversePInvokeByRef_StdCall)));
                break;
            default:
                Assert.Fail("DoCallBack_MarshalStructByRef_StdCall:The structid (Managed Side) is wrong");
                break;
        }
    }

    private static void Run_TestMethod_DoCallBack_MarshalStructByRef_Cdecl()
    {
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.ComplexStructId);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.InnerSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.InnerArraySequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.CharSetAnsiSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.CharSetUnicodeSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.NumberSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.S3Id);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.S5Id);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.StringStructSequentialAnsiId);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.StringStructSequentialUnicodeId);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.S8Id);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.S9Id);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.IncludeOuterIntegerStructSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_Cdecl(StructID.S11Id);
    }

    private static void Run_TestMethod_DoCallBack_MarshalStructByRef_StdCall()
    {
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.ComplexStructId);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.InnerSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.InnerArraySequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.CharSetAnsiSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.CharSetUnicodeSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.NumberSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.S3Id);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.S5Id);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.StringStructSequentialAnsiId);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.StringStructSequentialUnicodeId);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.S8Id);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.S9Id);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.IncludeOuterIntegerStructSequentialId);
        TestMethod_DoCallBack_MarshalStructByRef_StdCall(StructID.S11Id);
    }

    //Reverse P/Invoke By Value
    private static void TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID structid)
    {
        switch (structid)
        {
            case StructID.ComplexStructId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructComplexeStructByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructComplexStructByVal_Cdecl(new ComplexStructByValCdeclcaller(TestMethodForStructComplexStruct_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.InnerSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructInnerSequentialByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructInnerSequentialByVal_Cdecl(new InnerSequentialByValCdeclcaller(TestMethodForStructInnerSequential_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.InnerArraySequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructInnerArraySequentialByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructInnerArraySequentialByVal_Cdecl(
                    new InnerArraySequentialByValCdeclcaller(TestMethodForStructInnerArraySequential_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.CharSetAnsiSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructCharSetAnsiSequentialByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructCharSetAnsiSequentialByVal_Cdecl
                    (new CharSetAnsiSequentialByValCdeclcaller(TestMethodForStructCharSetAnsiSequential_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.CharSetUnicodeSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructCharSetUnicodeSequentialByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_Cdecl(
                    new CharSetUnicodeSequentialByValCdeclcaller(TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.NumberSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructNumberSequentialByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructNumberSequentialByVal_Cdecl(new NumberSequentialByValCdeclcaller(TestMethodForStructNumberSequential_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.S3Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS3ByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS3ByVal_Cdecl(new S3ByValCdeclcaller(TestMethodForStructS3_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.S5Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS5ByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS5ByVal_Cdecl(new S5ByValCdeclcaller(TestMethodForStructS5_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.StringStructSequentialAnsiId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructStringStructSequentialAnsiByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructStringStructSequentialAnsiByVal_Cdecl(
                    new StringStructSequentialAnsiByValCdeclcaller(TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.StringStructSequentialUnicodeId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructStringStructSequentialUnicodeByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructStringStructSequentialUnicodeByVal_Cdecl(
                    new StringStructSequentialUnicodeByValCdeclcaller(TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.S8Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS8ByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS8ByVal_Cdecl(new S8ByValCdeclcaller(TestMethodForStructS8_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.S9Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS9ByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS9ByVal_Cdecl(new S9ByValCdeclcaller(TestMethodForStructS9_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.IncludeOuterIntegerStructSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl(
                    new IncludeOuterIntegerStructSequentialByValCdeclcaller(TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.S11Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS11ByVal_Cdecl...");
                Assert.True(DoCallBack_MarshalStructS11ByVal_Cdecl(new S11ByValCdeclcaller(TestMethodForStructS11_ReversePInvokeByVal_Cdecl)));
                break;
            case StructID.ByteStruct3Byte:
                Console.WriteLine("Calling DoCallBack_MarshalStructByVal_Cdecl_ByteStruct3Byte...");
                Assert.True(DoCallBack_MarshalStructByVal_Cdecl_ByteStruct3Byte(
                    new ByValCdeclcaller_ByteStruct3Byte(TestMethod_DoCallBack_MarshalStructByVal_ByteStruct3Byte_Cdecl)));
                break;
            default:
                Assert.Fail("DoCallBack_MarshalStructByVal_Cdecl:The structid (Managed Side) is wrong");
                break;
        }
    }

    private static void TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID structid)
    {
        switch (structid)
        {
            case StructID.ComplexStructId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructComplexStructByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructComplexStructByVal_StdCall(new ComplexStructByValStdCallcaller(TestMethodForStructComplexStruct_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.InnerSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructInnerSequentialByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructInnerSequentialByVal_StdCall(new InnerSequentialByValStdCallcaller(TestMethodForStructInnerSequential_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.InnerArraySequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructInnerArraySequentialByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructInnerArraySequentialByVal_StdCall(
                    new InnerArraySequentialByValStdCallcaller(TestMethodForStructInnerArraySequential_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.CharSetAnsiSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructCharSetAnsiSequentialByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructCharSetAnsiSequentialByVal_StdCall(
                    new CharSetAnsiSequentialByValStdCallcaller(TestMethodForStructCharSetAnsiSequential_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.CharSetUnicodeSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructCharSetUnicodeSequentialByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructCharSetUnicodeSequentialByVal_StdCall(
                    new CharSetUnicodeSequentialByValStdCallcaller(TestMethodForStructCharSetUnicodeSequential_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.NumberSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructNumberSequentialByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructNumberSequentialByVal_StdCall(new NumberSequentialByValStdCallcaller(TestMethodForStructNumberSequential_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.S3Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS3ByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructS3ByVal_StdCall(new S3ByValStdCallcaller(TestMethodForStructS3_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.S5Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS5ByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructS5ByVal_StdCall(new S5ByValStdCallcaller(TestMethodForStructS5_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.StringStructSequentialAnsiId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructStringStructSequentialAnsiByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructStringStructSequentialAnsiByVal_StdCall(
                    new StringStructSequentialAnsiByValStdCallcaller(TestMethodForStructStringStructSequentialAnsi_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.StringStructSequentialUnicodeId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructStringStructSequentialUnicodeByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructStringStructSequentialUnicodeByVal_StdCall(
                    new StringStructSequentialUnicodeByValStdCallcaller(TestMethodForStructStringStructSequentialUnicode_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.S8Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS8ByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructS8ByVal_StdCall(new S8ByValStdCallcaller(TestMethodForStructS8_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.S9Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS9ByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructS9ByVal_StdCall(new S9ByValStdCallcaller(TestMethodForStructS9_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.IncludeOuterIntegerStructSequentialId:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall(
                    new IncludeOuterIntegerStructSequentialByValStdCallcaller(TestMethodForStructIncludeOuterIntegerStructSequential_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.S11Id:
                Console.WriteLine("Calling ReversePInvoke_MarshalStructS11ByVal_StdCall...");
                Assert.True(DoCallBack_MarshalStructS11ByVal_StdCall(new S11ByValStdCallcaller(TestMethodForStructS11_ReversePInvokeByVal_StdCall)));
                break;
            case StructID.ByteStruct3Byte:
                Console.WriteLine("Calling DoCallBack_MarshalStructByVal_StdCall_ByteStruct3Byte...");
                Assert.True(DoCallBack_MarshalStructByVal_StdCall_ByteStruct3Byte(
                    new ByValStdCallcaller_ByteStruct3Byte(TestMethod_DoCallBack_MarshalStructByVal_ByteStruct3Byte_StdCall)));
                break;
            default:
                Assert.Fail("DoCallBack_MarshalStructByVal_StdCall:The structid (Managed Side) is wrong");
                break;
        }
    }

    private static void Run_TestMethod_DoCallBack_MarshalStructByVal_Cdecl()
    {
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.ComplexStructId);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.InnerSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.InnerArraySequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.CharSetAnsiSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.CharSetUnicodeSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.NumberSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.S3Id);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.S5Id);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.StringStructSequentialAnsiId);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.StringStructSequentialUnicodeId);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.S8Id);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.S9Id);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.IncludeOuterIntegerStructSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.S11Id);
        // Windows X86 has a long standing X86_ONLY logic that causes 3, 5,6,7 byte structure returns to behave incorrectly.
        if ((RuntimeInformation.ProcessArchitecture != Architecture.X86) || !OperatingSystem.IsWindows())
        {
            TestMethod_DoCallBack_MarshalStructByVal_Cdecl(StructID.ByteStruct3Byte);
        }
    }

    private static void Run_TestMethod_DoCallBack_MarshalStructByVal_StdCall()
    {
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.ComplexStructId);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.InnerSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.InnerArraySequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.CharSetAnsiSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.CharSetUnicodeSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.NumberSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.S3Id);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.S5Id);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.StringStructSequentialAnsiId);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.StringStructSequentialUnicodeId);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.S8Id);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.S9Id);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.IncludeOuterIntegerStructSequentialId);
        TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.S11Id);
        // Windows X86 has a long standing X86_ONLY logic that causes 3, 5,6,7 byte structure returns to behave incorrectly.
        if ((RuntimeInformation.ProcessArchitecture != Architecture.X86) || !OperatingSystem.IsWindows())
        {
            TestMethod_DoCallBack_MarshalStructByVal_StdCall(StructID.ByteStruct3Byte);
        }
    }

    #endregion

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try{

        //Reverse Pinvoke,ByRef,cdecl
        Console.WriteLine("Run the methods for marshaling struct Reverse P/Invoke ByRef");
        Run_TestMethod_DoCallBack_MarshalStructByRef_Cdecl();
        Run_TestMethod_DoCallBack_MarshalStructByRef_StdCall();

        //Reverse PInvoke,ByValue,Cdcel
        Console.WriteLine("Run the methods for marshaling struct Reverse P/Invoke ByVal");
        Run_TestMethod_DoCallBack_MarshalStructByVal_Cdecl();
        Run_TestMethod_DoCallBack_MarshalStructByVal_StdCall();

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
