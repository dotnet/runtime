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
        ComplexStructId
    }

    private static void testMethod(S9 s9)
    {
        Console.WriteLine("The first field of s9 is:", s9.i32);
    }

    #region Methods for the struct InnerSequential declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InnerSequentialByRefDelegateCdecl([In, Out]ref InnerSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool InnerSequentialByRefDelegateStdCall([In, Out]ref InnerSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructInnerSequentialByRef_Cdecl([In, Out]ref InnerSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern InnerSequentialByRefDelegateCdecl Get_MarshalStructInnerSequentialByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructInnerSequentialByRef_StdCall([In, Out]ref InnerSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern InnerSequentialByRefDelegateStdCall Get_MarshalStructInnerSequentialByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InnerSequentialByValDelegateCdecl([In, Out] InnerSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool InnerSequentialByValDelegateStdCall([In, Out] InnerSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructInnerSequentialByVal_Cdecl([In, Out] InnerSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern InnerSequentialByValDelegateCdecl Get_MarshalStructInnerSequentialByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructInnerSequentialByVal_StdCall([In, Out] InnerSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern InnerSequentialByValDelegateStdCall Get_MarshalStructInnerSequentialByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct InnerArraySequential declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InnerArraySequentialByRefDelegateCdecl([In, Out]ref InnerArraySequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool InnerArraySequentialByRefDelegateStdCall([In, Out]ref InnerArraySequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructInnerArraySequentialByRef_Cdecl([In, Out]ref InnerArraySequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern InnerArraySequentialByRefDelegateCdecl Get_MarshalStructInnerArraySequentialByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructInnerArraySequentialByRef_StdCall([In, Out]ref InnerArraySequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern InnerArraySequentialByRefDelegateStdCall Get_MarshalStructInnerArraySequentialByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InnerArraySequentialByValDelegateCdecl([In, Out] InnerArraySequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool InnerArraySequentialByValDelegateStdCall([In, Out] InnerArraySequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructInnerArraySequentialByVal_Cdecl([In, Out] InnerArraySequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern InnerArraySequentialByValDelegateCdecl Get_MarshalStructInnerArraySequentialByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructInnerArraySequentialByVal_StdCall([In, Out] InnerArraySequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern InnerArraySequentialByValDelegateStdCall Get_MarshalStructInnerArraySequentialByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct CharSetAnsiSequential declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CharSetAnsiSequentialByRefDelegateCdecl([In, Out]ref CharSetAnsiSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool CharSetAnsiSequentialByRefDelegateStdCall([In, Out]ref CharSetAnsiSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructCharSetAnsiSequentialByRef_Cdecl([In, Out]ref CharSetAnsiSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern CharSetAnsiSequentialByRefDelegateCdecl Get_MarshalStructCharSetAnsiSequentialByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructCharSetAnsiSequentialByRef_StdCall([In, Out]ref CharSetAnsiSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern CharSetAnsiSequentialByRefDelegateStdCall Get_MarshalStructCharSetAnsiSequentialByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CharSetAnsiSequentialByValDelegateCdecl([In, Out] CharSetAnsiSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool CharSetAnsiSequentialByValDelegateStdCall([In, Out] CharSetAnsiSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructCharSetAnsiSequentialByVal_Cdecl([In, Out] CharSetAnsiSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern CharSetAnsiSequentialByValDelegateCdecl Get_MarshalStructCharSetAnsiSequentialByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructCharSetAnsiSequentialByVal_StdCall([In, Out] CharSetAnsiSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern CharSetAnsiSequentialByValDelegateStdCall Get_MarshalStructCharSetAnsiSequentialByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct CharSetUnicodeSequential declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CharSetUnicodeSequentialByRefDelegateCdecl([In, Out]ref CharSetUnicodeSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool CharSetUnicodeSequentialByRefDelegateStdCall([In, Out]ref CharSetUnicodeSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructCharSetUnicodeSequentialByRef_Cdecl([In, Out]ref CharSetUnicodeSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern CharSetUnicodeSequentialByRefDelegateCdecl Get_MarshalStructCharSetUnicodeSequentialByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructCharSetUnicodeSequentialByRef_StdCall([In, Out]ref CharSetUnicodeSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern CharSetUnicodeSequentialByRefDelegateStdCall Get_MarshalStructCharSetUnicodeSequentialByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CharSetUnicodeSequentialByValDelegateCdecl([In, Out] CharSetUnicodeSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool CharSetUnicodeSequentialByValDelegateStdCall([In, Out] CharSetUnicodeSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructCharSetUnicodeSequentialByVal_Cdecl([In, Out] CharSetUnicodeSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern CharSetUnicodeSequentialByValDelegateCdecl Get_MarshalStructCharSetUnicodeSequentialByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructCharSetUnicodeSequentialByVal_StdCall([In, Out] CharSetUnicodeSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern CharSetUnicodeSequentialByValDelegateStdCall Get_MarshalStructCharSetUnicodeSequentialByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct NumberSequential declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool NumberSequentialByRefDelegateCdecl([In, Out]ref NumberSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool NumberSequentialByRefDelegateStdCall([In, Out]ref NumberSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructNumberSequentialByRef_Cdecl([In, Out]ref NumberSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern NumberSequentialByRefDelegateCdecl Get_MarshalStructNumberSequentialByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructNumberSequentialByRef_StdCall([In, Out]ref NumberSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern NumberSequentialByRefDelegateStdCall Get_MarshalStructNumberSequentialByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool NumberSequentialByValDelegateCdecl([In, Out] NumberSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool NumberSequentialByValDelegateStdCall([In, Out] NumberSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructNumberSequentialByVal_Cdecl([In, Out] NumberSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern NumberSequentialByValDelegateCdecl Get_MarshalStructNumberSequentialByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructNumberSequentialByVal_StdCall([In, Out] NumberSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern NumberSequentialByValDelegateStdCall Get_MarshalStructNumberSequentialByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct S3 declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S3ByRefDelegateCdecl([In, Out]ref S3 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S3ByRefDelegateStdCall([In, Out]ref S3 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS3ByRef_Cdecl([In, Out]ref S3 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S3ByRefDelegateCdecl Get_MarshalStructS3ByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS3ByRef_StdCall([In, Out]ref S3 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S3ByRefDelegateStdCall Get_MarshalStructS3ByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S3ByValDelegateCdecl([In, Out] S3 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S3ByValDelegateStdCall([In, Out] S3 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS3ByVal_Cdecl([In, Out] S3 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S3ByValDelegateCdecl Get_MarshalStructS3ByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS3ByVal_StdCall([In, Out] S3 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S3ByValDelegateStdCall Get_MarshalStructS3ByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct S5 declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S5ByRefDelegateCdecl([In, Out]ref S5 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S5ByRefDelegateStdCall([In, Out]ref S5 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS5ByRef_Cdecl([In, Out]ref S5 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S5ByRefDelegateCdecl Get_MarshalStructS5ByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS5ByRef_StdCall([In, Out]ref S5 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S5ByRefDelegateStdCall Get_MarshalStructS5ByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S5ByValDelegateCdecl([In, Out] S5 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S5ByValDelegateStdCall([In, Out] S5 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS5ByVal_Cdecl([In, Out] S5 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S5ByValDelegateCdecl Get_MarshalStructS5ByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS5ByVal_StdCall([In, Out] S5 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S5ByValDelegateStdCall Get_MarshalStructS5ByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct StringStructSequentialAnsi declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool StringStructSequentialAnsiByRefDelegateCdecl([In, Out]ref StringStructSequentialAnsi argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool StringStructSequentialAnsiByRefDelegateStdCall([In, Out]ref StringStructSequentialAnsi argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructStringStructSequentialAnsiByRef_Cdecl([In, Out]ref StringStructSequentialAnsi argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern StringStructSequentialAnsiByRefDelegateCdecl Get_MarshalStructStringStructSequentialAnsiByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructStringStructSequentialAnsiByRef_StdCall([In, Out]ref StringStructSequentialAnsi argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern StringStructSequentialAnsiByRefDelegateStdCall Get_MarshalStructStringStructSequentialAnsiByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool StringStructSequentialAnsiByValDelegateCdecl([In, Out] StringStructSequentialAnsi argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool StringStructSequentialAnsiByValDelegateStdCall([In, Out] StringStructSequentialAnsi argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructStringStructSequentialAnsiByVal_Cdecl([In, Out] StringStructSequentialAnsi argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern StringStructSequentialAnsiByValDelegateCdecl Get_MarshalStructStringStructSequentialAnsiByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructStringStructSequentialAnsiByVal_StdCall([In, Out] StringStructSequentialAnsi argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern StringStructSequentialAnsiByValDelegateStdCall Get_MarshalStructStringStructSequentialAnsiByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct StringStructSequentialUnicode declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool StringStructSequentialUnicodeByRefDelegateCdecl([In, Out]ref StringStructSequentialUnicode argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool StringStructSequentialUnicodeByRefDelegateStdCall([In, Out]ref StringStructSequentialUnicode argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructStringStructSequentialUnicodeByRef_Cdecl([In, Out]ref StringStructSequentialUnicode argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern StringStructSequentialUnicodeByRefDelegateCdecl Get_MarshalStructStringStructSequentialUnicodeByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructStringStructSequentialUnicodeByRef_StdCall([In, Out]ref StringStructSequentialUnicode argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern StringStructSequentialUnicodeByRefDelegateStdCall Get_MarshalStructStringStructSequentialUnicodeByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool StringStructSequentialUnicodeByValDelegateCdecl([In, Out] StringStructSequentialUnicode argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool StringStructSequentialUnicodeByValDelegateStdCall([In, Out] StringStructSequentialUnicode argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructStringStructSequentialUnicodeByVal_Cdecl([In, Out] StringStructSequentialUnicode argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern StringStructSequentialUnicodeByValDelegateCdecl Get_MarshalStructStringStructSequentialUnicodeByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructStringStructSequentialUnicodeByVal_StdCall([In, Out] StringStructSequentialUnicode argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern StringStructSequentialUnicodeByValDelegateStdCall Get_MarshalStructStringStructSequentialUnicodeByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct S8 declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S8ByRefDelegateCdecl([In, Out]ref S8 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S8ByRefDelegateStdCall([In, Out]ref S8 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS8ByRef_Cdecl([In, Out]ref S8 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S8ByRefDelegateCdecl Get_MarshalStructS8ByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS8ByRef_StdCall([In, Out]ref S8 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S8ByRefDelegateStdCall Get_MarshalStructS8ByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S8ByValDelegateCdecl([In, Out] S8 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S8ByValDelegateStdCall([In, Out] S8 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS8ByVal_Cdecl([In, Out] S8 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S8ByValDelegateCdecl Get_MarshalStructS8ByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS8ByVal_StdCall([In, Out] S8 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S8ByValDelegateStdCall Get_MarshalStructS8ByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct S9 declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S9ByRefDelegateCdecl([In, Out]ref S9 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S9ByRefDelegateStdCall([In, Out]ref S9 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS9ByRef_Cdecl([In, Out]ref S9 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S9ByRefDelegateCdecl Get_MarshalStructS9ByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS9ByRef_StdCall([In, Out]ref S9 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S9ByRefDelegateStdCall Get_MarshalStructS9ByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S9ByValDelegateCdecl([In, Out] S9 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S9ByValDelegateStdCall([In, Out] S9 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS9ByVal_Cdecl([In, Out] S9 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S9ByValDelegateCdecl Get_MarshalStructS9ByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS9ByVal_StdCall([In, Out] S9 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S9ByValDelegateStdCall Get_MarshalStructS9ByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct IncludeOuterIntegerStructSequential declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool IncludeOuterIntegerStructSequentialByRefDelegateCdecl([In, Out]ref IncludeOuterIntegerStructSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool IncludeOuterIntegerStructSequentialByRefDelegateStdCall([In, Out]ref IncludeOuterIntegerStructSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl([In, Out]ref IncludeOuterIntegerStructSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern IncludeOuterIntegerStructSequentialByRefDelegateCdecl Get_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall([In, Out]ref IncludeOuterIntegerStructSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern IncludeOuterIntegerStructSequentialByRefDelegateStdCall Get_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool IncludeOuterIntegerStructSequentialByValDelegateCdecl([In, Out] IncludeOuterIntegerStructSequential argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool IncludeOuterIntegerStructSequentialByValDelegateStdCall([In, Out] IncludeOuterIntegerStructSequential argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl([In, Out] IncludeOuterIntegerStructSequential argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern IncludeOuterIntegerStructSequentialByValDelegateCdecl Get_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall([In, Out] IncludeOuterIntegerStructSequential argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern IncludeOuterIntegerStructSequentialByValDelegateStdCall Get_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct S11 declaration

    #region PassByRef

    //For Delegate Pinvoke ByRef
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S11ByRefDelegateCdecl([In, Out]ref S11 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S11ByRefDelegateStdCall([In, Out]ref S11 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS11ByRef_Cdecl([In, Out]ref S11 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S11ByRefDelegateCdecl Get_MarshalStructS11ByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS11ByRef_StdCall([In, Out]ref S11 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S11ByRefDelegateStdCall Get_MarshalStructS11ByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    //For Delegate Pinvoke ByVal
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool S11ByValDelegateCdecl([In, Out] S11 argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool S11ByValDelegateStdCall([In, Out] S11 argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructS11ByVal_Cdecl([In, Out] S11 argStr);
    //Delegate PInvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S11ByValDelegateCdecl Get_MarshalStructS11ByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructS11ByVal_StdCall([In, Out] S11 argStr);
    //Delegate PInvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern S11ByValDelegateStdCall Get_MarshalStructS11ByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods for the struct ComplexStruct declaration

    #region PassByRef

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool ComplexStructByRefDelegateCdecl([In, Out]ref ComplexStruct argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool ComplexStructByRefDelegateStdCall([In, Out]ref ComplexStruct argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructComplexStructByRef_Cdecl([In, Out]ref ComplexStruct argStr);
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern ComplexStructByRefDelegateCdecl Get_MarshalStructComplexStructByRef_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructComplexStructByRef_StdCall([In, Out]ref ComplexStruct argStr);
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern ComplexStructByRefDelegateStdCall Get_MarshalStructComplexStructByRef_StdCall_FuncPtr();

    #endregion

    #region PassByValue

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool ComplexStructByValDelegateCdecl([In, Out] ComplexStruct argStr);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool ComplexStructByValDelegateStdCall([In, Out] ComplexStruct argStr);
    //Pinvoke,cdecl
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalStructComplexStructByVal_Cdecl([In, Out] ComplexStruct argStr);
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern ComplexStructByValDelegateCdecl Get_MarshalStructComplexStructByVal_Cdecl_FuncPtr();
    //Pinvoke,stdcall
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalStructComplexStructByVal_StdCall([In, Out] ComplexStruct argStr);
    [DllImport("SeqPInvokeNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern ComplexStructByValDelegateStdCall Get_MarshalStructComplexStructByVal_StdCall_FuncPtr();

    #endregion

    #endregion

    #region Methods implementation

    //By Ref
    //Delegate P/Invoke
    unsafe private static void TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID structid)
    {
        Console.WriteLine("Delegate PInvoke,By Ref,Cdecl");
        switch (structid)
        {
            case StructID.ComplexStructId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructComplexStructByRef_Cdecl...");
                ComplexStruct cs = new ComplexStruct();
                cs.i = 321;
                cs.b = true;
                cs.str = "Managed";
                cs.type.idata = 123;
                cs.type.ptrdata = (IntPtr)0x120000;
                ComplexStructByRefDelegateCdecl caller1 = Get_MarshalStructComplexStructByRef_Cdecl_FuncPtr();
                Assert.True(caller1(ref cs));
                Assert.Equal(9999, cs.i);
                Assert.False(cs.b);
                Assert.Equal("Native", cs.str);
                Assert.Equal(-1, cs.type.idata);
                Assert.Equal(3.14159, cs.type.ddata);
                break;
            case StructID.InnerSequentialId:
                InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                InnerSequential change_is = Helper.NewInnerSequential(77, 77.0F, "changed string");
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructInnerSequentialByRef_Cdecl...");
                InnerSequentialByRefDelegateCdecl caller2 = Get_MarshalStructInnerSequentialByRef_Cdecl_FuncPtr();
                Assert.True(caller2(ref source_is));
                Assert.True(Helper.ValidateInnerSequential(source_is, change_is, "DelegatePInvoke_MarshalStructInnerSequentialByRef_Cdecl"));
                break;
            case StructID.InnerArraySequentialId:
                InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                InnerArraySequential change_ias = Helper.NewInnerArraySequential(77, 77.0F, "changed string");
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructInnerArraySequentialByRef_Cdecl...");
                InnerArraySequentialByRefDelegateCdecl caller3 = Get_MarshalStructInnerArraySequentialByRef_Cdecl_FuncPtr();
                Assert.True(caller3(ref source_ias));
                Assert.True(Helper.ValidateInnerArraySequential(source_ias, change_ias, "DelegatePInvoke_MarshalStructInnerArraySequentialByRef_Cdecl"));
                break;
            case StructID.CharSetAnsiSequentialId:
                CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                CharSetAnsiSequential change_csas = Helper.NewCharSetAnsiSequential("change string", 'n');
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructCharSetAnsiSequentialByRef_Cdecl...");
                CharSetAnsiSequentialByRefDelegateCdecl caller4 = Get_MarshalStructCharSetAnsiSequentialByRef_Cdecl_FuncPtr();
                Assert.True(caller4(ref source_csas));
                Assert.True(Helper.ValidateCharSetAnsiSequential(source_csas, change_csas, "DelegatePInvoke_MarshalStructCharSetAnsiSequentialByRef_Cdecl"));
                break;
            case StructID.CharSetUnicodeSequentialId:
                CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                CharSetUnicodeSequential change_csus = Helper.NewCharSetUnicodeSequential("change string", 'n');
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructCharSetUnicodeSequentialByRef_Cdecl...");
                CharSetUnicodeSequentialByRefDelegateCdecl caller5 = Get_MarshalStructCharSetUnicodeSequentialByRef_Cdecl_FuncPtr();
                Assert.True(caller5(ref source_csus));
                Assert.True(Helper.ValidateCharSetUnicodeSequential(source_csus, change_csus, "DelegatePInvoke_MarshalStructCharSetUnicodeSequentialByRef_Cdecl"));
                break;
            case StructID.NumberSequentialId:
                NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue,
                    sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                NumberSequential change_ns = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructNumberSequentialByRef_Cdecl...");
                NumberSequentialByRefDelegateCdecl caller6 = Get_MarshalStructNumberSequentialByRef_Cdecl_FuncPtr();
                Assert.True(caller6(ref source_ns));
                Assert.True(Helper.ValidateNumberSequential(source_ns, change_ns, "DelegatePInvoke_MarshalStructNumberSequentialByRef_Cdecl"));
                break;
            case StructID.S3Id:
                int[] iarr = new int[256];
                int[] icarr = new int[256];
                Helper.InitialArray(iarr, icarr);
                S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                S3 changeS3 = Helper.NewS3(false, "change string", icarr);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS3ByRef_Cdecl...");
                S3ByRefDelegateCdecl caller7 = Get_MarshalStructS3ByRef_Cdecl_FuncPtr();
                Assert.True(caller7(ref sourceS3));
                Assert.True(Helper.ValidateS3(sourceS3, changeS3, "DelegatePInvoke_MarshalStructS3ByRef_Cdecl"));
                break;
            case StructID.S5Id:
                Enum1 enums = Enum1.e1;
                Enum1 enumch = Enum1.e2;
                S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                S5 changeS5 = Helper.NewS5(64, "change string", enumch);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS5ByRef_Cdecl...");
                S5ByRefDelegateCdecl caller8 = Get_MarshalStructS5ByRef_Cdecl_FuncPtr();
                Assert.True(caller8(ref sourceS5));
                Assert.True(Helper.ValidateS5(sourceS5, changeS5, "DelegatePInvoke_MarshalStructS5ByRef_Cdecl"));
                break;
            case StructID.StringStructSequentialAnsiId:
                strOne = new String('a', 512);
                strTwo = new String('b', 512);
                StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                StringStructSequentialAnsi change_sssa = Helper.NewStringStructSequentialAnsi(strTwo, strOne);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructStringStructSequentialAnsiByRef_Cdecl...");
                StringStructSequentialAnsiByRefDelegateCdecl caller9 = Get_MarshalStructStringStructSequentialAnsiByRef_Cdecl_FuncPtr();
                Assert.True(caller9(ref source_sssa));
                Assert.True(Helper.ValidateStringStructSequentialAnsi(source_sssa, change_sssa, "DelegatePInvoke_MarshalStructStringStructSequentialAnsiByRef_Cdecl"));
                break;
            case StructID.StringStructSequentialUnicodeId:
                strOne = new String('a', 256);
                strTwo = new String('b', 256);
                StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                StringStructSequentialUnicode change_sssu = Helper.NewStringStructSequentialUnicode(strTwo, strOne);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructStringStructSequentialUnicodeByRef_Cdecl...");
                StringStructSequentialUnicodeByRefDelegateCdecl caller10 = Get_MarshalStructStringStructSequentialUnicodeByRef_Cdecl_FuncPtr();
                Assert.True(caller10(ref source_sssu));
                Assert.True(Helper.ValidateStringStructSequentialUnicode(source_sssu, change_sssu, "DelegatePInvoke_MarshalStructStringStructSequentialUnicodeByRef_Cdecl"));
                break;
            case StructID.S8Id:
                S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                S8 changeS8 = Helper.NewS8("world", false, 1, 256, 256, 64);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS8ByRef_Cdecl...");
                S8ByRefDelegateCdecl caller11 = Get_MarshalStructS8ByRef_Cdecl_FuncPtr();
                Assert.True(caller11(ref sourceS8));
                Assert.True(Helper.ValidateS8(sourceS8, changeS8, "DelegatePInvoke_MarshalStructS8ByRef_Cdecl"));
                break;
            case StructID.S9Id:
                S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                S9 changeS9 = Helper.NewS9(256, null);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS9ByRef_Cdecl...");
                S9ByRefDelegateCdecl caller12 = Get_MarshalStructS9ByRef_Cdecl_FuncPtr();
                Assert.True(caller12(ref sourceS9));
                Assert.True(Helper.ValidateS9(sourceS9, changeS9, "DelegatePInvoke_MarshalStructS9ByRef_Cdecl"));
                break;
            case StructID.IncludeOuterIntegerStructSequentialId:
                IncludeOuterIntegerStructSequential sourceIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(32, 32);
                IncludeOuterIntegerStructSequential changeIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(64, 64);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl...");
                IncludeOuterIntegerStructSequentialByRefDelegateCdecl caller13 = Get_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl_FuncPtr();
                Assert.True(caller13(ref sourceIncludeOuterIntegerStructSequential));
                Assert.True(Helper.ValidateIncludeOuterIntegerStructSequential(sourceIncludeOuterIntegerStructSequential,
                    changeIncludeOuterIntegerStructSequential, "DelegatePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByRef_Cdecl"));
                break;
            case StructID.S11Id:
                S11 sourceS11 = Helper.NewS11((int*)(32), 32);
                S11 changeS11 = Helper.NewS11((int*)(64), 64);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS11ByRef_Cdecl...");
                S11ByRefDelegateCdecl caller14 = Get_MarshalStructS11ByRef_Cdecl_FuncPtr();
                Assert.True(caller14(ref sourceS11));
                Assert.True(Helper.ValidateS11(sourceS11, changeS11, "DelegatePInvoke_MarshalStructS11ByRef_Cdecl"));
                break;
            default:
                Assert.True(false, "TestMethod_DelegatePInvoke_MarshalByRef_Cdecl:The structid (Managed Side) is wrong");
                break;
        }
    }

    unsafe private static void TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID structid)
    {
        Console.WriteLine("Delegate PInvoke,By Ref,StdCall");
        switch (structid)
        {
            case StructID.ComplexStructId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructComplexStructByRef_StdCall...");
                ComplexStruct cs = new ComplexStruct();
                cs.i = 321;
                cs.b = true;
                cs.str = "Managed";
                cs.type.idata = 123;
                cs.type.ptrdata = (IntPtr)0x120000;
                ComplexStructByRefDelegateStdCall caller1 = Get_MarshalStructComplexStructByRef_StdCall_FuncPtr();
                Assert.True(caller1(ref cs));
                Assert.Equal(9999, cs.i);
                Assert.False(cs.b);
                Assert.Equal("Native", cs.str);
                Assert.Equal(-1, cs.type.idata);
                Assert.Equal(3.14159, cs.type.ddata);
                break;
            case StructID.InnerSequentialId:
                InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                InnerSequential change_is = Helper.NewInnerSequential(77, 77.0F, "changed string");
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructInnerSequentialByRef_StdCall...");
                InnerSequentialByRefDelegateStdCall caller2 = Get_MarshalStructInnerSequentialByRef_StdCall_FuncPtr();
                Assert.True(caller2(ref source_is));
                Assert.True(Helper.ValidateInnerSequential(source_is, change_is, "DelegatePInvoke_MarshalStructInnerSequentialByRef_StdCall"));
                break;
            case StructID.InnerArraySequentialId:
                InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                InnerArraySequential change_ias = Helper.NewInnerArraySequential(77, 77.0F, "changed string");
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructInnerArraySequentialByRef_StdCall...");
                InnerArraySequentialByRefDelegateStdCall caller3 = Get_MarshalStructInnerArraySequentialByRef_StdCall_FuncPtr();
                Assert.True(caller3(ref source_ias));
                Assert.True(Helper.ValidateInnerArraySequential(source_ias, change_ias, "DelegatePInvoke_MarshalStructInnerArraySequentialByRef_StdCall"));
                break;
            case StructID.CharSetAnsiSequentialId:
                CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                CharSetAnsiSequential change_csas = Helper.NewCharSetAnsiSequential("change string", 'n');
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructCharSetAnsiSequentialByRef_StdCall...");
                CharSetAnsiSequentialByRefDelegateStdCall caller4 = Get_MarshalStructCharSetAnsiSequentialByRef_StdCall_FuncPtr();
                Assert.True(caller4(ref source_csas));
                Assert.True(Helper.ValidateCharSetAnsiSequential(source_csas, change_csas, "DelegatePInvoke_MarshalStructCharSetAnsiSequentialByRef_StdCall"));
                break;
            case StructID.CharSetUnicodeSequentialId:
                CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                CharSetUnicodeSequential change_csus = Helper.NewCharSetUnicodeSequential("change string", 'n');
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructCharSetUnicodeSequentialByRef_StdCall...");
                CharSetUnicodeSequentialByRefDelegateStdCall caller5 = Get_MarshalStructCharSetUnicodeSequentialByRef_StdCall_FuncPtr();
                Assert.True(caller5(ref source_csus));
                Assert.True(Helper.ValidateCharSetUnicodeSequential(source_csus, change_csus, "DelegatePInvoke_MarshalStructCharSetUnicodeSequentialByRef_StdCall"));
                break;
            case StructID.NumberSequentialId:
                NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue,
                    sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                NumberSequential change_ns = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructNumberSequentialByRef_StdCall...");
                NumberSequentialByRefDelegateStdCall caller6 = Get_MarshalStructNumberSequentialByRef_StdCall_FuncPtr();
                Assert.True(caller6(ref source_ns));
                Assert.True(Helper.ValidateNumberSequential(source_ns, change_ns, "DelegatePInvoke_MarshalStructNumberSequentialByRef_StdCall"));
                break;
            case StructID.S3Id:
                int[] iarr = new int[256];
                int[] icarr = new int[256];
                Helper.InitialArray(iarr, icarr);
                S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                S3 changeS3 = Helper.NewS3(false, "change string", icarr);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS3ByRef_StdCall...");
                S3ByRefDelegateStdCall caller7 = Get_MarshalStructS3ByRef_StdCall_FuncPtr();
                Assert.True(caller7(ref sourceS3));
                Assert.True(Helper.ValidateS3(sourceS3, changeS3, "DelegatePInvoke_MarshalStructS3ByRef_StdCall"));
                break;
            case StructID.S5Id:
                Enum1 enums = Enum1.e1;
                Enum1 enumch = Enum1.e2;
                S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                S5 changeS5 = Helper.NewS5(64, "change string", enumch);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS5ByRef_StdCall...");
                S5ByRefDelegateStdCall caller8 = Get_MarshalStructS5ByRef_StdCall_FuncPtr();
                Assert.True(caller8(ref sourceS5));
                Assert.True(Helper.ValidateS5(sourceS5, changeS5, "DelegatePInvoke_MarshalStructS5ByRef_StdCall"));
                break;
            case StructID.StringStructSequentialAnsiId:
                strOne = new String('a', 512);
                strTwo = new String('b', 512);
                StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                StringStructSequentialAnsi change_sssa = Helper.NewStringStructSequentialAnsi(strTwo, strOne);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructStringStructSequentialAnsiByRef_StdCall...");
                StringStructSequentialAnsiByRefDelegateStdCall caller9 = Get_MarshalStructStringStructSequentialAnsiByRef_StdCall_FuncPtr();
                Assert.True(caller9(ref source_sssa));
                Assert.True(Helper.ValidateStringStructSequentialAnsi(source_sssa, change_sssa, "DelegatePInvoke_MarshalStructStringStructSequentialAnsiByRef_StdCall"));
                break;
            case StructID.StringStructSequentialUnicodeId:
                strOne = new String('a', 256);
                strTwo = new String('b', 256);
                StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                StringStructSequentialUnicode change_sssu = Helper.NewStringStructSequentialUnicode(strTwo, strOne);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructStringStructSequentialUnicodeByRef_StdCall...");
                StringStructSequentialUnicodeByRefDelegateStdCall caller10 = Get_MarshalStructStringStructSequentialUnicodeByRef_StdCall_FuncPtr();
                Assert.True(caller10(ref source_sssu));
                Assert.True(Helper.ValidateStringStructSequentialUnicode(source_sssu, change_sssu, "DelegatePInvoke_MarshalStructStringStructSequentialUnicodeByRef_StdCall"));
                break;
            case StructID.S8Id:
                S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                S8 changeS8 = Helper.NewS8("world", false, 1, 256, 256, 64);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS8ByRef_StdCall...");
                S8ByRefDelegateStdCall caller11 = Get_MarshalStructS8ByRef_StdCall_FuncPtr();
                Assert.True(caller11(ref sourceS8));
                Assert.True(Helper.ValidateS8(sourceS8, changeS8, "DelegatePInvoke_MarshalStructS8ByRef_StdCall"));
                break;
            case StructID.S9Id:
                S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                S9 changeS9 = Helper.NewS9(256, null);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS9ByRef_StdCall...");
                S9ByRefDelegateStdCall caller12 = Get_MarshalStructS9ByRef_StdCall_FuncPtr();
                Assert.True(caller12(ref sourceS9));
                Assert.True(Helper.ValidateS9(sourceS9, changeS9, "DelegatePInvoke_MarshalStructS9ByRef_StdCall"));
                break;
            case StructID.IncludeOuterIntegerStructSequentialId:
                IncludeOuterIntegerStructSequential sourceIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(32, 32);
                IncludeOuterIntegerStructSequential changeIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(64, 64);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall...");
                IncludeOuterIntegerStructSequentialByRefDelegateStdCall caller13 = Get_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall_FuncPtr();
                Assert.True(caller13(ref sourceIncludeOuterIntegerStructSequential));
                Assert.True(Helper.ValidateIncludeOuterIntegerStructSequential(sourceIncludeOuterIntegerStructSequential,
                    changeIncludeOuterIntegerStructSequential, "DelegatePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByRef_StdCall"));
                break;
            case StructID.S11Id:
                S11 sourceS11 = Helper.NewS11((int*)(32), 32);
                S11 changeS11 = Helper.NewS11((int*)(64), 64);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS11ByRef_StdCall...");
                S11ByRefDelegateStdCall caller14 = Get_MarshalStructS11ByRef_StdCall_FuncPtr();
                Assert.True(caller14(ref sourceS11));
                Assert.True(Helper.ValidateS11(sourceS11, changeS11, "DelegatePInvoke_MarshalStructS11ByRef_StdCall"));
                break;
            default:
                Assert.True(false, "TestMethod_DelegatePInvoke_MarshalByRef_StdCall:The structid (Managed Side) is wrong");
                break;
        }
    }

    unsafe private static void TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID structid)
    {
        Console.WriteLine("Delegate PInvoke,By Value,Cdecl");
        switch (structid)
        {
            case StructID.ComplexStructId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructComplexStructByVal_Cdecl...");
                ComplexStruct cs = new ComplexStruct();
                cs.i = 321;
                cs.b = true;
                cs.str = "Managed";
                cs.type.idata = 123;
                cs.type.ptrdata = (IntPtr)0x120000;
                ComplexStructByValDelegateCdecl caller1 = Get_MarshalStructComplexStructByVal_Cdecl_FuncPtr();
                Assert.True(caller1(cs));
                Assert.Equal(321, cs.i);
                Assert.True(cs.b);
                Assert.Equal("Managed", cs.str);
                Assert.Equal(123, cs.type.idata);
                Assert.Equal(0x120000, (int)cs.type.ptrdata);
                break;
            case StructID.InnerSequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructInnerSequentialByVal_Cdecl...");
                InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                InnerSequential clone_is1 = Helper.NewInnerSequential(1, 1.0F, "some string");
                InnerSequentialByValDelegateCdecl caller2 = Get_MarshalStructInnerSequentialByVal_Cdecl_FuncPtr();
                Assert.True(caller2(source_is));
                Assert.True(Helper.ValidateInnerSequential(source_is, clone_is1, "DelegatePInvoke_MarshalStructInnerSequentialByVal_Cdecl"));
                break;
            case StructID.InnerArraySequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructInnerArraySequentialByVal_Cdecl...");
                InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                InnerArraySequential clone_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                InnerArraySequentialByValDelegateCdecl caller3 = Get_MarshalStructInnerArraySequentialByVal_Cdecl_FuncPtr();
                Assert.True(caller3(source_ias));
                Assert.True(Helper.ValidateInnerArraySequential(source_ias, clone_ias, "DelegatePInvoke_MarshalStructInnerArraySequentialByVal_Cdecl"));
                break;
            case StructID.CharSetAnsiSequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructCharSetAnsiSequentialByVal_Cdecl...");
                CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                CharSetAnsiSequential clone_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                CharSetAnsiSequentialByValDelegateCdecl caller4 = Get_MarshalStructCharSetAnsiSequentialByVal_Cdecl_FuncPtr();
                Assert.True(caller4(source_csas));
                Assert.True(Helper.ValidateCharSetAnsiSequential(source_csas, clone_csas, "DelegatePInvoke_MarshalStructCharSetAnsiSequentialByVal_Cdecl"));
                break;
            case StructID.CharSetUnicodeSequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructCharSetUnicodeSequentialByVal_Cdecl...");
                CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                CharSetUnicodeSequential clone_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                CharSetUnicodeSequentialByValDelegateCdecl caller5 = Get_MarshalStructCharSetUnicodeSequentialByVal_Cdecl_FuncPtr();
                Assert.True(caller5(source_csus));
                Assert.True(Helper.ValidateCharSetUnicodeSequential(source_csus, clone_csus, "DelegatePInvoke_MarshalStructCharSetUnicodeSequentialByVal_Cdecl"));
                break;
            case StructID.NumberSequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructNumberSequentialByVal_Cdecl...");
                NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue,
                    sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                NumberSequential clone_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue,
                    sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                NumberSequentialByValDelegateCdecl caller6 = Get_MarshalStructNumberSequentialByVal_Cdecl_FuncPtr();
                Assert.True(caller6(source_ns));
                Assert.True(Helper.ValidateNumberSequential(source_ns, clone_ns, "DelegatePInvoke_MarshalStructNumberSequentialByVal_Cdecl"));
                break;
            case StructID.S3Id:
                int[] iarr = new int[256];
                int[] icarr = new int[256];
                Helper.InitialArray(iarr, icarr);
                S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                S3 cloneS3 = Helper.NewS3(true, "some string", iarr);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS3ByVal_Cdecl...");
                S3ByValDelegateCdecl caller7 = Get_MarshalStructS3ByVal_Cdecl_FuncPtr();
                Assert.True(caller7(sourceS3));
                Assert.True(Helper.ValidateS3(sourceS3, cloneS3, "DelegatePInvoke_MarshalStructS3ByVal_Cdecl"));
                break;
            case StructID.S5Id:
                Enum1 enums = Enum1.e1;
                S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                S5 cloneS5 = Helper.NewS5(32, "some string", enums);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS5ByVal_Cdecl...");
                S5ByValDelegateCdecl caller8 = Get_MarshalStructS5ByVal_Cdecl_FuncPtr();
                Assert.True(caller8(sourceS5));
                Assert.True(Helper.ValidateS5(sourceS5, cloneS5, "DelegatePInvoke_MarshalStructS5ByVal_Cdecl"));
                break;
            case StructID.StringStructSequentialAnsiId:
                strOne = new String('a', 512);
                strTwo = new String('b', 512);
                StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                StringStructSequentialAnsi clone_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructStringStructSequentialAnsiByVal_Cdecl...");
                StringStructSequentialAnsiByValDelegateCdecl caller9 = Get_MarshalStructStringStructSequentialAnsiByVal_Cdecl_FuncPtr();
                Assert.True(caller9(source_sssa));
                Assert.True(Helper.ValidateStringStructSequentialAnsi(source_sssa, clone_sssa, "DelegatePInvoke_MarshalStructStringStructSequentialAnsiByVal_Cdecl"));
                break;
            case StructID.StringStructSequentialUnicodeId:
                strOne = new String('a', 256);
                strTwo = new String('b', 256);
                StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                StringStructSequentialUnicode clone_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructStringStructSequentialUnicodeByVal_Cdecl...");
                StringStructSequentialUnicodeByValDelegateCdecl caller10 = Get_MarshalStructStringStructSequentialUnicodeByVal_Cdecl_FuncPtr();
                Assert.True(caller10(source_sssu));
                Assert.True(Helper.ValidateStringStructSequentialUnicode(source_sssu, clone_sssu, "DelegatePInvoke_MarshalStructStringStructSequentialUnicodeByVal_Cdecl"));
                break;
            case StructID.S8Id:
                S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                S8 cloneS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS8ByVal_Cdecl...");
                S8ByValDelegateCdecl caller11 = Get_MarshalStructS8ByVal_Cdecl_FuncPtr();
                Assert.True(caller11(sourceS8));
                Assert.True(Helper.ValidateS8(sourceS8, cloneS8, "DelegatePInvoke_MarshalStructS8ByVal_Cdecl"));
                break;
            case StructID.S9Id:
                S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                S9 cloneS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS9ByVal_Cdecl...");
                S9ByValDelegateCdecl caller12 = Get_MarshalStructS9ByVal_Cdecl_FuncPtr();
                Assert.True(caller12(sourceS9));
                Assert.True(Helper.ValidateS9(sourceS9, cloneS9, "DelegatePInvoke_MarshalStructS9ByVal_Cdecl"));
                break;
            case StructID.IncludeOuterIntegerStructSequentialId:
                IncludeOuterIntegerStructSequential sourceIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(32, 32);
                IncludeOuterIntegerStructSequential cloneIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(32, 32);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl...");
                IncludeOuterIntegerStructSequentialByValDelegateCdecl caller13 = Get_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl_FuncPtr();
                Assert.True(caller13(sourceIncludeOuterIntegerStructSequential));
                Assert.True(Helper.ValidateIncludeOuterIntegerStructSequential(sourceIncludeOuterIntegerStructSequential,
                    cloneIncludeOuterIntegerStructSequential, "DelegatePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByVal_Cdecl"));
                break;
            case StructID.S11Id:
                S11 sourceS11 = Helper.NewS11((int*)(32), 32);
                S11 cloneS11 = Helper.NewS11((int*)(32), 32);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS11ByVal_Cdecl...");
                S11ByValDelegateCdecl caller14 = Get_MarshalStructS11ByVal_Cdecl_FuncPtr();
                Assert.True(caller14(sourceS11));
                Assert.True(Helper.ValidateS11(sourceS11, cloneS11, "DelegatePInvoke_MarshalStructS11ByVal_Cdecl"));
                break;
            default:
                Assert.True(false, "TestMethod_DelegatePInvoke_MarshalByVal_Cdecl:The structid (Managed Side) is wrong");
                break;
        }
    }

    unsafe private static void TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID structid)
    {
        Console.WriteLine("Delegate PInvoke,By Value,StdCall");
        switch (structid)
        {
            case StructID.ComplexStructId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructComplexStructByVal_StdCall...");
                ComplexStruct cs = new ComplexStruct();
                cs.i = 321;
                cs.b = true;
                cs.str = "Managed";
                cs.type.idata = 123;
                cs.type.ptrdata = (IntPtr)0x120000;
                ComplexStructByValDelegateStdCall caller1 = Get_MarshalStructComplexStructByVal_StdCall_FuncPtr();
                Assert.True(caller1(cs));
                Assert.Equal(321, cs.i);
                Assert.True(cs.b);
                Assert.Equal("Managed", cs.str);
                Assert.Equal(123, cs.type.idata);
                Assert.Equal(0x120000, (int)cs.type.ptrdata);
                break;
            case StructID.InnerSequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructInnerSequentialByVal_StdCall...");
                InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                InnerSequential clone_is1 = Helper.NewInnerSequential(1, 1.0F, "some string");
                InnerSequentialByValDelegateStdCall caller2 = Get_MarshalStructInnerSequentialByVal_StdCall_FuncPtr();
                Assert.True(caller2(source_is));
                Assert.True(Helper.ValidateInnerSequential(source_is, clone_is1, "DelegatePInvoke_MarshalStructInnerSequentialByVal_StdCall"));
                break;
            case StructID.InnerArraySequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructInnerArraySequentialByVal_StdCall...");
                InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                InnerArraySequential clone_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                InnerArraySequentialByValDelegateStdCall caller3 = Get_MarshalStructInnerArraySequentialByVal_StdCall_FuncPtr();
                Assert.True(caller3(source_ias));
                Assert.True(Helper.ValidateInnerArraySequential(source_ias, clone_ias, "DelegatePInvoke_MarshalStructInnerArraySequentialByVal_StdCall"));
                break;
            case StructID.CharSetAnsiSequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructCharSetAnsiSequentialByVal_StdCall...");
                CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                CharSetAnsiSequential clone_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                CharSetAnsiSequentialByValDelegateStdCall caller4 = Get_MarshalStructCharSetAnsiSequentialByVal_StdCall_FuncPtr();
                Assert.True(caller4(source_csas));
                Assert.True(Helper.ValidateCharSetAnsiSequential(source_csas, clone_csas, "DelegatePInvoke_MarshalStructCharSetAnsiSequentialByVal_StdCall"));
                break;
            case StructID.CharSetUnicodeSequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructCharSetUnicodeSequentialByVal_StdCall...");
                CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                CharSetUnicodeSequential clone_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                CharSetUnicodeSequentialByValDelegateStdCall caller5 = Get_MarshalStructCharSetUnicodeSequentialByVal_StdCall_FuncPtr();
                Assert.True(caller5(source_csus));
                Assert.True(Helper.ValidateCharSetUnicodeSequential(source_csus, clone_csus, "DelegatePInvoke_MarshalStructCharSetUnicodeSequentialByVal_StdCall"));
                break;
            case StructID.NumberSequentialId:
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructNumberSequentialByVal_StdCall...");
                NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue,
                    sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                NumberSequential clone_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue,
                    sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                NumberSequentialByValDelegateStdCall caller6 = Get_MarshalStructNumberSequentialByVal_StdCall_FuncPtr();
                Assert.True(caller6(source_ns));
                Assert.True(Helper.ValidateNumberSequential(source_ns, clone_ns, "DelegatePInvoke_MarshalStructNumberSequentialByVal_StdCall"));
                break;
            case StructID.S3Id:
                int[] iarr = new int[256];
                int[] icarr = new int[256];
                Helper.InitialArray(iarr, icarr);
                S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                S3 cloneS3 = Helper.NewS3(true, "some string", iarr);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS3ByVal_StdCall...");
                S3ByValDelegateStdCall caller7 = Get_MarshalStructS3ByVal_StdCall_FuncPtr();
                Assert.True(caller7(sourceS3));
                Assert.True(Helper.ValidateS3(sourceS3, cloneS3, "DelegatePInvoke_MarshalStructS3ByVal_StdCall"));
                break;
            case StructID.S5Id:
                Enum1 enums = Enum1.e1;
                S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                S5 cloneS5 = Helper.NewS5(32, "some string", enums);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS5ByVal_StdCall...");
                S5ByValDelegateStdCall caller8 = Get_MarshalStructS5ByVal_StdCall_FuncPtr();
                Assert.True(caller8(sourceS5));
                Assert.True(Helper.ValidateS5(sourceS5, cloneS5, "DelegatePInvoke_MarshalStructS5ByVal_StdCall"));
                break;
            case StructID.StringStructSequentialAnsiId:
                strOne = new String('a', 512);
                strTwo = new String('b', 512);
                StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                StringStructSequentialAnsi clone_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructStringStructSequentialAnsiByVal_StdCall...");
                StringStructSequentialAnsiByValDelegateStdCall caller9 = Get_MarshalStructStringStructSequentialAnsiByVal_StdCall_FuncPtr();
                Assert.True(caller9(source_sssa));
                Assert.True(Helper.ValidateStringStructSequentialAnsi(source_sssa, clone_sssa, "DelegatePInvoke_MarshalStructStringStructSequentialAnsiByVal_StdCall"));
                break;
            case StructID.StringStructSequentialUnicodeId:
                strOne = new String('a', 256);
                strTwo = new String('b', 256);
                StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                StringStructSequentialUnicode clone_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructStringStructSequentialUnicodeByVal_StdCall...");
                StringStructSequentialUnicodeByValDelegateStdCall caller10 = Get_MarshalStructStringStructSequentialUnicodeByVal_StdCall_FuncPtr();
                Assert.True(caller10(source_sssu));
                Assert.True(Helper.ValidateStringStructSequentialUnicode(source_sssu, clone_sssu, "DelegatePInvoke_MarshalStructStringStructSequentialUnicodeByVal_StdCall"));
                break;
            case StructID.S8Id:
                S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                S8 cloneS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS8ByVal_StdCall...");
                S8ByValDelegateStdCall caller11 = Get_MarshalStructS8ByVal_StdCall_FuncPtr();
                Assert.True(caller11(sourceS8));
                Assert.True(Helper.ValidateS8(sourceS8, cloneS8, "DelegatePInvoke_MarshalStructS8ByVal_StdCall"));
                break;
            case StructID.S9Id:
                S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                S9 cloneS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS9ByVal_StdCall...");
                S9ByValDelegateStdCall caller12 = Get_MarshalStructS9ByVal_StdCall_FuncPtr();
                Assert.True(caller12(sourceS9));
                Assert.True(Helper.ValidateS9(sourceS9, cloneS9, "DelegatePInvoke_MarshalStructS9ByVal_StdCall"));
                break;
            case StructID.IncludeOuterIntegerStructSequentialId:
                IncludeOuterIntegerStructSequential sourceIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(32, 32);
                IncludeOuterIntegerStructSequential cloneIncludeOuterIntegerStructSequential = Helper.NewIncludeOuterIntegerStructSequential(32, 32);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall...");
                IncludeOuterIntegerStructSequentialByValDelegateStdCall caller13 = Get_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall_FuncPtr();
                Assert.True(caller13(sourceIncludeOuterIntegerStructSequential));
                Assert.True(Helper.ValidateIncludeOuterIntegerStructSequential(sourceIncludeOuterIntegerStructSequential,
                    cloneIncludeOuterIntegerStructSequential, "DelegatePInvoke_MarshalStructIncludeOuterIntegerStructSequentialByVal_StdCall"));
                break;
            case StructID.S11Id:
                S11 sourceS11 = Helper.NewS11((int*)(32), 32);
                S11 cloneS11 = Helper.NewS11((int*)(32), 32);
                Console.WriteLine("Calling DelegatePInvoke_MarshalStructS11ByVal_StdCall...");
                S11ByValDelegateStdCall caller14 = Get_MarshalStructS11ByVal_StdCall_FuncPtr();
                Assert.True(caller14(sourceS11));
                break;
            default:
                Assert.True(false, "TestMethod_DelegatePInvoke_MarshalByVal_StdCall:The structid (Managed Side) is wrong");
                break;
        }
    }

    #endregion

    #region By Ref

    unsafe private static void Run_TestMethod_DelegatePInvoke_MarshalByRef_Cdecl()
    {
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.ComplexStructId);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.InnerSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.InnerArraySequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.CharSetAnsiSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.CharSetUnicodeSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.NumberSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.S3Id);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.S5Id);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.StringStructSequentialAnsiId);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.StringStructSequentialUnicodeId);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.S8Id);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.S9Id);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.IncludeOuterIntegerStructSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_Cdecl(StructID.S11Id);
    }

    unsafe private static void Run_TestMethod_DelegatePInvoke_MarshalByRef_StdCall()
    {
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.ComplexStructId);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.InnerSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.InnerArraySequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.CharSetAnsiSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.CharSetUnicodeSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.NumberSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.S3Id);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.S5Id);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.StringStructSequentialAnsiId);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.StringStructSequentialUnicodeId);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.S8Id);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.S9Id);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.IncludeOuterIntegerStructSequentialId);
        TestMethod_DelegatePInvoke_MarshalByRef_StdCall(StructID.S11Id);
    }

    #endregion

    #region By Value

    unsafe private static void Run_TestMethod_DelegatePInvoke_MarshalByVal_Cdecl()
    {
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.ComplexStructId);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.InnerSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.InnerArraySequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.CharSetAnsiSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.CharSetUnicodeSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.NumberSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.S3Id);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.S5Id);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.StringStructSequentialAnsiId);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.StringStructSequentialUnicodeId);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.S8Id);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.S9Id);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.IncludeOuterIntegerStructSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_Cdecl(StructID.S11Id);
    }

    unsafe private static void Run_TestMethod_DelegatePInvoke_MarshalByVal_StdCall()
    {
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.ComplexStructId);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.InnerSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.InnerArraySequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.CharSetAnsiSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.CharSetUnicodeSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.NumberSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.S3Id);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.S5Id);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.StringStructSequentialAnsiId);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.StringStructSequentialUnicodeId);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.S8Id);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.S9Id);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.IncludeOuterIntegerStructSequentialId);
        TestMethod_DelegatePInvoke_MarshalByVal_StdCall(StructID.S11Id);
    }

    #endregion

    static int Main()
    {
        try{
            Console.WriteLine("\nRun the methods for marshaling structure Delegate P/Invoke ByRef");
            Run_TestMethod_DelegatePInvoke_MarshalByRef_Cdecl();
            Run_TestMethod_DelegatePInvoke_MarshalByRef_StdCall();

            Console.WriteLine("\nRun the methods for marshaling structure Delegate P/Invoke ByVal/n");
            Run_TestMethod_DelegatePInvoke_MarshalByVal_Cdecl();
            Run_TestMethod_DelegatePInvoke_MarshalByVal_StdCall();

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
