// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;

#pragma warning disable 618 // Must test deprecated features

[ComVisible(true)]
[Guid(Server.Contract.Guids.StringTesting)]
public class StringTesting : Server.Contract.IStringTesting
{
    private static string Reverse(string s)
    {
        var chars = s.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    [return: MarshalAs(UnmanagedType.LPStr)]
    public string Add_LPStr(
        [MarshalAs(UnmanagedType.LPStr)] string a,
        [MarshalAs(UnmanagedType.LPStr)] string b)
    {
        return a + b;
    }

    [return: MarshalAs(UnmanagedType.LPWStr)]
    public string Add_LPWStr(
        [MarshalAs(UnmanagedType.LPWStr)] string a,
        [MarshalAs(UnmanagedType.LPWStr)] string b)
    {
        return a + b;
    }

    [return: MarshalAs(UnmanagedType.BStr)]
    public string Add_BStr(
        [MarshalAs(UnmanagedType.BStr)] string a,
        [MarshalAs(UnmanagedType.BStr)] string b)
    {
        return a + b;
    }

    // LPStr

    [return: MarshalAs(UnmanagedType.LPStr)]
    public string Reverse_LPStr([MarshalAs(UnmanagedType.LPStr)] string a)
    {
        return Reverse(a);
    }

    [return: MarshalAs(UnmanagedType.LPStr)]
    public string Reverse_LPStr_Ref([MarshalAs(UnmanagedType.LPStr)] ref string a)
    {
        return Reverse(a);
    }

    [return: MarshalAs(UnmanagedType.LPStr)]
    public string Reverse_LPStr_InRef([In][MarshalAs(UnmanagedType.LPStr)] ref string a)
    {
        return Reverse(a);
    }

    public void Reverse_LPStr_Out([MarshalAs(UnmanagedType.LPStr)] string a, [MarshalAs(UnmanagedType.LPStr)] out string b)
    {
        b = Reverse(a);
    }

    public void Reverse_LPStr_OutAttr([MarshalAs(UnmanagedType.LPStr)] string a, [Out][MarshalAs(UnmanagedType.LPStr)] string b)
    {
        b = Reverse(a);
    }

    [return: MarshalAs(UnmanagedType.LPStr)]
    public StringBuilder Reverse_SB_LPStr([MarshalAs(UnmanagedType.LPStr)] StringBuilder a)
    {
        return new StringBuilder(Reverse(a.ToString()));
    }

    [return: MarshalAs(UnmanagedType.LPStr)]
    public StringBuilder Reverse_SB_LPStr_Ref([MarshalAs(UnmanagedType.LPStr)] ref StringBuilder a)
    {
        return new StringBuilder(Reverse(a.ToString()));
    }

    [return: MarshalAs(UnmanagedType.LPStr)]
    public StringBuilder Reverse_SB_LPStr_InRef([In][MarshalAs(UnmanagedType.LPStr)] ref StringBuilder a)
    {
        return new StringBuilder(Reverse(a.ToString()));
    }

    public void Reverse_SB_LPStr_Out([MarshalAs(UnmanagedType.LPStr)] StringBuilder a, [MarshalAs(UnmanagedType.LPStr)] out StringBuilder b)
    {
        b = new StringBuilder(Reverse(a.ToString()));
    }

    public void Reverse_SB_LPStr_OutAttr([MarshalAs(UnmanagedType.LPStr)] StringBuilder a, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder b)
    {
        b.Append(Reverse(a.ToString()));
    }

    // LPWStr

    [return: MarshalAs(UnmanagedType.LPWStr)]
    public string Reverse_LPWStr([MarshalAs(UnmanagedType.LPWStr)] string a)
    {
        return Reverse(a);
    }

    [return: MarshalAs(UnmanagedType.LPWStr)]
    public string Reverse_LPWStr_Ref([MarshalAs(UnmanagedType.LPWStr)] ref string a)
    {
        return Reverse(a);
    }

    [return: MarshalAs(UnmanagedType.LPWStr)]
    public string Reverse_LPWStr_InRef([In][MarshalAs(UnmanagedType.LPWStr)] ref string a)
    {
        return Reverse(a);
    }

    public void Reverse_LPWStr_Out([MarshalAs(UnmanagedType.LPWStr)] string a, [MarshalAs(UnmanagedType.LPWStr)] out string b)
    {
        b = Reverse(a);
    }

    // This behavior is the "desired" behavior for a string passed by-value with an [Out] attribute.
    // However, block calling a COM or P/Invoke stub with an "[Out] string" parameter since that would allow users to
    // edit an immutable string value in place. So, in the NetClient.Primitives.StringTests tests, we expect a MarshalDirectiveException.
    public void Reverse_LPWStr_OutAttr([MarshalAs(UnmanagedType.LPWStr)] string a, [Out][MarshalAs(UnmanagedType.LPWStr)] string b)
    {
        b = Reverse(a);
    }

    [return: MarshalAs(UnmanagedType.LPWStr)]
    public StringBuilder Reverse_SB_LPWStr([MarshalAs(UnmanagedType.LPWStr)] StringBuilder a)
    {
        return new StringBuilder(Reverse(a.ToString()));
    }

    [return: MarshalAs(UnmanagedType.LPWStr)]
    public StringBuilder Reverse_SB_LPWStr_Ref([MarshalAs(UnmanagedType.LPWStr)] ref StringBuilder a)
    {
        return new StringBuilder(Reverse(a.ToString()));
    }

    [return: MarshalAs(UnmanagedType.LPWStr)]
    public StringBuilder Reverse_SB_LPWStr_InRef([In][MarshalAs(UnmanagedType.LPWStr)] ref StringBuilder a)
    {
        return new StringBuilder(Reverse(a.ToString()));
    }

    public void Reverse_SB_LPWStr_Out([MarshalAs(UnmanagedType.LPWStr)] StringBuilder a, [MarshalAs(UnmanagedType.LPWStr)] out StringBuilder b)
    {
        b = new StringBuilder(Reverse(a.ToString()));
    }

    public void Reverse_SB_LPWStr_OutAttr([MarshalAs(UnmanagedType.LPWStr)] StringBuilder a, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder b)
    {
        b.Append(Reverse(a.ToString()));
    }

    // BSTR

    [return: MarshalAs(UnmanagedType.BStr)]
    public string Reverse_BStr([MarshalAs(UnmanagedType.BStr)] string a)
    {
        return Reverse(a);
    }

    [return: MarshalAs(UnmanagedType.BStr)]
    public string Reverse_BStr_Ref([MarshalAs(UnmanagedType.BStr)] ref string a)
    {
        return Reverse(a);
    }

    [return: MarshalAs(UnmanagedType.BStr)]
    public string Reverse_BStr_InRef([In][MarshalAs(UnmanagedType.BStr)] ref string a)
    {
        return Reverse(a);
    }

    public void Reverse_BStr_Out([MarshalAs(UnmanagedType.BStr)] string a, [MarshalAs(UnmanagedType.BStr)] out string b)
    {
        b = Reverse(a);
    }

    public void Reverse_BStr_OutAttr([MarshalAs(UnmanagedType.BStr)] string a, [Out][MarshalAs(UnmanagedType.BStr)] string b)
    {
        b = Reverse(a);
    }

    
    [LCIDConversion(1)]
    [return:MarshalAs(UnmanagedType.LPWStr)]
    public string Reverse_LPWStr_With_LCID([MarshalAs(UnmanagedType.LPWStr)] string a)
    {
        return Reverse(a);
    }

    [LCIDConversion(0)]
    public void Pass_Through_LCID(out int lcid)
    {
        lcid = CultureInfo.CurrentCulture.LCID;
    }
}
