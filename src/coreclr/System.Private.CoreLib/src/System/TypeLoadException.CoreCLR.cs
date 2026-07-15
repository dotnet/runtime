// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public partial class TypeLoadException : SystemException
    {
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

        [UnmanagedCallersOnly]
        internal static unsafe void Create(char* pClassName, char* pAssemblyName, char* pMessageArg, int resourceId, object* pResult, Exception* pException)
        {
            try
            {
                string? className = pClassName is not null ? new string(pClassName) : null;
                string? assemblyName = pAssemblyName is not null ? new string(pAssemblyName) : null;
                string? messageArg = pMessageArg is not null ? new string(pMessageArg) : null;
                *pResult = new TypeLoadException(className, assemblyName, messageArg, resourceId);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
    }
}
