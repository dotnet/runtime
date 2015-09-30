// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public struct ImportsBlobReader : IEnumerator<ImportDefinition>
    {
        private BlobReader _reader;
        private ImportDefinition _current;

        public unsafe ImportsBlobReader(byte* buffer, int length)
            : this(MemoryBlock.CreateChecked(buffer, length))
        {
        }

        internal ImportsBlobReader(MemoryBlock block)
        {
            _reader = new BlobReader(block);
            _current = default(ImportDefinition);
        }

        /// <exception cref="BadImageFormatException">Invalid blob format.</exception>
        public bool MoveNext()
        {
            if (_reader.RemainingBytes == 0)
            {
                return false;
            }

            var kind = (ImportDefinitionKind)_reader.ReadByte();

            switch (kind)
            {
                case ImportDefinitionKind.ImportType:
                    _current = new ImportDefinition(
                        kind,
                        typeOrNamespace: _reader.ReadTypeHandle());

                    break;

                case ImportDefinitionKind.ImportNamespace:
                    _current = new ImportDefinition(
                        kind,
                        typeOrNamespace: MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()));

                    break;

                case ImportDefinitionKind.ImportAssemblyNamespace:
                    _current = new ImportDefinition(
                        kind,
                        assembly: MetadataTokens.AssemblyReferenceHandle(_reader.ReadCompressedInteger()),
                        typeOrNamespace: MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()));

                    break;

                case ImportDefinitionKind.ImportAssemblyReferenceAlias:
                    _current = new ImportDefinition(
                        kind,
                        alias: MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()));

                    break;

                case ImportDefinitionKind.AliasAssemblyReference:
                    _current = new ImportDefinition(
                        kind,
                        alias: MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()),
                        assembly: MetadataTokens.AssemblyReferenceHandle(_reader.ReadCompressedInteger()));

                    break;

                case ImportDefinitionKind.AliasType:
                    _current = new ImportDefinition(
                        kind,
                        alias: MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()),
                        typeOrNamespace: _reader.ReadTypeHandle());

                    break;

                case ImportDefinitionKind.ImportXmlNamespace:
                case ImportDefinitionKind.AliasNamespace:
                    _current = new ImportDefinition(
                        kind,
                        alias: MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()),
                        typeOrNamespace: MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()));

                    break;

                case ImportDefinitionKind.AliasAssemblyNamespace:
                    _current = new ImportDefinition(
                        kind,
                        alias: MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()),
                        assembly: MetadataTokens.AssemblyReferenceHandle(_reader.ReadCompressedInteger()),
                        typeOrNamespace: MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()));

                    break;

                default:
                    throw new BadImageFormatException(string.Format(SR.InvalidImportDefinitionKind, kind));
            }

            return true;
        }

        public ImportDefinition Current
        {
            get { return _current; }
        }

        object IEnumerator.Current
        {
            get { return _current; }
        }

        public void Reset()
        {
            _reader.SeekOffset(0);
            _current = default(ImportDefinition);
        }

        public void Dispose()
        {
        }

        private string GetDebuggerDisplay()
        {
            return _reader.GetDebuggerDisplay();
        }
    }
}
