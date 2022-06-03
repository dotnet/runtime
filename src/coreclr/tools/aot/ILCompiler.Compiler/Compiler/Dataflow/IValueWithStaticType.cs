// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Internal.TypeSystem;

#nullable enable

namespace ILCompiler.Dataflow
{
    interface IValueWithStaticType
    {
        /// <summary>
        /// The IL type of the value, represented as closely as possible, but not always exact.  It can be null, for
        /// example, when the analysis is imprecise or operating on malformed IL.
        /// </summary>
        TypeDesc? StaticType { get; }
    }
}
