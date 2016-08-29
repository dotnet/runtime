// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading {
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;

    [Serializable]
    [ComVisibleAttribute(false)]
#if !FEATURE_CORECLR
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=2.0.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
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

