// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

using System.Diagnostics;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Provides access
    ///       to get and set option values for a designer.
    ///    </para>
    /// </summary>
    public interface IDesignerOptionService
    {
        /// <summary>
        ///    <para>Gets the value of an option defined in this package.</para>
        /// </summary>
        object GetOptionValue(string pageName, string valueName);

        /// <summary>
        ///    <para>Sets the value of an option defined in this package.</para>
        /// </summary>
        void SetOptionValue(string pageName, string valueName, object value);
    }
}

