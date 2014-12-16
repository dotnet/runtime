// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct ModuleDefinition
    {
        private readonly MetadataReader _reader;

        internal ModuleDefinition(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
        }

        public int Generation
        {
            get
            {
                return (int)_reader.ModuleTable.GetGeneration();
            }
        }

        public StringHandle Name
        {
            get
            {
                return _reader.ModuleTable.GetName();
            }
        }

        public GuidHandle Mvid
        {
            get
            {
                return _reader.ModuleTable.GetMvid();
            }
        }

        public GuidHandle GenerationId
        {
            get
            {
                return _reader.ModuleTable.GetEncId();
            }
        }

        public GuidHandle BaseGenerationId
        {
            get
            {
                return _reader.ModuleTable.GetEncBaseId();
            }
        }
    }
}