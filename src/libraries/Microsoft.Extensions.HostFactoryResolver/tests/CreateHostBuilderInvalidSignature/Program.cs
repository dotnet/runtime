// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MockHostTypes;

namespace CreateHostBuilderInvalidSignature
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var webHost = CreateHostBuilder(null, args).Build();
        }

        // Extra parameter
        private static IHostBuilder CreateHostBuilder(object extraParam, string[] args) => null;
    }
}
