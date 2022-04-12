// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Text;

namespace System.IO.Compression
{
    public static partial class ZipFileExtensions
    {
        internal static string SanitizeZipFilePath(string zipPath)
        {
            StringBuilder builder = new StringBuilder(zipPath);
            for (int i = 0; i < zipPath.Length; i++)
            {
                if (((int)builder[i] >= 0 && (int)builder[i] < 32) ||
                   builder[i] == '?' || builder[i] == ':' ||
                   builder[i] == '*' || builder[i] == '"' ||
                   builder[i] == '<' || builder[i] == '>' ||
                   builder[i] == '|')
                {
                    builder[i] = '_';
                }
            }
            return builder.ToString();
        }
    }
}
