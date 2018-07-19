// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using Xunit;

namespace System.Drawing.Design.Tests
{
    public class PropertyValueUIItemTests
    {
        private void Dummy_PropertyValueUIItemInvokeHandler(ITypeDescriptorContext context, PropertyDescriptor propDesc, PropertyValueUIItem invokedItem) { }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_Throws_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("uiItemImage", () => new PropertyValueUIItem(null, Dummy_PropertyValueUIItemInvokeHandler, "toolTip"));

            using (Image img = Image.FromFile(Path.Combine("bitmaps", "nature24bits.jpg")))
            {
                AssertExtensions.Throws<ArgumentNullException>("handler", () => new PropertyValueUIItem(img, null, "toolTip"));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_PropertiesAssignedCorrectly()
        {
            string toolTip = "Custom toolTip";
            using (Image img = Image.FromFile(Path.Combine("bitmaps", "nature24bits.jpg")))
            {
                var propertyValue = new PropertyValueUIItem(img, Dummy_PropertyValueUIItemInvokeHandler, toolTip);
                Assert.Equal(img, propertyValue.Image);
                Assert.Equal(Dummy_PropertyValueUIItemInvokeHandler, propertyValue.InvokeHandler);
                Assert.Equal(toolTip, propertyValue.ToolTip);
            }
        }
    }
}
