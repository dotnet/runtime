// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public class TestVariant
    {
        public string Tfm { get; set; }
        public ApplicationType ApplicationType { get; set; }
        public RuntimeArchitecture Architecture { get; set; }

        public string Skip { get; set; }

        public override string ToString()
        {
            // For debug and test explorer view
            return $"TFM: {Tfm}, Type: {ApplicationType}, Arch: {Architecture}";
        }
    }
}
