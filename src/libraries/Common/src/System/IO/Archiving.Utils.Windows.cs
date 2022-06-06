// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO
{
    internal static partial class ArchivingUtils
    {
        internal static string SanitizeEntryFilePath(string entryPath)
        {
            StringBuilder builder = new StringBuilder(entryPath);
            for (int i = 0; i < entryPath.Length; i++)
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
