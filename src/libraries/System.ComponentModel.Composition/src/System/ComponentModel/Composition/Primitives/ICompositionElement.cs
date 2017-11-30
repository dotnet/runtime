// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition.Primitives
{
    /// <summary>
    ///     Represents an element that participates in composition.
    /// </summary>
    public interface ICompositionElement
    {
        /// <summary>
        ///     Gets the display name of the composition element.
        /// </summary>
        /// <value>
        ///     A <see cref="String"/> containing a human-readable display name of the <see cref="ICompositionElement"/>.
        /// </value>
        /// <remarks>
        ///     <note type="implementnotes">
        ///         Implementors of this property should never return <see langword="null"/> or an empty 
        ///         string ("").
        ///     </note>
        /// </remarks>
        string DisplayName
        {
            get;
        }

        /// <summary>
        ///     Gets the composition element from which the current composition element
        ///     originated.
        /// </summary>
        /// <value>
        ///     A <see cref="ICompositionElement"/> from which the current 
        ///     <see cref="ICompositionElement"/> originated, or <see langword="null"/> 
        ///     if the <see cref="ICompositionElement"/> is the root composition element.
        /// </value>
        ICompositionElement Origin
        {
            get;
        }
    }
}
