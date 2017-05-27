// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Reflection
{
    public class TargetException : ApplicationException
    {
        public TargetException()
            : this(null)
        {
        }

        public TargetException(string message)
            : this(message, null)
        {
        }

        public TargetException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_TARGET;
        }

        protected TargetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
