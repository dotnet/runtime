// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    // Access modes used when calling IImage::LockBits
    /// <summary>
    /// Indicates the access mode for an <see cref='Image'/>.
    /// </summary>
    public enum ImageLockMode
    {
        /// <summary>
        /// Specifies the image is read-only.
        /// </summary>
        ReadOnly = 0x0001,
        /// <summary>
        /// Specifies the image is write-only.
        /// </summary>
        WriteOnly = 0x0002,
        /// <summary>
        /// Specifies the image is read-write.
        /// </summary>
        ReadWrite = ReadOnly | WriteOnly,
        /// <summary>
        /// Indicates the image resides in a user input buffer, to which the user controls access.
        /// </summary>
        UserInputBuffer = 0x0004,
    }
}
