// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// The IPersistComponentSettings interface enables components hosted in an application to persist their
    /// settings in a manner transparent to the application. However, in some cases, the application may want to 
    /// override the provider(s) specified by a component. For example, at design time, we may want to persist
    /// settings differently. This service enables this scenario. The ApplicationSettingsBase class queries this
    /// service from the owner component's site.
    /// </summary>
    public interface ISettingsProviderService
    {
        /// <summary>
        /// Queries the service whether it wants to override the provider for the given SettingsProperty. If it
        /// doesn't want to, it should return null, in which the provider will remain unchanged.
        /// </summary>
        SettingsProvider GetSettingsProvider(SettingsProperty property);
    }
}
