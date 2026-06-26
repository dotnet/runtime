// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Delegate))]
internal sealed partial class Delegate : IData<Delegate>
{
    [Field] public TargetPointer Target { get; }
    [Field] public TargetCodePointer MethodPtr { get; }
    [Field] public TargetCodePointer MethodPtrAux { get; }
}

[CdacType(nameof(DataType.MulticastDelegate))]
internal sealed partial class MulticastDelegate : IData<MulticastDelegate>
{
    [Field] public TargetPointer InvocationList { get; }
    [Field] public TargetNInt InvocationCount { get; }
}
