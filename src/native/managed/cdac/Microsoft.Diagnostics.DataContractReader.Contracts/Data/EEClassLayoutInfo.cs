// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EEClassLayoutInfo))]
internal sealed partial class EEClassLayoutInfo : IData<EEClassLayoutInfo>
{
    public enum Type : byte
    {
        Auto = 0,
        Sequential = 1,
        Explicit = 2,
        CStruct = 3,
        CUnion = 4,
    }

    // EEClassLayoutInfo::e_BLITTABLE
    private const byte BlittableFlag = 0x01;

    [Field] public partial byte LayoutType { get; }

    [Field] public partial byte AlignmentRequirement { get; }

    [Field] public partial byte Flags { get; }

    public bool IsBlittable => (Flags & BlittableFlag) != 0;
}
