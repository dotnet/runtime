// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    /// <summary>Handles a <see cref="PosixSignal"/>.</summary>
    public sealed partial class PosixSignalRegistration : IDisposable
    {
        /// <summary>The state associated with this registration.</summary>
        /// <remarks>
        /// This is separate from the registration instance so that this token may be stored
        /// in a statically rooted table, with a finalizer on the registration able to remove it.
        /// </remarks>
        private Token? _token;

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
        public static PosixSignalRegistration Create(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return Register(signal, handler);
        }

        /// <summary>Initializes the registration to wrap the specified token.</summary>
        private PosixSignalRegistration(Token token) => _token = token;

        /// <summary>Unregister the handler.</summary>
        public void Dispose()
        {
            Unregister();
            GC.SuppressFinalize(this);
        }

        /// <summary>Unregister the handler.</summary>
        ~PosixSignalRegistration() => Unregister();

        /// <summary>The state associated with a registration.</summary>
        private sealed class Token
        {
            public Token(PosixSignal signal, Action<PosixSignalContext> handler)
            {
                Signal = signal;
                Handler = handler;
            }

            public Token(PosixSignal signal, int sigNo, Action<PosixSignalContext> handler)
            {
                Signal = signal;
                Handler = handler;
                SigNo = sigNo;
            }

            public PosixSignal Signal { get; }
            public Action<PosixSignalContext> Handler { get; }
            public int SigNo { get; }
        }
    }
}
