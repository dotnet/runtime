// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests
    {
        private string WriteScriptFile(string directory, string name, int returnValue)
        {
            string filename = Path.Combine(directory, name);
            filename += ".bat";
            File.WriteAllText(filename, $"exit {returnValue}");
            return filename;
        }
    }
}
