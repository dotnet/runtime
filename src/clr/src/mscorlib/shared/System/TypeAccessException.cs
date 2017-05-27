// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    // TypeAccessException derives from TypeLoadException rather than MemberAccessException because in
    // pre-v4 releases of the runtime TypeLoadException was used in lieu of a TypeAccessException.
    public class TypeAccessException : TypeLoadException
    {
        public TypeAccessException()
            : base(SR.Arg_TypeAccessException)
        {
            HResult = __HResults.COR_E_TYPEACCESS;
        }

        public TypeAccessException(string message)
            : base(message)
        {
            HResult = __HResults.COR_E_TYPEACCESS;
        }

        public TypeAccessException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_TYPEACCESS;
        }

        protected TypeAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
