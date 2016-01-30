// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.IO
{
    //Thrown when trying to access a drive that is not available.
    public class DriveNotFoundException : IOException
    {
        public DriveNotFoundException()
            : base(SR.IO_DriveNotFound)
        {
            HResult = HResults.COR_E_DIRECTORYNOTFOUND;
        }

        public DriveNotFoundException(String message)
            : base(message)
        {
            HResult = HResults.COR_E_DIRECTORYNOTFOUND;
        }

        public DriveNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_DIRECTORYNOTFOUND;
        }
    }
}
