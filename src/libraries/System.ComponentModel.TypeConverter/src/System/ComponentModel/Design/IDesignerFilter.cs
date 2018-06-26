// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides access to, and an interface for filtering, the dictionaries that store the
    /// properties, attributes, or events of a component.
    /// </summary>
    public interface IDesignerFilter
    {
        /// <summary>
        /// Allows a designer to filter the set of attributes the component being designed will expose
        /// through the <see cref='System.ComponentModel.TypeDescriptor'/> object.
        /// </summary>
        void PostFilterAttributes(IDictionary attributes);

        /// <summary>
        /// Allows a designer to filter the set of events the component being designed will expose
        /// through the <see cref='System.ComponentModel.TypeDescriptor'/> object.
        /// </summary>
        void PostFilterEvents(IDictionary events);

        /// <summary>
        /// Allows a designer to filter the set of properties the component being designed will expose
        /// through the <see cref='System.ComponentModel.TypeDescriptor'/> object.
        /// </summary>
        void PostFilterProperties(IDictionary properties);

        /// <summary>
        /// Allows a designer to filter the set of attributes the component being designed will expose
        /// through the <see cref='System.ComponentModel.TypeDescriptor'/> object.
        /// </summary>
        void PreFilterAttributes(IDictionary attributes);

        /// <summary>
        /// Allows a designer to filter the set of events the component being designed will expose
        /// through the <see cref='System.ComponentModel.TypeDescriptor'/> object.
        /// </summary>
        void PreFilterEvents(IDictionary events);

        /// <summary>
        /// Allows a designer to filter the set of properties the component being designed will expose
        /// through the <see cref='System.ComponentModel.TypeDescriptor'/> object.
        /// </summary>
        void PreFilterProperties(IDictionary properties);
    }
}

