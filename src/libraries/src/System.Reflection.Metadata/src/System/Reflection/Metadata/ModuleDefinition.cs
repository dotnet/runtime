// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct ModuleDefinition
    {
        private readonly MetadataReader reader;

        internal ModuleDefinition(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
        }

        public int Generation
        {
            get
            {
                return (int)reader.ModuleTable.GetGeneration();
            }
        }

        public StringHandle Name
        {
            get
            {
                return reader.ModuleTable.GetName();
            }
        }

        public GuidHandle Mvid
        {
            get
            {
                return reader.ModuleTable.GetMvid();
            }
        }

        public GuidHandle GenerationId
        {
            get
            {
                return reader.ModuleTable.GetEncId();
            }
        }

        public GuidHandle BaseGenerationId
        {
            get
            {
                return reader.ModuleTable.GetEncBaseId();
            }
        }
    }
}