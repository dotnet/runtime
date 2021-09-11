// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace NoSpecialEntryPointPattern
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder().Build();
        }
    }
}
