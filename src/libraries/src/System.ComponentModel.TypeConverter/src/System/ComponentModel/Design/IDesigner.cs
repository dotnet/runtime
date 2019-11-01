// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides the basic framework for building a custom designer.
    /// This interface stores the verbs available to the designer, as well as basic
    /// services for the designer.
    /// </summary>
    public interface IDesigner : IDisposable
    {
        /// <summary>
        /// Gets or sets the base component this designer is designing.
        /// </summary>
        IComponent Component { get; }

        /// <summary>
        /// Gets or sets the design-time verbs supported by the designer.
        /// </summary>
        DesignerVerbCollection Verbs { get; }

        /// <summary>
        /// Performs the default action for this designer.
        /// </summary>
        void DoDefaultAction();

        /// <summary>
        /// Initializes the designer with the given component.
        /// </summary>
        void Initialize(IComponent component);
    }
}
