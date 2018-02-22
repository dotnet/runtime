// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Win32
{
    /// <devdoc>
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.PowerModeChanged'/> event.</para>
    /// </devdoc>
    public class PowerModeChangedEventArgs : EventArgs
    {
        private readonly PowerModes _mode;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.PowerModeChangedEventArgs'/> class.</para>
        /// </devdoc>
        public PowerModeChangedEventArgs(PowerModes mode)
        {
            _mode = mode;
        }

        /// <devdoc>
        ///    <para>Gets the power mode.</para>
        /// </devdoc>
        public PowerModes Mode
        {
            get
            {
                return _mode;
            }
        }
    }
}

