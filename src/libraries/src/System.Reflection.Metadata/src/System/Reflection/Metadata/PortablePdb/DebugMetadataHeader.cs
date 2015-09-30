// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    public sealed class DebugMetadataHeader
    {
        public readonly MethodDefinitionHandle EntryPoint;

        internal DebugMetadataHeader(MethodDefinitionHandle entryPoint)
        {
            EntryPoint = entryPoint;
        }
    }
}
