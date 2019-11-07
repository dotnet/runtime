// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Use this attribute to mark properties on a settings class that are to be treated
    /// as settings. ApplicationSettingsBase will ignore all properties not marked with
    /// this or a derived attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingAttribute : Attribute
    {
    }
}
