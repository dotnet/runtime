// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class DefaultDllImportSearchPathsAttributeTests
    {
        [Theory]
        [InlineData((DllImportSearchPath)(-1))]
        [InlineData(DllImportSearchPath.AssemblyDirectory)]
        [InlineData((DllImportSearchPath)int.MaxValue)]
        public void Ctor_Paths(DllImportSearchPath paths)
        {
            var attribute = new DefaultDllImportSearchPathsAttribute(paths);
            Assert.Equal(paths, attribute.Paths);
        }
    }
}
