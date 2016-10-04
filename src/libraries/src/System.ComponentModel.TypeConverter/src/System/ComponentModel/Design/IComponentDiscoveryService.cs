// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel.Design;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// This service allows design-time enumeration of components across the toolbox
    /// and other available types at design-time.
    /// </summary>
    public interface IComponentDiscoveryService
    {
        /// <summary>
        ///     Retrieves the list of available component types, i.e. types implementing
        ///     IComponent. If baseType is null, all components are retrieved; otherwise
        ///     only component types derived from the specified baseType are returned.
        /// </summary>    
        ICollection GetComponentTypes(IDesignerHost designerHost, Type baseType);
    }
}
