// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///     A "nested container" is an object that logically contains zero or more child
    ///     components and is controlled (owned) by some parent component.
    ///    
    ///     In this context, "containment" refers to logical containment, not visual
    ///     containment.  Components and containers can be used in a variety of
    ///     scenarios, including both visual and non-visual scenarios.
    /// </summary>
    public interface INestedContainer : IContainer
    {
        /// <summary>
        ///     The component that owns this nested container.
        /// </summary>
        IComponent Owner { get; }
    }
}

