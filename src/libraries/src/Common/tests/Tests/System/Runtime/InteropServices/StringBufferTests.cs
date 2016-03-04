// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Tests.System.Runtime.InteropServices
{
    public class StringBufferTests
    {
        private const string TestString = ".NET Core is awesome.";

        [Fact]
        public void CanIndexChar()
        {
            using (var buffer = new StringBuffer())
            {
                buffer.Length = 1;
                buffer[0] = 'Q';
                Assert.Equal(buffer[0], 'Q');
            }
        }

        [Fact]
        public unsafe void CreateFromString()
        {
            string testString = "Test";
            using (var buffer = new StringBuffer(testString))
            {
                Assert.Equal((uint)testString.Length, buffer.Length);
                Assert.Equal((uint)testString.Length + 1, buffer.CharCapacity);

                for (int i = 0; i < testString.Length; i++)
                {
                    Assert.Equal(testString[i], buffer[(uint)i]);
                }

                // Check the null termination
                Assert.Equal('\0', buffer.CharPointer[testString.Length]);

                Assert.Equal(testString, buffer.ToString());
            }
        }

        [Fact]
        public void ReduceLength()
        {
            using (var buffer = new StringBuffer("Food"))
            {
                Assert.Equal((uint)5, buffer.CharCapacity);
                buffer.Length = 3;
                Assert.Equal("Foo", buffer.ToString());
                // Shouldn't reduce capacity when dropping length
                Assert.Equal((uint)5, buffer.CharCapacity);
            }
        }

        [Fact]
        public void GetOverIndexThrowsArgumentOutOfRange()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { char c = buffer[0]; });
            }
        }

        [Fact]
        public void SetOverIndexThrowsArgumentOutOfRange()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { buffer[0] = 'Q'; });
            }
        }

        [Theory,
            InlineData(@"Foo", @"Foo", true),
            InlineData(@"Foo", @"foo", false),
            InlineData(@"Foobar", @"Foo", true),
            InlineData(@"Foobar", @"foo", false),
            InlineData(@"Fo", @"Foo", false),
            InlineData(@"Fo", @"foo", false),
            InlineData(@"", @"", true),
            InlineData(@"", @"f", false),
            InlineData(@"f", @"", true),
            ]
        public void StartsWith(string source, string value, bool expected)
        {
            using (var buffer = new StringBuffer(source))
            {
                Assert.Equal(expected, buffer.StartsWith(value));
            }
        }

        [Fact]
        public void StartsWithNullThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentNullException>(() => buffer.StartsWith(null));
            }
        }

        [Fact]
        public void SubstringEqualsNegativeCountThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SubstringEquals("", startIndex: 0, count: -2));
            }
        }

        [Fact]
        public void SubstringEqualsOverSizeCountThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SubstringEquals("", startIndex: 0, count: 1));
            }
        }

        [Fact]
        public void SubstringEqualsOverSizeCountWithIndexThrows()
        {
            using (var buffer = new StringBuffer("A"))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SubstringEquals("", startIndex: 1, count: 1));
            }
        }

        [Theory,
            InlineData(@"", null, 0, 0, false),
            InlineData(@"", @"", 0, 0, true),
            InlineData(@"", @"", 0, -1, true),
            InlineData(@"A", @"", 0, -1, false),
            InlineData(@"", @"A", 0, -1, false),
            InlineData(@"Foo", @"Foo", 0, -1, true),
            InlineData(@"Foo", @"foo", 0, -1, false),
            InlineData(@"Foo", @"Foo", 1, -1, false),
            InlineData(@"Foo", @"Food", 0, -1, false),
            InlineData(@"Food", @"Foo", 0, -1, false),
            InlineData(@"Food", @"Foo", 0, 3, true),
            InlineData(@"Food", @"ood", 1, 3, true),
            InlineData(@"Food", @"ooD", 1, 3, false),
            InlineData(@"Food", @"ood", 1, 2, false),
            InlineData(@"Food", @"Food", 0, 3, false),
            ]
        public void SubstringEquals(string source, string value, int startIndex, int count, bool expected)
        {
            using (var buffer = new StringBuffer(source))
            {
                Assert.Equal(expected, buffer.SubstringEquals(value, startIndex: (uint)startIndex, count: count));
            }
        }

        [Theory,
            InlineData(@"", @"", 0, -1, @""),
            InlineData(@"", @"", 0, 0, @""),
            InlineData(@"", @"A", 0, -1, @"A"),
            InlineData(@"", @"A", 0, 0, @""),
            InlineData(@"", @"Aa", 0, -1, @"Aa"),
            InlineData(@"", @"Aa", 0, 0, @""),
            InlineData(@"", "Aa\0", 0, -1, "Aa\0"),
            InlineData(@"", "Aa\0", 0, 3, "Aa\0"),
            InlineData(@"", @"AB", 0, -1, @"AB"),
            InlineData(@"", @"AB", 0, 1, @"A"),
            InlineData(@"", @"AB", 1, 1, @"B"),
            InlineData(@"", @"AB", 1, -1, @"B"),
            InlineData(@"", @"ABC", 1, -1, @"BC"),
            InlineData(null, @"", 0, -1, @""),
            InlineData(null, @"", 0, 0, @""),
            InlineData(null, @"A", 0, -1, @"A"),
            InlineData(null, @"A", 0, 0, @""),
            InlineData(null, @"Aa", 0, -1, @"Aa"),
            InlineData(null, @"Aa", 0, 0, @""),
            InlineData(null, "Aa\0", 0, -1, "Aa\0"),
            InlineData(null, "Aa\0", 0, 3, "Aa\0"),
            InlineData(null, @"AB", 0, -1, @"AB"),
            InlineData(null, @"AB", 0, 1, @"A"),
            InlineData(null, @"AB", 1, 1, @"B"),
            InlineData(null, @"AB", 1, -1, @"B"),
            InlineData(null, @"ABC", 1, -1, @"BC"),
            InlineData(@"Q", @"", 0, -1, @"Q"),
            InlineData(@"Q", @"", 0, 0, @"Q"),
            InlineData(@"Q", @"A", 0, -1, @"QA"),
            InlineData(@"Q", @"A", 0, 0, @"Q"),
            InlineData(@"Q", @"Aa", 0, -1, @"QAa"),
            InlineData(@"Q", @"Aa", 0, 0, @"Q"),
            InlineData(@"Q", "Aa\0", 0, -1, "QAa\0"),
            InlineData(@"Q", "Aa\0", 0, 3, "QAa\0"),
            InlineData(@"Q", @"AB", 0, -1, @"QAB"),
            InlineData(@"Q", @"AB", 0, 1, @"QA"),
            InlineData(@"Q", @"AB", 1, 1, @"QB"),
            InlineData(@"Q", @"AB", 1, -1, @"QB"),
            InlineData(@"Q", @"ABC", 1, -1, @"QBC"),
            ]
        public void AppendTests(string source, string value, int startIndex, int count, string expected)
        {
            // From string
            using (var buffer = new StringBuffer(source))
            {
                buffer.Append(value, startIndex, count);
                Assert.Equal(expected, buffer.ToString());
            }

            // From buffer
            using (var buffer = new StringBuffer(source))
            using (var valueBuffer = new StringBuffer(value))
            {
                if (count == -1)
                    buffer.Append(valueBuffer, (uint)startIndex, valueBuffer.Length - (uint)startIndex);
                else
                    buffer.Append(valueBuffer, (uint)startIndex, (uint)count);
                Assert.Equal(expected, buffer.ToString());
            }
        }

        [Fact]
        public void AppendNullStringThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentNullException>(() => buffer.Append((string)null));
            }
        }

        [Fact]
        public void AppendNullStringBufferThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentNullException>(() => buffer.Append((StringBuffer)null, 0, 0));
            }
        }

        [Fact]
        public void AppendNegativeIndexThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Append("a", startIndex: -1));
            }
        }

        [Fact]
        public void AppendOverIndexThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Append("", startIndex: 1));
            }
        }

        [Fact]
        public void AppendOverCountThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Append("", startIndex: 0, count: 1));
            }
        }

        [Fact]
        public void AppendOverCountWithIndexThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Append("A", startIndex: 1, count: 1));
            }
        }

        [Fact]
        public void ToStringIndexOverLengthThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Substring(startIndex: 1));
            }
        }

        [Fact]
        public void ToStringNegativeCountThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Substring(startIndex: 0, count: -2));
            }
        }

        [Fact]
        public void ToStringCountOverLengthThrows()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Substring(startIndex: 0, count: 1));
            }
        }

        [Theory,
            InlineData(@"", 0, -1, @""),
            InlineData(@"A", 0, -1, @"A"),
            InlineData(@"AB", 0, -1, @"AB"),
            InlineData(@"AB", 0, 1, @"A"),
            InlineData(@"AB", 1, 1, @"B"),
            InlineData(@"AB", 1, -1, @"B"),
            InlineData(@"", 0, 0, @""),
            InlineData(@"A", 0, 0, @""),
            ]
        public void ToStringTest(string source, int startIndex, int count, string expected)
        {
            using (var buffer = new StringBuffer(source))
            {
                Assert.Equal(expected, buffer.Substring(startIndex: (uint)startIndex, count: count));
            }
        }

        [Fact]
        public unsafe void SetLengthToFirstNullNoNull()
        {
            using (var buffer = new StringBuffer("A"))
            {
                // Wipe out the last null
                buffer.CharPointer[buffer.Length] = 'B';
                buffer.SetLengthToFirstNull();
                Assert.Equal((ulong)1, buffer.Length);
            }
        }

        [Fact]
        public unsafe void SetLengthToFirstNullEmptyBuffer()
        {
            using (var buffer = new StringBuffer())
            {
                buffer.SetLengthToFirstNull();
                Assert.Equal((ulong)0, buffer.Length);
            }
        }

        [Theory,
            InlineData(@"", 0, 0),
            InlineData(@"Foo", 3, 3),
            InlineData("\0", 1, 0),
            InlineData("Foo\0Bar", 7, 3),
            ]
        public unsafe void SetLengthToFirstNullTests(string content, ulong startLength, ulong endLength)
        {
            using (var buffer = new StringBuffer(content))
            {
                // With existing content
                Assert.Equal(startLength, buffer.Length);
                buffer.SetLengthToFirstNull();
                Assert.Equal(endLength, buffer.Length);

                // Clear the buffer & manually copy in
                buffer.Length = 0;
                fixed (char* contentPointer = content)
                {
                    Buffer.MemoryCopy(contentPointer, buffer.CharPointer, (long)buffer.CharCapacity * 2, content.Length * sizeof(char));
                }

                Assert.Equal((uint)0, buffer.Length);
                buffer.SetLengthToFirstNull();
                Assert.Equal(endLength, buffer.Length);
            }
        }

        [Theory,
            InlineData("foo", new char[] { }, "foo"),
            InlineData("foo", null, "foo"),
            InlineData("foo", new char[] { 'b' }, "foo"),
            InlineData("", new char[] { }, ""),
            InlineData("", null, ""),
            InlineData("", new char[] { 'b' }, ""),
            InlineData("foo", new char[] { 'o' }, "f"),
            InlineData("foo", new char[] { 'o', 'f' }, ""),
            // Add a couple cases to try and get the trim to walk off the front of the buffer.
            InlineData("foo", new char[] { 'o', 'f', '\0' }, ""),
            InlineData("foo", new char[] { 'o', 'f', '\u9000' }, "")
            ]
        public void TrimEnd(string content, char[] trimChars, string expected)
        {
            // We want equivalence with built-in string behavior
            using (var buffer = new StringBuffer(content))
            {
                buffer.TrimEnd(trimChars);
                Assert.Equal(expected, buffer.ToString());
            }
        }

        [Theory,
            InlineData(@"Foo", @"Bar", 0, 0, 3, "Bar"),
            InlineData(@"Foo", @"Bar", 0, 0, -1, "Bar"),
            InlineData(@"Foo", @"Bar", 3, 0, 3, "FooBar"),
            InlineData(@"", @"Bar", 0, 0, 3, "Bar"),
            InlineData(@"Foo", @"Bar", 1, 0, 3, "FBar"),
            InlineData(@"Foo", @"Bar", 1, 1, 2, "Far"),
            ]
        public void CopyFromString(string content, string source, uint bufferIndex, int sourceIndex, int count, string expected)
        {
            using (var buffer = new StringBuffer(content))
            {
                buffer.CopyFrom(bufferIndex, source, sourceIndex, count);
                Assert.Equal(expected, buffer.ToString());
            }
        }

        [Fact]
        public void CopyFromStringThrowsOnNull()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentNullException>(() => { buffer.CopyFrom(0, null); });
            }
        }

        [Fact]
        public void CopyFromStringThrowsIndexingBeyondBufferLength()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { buffer.CopyFrom(1, ""); });
            }
        }

        [Theory,
            InlineData("", 0, 1),
            InlineData("", 1, 0),
            InlineData("", 1, -1),
            InlineData("", 2, 0),
            InlineData("Foo", 3, 1),
            InlineData("Foo", 4, 0),
            ]
        public void CopyFromStringThrowsIndexingBeyondStringLength(string value, int index, int count)
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { buffer.CopyFrom(0, value, index, count); });
            }
        }

        [Theory,
            InlineData(@"Foo", @"Bar", 0, 0, 3, "Bar"),
            InlineData(@"Foo", @"Bar", 3, 0, 3, "FooBar"),
            InlineData(@"", @"Bar", 0, 0, 3, "Bar"),
            InlineData(@"Foo", @"Bar", 1, 0, 3, "FBar"),
            InlineData(@"Foo", @"Bar", 1, 1, 2, "Far"),
            ]
        public void CopyToBufferString(string destination, string content, uint destinationIndex, uint bufferIndex, uint count, string expected)
        {
            using (var buffer = new StringBuffer(content))
            using (var destinationBuffer = new StringBuffer(destination))
            {
                buffer.CopyTo(bufferIndex, destinationBuffer, destinationIndex, count);
                Assert.Equal(expected, destinationBuffer.ToString());
            }
        }

        [Fact]
        public void CopyToBufferThrowsOnNull()
        {
            using (var buffer = new StringBuffer())
            {
                Assert.Throws<ArgumentNullException>(() => { buffer.CopyTo(0, null, 0, 0); });
            }
        }

        [Theory,
            InlineData("", 0, 1),
            InlineData("", 1, 0),
            InlineData("", 2, 0),
            InlineData("Foo", 3, 1),
            InlineData("Foo", 4, 0),
            ]
        public void CopyToBufferThrowsIndexingBeyondSourceBufferLength(string source, uint index, uint count)
        {
            using (var buffer = new StringBuffer(source))
            using (var targetBuffer = new StringBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { buffer.CopyTo(index, targetBuffer, 0, count); });
            }
        }
    }
}
