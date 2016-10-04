// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

using System.Diagnostics;

using System;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>Provides a set of utilities
    ///       for analyzing and identifying inherited components.</para>
    /// </summary>
    public interface IInheritanceService
    {
        /// <summary>
        ///    <para>
        ///       Adds inherited components from the specified component to the specified container.
        ///    </para>
        /// </summary>
        void AddInheritedComponents(IComponent component, IContainer container);

        /// <summary>
        ///    <para>
        ///       Gets the inheritance attribute of the specified
        ///       component. If the component is not being inherited, this method will return the
        ///       value <see cref='System.ComponentModel.InheritanceAttribute.NotInherited'/>. 
        ///       Otherwise it will return the inheritance attribute for this component.      
        ///    </para>
        /// </summary>
        InheritanceAttribute GetInheritanceAttribute(IComponent component);
    }
}

