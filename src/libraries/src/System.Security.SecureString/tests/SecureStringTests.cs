// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Xunit;

public static class SecureStringTest
{
    // With the current Unix implementation of SecureString, allocating more than a certain
    // number of pages worth of memory will likely result in OOMs unless in a privileged process.
    private static readonly bool s_isWindowsOrPrivilegedUnix = 
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || 
        Interop.Sys.GetEUid() == 0;

    private static void VerifyString(SecureString ss, string exString)
    {
        IntPtr uniStr = IntPtr.Zero;
        try
        {
            uniStr = SecureStringMarshal.SecureStringToCoTaskMemUnicode(ss);
            string acString = Marshal.PtrToStringUni(uniStr);

            Assert.Equal(exString.Length, acString.Length);
            Assert.Equal(exString, acString);
        }
        finally
        {
            if (uniStr != IntPtr.Zero)
                SecureStringMarshal.ZeroFreeCoTaskMemUnicode(uniStr);
        }
    }

    private static SecureString CreateSecureString(string exValue)
    {
        SecureString ss = null;

        if (string.IsNullOrEmpty(exValue))
            ss = new SecureString();
        else
        {
            unsafe
            {
                fixed (char* mychars = exValue.ToCharArray())
                    ss = new SecureString(mychars, exValue.Length);
            }
        }

        Assert.NotNull(ss);
        return ss;
    }

    private static void CreateAndVerifySecureString(string exValue)
    {
        using (SecureString ss = CreateSecureString(exValue))
        {
            VerifyString(ss, exValue);
        }
    }


    [Fact]
    public static void SecureString_Ctor()
    {
        CreateAndVerifySecureString(string.Empty);
    }

    [Fact]
    public static unsafe void SecureString_Ctor_CharInt()
    {
        // 1. Positive cases
        CreateAndVerifySecureString("test");
        if (s_isWindowsOrPrivilegedUnix)
        {
            CreateAndVerifySecureString(new string('a', UInt16.MaxValue + 1)/*Max allowed length is 65536*/);
        }

        // 2. Negative cases
        Assert.Throws<ArgumentNullException>(() => new SecureString(null, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => { fixed (char* chars = "test") new SecureString(chars, -1); });
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSecureString(new string('a', UInt16.MaxValue + 2 /*65537: Max allowed length is 65536*/)));
    }


    [Fact]
    public static void SecureString_AppendChar()
    {
        using (SecureString testString = CreateSecureString(string.Empty))
        {
            StringBuilder sb = new StringBuilder();

            testString.AppendChar('u');
            sb.Append('u');
            VerifyString(testString, sb.ToString());

            //Append another character.
            testString.AppendChar(char.MaxValue);
            sb.Append(char.MaxValue);
            VerifyString(testString, sb.ToString());
        }

        if (s_isWindowsOrPrivilegedUnix)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { using (SecureString ss = CreateSecureString(new string('a', UInt16.MaxValue + 1))) ss.AppendChar('a'); });
        }
        Assert.Throws<InvalidOperationException>(() => { using (SecureString ss = CreateSecureString(string.Empty)) { ss.MakeReadOnly(); ss.AppendChar('k'); } });
    }

    [Fact]
    public static void SecureString_Clear()
    {
        String exString = String.Empty;
        using (SecureString testString = CreateSecureString(exString))
        {
            testString.Clear();
            VerifyString(testString, exString);
        }

        using (SecureString testString = CreateSecureString("test"))
        {
            testString.Clear();
            VerifyString(testString, String.Empty);
        }

        // Check if readOnly
        Assert.Throws<InvalidOperationException>(() => { using (SecureString ss = new SecureString()) { ss.MakeReadOnly(); ss.Clear(); } });
        // Check if secureString has been disposed.
        Assert.Throws<ObjectDisposedException>(() => { using (SecureString ss = CreateSecureString(new string('a', 100))) { ss.Dispose(); ss.Clear(); } });
    }

    [Fact]
    public static void SecureString_Copy()
    {
        string exString = new string('a', 4000);
        using (SecureString testString = CreateSecureString(exString))
        {
            using (SecureString copy_string = testString.Copy())
                VerifyString(copy_string, exString);
        }

        //ObjectDisposedException.
        {
            SecureString testString = CreateSecureString("SomeValue");
            testString.Dispose();
            Assert.Throws<ObjectDisposedException>(() => testString.Copy());
        }

        //Check for ReadOnly.
        exString = "test";
        using (SecureString testString = CreateSecureString(exString))
        {
            testString.MakeReadOnly();
            using (SecureString copy_string = testString.Copy())
            {
                VerifyString(copy_string, exString);
                Assert.True(testString.IsReadOnly());
                Assert.False(copy_string.IsReadOnly());
            }
        }
    }


    [Fact]
    public static void SecureString_InsertAt()
    {
        using (SecureString testString = CreateSecureString("bd"))
        {
            testString.InsertAt(0, 'a');
            VerifyString(testString, "abd");

            testString.InsertAt(3, 'e');
            VerifyString(testString, "abde");

            testString.InsertAt(2, 'c');
            VerifyString(testString, "abcde");

            Assert.Throws<ArgumentOutOfRangeException>(() => testString.InsertAt(-1, 'S'));
            Assert.Throws<ArgumentOutOfRangeException>(() => testString.InsertAt(6, 'S'));
        }

        if (s_isWindowsOrPrivilegedUnix)
        {
            using (SecureString testString = CreateSecureString(new string('a', UInt16.MaxValue + 1)))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => testString.InsertAt(22, 'S'));
            }
        }

        using (SecureString testString = CreateSecureString("test"))
        {
            testString.MakeReadOnly();
            Assert.Throws<InvalidOperationException>(() => testString.InsertAt(2, 'S'));
        }

        {
            SecureString testString = CreateSecureString("test");
            testString.Dispose();
            Assert.Throws<ObjectDisposedException>(() => testString.InsertAt(2, 'S'));
        }
    }


    [Fact]
    public static void SecureString_IsReadOnly()
    {
        using (SecureString testString = CreateSecureString("test"))
        {
            Assert.False(testString.IsReadOnly());

            testString.MakeReadOnly();
            Assert.True(testString.IsReadOnly());
        }

        {
            SecureString testString = CreateSecureString("test");
            testString.Dispose();
            Assert.Throws<ObjectDisposedException>(() => testString.IsReadOnly());
        }
    }


    [Fact]
    public static void SecureString_MakeReadOnly()
    {
        using (SecureString testString = CreateSecureString("test"))
        {
            Assert.False(testString.IsReadOnly());

            testString.MakeReadOnly();
            Assert.True(testString.IsReadOnly());

            testString.MakeReadOnly();
            Assert.True(testString.IsReadOnly());
        }

        {
            SecureString testString = CreateSecureString("test");
            testString.Dispose();
            Assert.Throws<ObjectDisposedException>(() => testString.MakeReadOnly());
        }
    }

    [Fact]
    public static void SecureString_RemoveAt()
    {
        using (SecureString testString = CreateSecureString("abcde"))
        {
            testString.RemoveAt(3);
            VerifyString(testString, "abce");

            testString.RemoveAt(3);
            VerifyString(testString, "abc");

            testString.RemoveAt(0);
            VerifyString(testString, "bc");

            testString.RemoveAt(1);
            VerifyString(testString, "b");

            testString.RemoveAt(0);
            VerifyString(testString, "");

            testString.AppendChar('f');
            VerifyString(testString, "f");

            testString.AppendChar('g');
            VerifyString(testString, "fg");

            testString.RemoveAt(0);
            VerifyString(testString, "g");
        }

        if (s_isWindowsOrPrivilegedUnix)
        {
            using (SecureString testString = CreateSecureString(new string('a', UInt16.MaxValue + 1)))
            {
                testString.RemoveAt(22);
                testString.AppendChar('a');
                VerifyString(testString, new string('a', UInt16.MaxValue + 1));
            }
        }

        using (SecureString testString = CreateSecureString("test"))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => testString.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => testString.RemoveAt(testString.Length));
            Assert.Throws<ArgumentOutOfRangeException>(() => testString.RemoveAt(testString.Length + 1));

            testString.MakeReadOnly();
            Assert.Throws<InvalidOperationException>(() => testString.RemoveAt(0));
        }

        {
            SecureString testString = CreateSecureString("test");
            testString.Dispose();
            Assert.Throws<ObjectDisposedException>(() => testString.RemoveAt(0));
        }
    }

    [Fact]
    public static void SecureString_SetAt()
    {
        using (SecureString testString = CreateSecureString("abc"))
        {
            testString.SetAt(2, 'f');
            VerifyString(testString, "abf");

            testString.SetAt(0, 'd');
            VerifyString(testString, "dbf");

            testString.SetAt(1, 'e');
            VerifyString(testString, "def");
        }

        if (s_isWindowsOrPrivilegedUnix)
        {
            string exString = new string('a', UInt16.MaxValue + 1);
            using (SecureString testString = CreateSecureString(exString))
            {
                testString.SetAt(22, 'b');
                char[] chars = exString.ToCharArray();
                chars[22] = 'b';
                VerifyString(testString, new string(chars));
            }
        }

        using (SecureString testString = CreateSecureString("test"))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => testString.SetAt(-1, 'a'));
            Assert.Throws<ArgumentOutOfRangeException>(() => testString.SetAt(testString.Length, 'b'));
            Assert.Throws<ArgumentOutOfRangeException>(() => testString.SetAt(testString.Length + 1, 'c'));

            testString.MakeReadOnly();
            Assert.Throws<InvalidOperationException>(() => testString.SetAt(0, 'd'));
        }

        {
            SecureString testString = CreateSecureString("test");
            testString.Dispose();
            Assert.Throws<ObjectDisposedException>(() => testString.SetAt(0, 'e'));
        }
    }

    [Fact]
    public static void SecureStringMarshal_ArgValidation()
    {
        Assert.Throws<ArgumentNullException>(() => SecureStringMarshal.SecureStringToCoTaskMemUnicode(null));
    }

    [Fact]
    public static void SecureString_RepeatedCtorDispose()
    {
        string str = new string('a', 4000);
        for (int i = 0; i < 1000; i++)
        {
            CreateSecureString(str).Dispose();
        }
    }

    [Fact]
    [OuterLoop]
    public static void SecureString_Growth()
    {
        string starting = new string('a', 6000);
        StringBuilder sb = new StringBuilder(starting);
        using (SecureString testString = CreateSecureString(starting))
        {
            for (int i = 0; i < 4000; i++)
            {
                char c = (char)('a' + (i % 26));
                testString.AppendChar(c);
                sb.Append(c);
            }
            VerifyString(testString, sb.ToString());
        }
    }

}
