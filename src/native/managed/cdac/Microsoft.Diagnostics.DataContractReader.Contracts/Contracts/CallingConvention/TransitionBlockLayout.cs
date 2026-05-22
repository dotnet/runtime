// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

internal sealed class TransitionBlockLayout
{
    public Target Target { get; }
    public int SizeOfTransitionBlock { get; }
    public int ArgumentRegistersOffset { get; }
    public int FirstGCRefMapSlot { get; }
    public int OffsetOfArgs { get; }
    public int OffsetOfFloatArgumentRegisters { get; }
    public int PointerSize { get; }
    public RuntimeInfoArchitecture Architecture { get; }
    public RuntimeInfoOperatingSystem OperatingSystem { get; }

    public TransitionBlockLayout(Target target)
    {
        Target = target;
        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;
        Architecture = runtimeInfo.GetTargetArchitecture();
        OperatingSystem = runtimeInfo.GetTargetOperatingSystem();
        PointerSize = target.PointerSize;

        Target.TypeInfo tbType = target.GetTypeInfo(DataType.TransitionBlock);
        SizeOfTransitionBlock = (int)tbType.Size!;
        ArgumentRegistersOffset = tbType.Fields["ArgumentRegistersOffset"].Offset;
        FirstGCRefMapSlot = tbType.Fields["FirstGCRefMapSlot"].Offset;
        OffsetOfArgs = tbType.Fields["OffsetOfArgs"].Offset;
        OffsetOfFloatArgumentRegisters = tbType.Fields.ContainsKey("OffsetOfFloatArgumentRegisters")
            ? tbType.Fields["OffsetOfFloatArgumentRegisters"].Offset
            : 0;
    }
}
