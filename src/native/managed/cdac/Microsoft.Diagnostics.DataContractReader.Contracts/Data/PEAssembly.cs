// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PEAssembly))]
internal sealed partial class PEAssembly : IData<PEAssembly>
{
    [Field] public partial TargetPointer PEImage { get; }
    [Field] public partial TargetPointer AssemblyBinder { get; }
    [Field] public partial TargetPointer MDImport { get; }
}
