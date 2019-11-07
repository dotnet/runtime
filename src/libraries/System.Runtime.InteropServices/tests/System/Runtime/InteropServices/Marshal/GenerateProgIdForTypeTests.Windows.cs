// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GenerateProgIdForTypeTests
    {
        [Fact]
        public void GenerateProgIdForType_ImportType_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Marshal.GenerateProgIdForType(typeof(ComImportObject)));
        }
    }
}
