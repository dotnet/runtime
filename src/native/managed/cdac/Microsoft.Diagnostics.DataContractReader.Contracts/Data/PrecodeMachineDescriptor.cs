// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PrecodeMachineDescriptor))]
internal sealed partial class PrecodeMachineDescriptor : IData<PrecodeMachineDescriptor>
{
    private byte[]? _fixupBytes;
    private bool _fixupBytesRead;
    private byte[]? _fixupIgnoredBytes;
    private bool _fixupIgnoredBytesRead;
    private byte[]? _stubBytes;
    private bool _stubBytesRead;
    private byte[]? _stubIgnoredBytes;
    private bool _stubIgnoredBytesRead;

    [Field] public partial byte InvalidPrecodeType { get; }
    [Field] public partial byte StubPrecodeType { get; }
    [Field] public partial uint StubCodePageSize { get; }

    [Field] public partial byte? OffsetOfPrecodeType { get; } // Not present for version 3 and above
    [Field] public partial byte? ReadWidthOfPrecodeType { get; } // Not present for version 3 and above
    [Field] public partial byte? ShiftOfPrecodeType { get; } // Not present for version 3 and above
    [Field] public partial byte? PInvokeImportPrecodeType { get; }
    [Field] public partial byte? FixupPrecodeType { get; }
    [Field] public partial byte? ThisPointerRetBufPrecodeType { get; }
    [Field] public partial byte? InterpreterPrecodeType { get; } // May be present for version 3 and above
    [Field] public partial byte? UMEntryPrecodeType { get; } // May be present for version 3 and above
    [Field] public partial byte? DynamicHelperPrecodeType { get; } // May be present for version 3 and above
    [Field] public partial byte? FixupStubPrecodeSize { get; } // Present for version 3 and above

    [DataDescriptorDependency(nameof(FixupStubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(FixupBytes), "uint8[]")]
    public byte[]? FixupBytes
        => ReadBytes(ref _fixupBytes, ref _fixupBytesRead, FixupStubPrecodeSize, nameof(FixupBytes));

    [DataDescriptorDependency(nameof(FixupStubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(FixupIgnoredBytes), "uint8[]")]
    public byte[]? FixupIgnoredBytes
        => ReadBytes(ref _fixupIgnoredBytes, ref _fixupIgnoredBytesRead, FixupStubPrecodeSize, nameof(FixupIgnoredBytes));

    [Field] public partial byte? StubPrecodeSize { get; } // Present for version 3 and above

    [DataDescriptorDependency(nameof(StubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(StubBytes), "uint8[]")]
    public byte[]? StubBytes
        => ReadBytes(ref _stubBytes, ref _stubBytesRead, StubPrecodeSize, nameof(StubBytes));

    [DataDescriptorDependency(nameof(StubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(StubIgnoredBytes), "uint8[]")]
    public byte[]? StubIgnoredBytes
        => ReadBytes(ref _stubIgnoredBytes, ref _stubIgnoredBytesRead, StubPrecodeSize, nameof(StubIgnoredBytes));

    private byte[]? ReadBytes(ref byte[]? bytes, ref bool isRead, byte? size, string fieldName)
    {
        if (!isRead)
        {
            if (size is byte length)
            {
                Target.TypeInfo type = _target.GetTypeInfo(DataType.PrecodeMachineDescriptor);
                bytes = new byte[length];
                _target.ReadBuffer(Address + (ulong)type.Fields[fieldName].Offset, bytes);
            }

            isRead = true;
        }

        return bytes;
    }
}
