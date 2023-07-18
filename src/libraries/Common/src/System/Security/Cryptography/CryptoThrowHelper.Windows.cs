// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Security.Cryptography;

#if !NETCOREAPP3_1_OR_GREATER
using System.Diagnostics;
using System.Runtime.Serialization;
#endif

namespace Internal.Cryptography
{
    internal static class CryptoThrowHelper
    {
        public static CryptographicException ToCryptographicException(this int hr)
        {
            string message = Interop.Kernel32.GetMessage(hr);

            // If the incoming value is non-negative, it's a Win32 error instead of an
            // HRESULT. We'll convert it to an HRESULT now (subsystem = 0x0007 [win32]).
            if (hr >= 0)
            {
                hr = (hr & 0x0000FFFF) | unchecked((int)0x80070000);
            }

#if NETCOREAPP3_1_OR_GREATER
            return new CryptographicException(message)
            {
                HResult = hr
            };
#else
            // Prior to .NET Core 3.1, the Exception.HResult property was not publicly
            // settable, and CryptographicException did not have a ctor which allowed
            // setting both the message and the HRESULT. We use a subclassed helper
            // type to allow flowing both pieces of data to receivers.

            return new WindowsCryptographicException(hr, message);
#endif
        }

#if !NETCOREAPP3_1_OR_GREATER
        [Serializable]
        private sealed class WindowsCryptographicException : CryptographicException
        {
            // No need for a serialization ctor: we swap the active type during serialization.

            public WindowsCryptographicException(int hr, string message)
                : base(message)
            {
                HResult = hr;
            }

#if NET8_0_OR_GREATER
            [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
            [EditorBrowsable(EditorBrowsableState.Never)]
#endif
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // This exception shouldn't be serialized since it's a private implementation
                // detail potentially copied across multiple different assemblies. We'll
                // instead ask the serializer to pretend that we're a normal CryptographicException.

                info.SetType(typeof(CryptographicException));
                base.GetObjectData(info, context);
            }
        }
#endif
    }
}
