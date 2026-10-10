// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(ComWrappersObject.ManagedTypeName)]
internal sealed partial class ComWrappersObject : IData<ComWrappersObject>
{
    internal const string ManagedTypeName = "System.Runtime.InteropServices.ComWrappersObject";

    [Field("_nativeObjectWrapper")]
    public partial TargetPointer NativeObjectWrapper { get; }
}
