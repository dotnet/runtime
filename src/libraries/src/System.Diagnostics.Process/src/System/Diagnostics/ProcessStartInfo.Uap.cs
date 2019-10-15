// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    public sealed partial class ProcessStartInfo
    {
        public string[] Verbs => Array.Empty<string>();

        // Not available on Uap as ShellExecuteEx isn't whitelisted. Note that using ShellExecuteEx
        // also depends on being able to change the apartment state for a thread to STA (CLR is MTA).
        public bool UseShellExecute
        {
            get { return false; }
            set { if (value) throw new PlatformNotSupportedException(SR.UseShellExecuteNotSupported); }
        }
    }
}
