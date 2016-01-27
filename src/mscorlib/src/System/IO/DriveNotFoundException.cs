// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//============================================================
//
// 
//
//  Purpose: Exception for accessing a drive that is not available.
//
//
//============================================================
using System;
using System.Runtime.Serialization;

namespace System.IO {

    //Thrown when trying to access a drive that is not availabe.
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class DriveNotFoundException : IOException {
        public DriveNotFoundException() 
            : base(Environment.GetResourceString("Arg_DriveNotFoundException")) {
            SetErrorCode(__HResults.COR_E_DIRECTORYNOTFOUND);
        }
    
        public DriveNotFoundException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_DIRECTORYNOTFOUND);
        }
    
        public DriveNotFoundException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_DIRECTORYNOTFOUND);
        }
        
        protected DriveNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
