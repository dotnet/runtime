// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// <para>Represents the Unix-like filesystem permissions or access modes.</para>
    /// <para>This enumeration supports a bitwise combination of its member values.</para>
    /// </summary>
    [Flags]
    public enum TarFileMode
    {
        /// <summary>
        /// No permissions.
        /// </summary>
        None = 0,
        /// <summary>
        /// Execute permission for others.
        /// </summary>
        OtherExecute = 1,
        /// <summary>
        /// Write permission for others.
        /// </summary>
        OtherWrite = 2,
        /// <summary>
        /// Read permission for others.
        /// </summary>
        OtherRead = 4,
        /// <summary>
        /// Execute permission for group.
        /// </summary>
        GroupExecute = 8,
        /// <summary>
        /// Write permission for group.
        /// </summary>
        GroupWrite = 16,
        /// <summary>
        /// Read permission for group.
        /// </summary>
        GroupRead = 32,
        /// <summary>
        /// Execute permission for user.
        /// </summary>
        UserExecute = 64,
        /// <summary>
        /// Write permission for user.
        /// </summary>
        UserWrite = 128,
        /// <summary>
        /// Read permission for user.
        /// </summary>
        UserRead = 256,
        /// <summary>
        /// Sticky bit special permission.
        /// </summary>
        StickyBit = 512,
        /// <summary>
        /// Group special permission or <c>setgid</c>.
        /// </summary>
        GroupSpecial = 1024,
        /// <summary>
        /// User special permission o <c>setuid</c>.
        /// </summary>
        UserSpecial = 2048,
    }
}
