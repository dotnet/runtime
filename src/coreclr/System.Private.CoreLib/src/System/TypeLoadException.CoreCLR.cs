// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System
{
    public partial class TypeLoadException : SystemException
    {
        // This is called from inside the EE.
        private TypeLoadException(string? className,
            string? assemblyName,
            string? messageArg,
            int resourceId)
            : base(null)
        {
            HResult = HResults.COR_E_TYPELOAD;
            _className = className;
            _assemblyName = assemblyName;
            _messageArg = messageArg;
            _resourceId = resourceId;

            // Set the _message field eagerly; debuggers look at this field to
            // display error info. They don't call the Message property.
            SetMessageField();
        }

        private void SetMessageField()
        {
            if (_message == null)
            {
                if (_className == null && _resourceId == 0)
                {
                    _message = SR.Arg_TypeLoadException;
                }
                else
                {
                    _assemblyName ??= SR.IO_UnknownFileName;
                    _className ??= SR.IO_UnknownFileName;

                    string? format = null;
                    GetTypeLoadExceptionMessage(_resourceId, new StringHandleOnStack(ref format));
                    _message = string.Format(format!, _className, _assemblyName, _messageArg);
                }
            }
        }

        [LibraryImport(RuntimeHelpers.QCall)]
        private static partial void GetTypeLoadExceptionMessage(int resourceId, StringHandleOnStack retString);
    }
}
