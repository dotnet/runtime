// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.Collections;
using System.ComponentModel;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>Provides access to, and
    ///       an interface for filtering, the dictionaries that store the properties, attributes, or events of a component.</para>
    /// </summary>
    public interface IDesignerFilter
    {
        /// <summary>
        ///    <para> Allows a designer to filter the set of
        ///       attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/> object.</para>
        /// </summary>
        void PostFilterAttributes(IDictionary attributes);

        /// <summary>
        ///    <para> Allows a designer to filter the set of events
        ///       the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        ///       object.</para>
        /// </summary>
        void PostFilterEvents(IDictionary events);

        /// <summary>
        ///    <para> Allows a designer to filter the set of properties
        ///       the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        ///       object.</para>
        /// </summary>
        void PostFilterProperties(IDictionary properties);

        /// <summary>
        ///    <para> Allows a designer to filter the set of
        ///       attributes the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        ///       object.</para>
        /// </summary>
        void PreFilterAttributes(IDictionary attributes);

        /// <summary>
        ///    <para> Allows a designer to filter the set of events
        ///       the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        ///       object.</para>
        /// </summary>
        void PreFilterEvents(IDictionary events);

        /// <summary>
        ///    <para> Allows a designer to filter the set of properties
        ///       the component being designed will expose through the <see cref='System.ComponentModel.TypeDescriptor'/>
        ///       object.</para>
        /// </summary>
        void PreFilterProperties(IDictionary properties);
    }
}

