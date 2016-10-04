// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

using System.Diagnostics;
using System;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Provides an interface to get names and references to objects. These
    ///       methods can search using the specified name or reference.
    ///    </para>
    /// </summary>
    public interface IReferenceService
    {
        /// <summary>
        ///    <para>
        ///       Gets the base component that anchors this reference.
        ///    </para>
        /// </summary>
        IComponent GetComponent(object reference);

        /// <summary>
        ///    <para>
        ///       Gets a reference for the specified name.
        ///    </para>
        /// </summary>
        object GetReference(string name);

        /// <summary>
        ///    <para>
        ///       Gets the name for this reference.
        ///    </para>
        /// </summary>
        string GetName(object reference);

        /// <summary>
        ///    <para>
        ///       Gets all available references.
        ///    </para>
        /// </summary>
        object[] GetReferences();

        /// <summary>
        ///    <para>
        ///       Gets all available references of this type.
        ///    </para>
        /// </summary>
        object[] GetReferences(Type baseType);
    }
}
