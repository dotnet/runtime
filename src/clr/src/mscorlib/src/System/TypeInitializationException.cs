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
using System;
using System.Runtime.Serialization;
using System.Globalization;
using System.Security.Permissions;

namespace System {
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class TypeInitializationException : SystemException {
        private String _typeName;

        // This exception is not creatable without specifying the
        //    inner exception.
        private TypeInitializationException()
            : base(Environment.GetResourceString("TypeInitialization_Default")) {
            SetErrorCode(__HResults.COR_E_TYPEINITIALIZATION);
        }

        // This is called from within the runtime.  I believe this is necessary
        // for Interop only, though it's not particularly useful.
        private TypeInitializationException(String message) : base(message) {
            SetErrorCode(__HResults.COR_E_TYPEINITIALIZATION);
        }
        
        public TypeInitializationException(String fullTypeName, Exception innerException) : base(Environment.GetResourceString("TypeInitialization_Type", fullTypeName), innerException) {
            _typeName = fullTypeName;
            SetErrorCode(__HResults.COR_E_TYPEINITIALIZATION);
        }

        internal TypeInitializationException(SerializationInfo info, StreamingContext context) : base(info, context) {
            _typeName = info.GetString("TypeName");
        }

        public String TypeName
        {
            get {
                if (_typeName == null) {
                    return String.Empty;
                }
                return _typeName;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("TypeName",TypeName,typeof(String));
        }

    }
}
