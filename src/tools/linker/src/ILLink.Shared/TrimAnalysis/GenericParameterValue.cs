// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// This is a System.Type value which represents generic parameter (basically result of typeof(T))
    /// Its actual type is unknown, but it can have annotations.
    /// </summary>
    sealed partial record GenericParameterValue : ValueWithDynamicallyAccessedMembers
    {
        public readonly GenericParameterProxy GenericParameter;
    }
}
