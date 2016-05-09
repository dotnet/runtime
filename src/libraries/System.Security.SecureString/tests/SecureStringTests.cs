// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Security.Tests
{
    public static class SecureStringTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(ushort.MaxValue + 1)] // max allowed length
        public static void Ctor(int length)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append((char)('a' + (i % 26)));
            }
            string expected = sb.ToString();

            using (SecureString actual = CreateSecureString(expected))
            {
                AssertEquals(expected, actual);
            }
        }

        [Fact]
        public static unsafe void Ctor_CharInt_Invalid()
        {
            Assert.Throws<ArgumentNullException>("value", () => new SecureString(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>("length", () => { fixed (char* chars = "test") new SecureString(chars, -1); });
            Assert.Throws<ArgumentOutOfRangeException>("length", () => CreateSecureString(new string('a', ushort.MaxValue + 2 /*65537: Max allowed length is 65536*/)));
        }

        [Fact]
        public static void AppendChar()
        {
            using (SecureString testString = CreateSecureString(string.Empty))
            {
                var expected = new StringBuilder();
                foreach (var ch in new[] { 'a', 'b', 'c', 'd' })
                {
                    testString.AppendChar(ch);
                    expected.Append(ch);
                    AssertEquals(expected.ToString(), testString);
                }
            }
        }

        [Fact]
        public static void AppendChar_TooLong_Throws()
        {
            using (SecureString ss = CreateSecureString(new string('a', ushort.MaxValue + 1)))
            {
                Assert.Throws<ArgumentOutOfRangeException>("capacity", () => ss.AppendChar('a'));
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("test")]
        public static void Clear(string initialValue)
        {
            using (SecureString testString = CreateSecureString(initialValue))
            {
                testString.Clear();
                AssertEquals(string.Empty, testString);
            }
        }

        [Fact]
        public static void MakeReadOnly_AllOtherModificationsThrow()
        {
            using (SecureString ss = CreateSecureString("test"))
            {
                Assert.False(ss.IsReadOnly());
                ss.MakeReadOnly();
                Assert.True(ss.IsReadOnly());
                Assert.Throws<InvalidOperationException>(() => ss.AppendChar('a'));
                Assert.Throws<InvalidOperationException>(() => ss.Clear());
                Assert.Throws<InvalidOperationException>(() => ss.InsertAt(0, 'a'));
                Assert.Throws<InvalidOperationException>(() => ss.RemoveAt(0));
                Assert.Throws<InvalidOperationException>(() => ss.SetAt(0, 'a'));
                ss.MakeReadOnly();
            }
        }

        [Fact]
        public static void Dispose_AllOtherOperationsThrow()
        {
            SecureString ss = CreateSecureString("test");
            ss.Dispose();
            Assert.Throws<ObjectDisposedException>(() => ss.AppendChar('a'));
            Assert.Throws<ObjectDisposedException>(() => ss.Clear());
            Assert.Throws<ObjectDisposedException>(() => ss.Copy());
            Assert.Throws<ObjectDisposedException>(() => ss.InsertAt(0, 'a'));
            Assert.Throws<ObjectDisposedException>(() => ss.IsReadOnly());
            Assert.Throws<ObjectDisposedException>(() => ss.Length);
            Assert.Throws<ObjectDisposedException>(() => ss.MakeReadOnly());
            Assert.Throws<ObjectDisposedException>(() => ss.RemoveAt(0));
            Assert.Throws<ObjectDisposedException>(() => ss.SetAt(0, 'a'));
            ss.Dispose();
        }

        [Fact]
        public static void Copy()
        {
            string expected = new string('a', 4000);
            using (SecureString testString = CreateSecureString(expected))
            using (SecureString copyString = testString.Copy())
            {
                Assert.False(copyString.IsReadOnly());
                AssertEquals(expected, copyString);
            }

            using (SecureString testString = CreateSecureString(expected))
            {
                testString.MakeReadOnly();
                using (SecureString copyString = testString.Copy())
                {
                    Assert.False(copyString.IsReadOnly());
                    AssertEquals(expected, copyString);
                }
            }
        }

        [Fact]
        public static void InsertAt()
        {
            using (SecureString testString = CreateSecureString("bd"))
            {
                testString.InsertAt(0, 'a');
                AssertEquals("abd", testString);

                testString.InsertAt(3, 'e');
                AssertEquals("abde", testString);

                testString.InsertAt(2, 'c');
                AssertEquals("abcde", testString);
            }
        }

        [Fact]
        public static void InsertAt_Invalid_Throws()
        {
            using (SecureString testString = CreateSecureString("bd"))
            {
                Assert.Throws<ArgumentOutOfRangeException>("index", () => testString.InsertAt(-1, 'S'));
                Assert.Throws<ArgumentOutOfRangeException>("index", () => testString.InsertAt(6, 'S'));
            }

            using (SecureString testString = CreateSecureString(new string('a', ushort.MaxValue + 1)))
            {
                Assert.Throws<ArgumentOutOfRangeException>("capacity", () => testString.InsertAt(22, 'S'));
            }
        }

        [Fact]
        public static void RemoveAt()
        {
            using (SecureString testString = CreateSecureString("abcde"))
            {
                testString.RemoveAt(3);
                AssertEquals("abce", testString);

                testString.RemoveAt(3);
                AssertEquals("abc", testString);

                testString.RemoveAt(0);
                AssertEquals("bc", testString);

                testString.RemoveAt(1);
                AssertEquals("b", testString);

                testString.RemoveAt(0);
                AssertEquals("", testString);

                testString.AppendChar('f');
                AssertEquals("f", testString);

                testString.AppendChar('g');
                AssertEquals("fg", testString);

                testString.RemoveAt(0);
                AssertEquals("g", testString);
            }

            using (SecureString testString = CreateSecureString(new string('a', ushort.MaxValue + 1)))
            {
                testString.RemoveAt(22);
                testString.AppendChar('a');
                AssertEquals(new string('a', ushort.MaxValue + 1), testString);
            }
        }

        [Fact]
        public static void RemoveAt_Invalid_Throws()
        {
            using (SecureString testString = CreateSecureString("test"))
            {
                Assert.Throws<ArgumentOutOfRangeException>("index", () => testString.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>("index", () => testString.RemoveAt(testString.Length));
                Assert.Throws<ArgumentOutOfRangeException>("index", () => testString.RemoveAt(testString.Length + 1));
            }
        }

        [Fact]
        public static void SetAt()
        {
            using (SecureString testString = CreateSecureString("abc"))
            {
                testString.SetAt(2, 'f');
                AssertEquals("abf", testString);

                testString.SetAt(0, 'd');
                AssertEquals("dbf", testString);

                testString.SetAt(1, 'e');
                AssertEquals("def", testString);
            }

            string expected = new string('a', ushort.MaxValue + 1);
            using (SecureString testString = CreateSecureString(expected))
            {
                testString.SetAt(22, 'b');
                char[] chars = expected.ToCharArray();
                chars[22] = 'b';
                AssertEquals(new string(chars), testString);
            }
        }

        [Fact]
        public static void SetAt_Invalid_Throws()
        {
            using (SecureString testString = CreateSecureString("test"))
            {
                Assert.Throws<ArgumentOutOfRangeException>("index", () => testString.SetAt(-1, 'a'));
                Assert.Throws<ArgumentOutOfRangeException>("index", () => testString.SetAt(testString.Length, 'b'));
                Assert.Throws<ArgumentOutOfRangeException>("index", () => testString.SetAt(testString.Length + 1, 'c'));
            }
        }

        [Fact]
        public static void SecureStringMarshal_NullArgsAllowed_IntPtrZero()
        {
            Assert.Equal(IntPtr.Zero, SecureStringMarshal.SecureStringToCoTaskMemAnsi(null));
            Assert.Equal(IntPtr.Zero, SecureStringMarshal.SecureStringToCoTaskMemUnicode(null));
            Assert.Equal(IntPtr.Zero, SecureStringMarshal.SecureStringToGlobalAllocAnsi(null));
            Assert.Equal(IntPtr.Zero, SecureStringMarshal.SecureStringToGlobalAllocUnicode(null));
        }

        [Fact]
        public static void RepeatedCtorDispose()
        {
            string str = new string('a', 4000);
            for (int i = 0; i < 1000; i++)
            {
                CreateSecureString(str).Dispose();
            }
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(1000, false)]
        [InlineData(1000, true)]
        public static void SecureStringMarshal_Ansi_Roundtrip(int length, bool allocHGlobal)
        {
            string input = new string(Enumerable.Range(0, length).Select(i => (char)('a' + i)).ToArray());

            IntPtr marshaledString = Marshal.StringToHGlobalAnsi(input);
            string expectedAnsi = Marshal.PtrToStringAnsi(marshaledString);
            Marshal.FreeHGlobal(marshaledString);

            using (SecureString ss = CreateSecureString(input))
            {
                IntPtr marshaledSecureString = allocHGlobal ? 
                    SecureStringMarshal.SecureStringToGlobalAllocAnsi(ss) :
                    SecureStringMarshal.SecureStringToCoTaskMemAnsi(ss);

                string actualAnsi = Marshal.PtrToStringAnsi(marshaledSecureString);

                if (allocHGlobal)
                {
                    Marshal.FreeHGlobal(marshaledSecureString);
                }
                else
                {
                    Marshal.FreeCoTaskMem(marshaledSecureString);
                }

                Assert.Equal(expectedAnsi, actualAnsi);
            }
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(1000, false)]
        [InlineData(1000, true)]
        public static void SecureStringMarshal_Unicode_Roundtrip(int length, bool allocHGlobal)
        {
            string input = new string(Enumerable.Range(0, length).Select(i => (char)('a' + i)).ToArray());

            IntPtr marshaledString = Marshal.StringToHGlobalUni(input);
            string expectedAnsi = Marshal.PtrToStringUni(marshaledString);
            Marshal.FreeHGlobal(marshaledString);

            using (SecureString ss = CreateSecureString(input))
            {
                IntPtr marshaledSecureString = allocHGlobal ?
                    SecureStringMarshal.SecureStringToGlobalAllocUnicode(ss) :
                    SecureStringMarshal.SecureStringToCoTaskMemUnicode(ss);

                string actualAnsi = Marshal.PtrToStringUni(marshaledSecureString);

                if (allocHGlobal)
                {
                    Marshal.FreeHGlobal(marshaledSecureString);
                }
                else
                {
                    Marshal.FreeCoTaskMem(marshaledSecureString);
                }

                Assert.Equal(expectedAnsi, actualAnsi);
            }
        }

        [Fact]
        [OuterLoop]
        public static void Stress_Growth()
        {
            string starting = new string('a', 6000);
            var sb = new StringBuilder(starting);
            using (SecureString testString = CreateSecureString(starting))
            {
                for (int i = 0; i < 4000; i++)
                {
                    char c = (char)('a' + (i % 26));
                    testString.AppendChar(c);
                    sb.Append(c);
                }
                AssertEquals(sb.ToString(), testString);
            }
        }

        private static unsafe void AssertEquals(string expected, SecureString actual)
        {
            Assert.Equal(expected, CreateString(actual));
        }

        // WARNING:
        // A key value of SecureString is in keeping string data off of the GC heap, such that it can
        // be reliably cleared when no longer needed.  Creating a SecureString from a string or converting
        // a SecureString to a string diminishes that value. These conversion functions are for testing that 
        // SecureString works, and does not represent a pattern to follow in any non-test situation.

        private static unsafe SecureString CreateSecureString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new SecureString();
            }

            fixed (char* mychars = value.ToCharArray())
            {
                return new SecureString(mychars, value.Length);
            }
        }

        private static string CreateString(SecureString value)
        {
            IntPtr ptr = SecureStringMarshal.SecureStringToGlobalAllocUnicode(value);
            try
            {
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}
