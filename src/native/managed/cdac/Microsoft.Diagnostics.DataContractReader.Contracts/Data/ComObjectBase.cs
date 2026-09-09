// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(ComObjectBase.ManagedTypeName)]
internal sealed partial class ComObjectBase : IData<ComObjectBase>
{
    internal const string ManagedTypeName = "System.Runtime.InteropServices.ComObjectBase";

    [Field("_nativeObjectWrapper")]
    public partial TargetPointer NativeObjectWrapper { get; }
}
