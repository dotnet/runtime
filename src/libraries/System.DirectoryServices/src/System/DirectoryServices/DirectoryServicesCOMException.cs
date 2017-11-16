// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.DirectoryServices.Interop;

namespace System.DirectoryServices
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.DirectoryServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DirectoryServicesCOMException : COMException, ISerializable
    {
        public DirectoryServicesCOMException() { }

        public DirectoryServicesCOMException(string message) : base(message) { }

        public DirectoryServicesCOMException(string message, Exception inner) : base(message, inner) { }

        protected DirectoryServicesCOMException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        internal DirectoryServicesCOMException(string extendedMessage, int extendedError, COMException e) : base(e.Message, e.ErrorCode)
        {
            ExtendedError = extendedError;
            ExtendedErrorMessage = extendedMessage;
        }

        public int ExtendedError { get; }

        public string ExtendedErrorMessage { get; }
        
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);
        }
    }

    internal class COMExceptionHelper
    {
        internal static Exception CreateFormattedComException(int hr)
        {
            string errorMsg = "";
            StringBuilder sb = new StringBuilder(256);
            int result = SafeNativeMethods.FormatMessageW(SafeNativeMethods.FORMAT_MESSAGE_IGNORE_INSERTS |
                                       SafeNativeMethods.FORMAT_MESSAGE_FROM_SYSTEM |
                                       SafeNativeMethods.FORMAT_MESSAGE_ARGUMENT_ARRAY,
                                       0, hr, 0, sb, sb.Capacity + 1, 0);
            if (result != 0)
            {
                errorMsg = sb.ToString(0, result);
            }
            else
            {
                errorMsg = SR.Format(SR.DSUnknown , Convert.ToString(hr, 16));
            }

            return CreateFormattedComException(new COMException(errorMsg, hr));
        }

        internal static Exception CreateFormattedComException(COMException e)
        {
            // get extended error information
            StringBuilder errorBuffer = new StringBuilder(256);
            StringBuilder nameBuffer = new StringBuilder();
            int error = 0;
            SafeNativeMethods.ADsGetLastError(out error, errorBuffer, 256, nameBuffer, 0);

            if (error != 0)
                return new DirectoryServicesCOMException(errorBuffer.ToString(), error, e);
            else
                return e;
        }
    }
}
