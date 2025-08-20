// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public partial class FileLoadException
    {
#pragma warning disable IDE0060
        internal static string FormatFileLoadExceptionMessage(string? fileName, int hResult)
        {
            return fileName != null ?
                $"Could not load file or assembly '{fileName}' or one of its dependencies." :
                "Could not load file or assembly or one of its dependencies.";
        }
#pragma warning restore IDE0060
    }
}
