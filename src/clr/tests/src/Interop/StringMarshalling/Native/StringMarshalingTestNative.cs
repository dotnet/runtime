// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

class StringMarshalingTestNative
{
#if LPTSTR
    private const string NativeLibraryName = "LPTStrTestNative";
    private const UnmanagedType StringMarshalingType = UnmanagedType.LPTStr;
#elif LPSTR
    private const string NativeLibraryName = "LPStrTestNative";
    private const UnmanagedType StringMarshalingType = UnmanagedType.LPStr;
#elif BSTR
    private const string NativeLibraryName = "BStrTestNative";
    private const UnmanagedType StringMarshalingType = UnmanagedType.BStr;
#elif ANSIBSTR
    private const string NativeLibraryName = "AnsiBStrTestNative";
#pragma warning disable 0618
    private const UnmanagedType StringMarshalingType = UnmanagedType.AnsiBStr;
#pragma warning restore 0618
#else
#error A string marshaling type must be specified.
#endif

    public struct StringInStruct
    {
        [MarshalAs(StringMarshalingType)]
        public string str;
    }

    [DllImport(NativeLibraryName)]
    public static extern bool MatchFunctionName([MarshalAs(StringMarshalingType)] string actual);
    [DllImport(NativeLibraryName)]
    public static extern bool MatchFunctionNameByRef([MarshalAs(StringMarshalingType)] ref string actual);
    [DllImport(NativeLibraryName)]
    public static extern bool MatchFunctionNameInStruct(StringInStruct str);
    [DllImport(NativeLibraryName)]
    public static extern void ReverseInplace([MarshalAs(StringMarshalingType)] StringBuilder str);
    [DllImport(NativeLibraryName)]
    public static extern void ReverseInplaceByrefInStruct(ref StringInStruct str);
    [DllImport(NativeLibraryName)]
    public static extern void ReverseInplaceByref([MarshalAs(StringMarshalingType)] ref string str);
    [DllImport(NativeLibraryName)]
    public static extern void ReverseInplaceByref([MarshalAs(StringMarshalingType)] ref StringBuilder str);
    [DllImport(NativeLibraryName)]
    public static extern void Reverse([MarshalAs(StringMarshalingType)] string original, [MarshalAs(StringMarshalingType)] out string reversed);
    [DllImport(NativeLibraryName)]
    [return: MarshalAs(StringMarshalingType)]
    public static extern string ReverseAndReturn([MarshalAs(StringMarshalingType)] string str);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool VerifyReversedCallback([MarshalAs(StringMarshalingType)] string original, [MarshalAs(StringMarshalingType)] string reversed);
    
    [DllImport(NativeLibraryName)]
    public static extern bool VerifyReversed([MarshalAs(StringMarshalingType)] string str, VerifyReversedCallback callback);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReverseCallback([MarshalAs(StringMarshalingType)] string original, [MarshalAs(StringMarshalingType)] out string reversed);

    [DllImport(NativeLibraryName)]
    public static extern bool ReverseInCallback([MarshalAs(StringMarshalingType)] string str, ReverseCallback callback);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReverseInplaceCallback([MarshalAs(StringMarshalingType)] StringBuilder original);

    [DllImport(NativeLibraryName)]
    public static extern bool ReverseInplaceInCallback([MarshalAs(StringMarshalingType)] StringBuilder str, ReverseInplaceCallback callback);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(StringMarshalingType)]
    public delegate string ReverseCallbackReturned([MarshalAs(StringMarshalingType)] string str);
    
    [DllImport(NativeLibraryName)]
    public static extern bool ReverseInCallbackReturned([MarshalAs(StringMarshalingType)] string str, ReverseCallbackReturned callback);
}
