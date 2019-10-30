// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public readonly struct ModuleDefinition
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
                return _reader.ModuleTable.GetGeneration();
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

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, EntityHandle.ModuleDefinition);
        }
    }
}
