// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;

class IjwHelper
{
    private const string ijwHostName = "mscoree.dll";

    public static Assembly LoadIjwAssembly(string name)
    {
        // Load our mock ijwhost before we load the IJW assembly.
        NativeLibrary.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ijwHostName));

        return Assembly.Load(name);
    }
}
