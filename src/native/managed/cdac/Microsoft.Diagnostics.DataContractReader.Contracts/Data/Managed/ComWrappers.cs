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
    // The accessors dereference the static slot and return the object pointer.
    public static TargetPointer AllManagedObjectWrapperTable(Target target)
        => target.ReadPointer(target.Contracts.ManagedTypeSource.GetStaticFieldAddress(FullyQualifiedName, AllManagedObjectWrapperTableFieldName));

    public static TargetPointer NativeObjectWrapperTable(Target target)
        => target.ReadPointer(target.Contracts.ManagedTypeSource.GetStaticFieldAddress(FullyQualifiedName, NativeObjectWrapperTableFieldName));

    static ComWrappers IData<ComWrappers>.Create(Target target, TargetPointer address) => new ComWrappers();

    private ComWrappers() { }
}
