// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// Specifies how to trim characters from a string that does not completely fit into a layout shape.
    /// </summary>
    public enum StringTrimming
    {
        /// <summary>
        /// Specifies no trimming.
        /// </summary>
        None = 0,

        /// <summary>
        /// Specifies that the string is broken at the boundary of the last character
        /// that is inside the layout rectangle. This is the default.
        /// </summary>
        Character = 1,

        /// <summary>
        /// Specifies that the string is broken at the boundary of the last word that is inside the layout rectangle.
        /// </summary>
        Word = 2,

        /// <summary>
        /// Specifies that the string is broken at the boundary of the last character that is inside 
        /// the layout rectangle and an ellipsis (...) is inserted after the character. 
        /// </summary>
        EllipsisCharacter = 3,

        /// <summary>
        /// Specifies that the string is broken at the boundary of the last word that is inside the 
        /// layout rectangle and an ellipsis (...) is inserted after the word.
        /// </summary>
        EllipsisWord = 4,

        /// <summary>
        /// Specifies that the center is removed from the string and replaced by an ellipsis. 
        /// The algorithm keeps as much of the last portion of the string as possible. 
        /// </summary>
        EllipsisPath = 5
    }
}

