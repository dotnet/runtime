// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public partial class FileNotFoundException
    {
        internal FileNotFoundException(string? fileName, int hResult)
            : base(null)
        {
            HResult = hResult;
            FileName = fileName;
            SetMessageField();
        }
    }
}
