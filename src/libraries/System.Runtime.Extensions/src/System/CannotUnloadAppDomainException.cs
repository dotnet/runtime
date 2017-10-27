// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class CannotUnloadAppDomainException : SystemException
    {
        internal const int COR_E_CANNOTUNLOADAPPDOMAIN = unchecked((int)0x80131015); // corresponds to __HResults.COR_E_CANNOTUNLOADAPPDOMAIN in corelib
        public CannotUnloadAppDomainException()
            : base(SR.Arg_CannotUnloadAppDomainException)
        {
            HResult = COR_E_CANNOTUNLOADAPPDOMAIN;
        }

        public CannotUnloadAppDomainException(String message)
            : base(message)
        {
            HResult = COR_E_CANNOTUNLOADAPPDOMAIN;
        }

        public CannotUnloadAppDomainException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = COR_E_CANNOTUNLOADAPPDOMAIN;
        }

        protected CannotUnloadAppDomainException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
