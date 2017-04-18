// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
            string expected = CreateString(length);
            using (SecureString actual = CreateSecureString(expected))
            {
                AssertEquals(expected, actual);
            }
        }

        [Fact]
        public static unsafe void Ctor_CharInt_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new SecureString(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => { fixed (char* chars = "test") new SecureString(chars, -1); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => CreateSecureString(CreateString(ushort.MaxValue + 2 /*65537: Max allowed length is 65536*/)));
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
                AssertEquals(expected.ToString(), testString); // check last case one more time for idempotency
            }
        }

        [Fact]
        public static void AppendChar_TooLong_Throws()
        {
            using (SecureString ss = CreateSecureString(CreateString(ushort.MaxValue + 1)))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => ss.AppendChar('a'));
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("test")]
        public static void Clear(string initialValue)
        {
            using (SecureString testString = CreateSecureString(initialValue))
            {
                AssertEquals(initialValue, testString);
                testString.Clear();
                AssertEquals(string.Empty, testString);
            }
        }

        [Fact]
        public static void MakeReadOnly_ReadingSucceeds_AllOtherModificationsThrow()
        {
            string initialValue = "test";
            using (SecureString ss = CreateSecureString(initialValue))
            {
                Assert.False(ss.IsReadOnly());

                ss.MakeReadOnly();
                Assert.True(ss.IsReadOnly());

                // Reads succeed
                AssertEquals(initialValue, ss);
                Assert.Equal(initialValue.Length, ss.Length);
                using (SecureString other = ss.Copy())
                {
                    AssertEquals(initialValue, other);
                }
                ss.MakeReadOnly(); // ok to call again

                // Writes throw
                Assert.Throws<InvalidOperationException>(() => ss.AppendChar('a'));
                Assert.Throws<InvalidOperationException>(() => ss.Clear());
                Assert.Throws<InvalidOperationException>(() => ss.InsertAt(0, 'a'));
                Assert.Throws<InvalidOperationException>(() => ss.RemoveAt(0));
                Assert.Throws<InvalidOperationException>(() => ss.SetAt(0, 'a'));
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
            Assert.Throws<ObjectDisposedException>(() => SecureStringMarshal.SecureStringToCoTaskMemAnsi(ss));
            Assert.Throws<ObjectDisposedException>(() => SecureStringMarshal.SecureStringToCoTaskMemUnicode(ss));
            Assert.Throws<ObjectDisposedException>(() => SecureStringMarshal.SecureStringToGlobalAllocAnsi(ss));
            Assert.Throws<ObjectDisposedException>(() => SecureStringMarshal.SecureStringToGlobalAllocUnicode(ss));

            ss.Dispose(); // ok to call again
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4000)]
        public static void Copy(int length)
        {
            string expected = CreateString(length);
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
        public static void InsertAt_LongString()
        {
            string initialValue = CreateString(ushort.MaxValue);

            for (int iter = 0; iter < 2; iter++)
            {
                using (SecureString testString = CreateSecureString(initialValue))
                {
                    string expected = initialValue;
                    AssertEquals(expected, testString);

                    if (iter == 0) // add at the beginning
                    {
                        expected = 'b' + expected;
                        testString.InsertAt(0, 'b');
                    }
                    else // add at the end
                    {
                        expected += 'b';
                        testString.InsertAt(testString.Length, 'b');
                    }

                    AssertEquals(expected, testString);
                }
            }
        }

        [Fact]
        public static void InsertAt_Invalid_Throws()
        {
            using (SecureString testString = CreateSecureString("bd"))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => testString.InsertAt(-1, 'S'));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => testString.InsertAt(6, 'S'));
            }

            using (SecureString testString = CreateSecureString(CreateString(ushort.MaxValue + 1)))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => testString.InsertAt(22, 'S'));
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
        }

        [Fact]
        public static void RemoveAt_Largest()
        {
            string expected = CreateString(ushort.MaxValue + 1);
            using (SecureString testString = CreateSecureString(expected))
            {
                testString.RemoveAt(22);
                expected = expected.Substring(0, 22) + expected.Substring(23);
                AssertEquals(expected, testString);
            }
        }

        [Fact]
        public static void RemoveAt_Invalid_Throws()
        {
            using (SecureString testString = CreateSecureString("test"))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => testString.RemoveAt(-1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => testString.RemoveAt(testString.Length));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => testString.RemoveAt(testString.Length + 1));
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

            string expected = CreateString(ushort.MaxValue + 1);
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
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => testString.SetAt(-1, 'a'));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => testString.SetAt(testString.Length, 'b'));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => testString.SetAt(testString.Length + 1, 'c'));
            }
        }

        [Fact]
        public static void SecureStringMarshal_NullArgsAllowed_IntPtrZero()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => SecureStringMarshal.SecureStringToCoTaskMemAnsi(null));
            AssertExtensions.Throws<ArgumentNullException>("s", () => SecureStringMarshal.SecureStringToCoTaskMemUnicode(null));
            AssertExtensions.Throws<ArgumentNullException>("s", () => SecureStringMarshal.SecureStringToGlobalAllocAnsi(null));
            AssertExtensions.Throws<ArgumentNullException>("s", () => SecureStringMarshal.SecureStringToGlobalAllocUnicode(null));
        }

        [Fact]
        public static void RepeatedCtorDispose()
        {
            string str = CreateString(4000);
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
            string input = new string(Enumerable
                .Range(0, length)
                .Select(i => (char)('a' + i)) // include non-ASCII chars
                .ToArray());

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
            string input = new string(Enumerable
                .Range(0, length)
                .Select(i => (char)('a' + i)) // include non-ASCII chars
                .ToArray());

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
        public static void GrowAndContract_Small()
        {
            var rand = new Random(42);
            var sb = new StringBuilder(string.Empty);

            using (SecureString testString = CreateSecureString(string.Empty))
            {
                for (int loop = 0; loop < 3; loop++)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        char c = (char)('a' + rand.Next(0, 26));
                        int addPos = rand.Next(0, sb.Length);
                        testString.InsertAt(addPos, c);
                        sb.Insert(addPos, c);
                        AssertEquals(sb.ToString(), testString);
                    }
                    while (sb.Length > 0)
                    {
                        int removePos = rand.Next(0, sb.Length);
                        testString.RemoveAt(removePos);
                        sb.Remove(removePos, 1);
                        AssertEquals(sb.ToString(), testString);
                    }
                }
            }
        }

        [Fact]
        public static void Grow_Large()
        {
            string starting = CreateString(6000);
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

        [OuterLoop]
        [Theory]
        [InlineData(5)]
        public static void ThreadSafe_Stress(int executionTimeSeconds) // do some minimal verification that an instance can be used concurrently
        {
            using (var ss = new SecureString())
            {
                DateTimeOffset end = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(executionTimeSeconds);
                Task.WaitAll(Enumerable.Range(0, Environment.ProcessorCount).Select(_ => Task.Run(() =>
                {
                    var rand = new Random(Task.CurrentId.Value);
                    while (DateTimeOffset.UtcNow < end)
                    {
                        char c = (char)rand.Next(0, char.MaxValue);
                        switch (rand.Next(12))
                        {
                            case 0:
                                ss.AppendChar(c);
                                break;
                            case 1:
                                ss.InsertAt(0, c);
                                break;
                            case 2:
                                try { ss.SetAt(0, c); } catch (ArgumentOutOfRangeException) { }
                                break;
                            case 3:
                                ss.Copy().Dispose();
                                break;
                            case 4:
                                Assert.InRange(ss.Length, 0, ushort.MaxValue + 1);
                                break;
                            case 5:
                                ss.Clear();
                                break;
                            case 6:
                                try { ss.RemoveAt(0); } catch (ArgumentOutOfRangeException) { }
                                break;
                            case 7:
                                Assert.False(ss.IsReadOnly());
                                break;
                            case 8:
                                Marshal.ZeroFreeCoTaskMemAnsi(SecureStringMarshal.SecureStringToCoTaskMemAnsi(ss));
                                break;
                            case 9:
                                Marshal.ZeroFreeCoTaskMemUnicode(SecureStringMarshal.SecureStringToCoTaskMemUnicode(ss));
                                break;
                            case 10:
                                Marshal.ZeroFreeGlobalAllocAnsi(SecureStringMarshal.SecureStringToGlobalAllocAnsi(ss));
                                break;
                            case 11:
                                Marshal.ZeroFreeGlobalAllocUnicode(SecureStringMarshal.SecureStringToGlobalAllocUnicode(ss));
                                break;
                        }
                    }
                })).ToArray());
            }
        }

        private static unsafe void AssertEquals(string expected, SecureString actual)
        {
            Assert.Equal(expected, CreateString(actual));
        }

        private static string CreateString(int length)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append((char)('a' + (i % 26)));
            }
            return sb.ToString();
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
