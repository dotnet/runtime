// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class to wrap exceptions thrown by
**          a type's class initializer (.cctor).  This is sufficiently
**          distinct from a TypeLoadException, which means we couldn't
**          find the type.
**
**
=============================================================================*/

using System.Globalization;
using System.Runtime.Serialization;

namespace System
{
    public sealed class TypeInitializationException : SystemException
    {
        private String _typeName;

        // This exception is not creatable without specifying the
        //    inner exception.
        private TypeInitializationException()
            : base(SR.TypeInitialization_Default)
        {
            HResult = __HResults.COR_E_TYPEINITIALIZATION;
        }


        public TypeInitializationException(String fullTypeName, Exception innerException)
            : this(fullTypeName, SR.Format(SR.TypeInitialization_Type, fullTypeName), innerException)
        {
        }

        // This is called from within the runtime.  I believe this is necessary
        // for Interop only, though it's not particularly useful.
        internal TypeInitializationException(String message) : base(message)
        {
            HResult = __HResults.COR_E_TYPEINITIALIZATION;
        }

        internal TypeInitializationException(String fullTypeName, String message, Exception innerException)
            : base(message, innerException)
        {
            _typeName = fullTypeName;
            HResult = __HResults.COR_E_TYPEINITIALIZATION;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        public String TypeName
        {
            get
            {
                if (_typeName == null)
                {
                    return String.Empty;
                }
                return _typeName;
            }
        }
    }
}
