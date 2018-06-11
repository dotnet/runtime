// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies whether the template can be bound one-way or two-way.
    /// </summary>
    public enum BindingDirection
    {
        /// <summary>
        /// The template can only accept property values. Used with a generic ITemplate.
        /// </summary>
        OneWay = 0,

        /// <summary>
        /// The template can accept and expose property values. Used with an IBindableTemplate.
        /// </summary>
        TwoWay = 1
    }
}
