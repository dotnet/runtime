// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <include file='doc\PreviewPageInfo.uex' path='docs/doc[@for="PreviewPageInfo"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies print preview information for
    ///       a single page. This class cannot be inherited.
    ///    </para>
    /// </devdoc>
    public sealed class PreviewPageInfo
    {
        private Image _image;

        // Physical measures in hundredths of an inch
        private Size _physicalSize = Size.Empty;

        /// <include file='doc\PreviewPageInfo.uex' path='docs/doc[@for="PreviewPageInfo.PreviewPageInfo"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Printing.PreviewPageInfo'/>
        ///       class.
        ///    </para>
        /// </devdoc>
        public PreviewPageInfo(Image image, Size physicalSize)
        {
            _image = image;
            _physicalSize = physicalSize;
        }

        /// <include file='doc\PreviewPageInfo.uex' path='docs/doc[@for="PreviewPageInfo.Image"]/*' />
        /// <devdoc>
        ///    <para>Gets the image of the printed page.</para>
        /// </devdoc>
        public Image Image
        {
            get { return _image; }
        }

        // Physical measures in hundredths of an inch
        /// <include file='doc\PreviewPageInfo.uex' path='docs/doc[@for="PreviewPageInfo.PhysicalSize"]/*' />
        /// <devdoc>
        ///    <para> Gets the size of the printed page, in hundredths of an inch.</para>
        /// </devdoc>
        public Size PhysicalSize
        {
            get { return _physicalSize; }
        }
    }
}
