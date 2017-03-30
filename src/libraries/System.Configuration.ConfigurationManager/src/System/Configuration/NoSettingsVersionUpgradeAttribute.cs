// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Indicates that the provider should disable any logic that gets invoked when an application
    /// upgrade is detected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NoSettingsVersionUpgradeAttribute : Attribute
    {
    }
}
