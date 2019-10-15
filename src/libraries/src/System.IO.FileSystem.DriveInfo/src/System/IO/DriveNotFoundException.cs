// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.IO
{
    //Thrown when trying to access a drive that is not available.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class DriveNotFoundException : IOException
    {
        public DriveNotFoundException()
            : base(SR.IO_DriveNotFound)
        {
            HResult = HResults.COR_E_DIRECTORYNOTFOUND;
        }

        public DriveNotFoundException(string message)
            : base(message)
        {
            HResult = HResults.COR_E_DIRECTORYNOTFOUND;
        }

        public DriveNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_DIRECTORYNOTFOUND;
        }

        protected DriveNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
