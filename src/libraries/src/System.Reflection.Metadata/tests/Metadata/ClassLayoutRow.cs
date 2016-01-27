// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
