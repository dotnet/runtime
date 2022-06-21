// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO;

/// <summary>
/// <para>Represents the Unix filesystem permissions.</para>
/// <para>This enumeration supports a bitwise combination of its member values.</para>
/// </summary>
[Flags]
public enum UnixFileMode
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
    /// Execute permission for owner.
    /// </summary>
    UserExecute = 64,
    /// <summary>
    /// Write permission for owner.
    /// </summary>
    UserWrite = 128,
    /// <summary>
    /// Read permission for owner.
    /// </summary>
    UserRead = 256,
    /// <summary>
    /// Sticky bit permission.
    /// </summary>
    StickyBit = 512,
    /// <summary>
    /// Set Group permission.
    /// </summary>
    SetGroup = 1024,
    /// <summary>
    /// Set User permission.
    /// </summary>
    SetUser = 2048,
}
