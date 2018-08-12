// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GenerateGuidForTypeTests
    {
        [Fact]
        public void GenerateGuidForType_ComObject_ReturnsComGuid()
        {
            Assert.Equal(new Guid("927971f5-0939-11d1-8be1-00c04fd8d503"), Marshal.GenerateGuidForType(typeof(ComImportObject)));
        }
    }
}
