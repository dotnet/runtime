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
        public CannotUnloadAppDomainException()
            : base(SR.Arg_CannotUnloadAppDomainException)
        {
            HResult = HResults.COR_E_CANNOTUNLOADAPPDOMAIN;
        }

        public CannotUnloadAppDomainException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_CANNOTUNLOADAPPDOMAIN;
        }

        public CannotUnloadAppDomainException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_CANNOTUNLOADAPPDOMAIN;
        }

        protected CannotUnloadAppDomainException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
