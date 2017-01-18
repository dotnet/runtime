// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies that this object supports
    ///       a simple,
    ///       transacted notification for batch initialization.</para>
    /// </summary>
    public interface ISupportInitialize
    {
        /// <summary>
        ///    <para>
        ///       Signals
        ///       the object that initialization is starting.
        ///    </para>
        /// </summary>
        void BeginInit();

        /// <summary>
        ///    <para>
        ///       Signals the object that initialization is
        ///       complete.
        ///    </para>
        /// </summary>
        void EndInit();
    }
}
