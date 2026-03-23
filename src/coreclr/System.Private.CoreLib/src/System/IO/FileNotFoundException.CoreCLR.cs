// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public partial class FileNotFoundException
    {
        // Do not delete: this is invoked from native code.
        private FileNotFoundException(string? fileName, int hResult)
            : base(null)
        {
            HResult = hResult;
            FileName = fileName;
            SetMessageField();
        }

        // Do not delete: this is invoked from native code.
        // Used when the requesting assembly is known, to provide assembly load dependency context.
        private FileNotFoundException(string? fileName, string? requestingAssemblyChain, int hResult)
            : base(null)
        {
            HResult = hResult;
            FileName = fileName;
            SetMessageField();
            if (requestingAssemblyChain is not null)
                _message += Environment.NewLine + FileLoadException.FormatRequestingAssemblyChain(requestingAssemblyChain);
        }
    }
}
