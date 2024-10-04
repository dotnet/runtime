// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO.BinaryFormat
{
    [GenerateReaderWriter]
    public partial class BuildVersionCommandHeader
    {
        public MachPlatform Platform;
        public uint MinimumPlatformVersion { get; set; }
        public uint SdkVersion { get; set; }
        public uint NumberOfTools { get; set; }
    }
}
