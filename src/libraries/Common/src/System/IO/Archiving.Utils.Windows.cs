// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO
{
    internal static partial class ArchivingUtils
    {
        internal static string SanitizeEntryFilePath(string entryPath)
        {
            // Find the first illegal character in the entry path.
            for (int i = 0; i < entryPath.Length; i++)
            {
                switch (entryPath[i])
                {
                    // We found at least one character that needs to be replaced.
                    case < (char)32 or '?' or ':' or '*' or '"' or '<' or '>' or '|':
                        return string.Create(entryPath.Length, (i, entryPath), (dest, state) =>
                        {
                            string entryPath = state.entryPath;

                            // Copy over to the new string everything until the character, then
                            // substitute for the found character.
                            entryPath.AsSpan(0, state.i).CopyTo(dest);
                            dest[state.i] = '_';

                            // Continue looking for and replacing any more illegal characters.
                            for (int i = state.i + 1; i < entryPath.Length; i++)
                            {
                                char c = entryPath[i];
                                dest[i] = c switch
                                {
                                    < (char)32 or '?' or ':' or '*' or '"' or '<' or '>' or '|' => '_',
                                    _ => c,
                                };
                            }
                        });
                }
            }

            // There weren't any characters to sanitize.  Just return the original string.
            return entryPath;
        }
    }
}
