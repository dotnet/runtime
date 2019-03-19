// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class ValueStringBuilderTests
    {
        [Fact]
        public void Ctor_Default_CanAppend()
        {
            var vsb = default(ValueStringBuilder);
            Assert.Equal(0, vsb.Length);

            vsb.Append('a');
            Assert.Equal(1, vsb.Length);
            Assert.Equal("a", vsb.ToString());
        }

        [Fact]
        public void Ctor_Span_CanAppend()
        {
            var vsb = new ValueStringBuilder(new char[1]);
            Assert.Equal(0, vsb.Length);

            vsb.Append('a');
            Assert.Equal(1, vsb.Length);
            Assert.Equal("a", vsb.ToString());
        }

        [Fact]
        public void Ctor_InitialCapacity_CanAppend()
        {
            var vsb = new ValueStringBuilder(1);
            Assert.Equal(0, vsb.Length);

            vsb.Append('a');
            Assert.Equal(1, vsb.Length);
            Assert.Equal("a", vsb.ToString());
        }

        [Fact]
        public void Append_Char_MatchesStringBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueStringBuilder();
            for (int i = 1; i <= 100; i++)
            {
                sb.Append((char)i);
                vsb.Append((char)i);
            }

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToString());
        }

        [Fact]
        public void Append_String_MatchesStringBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueStringBuilder();
            for (int i = 1; i <= 100; i++)
            {
                string s = i.ToString();
                sb.Append(s);
                vsb.Append(s);
            }

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToString());
        }

        [Theory]
        [InlineData(0, 4*1024*1024)]
        [InlineData(1025, 4*1024*1024)]
        [InlineData(3*1024*1024, 6*1024*1024)]
        public void Append_String_Large_MatchesStringBuilder(int initialLength, int stringLength)
        {
            var sb = new StringBuilder(initialLength);
            var vsb = new ValueStringBuilder(new char[initialLength]);

            string s = new string('a', stringLength);
            sb.Append(s);
            vsb.Append(s);

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToString());
        }

        [Fact]
        public void Append_CharInt_MatchesStringBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueStringBuilder();
            for (int i = 1; i <= 100; i++)
            {
                sb.Append((char)i, i);
                vsb.Append((char)i, i);
            }

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToString());
        }

        [Fact]
        public unsafe void Append_PtrInt_MatchesStringBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueStringBuilder();
            for (int i = 1; i <= 100; i++)
            {
                string s = i.ToString();
                fixed (char* p = s)
                {
                    sb.Append(p, s.Length);
                    vsb.Append(p, s.Length);
                }
            }

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToString());
        }

        [Fact]
        public void AppendSpan_DataAppendedCorrectly()
        {
            var sb = new StringBuilder();
            var vsb = new ValueStringBuilder();

            for (int i = 1; i <= 1000; i++)
            {
                string s = i.ToString();

                sb.Append(s);

                Span<char> span = vsb.AppendSpan(s.Length);
                Assert.Equal(sb.Length, vsb.Length);

                s.AsSpan().CopyTo(span);
            }

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToString());
        }

        [Fact]
        public void Insert_IntCharInt_MatchesStringBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueStringBuilder();
            var rand = new Random(42);

            for (int i = 1; i <= 100; i++)
            {
                int index = rand.Next(sb.Length);
                sb.Insert(index, new string((char)i, 1), i);
                vsb.Insert(index, (char)i, i);
            }

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToString());
        }

        [Fact]
        public void AsSpan_ReturnsCorrectValue_DoesntClearBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueStringBuilder();

            for (int i = 1; i <= 100; i++)
            {
                string s = i.ToString();
                sb.Append(s);
                vsb.Append(s);
            }

            var resultString = new string(vsb.AsSpan());
            Assert.Equal(sb.ToString(), resultString);

            Assert.NotEqual(0, sb.Length);
            Assert.Equal(sb.Length, vsb.Length);
        }

        [Fact]
        public void ToString_ClearsBuilder_ThenReusable()
        {
            const string Text1 = "test";
            var vsb = new ValueStringBuilder();

            vsb.Append(Text1);
            Assert.Equal(Text1.Length, vsb.Length);

            string s = vsb.ToString();
            Assert.Equal(Text1, s);

            Assert.Equal(0, vsb.Length);
            Assert.Equal(string.Empty, vsb.ToString());
            Assert.True(vsb.TryCopyTo(Span<char>.Empty, out _));

            const string Text2 = "another test";
            vsb.Append(Text2);
            Assert.Equal(Text2.Length, vsb.Length);
            Assert.Equal(Text2, vsb.ToString());
        }

        [Fact]
        public void TryCopyTo_FailsWhenDestinationIsTooSmall_SucceedsWhenItsLargeEnough()
        {
            var vsb = new ValueStringBuilder();

            const string Text = "expected text";
            vsb.Append(Text);
            Assert.Equal(Text.Length, vsb.Length);

            Span<char> dst = new char[Text.Length - 1];
            Assert.False(vsb.TryCopyTo(dst, out int charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.Equal(0, vsb.Length);
        }

        [Fact]
        public void TryCopyTo_ClearsBuilder_ThenReusable()
        {
            const string Text1 = "test";
            var vsb = new ValueStringBuilder();

            vsb.Append(Text1);
            Assert.Equal(Text1.Length, vsb.Length);

            Span<char> dst = new char[Text1.Length];
            Assert.True(vsb.TryCopyTo(dst, out int charsWritten));
            Assert.Equal(Text1.Length, charsWritten);
            Assert.Equal(Text1, new string(dst));

            Assert.Equal(0, vsb.Length);
            Assert.Equal(string.Empty, vsb.ToString());
            Assert.True(vsb.TryCopyTo(Span<char>.Empty, out _));

            const string Text2 = "another test";
            vsb.Append(Text2);
            Assert.Equal(Text2.Length, vsb.Length);
            Assert.Equal(Text2, vsb.ToString());
        }

        [Fact]
        public void Dispose_ClearsBuilder_ThenReusable()
        {
            const string Text1 = "test";
            var vsb = new ValueStringBuilder();

            vsb.Append(Text1);
            Assert.Equal(Text1.Length, vsb.Length);

            vsb.Dispose();

            Assert.Equal(0, vsb.Length);
            Assert.Equal(string.Empty, vsb.ToString());
            Assert.True(vsb.TryCopyTo(Span<char>.Empty, out _));

            const string Text2 = "another test";
            vsb.Append(Text2);
            Assert.Equal(Text2.Length, vsb.Length);
            Assert.Equal(Text2, vsb.ToString());
        }

        [Fact]
        public unsafe void Indexer()
        {
            const string Text1 = "foobar";
            var vsb = new ValueStringBuilder();

            vsb.Append(Text1);

            Assert.Equal(vsb[3], 'b');
            vsb[3] = 'c';
            Assert.Equal(vsb[3], 'c');
        }

        [Fact]
        public void EnsureCapacity_IfRequestedCapacityWins()
        {
            // Note: constants used here may be dependent on minimal buffer size
            // the ArrayPool is able to return.
            Span<char> initialBuffer = stackalloc char[32];
            var builder = new ValueStringBuilder(initialBuffer);

            builder.EnsureCapacity(65);

            Assert.Equal(128, builder.Capacity);
        }

        [Fact]
        public void EnsureCapacity_IfBufferTimesTwoWins()
        {
            Span<char> initialBuffer = stackalloc char[32];
            var builder = new ValueStringBuilder(initialBuffer);

            builder.EnsureCapacity(33);

            Assert.Equal(64, builder.Capacity);
        }

        [Fact]
        public void EnsureCapacity_NoAllocIfNotNeeded()
        {
            // Note: constants used here may be dependent on minimal buffer size
            // the ArrayPool is able to return.
            Span<char> initialBuffer = stackalloc char[64];
            var builder = new ValueStringBuilder(initialBuffer);

            builder.EnsureCapacity(16);

            Assert.Equal(64, builder.Capacity);
        }
    }
}
