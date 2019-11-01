// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Design.Serialization.Tests
{
    public class DesignerLoaderTests
    {
        [Fact]
        public void Loading_Get_ReturnsFalse()
        {
            var loader = new TestDesignerLoader();
            Assert.False(loader.Loading);
        }

        [Fact]
        public void Flush_Invoke_Nop()
        {
            var loader = new TestDesignerLoader();
            loader.Flush();
        }

        private class TestDesignerLoader : DesignerLoader
        {
            public override void BeginLoad(IDesignerLoaderHost host) => throw new NotImplementedException();
            public override void Dispose() => throw new NotImplementedException();
        }
    }
}
