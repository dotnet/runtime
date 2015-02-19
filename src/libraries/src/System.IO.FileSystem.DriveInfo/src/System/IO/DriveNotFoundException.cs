// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.IO
{
    //Thrown when trying to access a drive that is not available.
    public class DriveNotFoundException : IOException
    {
        public DriveNotFoundException()
            : base(SR.IO_DriveNotFound)
        {
            HResult = __HResults.COR_E_DIRECTORYNOTFOUND;
        }

        public DriveNotFoundException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_DIRECTORYNOTFOUND;
        }

        public DriveNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_DIRECTORYNOTFOUND;
        }
    }
}
