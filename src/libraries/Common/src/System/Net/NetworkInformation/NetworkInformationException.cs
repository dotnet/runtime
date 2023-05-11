// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Net.NetworkInformation
{
    /// <devdoc>
    ///    <para>
    ///       Provides NetworkInformation exceptions to the application.
    ///    </para>
    /// </devdoc>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class NetworkInformationException : Win32Exception
    {
        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.NetworkInformation.NetworkInformationException'/> class with the default error code.
        ///    </para>
        /// </devdoc>
        public NetworkInformationException() : base(Marshal.GetLastPInvokeError())
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Net.NetworkInformation.NetworkInformationException'/> class with the specified error code.
        ///    </para>
        /// </devdoc>
        public NetworkInformationException(int errorCode) : base(errorCode)
        {
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected NetworkInformationException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }

        internal NetworkInformationException(SocketError socketError) : base((int)socketError)
        {
        }

        internal NetworkInformationException(string message) : base(message)
        {
        }

        internal NetworkInformationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int ErrorCode
        {
            // The base class returns the HResult with this property.
            // We need the Win32 error code, hence the override.
            get
            {
                return NativeErrorCode;
            }
        }
    }
}
