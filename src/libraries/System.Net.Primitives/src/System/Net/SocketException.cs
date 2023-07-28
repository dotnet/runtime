// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Net.Sockets
{
    /// <summary>Provides socket exceptions to the application.</summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class SocketException : Win32Exception
    {
        /// <summary>The SocketError or Int32 specified when constructing the exception.</summary>
        /// <remarks>Based on platform, this may or may not be the same as the underlying NativeErrorCode.</remarks>
        private readonly SocketError _errorCode;

        /// <summary>Creates a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the specified error code.</summary>
        public SocketException(int errorCode) : this((SocketError)errorCode)
        {
            // NOTE: SocketException(SocketError) isn't exposed publicly.  As a result, code with a SocketError calls
            // this ctor, e.g.
            //     SocketError error = ...;
            //     throw new SocketException((int)error);
            // That means we need to assume the errorCode is a SocketError value, rather than a platform-specific error code.
            // Hence, no translation on the supplied code.  This does mean on Unix there's a difference between:
            //     new SocketException(); // will treat the last error as a native error code and translate it appropriately
            // and:
            //     new SocketException(Marshal.GetLastPInvokeError()); // will treat the last error as a SocketError, inappropriately
            // but that's the least bad option right now.
        }

        /// <summary>Initializes a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the specified error code and optional message.</summary>
        public SocketException(int errorCode, string? message) : this((SocketError)errorCode, message)
        {
        }

        /// <summary>Creates a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the specified error code as SocketError.</summary>
        internal SocketException(SocketError socketError) : base(GetNativeErrorForSocketError(socketError))
        {
            _errorCode = socketError;
        }

        /// <summary>Initializes a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the specified error code as SocketError and optional message.</summary>
        internal SocketException(SocketError socketError, string? message) : base(GetNativeErrorForSocketError(socketError), message)
        {
            _errorCode = socketError;
        }

        public override string Message => base.Message;

        public SocketError SocketErrorCode => _errorCode;

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected SocketException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"{NativeErrorCode}:{Message}");
        }

        public override int ErrorCode => base.NativeErrorCode;
    }
}
