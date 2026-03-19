// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public partial class BadImageFormatException
    {
        // Do not delete: this is invoked from native code.
        private BadImageFormatException(string? fileName, int hResult)
            : base(null)
        {
            HResult = hResult;
            _fileName = fileName;
            SetMessageField();
        }

        // Do not delete: this is invoked from native code.
        // Used when the requesting assembly chain is known, to provide assembly load dependency context.
        private BadImageFormatException(string? fileName, string? requestingAssemblyChain, int hResult)
            : base(null)
        {
            HResult = hResult;
            _fileName = fileName;
            SetMessageField();
            if (requestingAssemblyChain is not null)
                _message += Environment.NewLine + IO.FileLoadException.FormatRequestingAssemblyChain(requestingAssemblyChain);
        }
    }
}
