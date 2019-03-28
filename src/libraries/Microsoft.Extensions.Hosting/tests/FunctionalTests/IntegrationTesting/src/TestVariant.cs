// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit.Abstractions;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public class TestVariant : IXunitSerializable
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

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Skip), Skip, typeof(string));
            info.AddValue(nameof(Tfm), Tfm, typeof(string));
            info.AddValue(nameof(ApplicationType), ApplicationType, typeof(ApplicationType));
            info.AddValue(nameof(Architecture), Architecture, typeof(RuntimeArchitecture));
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Skip = info.GetValue<string>(nameof(Skip));
            Tfm = info.GetValue<string>(nameof(Tfm));
            ApplicationType = info.GetValue<ApplicationType>(nameof(ApplicationType));
            Architecture = info.GetValue<RuntimeArchitecture>(nameof(Architecture));
        }
    }
}
