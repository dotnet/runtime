// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets whether the named machine is remote or local.</summary>
        /// <param name="machineName">The machine name.</param>
        /// <returns>true if the machine is remote; false if it's local.</returns>
        public static bool IsRemoteMachine(string machineName)
        {
            if (machineName == null)
                throw new ArgumentNullException(nameof(machineName));

            if (machineName.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(machineName), machineName));

            return IsRemoteMachineCore(machineName);
        }
    }
}
