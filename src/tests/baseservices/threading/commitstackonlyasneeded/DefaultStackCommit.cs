// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// using System.Configuration;
using System.Runtime.InteropServices;

namespace StackCommitTest
{
    class DefaultStackCommit
    {
        static int Main(string[] args)
        {
            int result = 1;
            bool commitEnabled = false;

            if (Utility.RunTest(commitEnabled))
            {
                result = 100;
            }

            Console.WriteLine(result == 100 ? "Success!" : "FAILED!");
            return result;
        }
    }
}
