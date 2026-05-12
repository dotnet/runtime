// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

internal sealed class ComWrappers : IData<ComWrappers>
{
    private const string FullyQualifiedName = "System.Runtime.InteropServices.ComWrappers";
    private const string AllManagedObjectWrapperTableFieldName = "s_allManagedObjectWrapperTable";
    private const string NativeObjectWrapperTableFieldName = "s_nativeObjectWrapperTable";

    public static TypeHandle TypeHandle(Target target)
        => target.Contracts.ManagedTypeSource.GetTypeHandle(FullyQualifiedName);

    // Both static fields are managed object references (the ConditionalWeakTable<,> instances).
    // The accessors dereference the static slot and return the object pointer, or
    // TargetPointer.Null when the static slot is not yet allocated (e.g. ComWrappers has not
    // been used in this process). Callers depend on the null-return contract — throwing here
    // would prevent them from reporting S_FALSE / "no data" through the Legacy SOS DAC path.
    public static TargetPointer AllManagedObjectWrapperTable(Target target)
        => target.Contracts.ManagedTypeSource.TryGetStaticFieldAddress(FullyQualifiedName, AllManagedObjectWrapperTableFieldName, out TargetPointer address)
            ? target.ReadPointer(address)
            : TargetPointer.Null;

    public static TargetPointer NativeObjectWrapperTable(Target target)
        => target.Contracts.ManagedTypeSource.TryGetStaticFieldAddress(FullyQualifiedName, NativeObjectWrapperTableFieldName, out TargetPointer address)
            ? target.ReadPointer(address)
            : TargetPointer.Null;

    static ComWrappers IData<ComWrappers>.Create(Target target, TargetPointer address) => new ComWrappers();

    private ComWrappers() { }
}
