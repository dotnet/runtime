// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** 
**
** Purpose: Exception class for attempt to access an unloaded AppDomain
**
**
=============================================================================*/


using System.Runtime.Serialization;

namespace System
{
    internal class AppDomainUnloadedException : SystemException
    {
        public AppDomainUnloadedException()
            : base(SR.Arg_AppDomainUnloadedException)
        {
            HResult = __HResults.COR_E_APPDOMAINUNLOADED;
        }

        //
        //This constructor is required for serialization.
        //
        protected AppDomainUnloadedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}

