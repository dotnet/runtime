// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata.Tests
{
    internal struct ClassLayoutRow
    {
        public readonly ushort PackingSize;
        public readonly uint ClassSize;
        public readonly TypeDefinitionHandle Parent;

        internal ClassLayoutRow(ushort packingSize, uint classSize, TypeDefinitionHandle parent)
        {
            this.PackingSize = packingSize;
            this.ClassSize = classSize;
            this.Parent = parent;
        }
    }
}