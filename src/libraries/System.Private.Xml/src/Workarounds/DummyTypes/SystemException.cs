// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    [System.Runtime.InteropServices.ComVisible(true)]
    internal class SystemException : Exception
    {
        //public SystemException() 
        //    : base(Environment.GetResourceString("Arg_SystemException")) {
        //    SetErrorCode(__HResults.COR_E_SYSTEM);
        //}

        //public SystemException(String message) 
        //    : base(message) {
        //    SetErrorCode(__HResults.COR_E_SYSTEM);
        //}

        public SystemException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_SYSTEM;
        }

        //BinCompat TODO: This is supposed to be defined in System.Exception. Removed the call to the base class as a workaround for now
        protected SystemException(SerializationInfo info, StreamingContext context) //: base(info, context)
        {
        }

        //BinCompat TODO: This is supposed to be defined in System.Exception and not here! Added this as just a workaround
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }
}
