// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public partial class TypeLoadException
    {
        // Called by runtime
        internal TypeLoadException(string className, string assemblyName)
            : this(null)
        {
            _className = className;
            _assemblyName = assemblyName;
        }

        // Because the Mono runtime has a dependency to a (string, string) constructor overload,
        // we add a dummy parameter with a default value to minimize native code changes.
        // In order to use this overload, callers should either pass three arguments, or specify
        // one of the parameters by name.
        internal TypeLoadException(string message, string typeName, bool useMessageTypeNameOverload = true)
            : base(message)
        {
            _ = useMessageTypeNameOverload;
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
