// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StringMarshalingTests
    {
        private readonly String[] TestStrings = new String[] {
                                    "", //Empty String
                                    "Test String",
                                    "A", //Single character string
                                    string.Concat(Enumerable.Repeat("This is a very long string as it repeats itself. ", 13)),
                                    "This \n is \n a \n multiline \n string",
                                    "This \0 is \0 a \0 string \0 with \0 nulls",
                                    "\0string",
                                    "string\0",
                                    "\0\0\0\0\0\0\0\0"
                                    };

        [Fact]
        public void StringToBStrToString()
        {
            foreach (String ts in TestStrings)
            {
                IntPtr ptr = Marshal.StringToBSTR(ts);
                try
                {
                    String str = Marshal.PtrToStringBSTR(ptr);
                    Assert.Equal(ts, str);
                }
                finally
                {
                    Marshal.FreeBSTR(ptr);
                }
            }
        }

        [Fact]
        public void StringToCoTaskMemAnsiToString()
        {
            foreach (String ts in TestStrings)
            {
                if (ts.Contains("\0"))
                    continue; //Skip the string with nulls case

                IntPtr AnsiStr = Marshal.StringToCoTaskMemAnsi(ts);
                try
                {
                    String str = Marshal.PtrToStringAnsi(AnsiStr);

                    Assert.Equal(ts, str);
                    if (ts.Length > 0)
                    {
                        String str2 = Marshal.PtrToStringAnsi(AnsiStr, ts.Length - 1);
                        Assert.Equal(ts.Substring(0, ts.Length - 1), str2);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(AnsiStr);
                }
            }
        }

        [Fact]
        public void StringToCoTaskMemUniToString()
        {
            foreach (String ts in TestStrings)
            {
                if (ts.Contains("\0"))
                    continue; //Skip the string with nulls case

                IntPtr UniStr = Marshal.StringToCoTaskMemUni(ts);
                try
                {
                    String str = Marshal.PtrToStringUni(UniStr);

                    Assert.Equal(ts, str);
                    if (ts.Length > 0)
                    {
                        String str2 = Marshal.PtrToStringUni(UniStr, ts.Length - 1);
                        Assert.Equal(ts.Substring(0, ts.Length - 1), str2);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(UniStr);
                }
            }
        }

        [Fact]
        public void StringToHGlobalAnsiToString()
        {
            foreach (String ts in TestStrings)
            {
                if (ts.Contains("\0"))
                    continue; //Skip the string with nulls case

                IntPtr AnsiStr = Marshal.StringToHGlobalAnsi(ts);
                try
                {
                    String str = Marshal.PtrToStringAnsi(AnsiStr);

                    Assert.Equal(ts, str);
                    if (ts.Length > 0)
                    {
                        String str2 = Marshal.PtrToStringAnsi(AnsiStr, ts.Length - 1);
                        Assert.Equal(ts.Substring(0, ts.Length - 1), str2);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(AnsiStr);
                }
            }
        }

        [Fact]
        public void StringToHGlobalUniToString()
        {
            foreach (String ts in TestStrings)
            {
                if (ts.Contains("\0"))
                    continue; //Skip the string with nulls case

                IntPtr UniStr = Marshal.StringToHGlobalUni(ts);
                try
                {
                    String str = Marshal.PtrToStringUni(UniStr);

                    Assert.Equal(ts, str);
                    if (ts.Length > 0)
                    {
                        String str2 = Marshal.PtrToStringUni(UniStr, ts.Length - 1);
                        Assert.Equal(ts.Substring(0, ts.Length - 1), str2);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(UniStr);
                }
            }

        }

#if netcoreapp
        [Fact]
        public void TestUTF8String()
        {
            foreach (String srcString in TestStrings)
            {
                // we assume string null terminated
                if (srcString.Contains("\0"))
                    continue;

                IntPtr ptrString = Marshal.StringToCoTaskMemUTF8(srcString);
                try
                {
                    string retString = Marshal.PtrToStringUTF8(ptrString);

                    Assert.Equal(srcString, retString);
                    if (srcString.Length > 0)
                    {
                        string retString2 = Marshal.PtrToStringUTF8(ptrString, srcString.Length - 1);
                        Assert.Equal(srcString.Substring(0, srcString.Length - 1), retString2);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(ptrString);
                }
            }
        }

        [Fact]
        public void TestNullString_UTF8()
        {
            Assert.Null(Marshal.PtrToStringUTF8(IntPtr.Zero));
        }
#endif

        [Fact]
        public void TestNullString()
        {
            Assert.Null(Marshal.PtrToStringUni(IntPtr.Zero));
            Assert.Null(Marshal.PtrToStringAnsi(IntPtr.Zero));
        }
    }
}
