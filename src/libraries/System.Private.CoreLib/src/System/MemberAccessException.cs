// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

////////////////////////////////////////////////////////////////////////////////
// MemberAccessException
// Thrown when we try accessing a member that we cannot
// access, due to it being removed, private or something similar.
////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    // The MemberAccessException is thrown when trying to access a class
    // member fails.
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MemberAccessException : SystemException
    {
        // Creates a new MemberAccessException with its message string set to
        // the empty string, its HRESULT set to COR_E_MEMBERACCESS,
        // and its ExceptionInfo reference set to null.
        public MemberAccessException()
            : base(SR.Arg_AccessException)
        {
            HResult = HResults.COR_E_MEMBERACCESS;
        }

        // Creates a new MemberAccessException with its message string set to
        // message, its HRESULT set to COR_E_ACCESS,
        // and its ExceptionInfo reference set to null.
        //
        public MemberAccessException(string? message)
            : base(message ?? SR.Arg_AccessException)
        {
            HResult = HResults.COR_E_MEMBERACCESS;
        }

        public MemberAccessException(string? message, Exception? inner)
            : base(message ?? SR.Arg_AccessException, inner)
        {
            HResult = HResults.COR_E_MEMBERACCESS;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected MemberAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
