// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal sealed class ImportingParameter : ImportingItem
    {
        public ImportingParameter(ContractBasedImportDefinition definition, ImportType importType)
            : base(definition, importType)
        {
        }
    }
}
