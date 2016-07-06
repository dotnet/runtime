// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ModuleCore;

namespace XslCompiledTransformTests
{
    internal class Program
    {
        public static int Main()
        {
            var args = @"DocType:XmlDocument  trace:false Host:None";
            return 100 + TestRunner.Execute(args);
        }
    }
}