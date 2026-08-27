// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    /// <summary>
    /// Simple wrapper to safely disable the normal media insertion prompt for
    /// removable media (floppies, cds, memory cards, etc.)
    /// </summary>
    /// <remarks>
    /// Note that removable media file systems lazily load. After starting the OS
    /// they won't be loaded until you have media in the drive- and as such the
    /// prompt won't happen. You have to have had media in at least once to get
    /// the file system to load and then have removed it.
    /// </remarks>
    internal struct DisableMediaInsertionPrompt : IDisposable
    {
        private bool _disableSuccess;
        private uint _oldMode;

        public static DisableMediaInsertionPrompt Create()
        {
            DisableMediaInsertionPrompt prompt = default;
#pragma warning disable CA1416 // SetThreadErrorMode is Windows-only; this type is compiled only for Windows.
            prompt._disableSuccess = Interop.Kernel32.SetThreadErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS, out prompt._oldMode);
#pragma warning restore CA1416
            return prompt;
        }

        public void Dispose()
        {
            if (_disableSuccess)
#pragma warning disable CA1416 // SetThreadErrorMode is Windows-only; this type is compiled only for Windows.
                Interop.Kernel32.SetThreadErrorMode(_oldMode, out _);
#pragma warning restore CA1416
        }
    }
}
