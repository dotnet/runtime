// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Delegate))]
internal sealed partial class Delegate : IData<Delegate>
{
    [Field] public partial TargetPointer HelperObject { get; }
    [Field] public partial TargetPointer Target { get; }
    [Field] public partial TargetCodePointer MethodPtr { get; }
    [Field] public partial TargetCodePointer MethodPtrAux { get; }
    [Field] public partial TargetNInt ExtraData { get; }
}
