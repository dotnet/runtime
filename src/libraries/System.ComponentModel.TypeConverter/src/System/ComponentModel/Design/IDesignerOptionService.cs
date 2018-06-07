// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides access to get and set option values for a designer.
    /// </summary>
    public interface IDesignerOptionService
    {
        /// <summary>
        /// Gets the value of an option defined in this package.
        /// </summary>
        object GetOptionValue(string pageName, string valueName);

        /// <summary>
        /// Sets the value of an option defined in this package.
        /// </summary>
        void SetOptionValue(string pageName, string valueName, object value);
    }
}
