// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
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

        //
        //This constructor is required for serialization.
        //
        protected CannotUnloadAppDomainException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
