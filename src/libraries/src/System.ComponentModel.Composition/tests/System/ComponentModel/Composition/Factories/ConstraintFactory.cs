// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Linq.Expressions;

namespace System.ComponentModel.Composition.Factories
{
    internal static class ConstraintFactory
    {
        public static Expression<Func<ExportDefinition, bool>> Create(string contractName)
        {
            return definition => definition.ContractName.Equals(contractName);
        }
    }
}
