// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
