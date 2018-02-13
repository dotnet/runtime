// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests
{
    public class ErrorHandlingTests : FileSystemTest
    {
        private class IgnoreErrors : FileSystemEnumerator<string>
        {
            public IgnoreErrors(string directory)
                : base(directory)
            { }

            public int ErrorCount { get; private set; }

            protected override string TransformEntry(ref FileSystemEntry entry)
            {
                throw new NotImplementedException();
            }

            protected override bool ContinueOnError(int error)
            {
                ErrorCount++;
                return true;
            }
        }

        [Fact]
        public void OpenErrorDoesNotHappenAgainOnMoveNext()
        {
            using (IgnoreErrors ie = new IgnoreErrors(Path.GetRandomFileName()))
            {
                Assert.Equal(1, ie.ErrorCount);
                Assert.False(ie.MoveNext());
                Assert.Equal(1, ie.ErrorCount);
            }
        }
    }
}
