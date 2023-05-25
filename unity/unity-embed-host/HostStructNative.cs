// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Unity.CoreCLRHelpers;

/// <summary>
/// Contains callbacks to native functions that are used by the managed code
/// </summary>
unsafe struct HostStructNative
{
    public delegate* unmanaged<byte*, void> unity_log;
}
