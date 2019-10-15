// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    ///     Indicates that a setting is to be stored on a per-user basis.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class UserScopedSettingAttribute : SettingAttribute
    {
    }
}
