// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides an interface to get names and references to objects. These
    /// methods can search using the specified name or reference.
    /// </summary>
    public interface IReferenceService
    {
        /// <summary>
        /// Gets the base component that anchors this reference.
        /// </summary>
        IComponent? GetComponent(object reference);

        /// <summary>
        /// Gets a reference for the specified name.
        /// </summary>
        object? GetReference(string name);

        /// <summary>
        /// Gets the name for this reference.
        /// </summary>
        string? GetName(object reference);

        /// <summary>
        /// Gets all available references.
        /// </summary>
        object[] GetReferences();

        /// <summary>
        /// Gets all available references of this type.
        /// </summary>
        object[] GetReferences(Type baseType);
    }
}
