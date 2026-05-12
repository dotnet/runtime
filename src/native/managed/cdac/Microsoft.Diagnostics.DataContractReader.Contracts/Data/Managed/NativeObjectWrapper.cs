// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

internal sealed class NativeObjectWrapper : IData<NativeObjectWrapper>
{
    private const string FullyQualifiedName = "System.Runtime.InteropServices.ComWrappers+NativeObjectWrapper";

    public static TypeHandle TypeHandle(Target target)
        => target.Contracts.ManagedTypeSource.GetTypeHandle(FullyQualifiedName);

    static NativeObjectWrapper IData<NativeObjectWrapper>.Create(Target target, TargetPointer address) => new NativeObjectWrapper();

    private NativeObjectWrapper() { }
}
