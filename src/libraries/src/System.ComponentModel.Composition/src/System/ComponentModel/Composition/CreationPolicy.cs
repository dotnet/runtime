// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    /// <summary>
    /// Option placed on a type that controls when the <see cref="Hosting.CompositionContainer"/> creates 
    /// a new instance of a <see cref="Primitives.ComposablePart"/>.
    /// </summary>
    public enum CreationPolicy : int
    {
        /// <summary>
        /// Let the <see cref="Hosting.CompositionContainer"/> choose the most appropriate <see cref="CreationPolicy"/>
        /// for the part given the current context. This is the default <see cref="CreationPolicy"/>, with
        /// the <see cref="Hosting.CompositionContainer"/> choosing <see cref="CreationPolicy.Shared"/> by default
        /// unless the <see cref="Primitives.ComposablePart"/> or importer requests <see cref="CreationPolicy.NonShared"/>.
        /// </summary>
        Any = 0,

        /// <summary>
        /// A single shared instance of the associated <see cref="Primitives.ComposablePart"/> will be created
        /// by the <see cref="Hosting.CompositionContainer"/> and shared by all requestors.
        /// </summary>
        Shared = 1,

        /// <summary>
        /// A new non-shared instance of the associated <see cref="Primitives.ComposablePart"/> will be created
        /// by the <see cref="Hosting.CompositionContainer"/> for every requestor.
        /// </summary>
        NonShared = 2,
    }
}
