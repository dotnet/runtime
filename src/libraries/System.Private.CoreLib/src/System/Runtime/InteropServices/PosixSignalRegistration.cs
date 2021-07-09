// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    /// <summary>Handles a <see cref="PosixSignal"/>.</summary>
    public sealed partial class PosixSignalRegistration : IDisposable
    {
        /// <summary>Registers a <paramref name="handler"/> that is invoked when the <paramref name="signal"/> occurs.</summary>
        /// <param name="signal">The signal to register for.</param>
        /// <param name="handler">The handler that gets invoked.</param>
        /// <returns>A <see cref="PosixSignalRegistration"/> instance that can be disposed to unregister the handler.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        /// <exception cref="PlatformNotSupportedException"><paramref name="signal"/> is not supported by the platform.</exception>
        /// <exception cref="IOException">An error occurred while setting up the signal handling or while installing the handler for the specified signal.</exception>
        /// <remarks>
        /// Raw values can be provided for <paramref name="signal"/> on Unix by casting them to <see cref="PosixSignal"/>.
        /// Default handling of the signal can be canceled through <see cref="PosixSignalContext.Cancel"/>.
        /// <see cref="PosixSignal.SIGINT"/> and <see cref="PosixSignal.SIGQUIT"/> can be canceled on both
        /// Windows and on Unix platforms; <see cref="PosixSignal.SIGTERM"/> can only be canceled on Unix.
        /// On Unix, terminal configuration can be canceled for <see cref="PosixSignal.SIGCHLD"/> and <see cref="PosixSignal.SIGCONT"/>.
        /// </remarks>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        public static partial PosixSignalRegistration Create(PosixSignal signal, Action<PosixSignalContext> handler);

        /// <summary>Unregister the handler.</summary>
        public partial void Dispose();

        ~PosixSignalRegistration() => Dispose();
    }
}
