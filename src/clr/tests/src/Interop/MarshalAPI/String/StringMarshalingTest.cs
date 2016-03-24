// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;

public class StringMarshalingTest
{
    private readonly String[] TestStrings = new String[] {
                                    "", //Empty String
                                    "Test String",
                                    "A", //Single character string
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself. " +
                                    "This is a very long string as it repeats itself.",
                                    "This \n is \n a \n multiline \n string",
                                    "This \0 is \0 a \0 string \0 with \0 nulls",
                                    "\0string",
                                    "string\0",
                                    "\0\0\0\0\0\0\0\0"
                                    };


    private void StringToBStrToString()
    {
        foreach (String ts in TestStrings)
        {

            IntPtr BStr = Marshal.StringToBSTR(ts);
            String str = Marshal.PtrToStringBSTR(BStr);

            if (!str.Equals(ts))
            {
                throw new Exception();
            }
            Marshal.FreeBSTR(BStr);


        }
    }

    private void StringToCoTaskMemAnsiToString()
    {
        foreach (String ts in TestStrings)
        {
            if (ts.Contains("\0"))
                continue; //Skip the string with nulls case


            IntPtr AnsiStr = Marshal.StringToCoTaskMemAnsi(ts);
            String str = Marshal.PtrToStringAnsi(AnsiStr);

            if (!str.Equals(ts))
            {
                throw new Exception();
            }
            if (ts.Length > 0)
            {
                String str2 = Marshal.PtrToStringAnsi(AnsiStr, ts.Length - 1);

                if (!str2.Equals(ts.Substring(0, ts.Length - 1)))
                {
                    throw new Exception();
                }
            }
            Marshal.FreeCoTaskMem(AnsiStr);


        }
    }

    private void StringToCoTaskMemUniToString()
    {
        foreach (String ts in TestStrings)
        {
            if (ts.Contains("\0"))
                continue; //Skip the string with nulls case


            IntPtr UniStr = Marshal.StringToCoTaskMemUni(ts);
            String str = Marshal.PtrToStringUni(UniStr);

            if (!str.Equals(ts))
            {
                throw new Exception();
            }
            if (ts.Length > 0)
            {
                String str2 = Marshal.PtrToStringUni(UniStr, ts.Length - 1);

                if (!str2.Equals(ts.Substring(0, ts.Length - 1)))
                {
                    throw new Exception();
                }
            }
            Marshal.FreeCoTaskMem(UniStr);


        }
    }

    private void StringToHGlobalAnsiToString()
    {
        foreach (String ts in TestStrings)
        {
            if (ts.Contains("\0"))
                continue; //Skip the string with nulls case

            IntPtr AnsiStr = Marshal.StringToHGlobalAnsi(ts);
            String str = Marshal.PtrToStringAnsi(AnsiStr);

            if (!str.Equals(ts))
            {
                throw new Exception();
            }
            if (ts.Length > 0)
            {
                String str2 = Marshal.PtrToStringAnsi(AnsiStr, ts.Length - 1);

                if (!str2.Equals(ts.Substring(0, ts.Length - 1)))
                {
                    throw new Exception();
                }
            }
            Marshal.FreeHGlobal(AnsiStr);



        }
    }

    private void StringToHGlobalUniToString()
    {
        foreach (String ts in TestStrings)
        {
            if (ts.Contains("\0"))
                continue; //Skip the string with nulls case


            IntPtr UniStr = Marshal.StringToHGlobalUni(ts);
            String str = Marshal.PtrToStringUni(UniStr);

            if (!str.Equals(ts))
            {
                throw new Exception();
            }
            if (ts.Length > 0)
            {
                String str2 = Marshal.PtrToStringUni(UniStr, ts.Length - 1);

                if (!str2.Equals(ts.Substring(0, ts.Length - 1)))
                {
                    throw new Exception();
                }
            }
            Marshal.FreeHGlobal(UniStr);
        }

    }


    public  bool RunTests()
    {
        StringToBStrToString();
        StringToCoTaskMemAnsiToString();
        StringToCoTaskMemUniToString();
        StringToHGlobalAnsiToString();
        StringToHGlobalUniToString();
        return true;
    }

    public static int Main(String[] unusedArgs)
    {
        return new StringMarshalingTest().RunTests() ? 100 : 99;
    }

}
