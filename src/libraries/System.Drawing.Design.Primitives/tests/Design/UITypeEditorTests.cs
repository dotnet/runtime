// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Design
{
    public class UITypeEditorTests
    {
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_Properties_DefaultValues()
        {
            var editor = new UITypeEditor();

            using (var bm = new Bitmap(10, 10))
            using (var graphics = Graphics.FromImage(bm))
            {
                Assert.False(editor.IsDropDownResizable);
                Assert.Equal(graphics, editor.EditValue(null, graphics));
                Assert.Equal(graphics, editor.EditValue(null, null, graphics));
                Assert.Equal(UITypeEditorEditStyle.None, editor.GetEditStyle());
                Assert.Equal(UITypeEditorEditStyle.None, editor.GetEditStyle(null));
                Assert.False(editor.GetPaintValueSupported());
                Assert.False(editor.GetPaintValueSupported(null));

                // nop
                editor.PaintValue(bm, graphics, Rectangle.Empty);
            }
        }
    }
}
