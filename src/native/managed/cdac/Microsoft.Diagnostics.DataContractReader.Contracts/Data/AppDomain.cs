// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.AppDomain))]
internal sealed partial class AppDomain : IData<AppDomain>
{
    [Field] public TargetPointer RootAssembly { get; }

    [FieldAddress]
    public TargetPointer AssemblyList { get; }

    [Field] public TargetPointer FriendlyName { get; }
}
