// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        private bool StartCore(ProcessStartInfo startInfo)
        {
            return startInfo.UseShellExecute
                ? throw new PlatformNotSupportedException(SR.UseShellExecuteNotSupported)
                : StartWithCreateProcess(startInfo);
        }

        public IntPtr MainWindowHandle => IntPtr.Zero;

        private bool CloseMainWindowCore() => false;

        public string MainWindowTitle => string.Empty;

        public bool Responding => true;

        private bool WaitForInputIdleCore(int milliseconds) => throw new InvalidOperationException(SR.InputIdleUnkownError);

        public void Kill(bool entireProcessTree) => throw new PlatformNotSupportedException();
    }
}
