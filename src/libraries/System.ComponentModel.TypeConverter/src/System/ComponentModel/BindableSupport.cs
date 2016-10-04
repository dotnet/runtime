// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies which values to say if property or event value can be bound to a data
    ///          element or another property or event's value.</para>
    /// </summary>
    public enum BindableSupport
    {
        /// <summary>
        ///    <para>
        ///       The property or event is bindable.
        ///    </para>
        /// </summary>
        No = 0x00,
        /// <summary>
        ///    <para>
        ///       The property or event is not bindable.
        ///    </para>
        /// </summary>
        Yes = 0x01,
        /// <summary>
        ///    <para>
        ///       The property or event is the default.
        ///    </para>
        /// </summary>
        Default = 0x02,
    }
}
