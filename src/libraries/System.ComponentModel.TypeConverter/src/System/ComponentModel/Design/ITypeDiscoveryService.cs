// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// The type discovery service is used to discover available types at design time,
    /// when the consumer doesn't know the names of existing types or referenced assemblies.
    /// </summary>
    public interface ITypeDiscoveryService
    {
        /// <summary>
        ///     Retrieves the list of available types. If baseType is null, all
        ///     types are returned. Otherwise, only types deriving from the
        ///     specified base type are returned. If bool excludeGlobalTypes is false, 
        ///     types from all referenced assemblies are checked. Otherwise,
        ///     only types from non-GAC referenced assemblies are checked. 
        /// </summary>
        ICollection GetTypes(Type baseType, bool excludeGlobalTypes);
    }
}
