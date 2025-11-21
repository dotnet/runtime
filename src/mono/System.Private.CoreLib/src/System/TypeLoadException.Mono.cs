// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System
{
    public partial class TypeLoadException
    {
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by runtime")]
        private TypeLoadException(string className, string assemblyName)
            : this(null)
        {
            _className = className;
            _assemblyName = assemblyName;
        }

        // Because the Mono runtime has a dependency on (string, string) constructors of exception types,
        // we add a dummy parameter with a default value.
        // In order to use this overload, callers should specify the typeName parameter by name.
        internal TypeLoadException(string message, string typeName, bool _ = true)
            : base(message)
        {
            HResult = HResults.COR_E_TYPELOAD;
            _className = typeName;
        }

        private void SetMessageField()
        {
            if (_message != null)
                return;

            if (_className == null)
            {
                _message = SR.Arg_TypeLoadException;
                return;
            }

            _message = SR.Format(SR.ClassLoad_General, _className, _assemblyName ?? SR.IO_UnknownFileName);
        }
    }
}
