// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class AppDomainUnloadedException : SystemException
    {
        internal const int COR_E_APPDOMAINUNLOADED = unchecked((int)0x80131014); // corresponds to __HResults.COR_E_APPDOMAINUNLOADED in corelib
        public AppDomainUnloadedException()
            : base(SR.Arg_AppDomainUnloadedException)
        {
            HResult = COR_E_APPDOMAINUNLOADED;
        }

        public AppDomainUnloadedException(string? message)
            : base(message)
        {
            HResult = COR_E_APPDOMAINUNLOADED;
        }

        public AppDomainUnloadedException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = COR_E_APPDOMAINUNLOADED;
        }

        protected AppDomainUnloadedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
