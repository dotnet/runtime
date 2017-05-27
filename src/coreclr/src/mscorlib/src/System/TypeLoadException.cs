// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class for type loading failures.
**
**
=============================================================================*/

using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Diagnostics.Contracts;

namespace System
{
    public class TypeLoadException : SystemException, ISerializable
    {
        public TypeLoadException()
            : base(SR.Arg_TypeLoadException)
        {
            HResult = __HResults.COR_E_TYPELOAD;
        }

        public TypeLoadException(String message)
            : base(message)
        {
            HResult = __HResults.COR_E_TYPELOAD;
        }

        public TypeLoadException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_TYPELOAD;
        }

        public override String Message
        {
            get
            {
                SetMessageField();
                return _message;
            }
        }

        private void SetMessageField()
        {
            if (_message == null)
            {
                if ((ClassName == null) &&
                    (ResourceId == 0))
                    _message = SR.Arg_TypeLoadException;

                else
                {
                    if (AssemblyName == null)
                        AssemblyName = SR.IO_UnknownFileName;
                    if (ClassName == null)
                        ClassName = SR.IO_UnknownFileName;

                    String format = null;
                    GetTypeLoadExceptionMessage(ResourceId, JitHelpers.GetStringHandleOnStack(ref format));
                    _message = String.Format(CultureInfo.CurrentCulture, format, ClassName, AssemblyName, MessageArg);
                }
            }
        }

        public String TypeName
        {
            get
            {
                if (ClassName == null)
                    return String.Empty;

                return ClassName;
            }
        }

        // This is called from inside the EE. 
        private TypeLoadException(String className,
                                  String assemblyName,
                                  String messageArg,
                                  int resourceId)
        : base(null)
        {
            HResult = __HResults.COR_E_TYPELOAD;
            ClassName = className;
            AssemblyName = assemblyName;
            MessageArg = messageArg;
            ResourceId = resourceId;

            // Set the _message field eagerly; debuggers look at this field to 
            // display error info. They don't call the Message property.
            SetMessageField();
        }

        protected TypeLoadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetTypeLoadExceptionMessage(int resourceId, StringHandleOnStack retString);

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        // If ClassName != null, GetMessage will construct on the fly using it
        // and ResourceId (mscorrc.dll). This allows customization of the
        // class name format depending on the language environment.
        private String ClassName;
        private String AssemblyName;
        private String MessageArg;
        internal int ResourceId;
    }
}
