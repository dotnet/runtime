// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Contains the data related to a VirtualMethodIndexAttribute, without references to Roslyn symbols.
    /// See <seealso cref="VirtualMethodIndexCompilationData"/> for a type with a reference to the StringMarshallingCustomType
    /// </summary>
    internal sealed record VirtualMethodIndexData(
        int Index,
        bool ImplicitThisParameter,
        MarshalDirection Direction,
        bool ExceptionMarshallingDefined,
        ExceptionMarshalling ExceptionMarshalling) : InteropAttributeData
    {

        public static VirtualMethodIndexData From(VirtualMethodIndexCompilationData virtualMethodIndex)
            => new VirtualMethodIndexData(
                virtualMethodIndex.Index,
                virtualMethodIndex.ImplicitThisParameter,
                virtualMethodIndex.Direction,
                virtualMethodIndex.ExceptionMarshallingDefined,
                virtualMethodIndex.ExceptionMarshalling)
            {
                IsUserDefined = virtualMethodIndex.IsUserDefined,
                SetLastError = virtualMethodIndex.SetLastError,
                StringMarshalling = virtualMethodIndex.StringMarshalling
            };
    }

    /// <summary>
    /// Contains the data related to a VirtualMethodIndexAttribute, with references to Roslyn symbols.
    /// Use <seealso cref="VirtualMethodIndexData"/> instead when using for incremental compilation state to avoid keeping a compilation alive
    /// </summary>
    internal sealed record VirtualMethodIndexCompilationData(int Index) : InteropAttributeCompilationData
    {
        public bool ImplicitThisParameter { get; init; }

        public MarshalDirection Direction { get; init; }

        public bool ExceptionMarshallingDefined { get; init; }

        public ExceptionMarshalling ExceptionMarshalling { get; init; }
        public INamedTypeSymbol? ExceptionMarshallingCustomType { get; init; }
    }
}
