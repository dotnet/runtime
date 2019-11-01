// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal interface IReflectionPartCreationInfo : ICompositionElement
    {
        Type GetPartType();
        Lazy<Type> GetLazyPartType();
        ConstructorInfo GetConstructor();
        IDictionary<string, object> GetMetadata();
        IEnumerable<ExportDefinition> GetExports();
        IEnumerable<ImportDefinition> GetImports();
        bool IsDisposalRequired { get; }
        bool IsIdentityComparison { get; }
    }
}
