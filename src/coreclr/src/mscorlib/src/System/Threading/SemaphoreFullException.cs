// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Threading {
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;

    [Serializable()]
    [ComVisibleAttribute(false)]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=2.0.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
    public class SemaphoreFullException : SystemException {
    
        public SemaphoreFullException() : base(Environment.GetResourceString("Threading_SemaphoreFullException")){
        }
    
        public SemaphoreFullException(String message) : base(message) {
        }

        public SemaphoreFullException(String message, Exception innerException) : base(message, innerException) {
        }
        
        protected SemaphoreFullException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }
}

