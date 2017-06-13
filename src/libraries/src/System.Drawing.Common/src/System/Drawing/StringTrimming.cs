// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <include file='doc\StringTrimming.uex' path='docs/doc[@for="StringTrimming"]/*' />
    /// <devdoc>
    ///    Specifies how to trim characters from a
    ///    string that does not completely fit into a layout shape.
    /// </devdoc>
    public enum StringTrimming
    {
        /// <include file='doc\StringTrimming.uex' path='docs/doc[@for="StringTrimming.None"]/*' />
        /// <devdoc>
        ///    Specifies no trimming.
        /// </devdoc>
        None = 0,

        /// <include file='doc\StringTrimming.uex' path='docs/doc[@for="StringTrimming.Character"]/*' />
        /// <devdoc>
        ///    Specifies that the string is broken at the boundary of the last character that is 
        ///    inside the layout rectangle. This is the default.
        /// </devdoc>
        Character = 1,

        /// <include file='doc\StringTrimming.uex' path='docs/doc[@for="StringTrimming.Word"]/*' />
        /// <devdoc>
        ///    Specifies that the string is broken at the boundary of the last word that is inside 
        ///    the layout rectangle.
        /// </devdoc>
        Word = 2,

        /// <include file='doc\StringTrimming.uex' path='docs/doc[@for="StringTrimming.EllipsisCharacter"]/*' />
        /// <devdoc>
        ///    Specifies that the string is broken at the boundary of the last character that is inside 
        ///    the layout rectangle and an ellipsis (...) is inserted after the character. 
        /// </devdoc>
        EllipsisCharacter = 3,

        /// <include file='doc\StringTrimming.uex' path='docs/doc[@for="StringTrimming.EllipsisWord"]/*' />
        /// <devdoc>
        ///    Specifies that the string is broken at the boundary of the last word that is inside the 
        ///    layout rectangle and an ellipsis (...) is inserted after the word.
        /// </devdoc>
        EllipsisWord = 4,

        /// <include file='doc\StringTrimming.uex' path='docs/doc[@for="StringTrimming.EllipsisPath"]/*' />
        /// <devdoc>
        ///    Specifies that the center is removed from the string and replaced by an ellipsis. 
        ///    The algorithm keeps as much of the last portion of the string as possible. 
        /// </devdoc>
        EllipsisPath = 5
    }
}

