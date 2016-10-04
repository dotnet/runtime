// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para> Provides the base class for a custom component 
    ///       editor.</para>
    /// </summary>
    public abstract class ComponentEditor
    {
        /// <summary>
        ///    <para>Gets a value indicating whether the component was modified.</para>
        /// </summary>
        public bool EditComponent(object component)
        {
            return EditComponent(null, component);
        }

        /// <summary>
        ///    <para>Gets a value indicating whether the component was modified.</para>
        /// </summary>
        public abstract bool EditComponent(ITypeDescriptorContext context, object component);
    }
}
