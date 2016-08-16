// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class PEHeaderBuilderTests
    {
        [Fact]
        public void Ctor_Errors()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaderBuilder(sectionAlignment: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaderBuilder(fileAlignment: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaderBuilder(sectionAlignment: 512, fileAlignment: 1024));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaderBuilder(sectionAlignment: 513));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaderBuilder(sectionAlignment: int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaderBuilder(fileAlignment: 513));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaderBuilder(fileAlignment: 64*1024*2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaderBuilder(fileAlignment: int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PEHeaderBuilder(fileAlignment: int.MinValue));
        }
    }
}
