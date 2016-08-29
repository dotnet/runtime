// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System {
   using  System.Runtime.Serialization;
   using  System.Runtime.CompilerServices;

   [Serializable]
#if !FEATURE_CORECLR
   [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
   [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
   public class TimeZoneNotFoundException : Exception {
       public TimeZoneNotFoundException(String message)
           : base(message) { }

       public TimeZoneNotFoundException(String message, Exception innerException)
           : base(message, innerException) { }

       protected TimeZoneNotFoundException(SerializationInfo info, StreamingContext context)
           : base(info, context) { }

       public TimeZoneNotFoundException() { }
   }
}
