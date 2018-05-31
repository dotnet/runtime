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
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class TypeLoadException : SystemException, ISerializable
    {
        public TypeLoadException()
            : base(SR.Arg_TypeLoadException)
        {
            HResult = HResults.COR_E_TYPELOAD;
        }

        public TypeLoadException(string message)
            : base(message)
        {
            HResult = HResults.COR_E_TYPELOAD;
        }

        public TypeLoadException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_TYPELOAD;
        }

        public override string Message
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

                    string format = null;
                    GetTypeLoadExceptionMessage(ResourceId, JitHelpers.GetStringHandleOnStack(ref format));
                    _message = string.Format(CultureInfo.CurrentCulture, format, ClassName, AssemblyName, MessageArg);
                }
            }
        }

        public string TypeName
        {
            get
            {
                if (ClassName == null)
                    return string.Empty;

                return ClassName;
            }
        }

        // This is called from inside the EE. 
        private TypeLoadException(string className,
                                  string assemblyName,
                                  string messageArg,
                                  int resourceId)
        : base(null)
        {
            HResult = HResults.COR_E_TYPELOAD;
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
            ClassName = info.GetString("TypeLoadClassName");
            AssemblyName = info.GetString("TypeLoadAssemblyName");
            MessageArg = info.GetString("TypeLoadMessageArg");
            ResourceId = info.GetInt32("TypeLoadResourceID");
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetTypeLoadExceptionMessage(int resourceId, StringHandleOnStack retString);

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("TypeLoadClassName", ClassName, typeof(string));
            info.AddValue("TypeLoadAssemblyName", AssemblyName, typeof(string));
            info.AddValue("TypeLoadMessageArg", MessageArg, typeof(string));
            info.AddValue("TypeLoadResourceID", ResourceId);
        }

        // If ClassName != null, GetMessage will construct on the fly using it
        // and ResourceId (mscorrc.dll). This allows customization of the
        // class name format depending on the language environment.
        private string ClassName;
        private string AssemblyName;
        private string MessageArg;
        internal int ResourceId;
    }
}
