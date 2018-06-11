// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// The Container and NestedContainer classes will call GetService for ContainerFilterService
    /// each time they need to construct a Components collection for return to a caller. 
    /// ContainerFilterService may return an updated collection of components. This allows
    /// an external service to modify the view of components that are returned from a container.
    /// </summary>
    public abstract class ContainerFilterService
    {
        protected ContainerFilterService()
        {
        }

        /// <summary>
        /// Filters the components collection by optionally returning a new, modified collection. 
        /// The default implementation returns the input collection, thereby performing no filtering.
        /// </summary>
        public virtual ComponentCollection FilterComponents(ComponentCollection components) => components;
    }
}
