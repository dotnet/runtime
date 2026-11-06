// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed partial class Thread
    {
        private bool _isPendingInterrupt;

        private void PlatformSpecificInitialize()
        {
        }

        private void PlatformSpecificInitializeExistingThread()
        {
        }

        public void Interrupt() => _isPendingInterrupt = true;

        internal void ThrowIfPendingInterrupt()
        {
            if (_isPendingInterrupt)
            {
                _isPendingInterrupt = false;
                throw new ThreadInterruptedException();
            }
        }
    }
}
