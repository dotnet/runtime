// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public readonly partial struct AssemblyDefinition
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
