// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Provider;

namespace System.Configuration
{
    public abstract class SettingsProvider : ProviderBase
    {
        public abstract SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection);
        public abstract void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection);
        public abstract string ApplicationName { get; set; }
    }
}
