// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    /// <summary>
    /// Indicates that a setting is to be stored on a per-application basis.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ApplicationScopedSettingAttribute : SettingAttribute
    {
    }
}
