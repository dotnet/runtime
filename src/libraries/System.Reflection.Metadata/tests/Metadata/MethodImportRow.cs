// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata.Tests
{
    internal struct MethodImportRow
    {
        public readonly MethodImport Import;
        public readonly MethodDefinitionHandle Parent;

        public MethodImportRow(MethodImport import, MethodDefinitionHandle parent)
        {
            Import = import;
            Parent = parent;
        }
    }
}
