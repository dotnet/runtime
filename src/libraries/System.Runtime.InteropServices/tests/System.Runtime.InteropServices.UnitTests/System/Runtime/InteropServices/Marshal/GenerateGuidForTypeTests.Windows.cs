// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GenerateGuidForTypeTests
    {
        private const string GuidStr = "708DDB5E-09B1-4550-A5D9-9D7DE2771C10";

        [ComImport]
        [Guid(GuidStr)]
        private class DummyObject { }

        [Fact]
        public void GenerateGuidForType_ComObject_ReturnsComGuid()
        {
            Assert.Equal(new Guid(GuidStr), Marshal.GenerateGuidForType(typeof(DummyObject)));
        }
    }
}
