// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public static class EditorBrowsableAttributeTestsNetStandard17
    {
        [Fact]
        public static void DefaultCtor_EditableBrowserState()
        {
            Assert.Equal(EditorBrowsableState.Always, new EditorBrowsableAttribute().State);
        }
    }
}