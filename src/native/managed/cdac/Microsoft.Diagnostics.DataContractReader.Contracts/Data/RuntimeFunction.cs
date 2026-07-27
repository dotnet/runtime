// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RuntimeFunction))]
internal sealed partial class RuntimeFunction : IData<RuntimeFunction>
{
    [Field] public partial uint BeginAddress { get; }

    // Not all platforms define EndAddress
    [Field] public partial uint? EndAddress { get; }
    [Field] public partial uint UnwindData { get; }
}
