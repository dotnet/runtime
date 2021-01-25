// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class InstanceCreationEditorTests
    {
        [Fact]
        public void Text_Get_ReturnsExpected()
        {
            var editor = new SubEditor();
            Assert.Equal("(New...)", editor.Text);
        }

        private class SubEditor : InstanceCreationEditor
        {
            public override object CreateInstance(ITypeDescriptorContext context, Type instanceType)
            {
                throw new NotImplementedException();
            }
        }
    }
}
