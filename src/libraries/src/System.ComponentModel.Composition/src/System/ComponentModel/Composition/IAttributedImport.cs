// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition
{
    internal interface IAttributedImport
    {
        string ContractName { get; }
        Type ContractType { get; }
        bool AllowRecomposition { get; }
        CreationPolicy RequiredCreationPolicy { get; }
        ImportCardinality Cardinality { get; }
        ImportSource Source { get; }
    }
}
