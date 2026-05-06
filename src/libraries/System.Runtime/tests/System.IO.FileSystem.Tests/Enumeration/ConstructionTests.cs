// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class ConstructionTests : FileSystemTest
    {
        [Fact]
        public void Enumerable_NullTransformThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("transform",
                () => new FileSystemEnumerable<string>(TestDirectory, transform: null));
        }

        [Fact]
        public void Enumerable_NullDirectoryThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("directory",
                () => new FileSystemEnumerable<string>(null, null));
        }

        private class TestEnumerator : FileSystemEnumerator<string>
        {
            public TestEnumerator(string directory, EnumerationOptions options)
                : base(directory, options)
            {
            }

            protected override string TransformEntry(ref FileSystemEntry entry)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Enumerator_NullDirectoryThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("directory",
                () => new TestEnumerator(null, null));
        }
    }
}
