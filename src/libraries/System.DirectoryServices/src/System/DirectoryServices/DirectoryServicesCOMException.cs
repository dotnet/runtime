// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.DirectoryServices
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DirectoryServicesCOMException : COMException, ISerializable
    {
        public DirectoryServicesCOMException() { }

        public DirectoryServicesCOMException(string? message) : base(message) { }

        public DirectoryServicesCOMException(string? message, Exception? inner) : base(message, inner) { }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected DirectoryServicesCOMException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        internal DirectoryServicesCOMException(string? extendedMessage, int extendedError, COMException e) : base(e.Message, e.ErrorCode)
        {
            ExtendedError = extendedError;
            ExtendedErrorMessage = extendedMessage;
        }

        public int ExtendedError { get; }

        public string? ExtendedErrorMessage { get; }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }

    internal static class COMExceptionHelper
    {
        internal static Exception CreateFormattedComException(int hr)
        {
            string errorMsg = new Win32Exception(hr).Message;
            return CreateFormattedComException(new COMException(errorMsg, hr));
        }

        internal static unsafe Exception CreateFormattedComException(COMException e)
        {
            // get extended error information
            const int ErrorBufferLength = 256;
            char* errorBuffer = stackalloc char[ErrorBufferLength];
            char nameBuffer = '\0';
            int error = 0;
            SafeNativeMethods.ADsGetLastError(out error, errorBuffer, ErrorBufferLength, &nameBuffer, 0);

            if (error != 0)
                return new DirectoryServicesCOMException(new string(errorBuffer), error, e);
            else
                return e;
        }
    }
}
