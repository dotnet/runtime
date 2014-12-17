// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
    public struct AssemblyDefinition
    {
        private readonly MetadataReader reader;

        internal AssemblyDefinition(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
        }

        private Handle Handle
        {
            get
            {
                return Handle.AssemblyDefinition;
            }
        }

        public AssemblyHashAlgorithm HashAlgorithm
        {
            get
            {
                return reader.AssemblyTable.GetHashAlgorithm();
            }
        }

        public Version Version
        {
            get
            {
                return reader.AssemblyTable.GetVersion();
            }
        }

        public AssemblyFlags Flags
        {
            get
            {
                return reader.AssemblyTable.GetFlags();
            }
        }

        public StringHandle Name
        {
            get
            {
                return reader.AssemblyTable.GetName();
            }
        }

        public StringHandle Culture
        {
            get
            {
                return reader.AssemblyTable.GetCulture();
            }
        }

        public BlobHandle PublicKey
        {
            get
            {
                return reader.AssemblyTable.GetPublicKey();
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes()
        {
            return new CustomAttributeHandleCollection(reader, Handle);
        }

        public DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes()
        {
            return new DeclarativeSecurityAttributeHandleCollection(reader, Handle);
        }
    }
}