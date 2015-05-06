// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata.Tests
{
    internal static class MetadataReaderTestHelpers
    {
        internal static IEnumerable<ConstantHandle> GetConstants(this MetadataReader reader)
        {
            for (int i = 1, n = reader.GetTableRowCount(TableIndex.Constant); i <= n; i++)
            {
                yield return MetadataTokens.ConstantHandle(i);
            }
        }

        internal static IEnumerable<StringHandle> GetReferencedModuleNames(this MetadataReader reader)
        {
            for (int i = 1, n = reader.GetTableRowCount(TableIndex.ModuleRef); i <= n; i++)
            {
                yield return reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(i)).Name;
            }
        }

        internal static ClassLayoutRow GetTypeLayout(this MetadataReader reader, TypeDefinitionHandle typeDef)
        {
            int rowId = reader.ClassLayoutTable.FindRow(typeDef);
            if (rowId == 0)
            {
                return default(ClassLayoutRow);
            }

            return GetTypeLayout(reader, rowId);
        }

        internal static ClassLayoutRow GetTypeLayout(this MetadataReader reader, int rowId)
        {
            return new ClassLayoutRow(
                reader.ClassLayoutTable.GetPackingSize(rowId),
                reader.ClassLayoutTable.GetClassSize(rowId),
                reader.ClassLayoutTable.GetParent(rowId));
        }
    }
}
