// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Collections.Generic
{
    internal ref partial struct ValueListBuilder<T>
    {
        public int Capacity => _span.Length;
        public string ToStringAndDispose()
        {
            string s = this.AsSpan().ToString();
            Dispose();
            return s;
        }
    }
}

namespace System.Collections.Generic.Tests
{
    /// <summary>
    /// Copied from <see cref="System.Text.Tests.ValueStringBuilderTests"/>.
    /// </summary>
    public class ValueListBuilderCharsTests
    {
        [Fact]
        public void Ctor_Default_CanAppend()
        {
            var vsb = default(ValueListBuilder<char>);
            Assert.Equal(0, vsb.Length);

            vsb.Append('a');
            Assert.Equal(1, vsb.Length);
            Assert.Equal("a", vsb.ToStringAndDispose());
        }

        [Fact]
        public void Ctor_Span_CanAppend()
        {
            var vsb = new ValueListBuilder<char>(new char[1]);
            Assert.Equal(0, vsb.Length);

            vsb.Append('a');
            Assert.Equal(1, vsb.Length);
            Assert.Equal("a", vsb.ToStringAndDispose());
        }

        [Fact]
        public void Ctor_InitialCapacity_CanAppend()
        {
            var vsb = new ValueListBuilder<char>(1);
            Assert.Equal(0, vsb.Length);

            vsb.Append('a');
            Assert.Equal(1, vsb.Length);
            Assert.Equal("a", vsb.ToStringAndDispose());
        }

        [Fact]
        public void Append_Char_MatchesStringBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueListBuilder<char>();
            for (int i = 1; i <= 100; i++)
            {
                sb.Append((char)i);
                vsb.Append((char)i);
            }

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToStringAndDispose());
        }

        [Fact]
        public void Append_String_MatchesStringBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueListBuilder<char>();
            for (int i = 1; i <= 100; i++)
            {
                string s = i.ToString();
                sb.Append(s);
                vsb.Append(s);
            }

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToStringAndDispose());
        }

        [Theory]
        [InlineData(0, 4 * 1024 * 1024)]
        [InlineData(1025, 4 * 1024 * 1024)]
        [InlineData(3 * 1024 * 1024, 6 * 1024 * 1024)]
        public void Append_String_Large_MatchesStringBuilder(int initialLength, int stringLength)
        {
            var sb = new StringBuilder(initialLength);
            var vsb = new ValueListBuilder<char>(new char[initialLength]);

            string s = new string('a', stringLength);
            sb.Append(s);
            vsb.Append(s);

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToStringAndDispose());
        }

        [Fact]
        public void AppendSpan_Capacity()
        {
            var vsb = new ValueListBuilder<char>();

            vsb.AppendSpan(17);
            Assert.Equal(32, vsb.Capacity);

            vsb.AppendSpan(100);
            Assert.Equal(128, vsb.Capacity);
        }

        [Fact]
        public void AppendSpan_DataAppendedCorrectly()
        {
            var sb = new StringBuilder();
            var vsb = new ValueListBuilder<char>();

            for (int i = 1; i <= 1000; i++)
            {
                string s = i.ToString();

                sb.Append(s);

                Span<char> span = vsb.AppendSpan(s.Length);
                Assert.Equal(sb.Length, vsb.Length);

                s.AsSpan().CopyTo(span);
            }

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToStringAndDispose());
        }


        [Fact]
        public void Insert_IntString_MatchesStringBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueListBuilder<char>();

            sb.Insert(0, new string('a', 6));
            vsb.Insert(0, new string('a', 6));
            Assert.Equal(6, vsb.Length);
            Assert.Equal(16, vsb.Capacity);

            sb.Insert(0, new string('b', 11));
            vsb.Insert(0, new string('b', 11));
            Assert.Equal(17, vsb.Length);
            Assert.Equal(32, vsb.Capacity);

            sb.Insert(0, new string('c', 15));
            vsb.Insert(0, new string('c', 15));
            Assert.Equal(32, vsb.Length);
            Assert.Equal(32, vsb.Capacity);

            sb.Length = 24;
            vsb.Length = 24;

            sb.Insert(0, new string('d', 40));
            vsb.Insert(0, new string('d', 40));
            Assert.Equal(64, vsb.Length);
            Assert.Equal(64, vsb.Capacity);

            Assert.Equal(sb.Length, vsb.Length);
            Assert.Equal(sb.ToString(), vsb.ToStringAndDispose());
        }

        [Fact]
        public void AsSpan_ReturnsCorrectValue_DoesntClearBuilder()
        {
            var sb = new StringBuilder();
            var vsb = new ValueListBuilder<char>();

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
            Assert.Equal(sb.ToString(), vsb.ToStringAndDispose());
        }

        [Fact]
        public void ToString_ClearsBuilder_ThenReusable()
        {
            const string Text1 = "test";
            var vsb = new ValueListBuilder<char>();

            vsb.Append(Text1);
            Assert.Equal(Text1.Length, vsb.Length);

            string s = vsb.ToStringAndDispose();
            Assert.Equal(Text1, s);

            Assert.Equal(0, vsb.Length);
            Assert.Equal(string.Empty, vsb.ToStringAndDispose());

            const string Text2 = "another test";
            vsb.Append(Text2);
            Assert.Equal(Text2.Length, vsb.Length);
            Assert.Equal(Text2, vsb.ToStringAndDispose());
        }

        [Fact]
        public void Dispose_ClearsBuilder_ThenReusable()
        {
            const string Text1 = "test";
            var vsb = new ValueListBuilder<char>();

            vsb.Append(Text1);
            Assert.Equal(Text1.Length, vsb.Length);

            vsb.Dispose();

            Assert.Equal(0, vsb.Length);
            Assert.Equal(string.Empty, vsb.ToStringAndDispose());

            const string Text2 = "another test";
            vsb.Append(Text2);
            Assert.Equal(Text2.Length, vsb.Length);
            Assert.Equal(Text2, vsb.ToStringAndDispose());
        }

        [Fact]
        public void Indexer()
        {
            const string Text1 = "foobar";
            var vsb = new ValueListBuilder<char>();

            vsb.Append(Text1);

            Assert.Equal('b', vsb[3]);
            vsb[3] = 'c';
            Assert.Equal('c', vsb[3]);
            vsb.Dispose();
        }
    }
}
