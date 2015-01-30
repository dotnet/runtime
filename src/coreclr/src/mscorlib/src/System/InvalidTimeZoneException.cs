// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System {
   using  System.Runtime.Serialization;
   using  System.Runtime.CompilerServices;

   [Serializable]
   [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
   [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
   public class InvalidTimeZoneException : Exception {
       public InvalidTimeZoneException(String message)
           : base(message) { }

       public InvalidTimeZoneException(String message, Exception innerException)
           : base(message, innerException) { }

       protected InvalidTimeZoneException(SerializationInfo info, StreamingContext context)
           : base(info, context) { }

       public InvalidTimeZoneException() { }
   }
}
