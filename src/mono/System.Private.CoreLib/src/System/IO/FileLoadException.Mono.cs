// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public partial class FileLoadException
    {
        internal static string FormatFileLoadExceptionMessage(string? fileName, int _ /*hResult*/)
        {
            return fileName != null ?
                $"Could not load file or assembly '{fileName}' or one of its dependencies." :
                "Could not load file or assembly or one of its dependencies.";
        }
    }
}
