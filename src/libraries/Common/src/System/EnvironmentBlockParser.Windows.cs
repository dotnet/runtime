// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.InteropServices;

namespace System
{
    internal static unsafe class EnvironmentBlockParser
    {
        internal static Hashtable Parse(char* block)
        {
            // Format for an environment block is:
            //     [=HiddenVar=value\0]* [Variable=value\0]* \0
            // See the description of Environment Blocks in MSDN's CreateProcess
            // page (null-terminated array of null-terminated strings). Note
            // the =HiddenVar's aren't always at the beginning.

            // Copy strings out, parsing into pairs and inserting into the table.
            // The first few environment variable entries start with an '='.
            // The current working directory of every drive (except for those drives
            // you haven't cd'ed into in your DOS window) are stored in the
            // environment block (as =C:=pwd) and the program's exit code is
            // as well (=ExitCode=00000000).
            Hashtable results = new Hashtable();
            char* currentPtr = block;
            while (true)
            {
                ReadOnlySpan<char> variable = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(currentPtr);
                if (variable.IsEmpty)
                {
                    break;
                }

                // Find the = separating the key and value. We skip entries that begin with =. We also skip entries that don't
                // have =, which can happen on some older OSes when the environment block gets corrupted.
                int i = variable.IndexOf('=');
                if (i > 0)
                {
                    // Add the key and value.
                    string key = new string(variable.Slice(0, i));
                    string value = new string(variable.Slice(i + 1));
                    try
                    {
                        // Add may throw if the environment block was corrupted leading to duplicate entries.
                        // We allow such throws and eat them (rather than proactively checking for duplication)
                        // to provide a non-fatal notification about the corruption.
                        results.Add(key, value);
                    }
                    catch (ArgumentException) { }
                }

                // Move to the end of this variable, after its terminator.
                currentPtr += variable.Length + 1;
            }

            return results;
        }
    }
}
