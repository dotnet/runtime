// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Components should implement this interface if they want to persist custom settings 
    /// in a hosting application. This interface allows the application author to tell a control
    /// whether to persist, when to load, save etc.
    /// </summary>
    public interface IPersistComponentSettings
    {
        /// <summary>
        /// Indicates to the implementor that settings should be persisted.
        /// </summary>
        bool SaveSettings { get; set; }

        /// <summary>
        /// Unique key that identifies an individual instance of a settings group(s). This key is needed
        /// to identify which instance of a component owns a given group(s) of settings. Usually, the component
        /// will frame its own key, but this property allows the hosting application to override it if necessary.
        /// </summary>
        string SettingsKey { get; set; }

        /// <summary>
        /// Tells the component to load its settings.
        /// </summary>
        void LoadComponentSettings();

        /// <summary>
        /// Tells the component to save its settings.
        /// </summary>
        void SaveComponentSettings();

        /// <summary>
        /// Tells the component to reset its settings. Typically, the component can call Reset on its settings class(es).
        /// </summary>
        void ResetComponentSettings();
    }
}
