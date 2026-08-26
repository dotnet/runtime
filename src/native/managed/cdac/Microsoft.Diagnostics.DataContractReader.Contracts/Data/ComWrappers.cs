// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType("System.Runtime.InteropServices.ComWrappers")]
internal sealed partial class ComWrappers : IData<ComWrappers>
{
    [StaticReference("s_allManagedObjectWrapperTable")]
    public static partial TargetPointer? AllManagedObjectWrapperTable(Target target);

    [StaticReference("s_nativeObjectWrapperTable")]
    public static partial TargetPointer? NativeObjectWrapperTable(Target target);
}
