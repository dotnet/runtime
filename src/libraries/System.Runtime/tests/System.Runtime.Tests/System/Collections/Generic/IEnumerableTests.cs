// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Collections.Generic.Tests
{
    public class IEnumerableTests
    {
        [Fact]
        public void SupportsRefStructT()
        {
            StringBuilder builder = new();
            foreach (ReadOnlySpan<char> c in new MySpanReturningEnumerable("hello"))
            {
                builder.Append(c);
            }
            Assert.Equal("hello", builder.ToString());
        }
    }

    internal sealed class MySpanReturningEnumerable(string value) : IEnumerable<ReadOnlySpan<char>>, IEnumerator<ReadOnlySpan<char>>
    {
        private int _index = -1;

        public IEnumerator<ReadOnlySpan<char>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public ReadOnlySpan<char> Current => value.AsSpan(_index, 1);

        public bool MoveNext() => ++_index < value.Length;

        public void Dispose() { }

        object IEnumerator.Current => throw new NotSupportedException();

        void IEnumerator.Reset() => throw new NotSupportedException();
    }
}
