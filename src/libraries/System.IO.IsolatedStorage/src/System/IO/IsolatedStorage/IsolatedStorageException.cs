// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.IO.IsolatedStorage
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class IsolatedStorageException : Exception, ISerializable
    {
        private const int COR_E_ISOSTORE = unchecked((int)0x80131450);

        // All the exceptions from IsolatedStorage are wrapped as IsolatedStorageException,
        // this field is used to provide the underlying exception under debugger.
        internal Exception? _underlyingException;

        public IsolatedStorageException()
            : base(SR.IsolatedStorage_Exception)
        {
            HResult = COR_E_ISOSTORE;
        }

        public IsolatedStorageException(string? message)
            : base(message ?? SR.IsolatedStorage_Exception)
        {
            HResult = COR_E_ISOSTORE;
        }

        public IsolatedStorageException(string? message, Exception? inner)
            : base(message ?? SR.IsolatedStorage_Exception, inner)
        {
            HResult = COR_E_ISOSTORE;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected IsolatedStorageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
