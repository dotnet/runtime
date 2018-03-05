// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Win32
{
    /// <devdoc>
    ///    <para> Specifies how the system
    ///       power mode changes.</para>
    /// </devdoc>
    public enum PowerModes
    {
        /// <devdoc>
        ///    <para> The system is about to resume.</para>
        /// </devdoc>
        Resume = 1,

        /// <devdoc>
        ///      The power mode status has changed.  This may
        ///      indicate a weak or charging battery, a transition
        ///      from AC power from battery, or other change in the
        ///      status of the system power supply.
        /// </devdoc>
        StatusChange = 2,

        /// <devdoc>
        ///      The system is about to be suspended.
        /// </devdoc>
        Suspend = 3,
    }
}

