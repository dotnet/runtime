// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Ecma335
{
    /// <summary>
    /// Provides an extension method to access the TypeDefinitionId column of the ExportedType table.
    /// </summary>
    public static class ExportedTypeExtensions
    {
        /// <summary>
        /// Gets a hint at the likely row number of the target type in the TypeDef table of its module. 
        /// If the namespaces and names do not match, resolution falls back to a full search of the 
        /// target TypeDef table. Ignored and should be zero if <see cref="ExportedType.IsForwarder"/> is
        /// true.
        /// </summary>
        public static int GetTypeDefinitionId(this ExportedType exportedType)
        {
            return exportedType.reader.ExportedTypeTable.GetTypeDefId(exportedType.rowId);
        }
    }
}
