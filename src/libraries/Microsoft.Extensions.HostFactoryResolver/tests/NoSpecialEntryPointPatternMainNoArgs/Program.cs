// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Hosting;

namespace NoSpecialEntryPointPatternMainNoArgs
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder().Build();
        }
    }
}
