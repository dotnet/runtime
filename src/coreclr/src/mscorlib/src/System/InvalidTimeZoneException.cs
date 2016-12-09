// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
   [Serializable]
   [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
   public class InvalidTimeZoneException : Exception
   {
       public InvalidTimeZoneException(String message)
           : base(message) { }

       public InvalidTimeZoneException(String message, Exception innerException)
           : base(message, innerException) { }

       protected InvalidTimeZoneException(SerializationInfo info, StreamingContext context)
           : base(info, context) { }

       public InvalidTimeZoneException() { }
   }
}
