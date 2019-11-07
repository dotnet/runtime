// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class ComponentEditorTests
    {
        [Fact]
        public void EditComponent_Invoke_ReturnsExpected()
        {
            var editor = new SubEditor();
            Assert.True(editor.EditComponent(10));
        }

        private class SubEditor : ComponentEditor
        {
            public override bool EditComponent(ITypeDescriptorContext context, object component)
            {
                Assert.Null(context);
                Assert.Equal(10, component);
                return true;
            }
        }
    }
}
