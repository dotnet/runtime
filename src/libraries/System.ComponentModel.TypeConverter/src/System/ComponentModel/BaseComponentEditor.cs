// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel
{
    /// <summary>
    /// Provides the base class for a custom component editor.
    /// </summary>
    public abstract class ComponentEditor
    {
        /// <summary>
        /// Gets a value indicating whether the component was modified.
        /// </summary>
        public bool EditComponent(object component) => EditComponent(null, component);

        /// <summary>
        /// Gets a value indicating whether the component was modified.
        /// </summary>
        public abstract bool EditComponent(ITypeDescriptorContext? context, object component);
    }
}
