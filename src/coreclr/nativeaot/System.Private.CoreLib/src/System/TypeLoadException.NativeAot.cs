// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public partial class TypeLoadException
    {
        internal TypeLoadException(string message, string typeName)
            : base(message)
        {
            HResult = HResults.COR_E_TYPELOAD;
            _className = typeName;
        }

        private void SetMessageField()
        {
            if (_message == null)
                _message = SR.Arg_TypeLoadException;
        }
    }
}
