// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// 
    /// </summary>
    public interface IIntellisenseBuilder
    {
        /// <summary>
        /// Return a localized name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Show the builder and return a boolean indicating whether value should be replaced with newValue
        /// - false if the user cancels for example
        ///
        /// language - indicates which language service is calling the builder
        /// value - expression being edited
        /// newValue - return the new value
        /// </summary> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        bool Show(string language, string value, ref string newValue);
    }
}
