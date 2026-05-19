// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

[CdacType(ManagedFullName = "System.Runtime.InteropServices.ComWrappers")]
internal sealed partial class ComWrappers : IData<ComWrappers>
{
    // Both static fields are managed object references (the ConditionalWeakTable<,> instances).
    // The accessors dereference the static slot and return the object pointer, or
    // TargetPointer.Null when the static slot is not yet allocated (e.g. ComWrappers has not
    // been used in this process). Callers depend on the null-return contract -- throwing here
    // would prevent them from reporting S_FALSE / "no data" through the Legacy SOS DAC path.
    [StaticReference("s_allManagedObjectWrapperTable")]
    public static partial TargetPointer AllManagedObjectWrapperTable(Target target);

    [StaticReference("s_nativeObjectWrapperTable")]
    public static partial TargetPointer NativeObjectWrapperTable(Target target);
}
