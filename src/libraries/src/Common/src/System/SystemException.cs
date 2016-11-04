// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

// TODO: remove this stub once System.SystemException is added to UWP
namespace System {
 
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class SystemException : Exception
    {
        public SystemException() 
            : base("System error.") {
        }
        
        public SystemException(String message) 
            : base(message) {
        }
        
        public SystemException(String message, Exception innerException) 
            : base(message, innerException) {
        }

        protected SystemException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}