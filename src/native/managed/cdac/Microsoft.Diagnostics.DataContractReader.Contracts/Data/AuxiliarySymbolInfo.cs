// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.AuxiliarySymbolInfo))]
internal sealed partial class AuxiliarySymbolInfo : IData<AuxiliarySymbolInfo>
{
    /// <summary>
    /// Code address of the auxiliary symbol. Named <c>CodeAddress</c> on the C#
    /// side to avoid colliding with the generator-emitted <c>Address</c>
    /// instance property; aliased to the descriptor field <c>Address</c>.
    /// </summary>
    [Field("Address")] public TargetCodePointer CodeAddress { get; }
    [Field] public TargetPointer Name { get; }
}
