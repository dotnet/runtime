// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies which values to say if property or event value can be bound to a data
    /// element or another property or event's value.
    /// </summary>
    public enum BindableSupport
    {
        /// <summary>
        /// The property or event is bindable.
        /// </summary>
        No = 0x00,

        /// <summary>
        /// The property or event is not bindable.
        /// </summary>
        Yes = 0x01,

        /// <summary>
        /// The property or event is the default.
        /// </summary>
        Default = 0x02,
    }
}
