// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sample
{
    public partial class Test
    {
        public static Task<int> Main(string[] args)
        {
            Console.WriteLine("Hello World!" + string.Join(", ", args));
            return Task.FromResult(0);
        }
    }
}
