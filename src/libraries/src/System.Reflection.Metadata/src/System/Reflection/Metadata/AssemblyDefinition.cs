// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct AssemblyDefinition
    {
        private readonly MetadataReader _reader;

        internal AssemblyDefinition(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
        }

        public AssemblyHashAlgorithm HashAlgorithm
        {
            get
            {
                return _reader.AssemblyTable.GetHashAlgorithm();
            }
        }

        public Version Version
        {
            get
            {
                return _reader.AssemblyTable.GetVersion();
            }
        }

        public AssemblyFlags Flags
        {
            get
            {
                return _reader.AssemblyTable.GetFlags();
            }
        }

        public StringHandle Name
        {
            get
            {
                return _reader.AssemblyTable.GetName();
            }
        }

        public StringHandle Culture
        {
            get
            {
                return _reader.AssemblyTable.GetCulture();
            }
        }

        public BlobHandle PublicKey
        {
            get
            {
                return _reader.AssemblyTable.GetPublicKey();
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(_reader, EntityHandle.AssemblyDefinition);
        }

        public DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes()
        {
            return new DeclarativeSecurityAttributeHandleCollection(_reader, EntityHandle.AssemblyDefinition);
        }
    }
}