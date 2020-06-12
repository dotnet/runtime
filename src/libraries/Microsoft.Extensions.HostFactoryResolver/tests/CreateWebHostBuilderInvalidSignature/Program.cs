// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MockHostTypes;

namespace CreateWebHostBuilderInvalidSignature
{
    public class Program
    {
        static void Main(string[] args)
        {
        }

        // Wrong return type
        public static IWebHost CreateWebHostBuilder(string[] args) => new WebHost();
    }
}
