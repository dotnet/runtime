// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using Internal.IL;

namespace Internal.TypeSystem.Ecma
{
    /// <summary>
    ///  Provides PdbSymbolReader for portable PDB via System.Reflection.Metadata
    /// </summary>
    public sealed class PortablePdbSymbolReader : PdbSymbolReader
    {
        private static unsafe MetadataReader TryOpenMetadataFile(string filePath, MetadataStringDecoder stringDecoder, out MemoryMappedViewAccessor mappedViewAccessor)
        {
            FileStream fileStream = null;
            MemoryMappedFile mappedFile = null;
            MemoryMappedViewAccessor accessor = null;
            try
            {
                // Create stream because CreateFromFile(string, ...) uses FileShare.None which is too strict
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false);
                mappedFile = MemoryMappedFile.CreateFromFile(
                    fileStream, null, fileStream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true);

                accessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                var safeBuffer = accessor.SafeMemoryMappedViewHandle;

                // Check whether this is a real metadata file to avoid thrown and caught exceptions
                // for non-portable .pdbs
                if (safeBuffer.Read<byte>(0) != 'B' || // COR20MetadataSignature
                    safeBuffer.Read<byte>(1) != 'S' ||
                    safeBuffer.Read<byte>(2) != 'J' ||
                    safeBuffer.Read<byte>(3) != 'B')
                {
                    mappedViewAccessor = null;
                    return null;
                }

                var metadataReader = new MetadataReader((byte*)safeBuffer.DangerousGetHandle(), (int)safeBuffer.ByteLength, MetadataReaderOptions.Default, stringDecoder);

                // MemoryMappedFile does not need to be kept around. MemoryMappedViewAccessor is enough.

                mappedViewAccessor = accessor;
                accessor = null;

                return metadataReader;
            }
            finally
            {
                if (accessor != null)
                    accessor.Dispose();
                if (mappedFile != null)
                    mappedFile.Dispose();
                if (fileStream != null)
                    fileStream.Dispose();
            }
        }

        public static PdbSymbolReader TryOpen(string pdbFilename, MetadataStringDecoder stringDecoder)
        {
            MemoryMappedViewAccessor mappedViewAccessor;
            MetadataReader reader = TryOpenMetadataFile(pdbFilename, stringDecoder, out mappedViewAccessor);
            if (reader == null)
                return null;

            return new PortablePdbSymbolReader(reader, mappedViewAccessor);
        }

        public static PdbSymbolReader TryOpenEmbedded(PEReader peReader, MetadataStringDecoder stringDecoder)
        {
            foreach (DebugDirectoryEntry debugEntry in peReader.ReadDebugDirectory())
            {
                if (debugEntry.Type != DebugDirectoryEntryType.EmbeddedPortablePdb)
                    continue;

                MetadataReaderProvider embeddedReaderProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(debugEntry);
                MetadataReader reader = embeddedReaderProvider.GetMetadataReader(MetadataReaderOptions.Default, stringDecoder);
                return new PortablePdbSymbolReader(reader, mappedViewAccessor: null);
            }

            return null;
        }

        private MetadataReader _reader;
        private MemoryMappedViewAccessor _mappedViewAccessor;

        private PortablePdbSymbolReader(MetadataReader reader, MemoryMappedViewAccessor mappedViewAccessor)
        {
            _reader = reader;
            _mappedViewAccessor = mappedViewAccessor;
        }

        public override void Dispose()
        {
            if (_mappedViewAccessor != null)
                _mappedViewAccessor.Dispose();
        }

        public override int GetStateMachineKickoffMethod(int methodToken)
        {
            var debugInformationHandle = ((MethodDefinitionHandle)MetadataTokens.EntityHandle(methodToken)).ToDebugInformationHandle();

            var debugInformation = _reader.GetMethodDebugInformation(debugInformationHandle);

            var kickoffMethod = debugInformation.GetStateMachineKickoffMethod();
            return kickoffMethod.IsNil ? 0 : MetadataTokens.GetToken(kickoffMethod);
        }

        public override IEnumerable<ILSequencePoint> GetSequencePointsForMethod(int methodToken)
        {
            var debugInformationHandle = ((MethodDefinitionHandle)MetadataTokens.EntityHandle(methodToken)).ToDebugInformationHandle();

            var debugInformation = _reader.GetMethodDebugInformation(debugInformationHandle);

            var sequencePoints = debugInformation.GetSequencePoints();

            DocumentHandle previousDocumentHandle = default;
            string previousDocumentUrl = null;

            foreach (var sequencePoint in sequencePoints)
            {
                if (sequencePoint.StartLine == SequencePoint.HiddenLine)
                    continue;

                string url;
                if (sequencePoint.Document == previousDocumentHandle)
                {
                    url = previousDocumentUrl;
                }
                else
                {
                    url = _reader.GetString(_reader.GetDocument(sequencePoint.Document).Name);
                    previousDocumentHandle = sequencePoint.Document;
                    previousDocumentUrl = url;
                }

                yield return new ILSequencePoint(sequencePoint.Offset, url, sequencePoint.StartLine);
            }
        }

        //
        // Gather the local details in a scope and then recurse to child scopes
        //
        private void ProbeScopeForLocals(List<ILLocalVariable> variables, LocalScopeHandle localScopeHandle)
        {
            var localScope = _reader.GetLocalScope(localScopeHandle);

            foreach (var localVariableHandle in localScope.GetLocalVariables())
            {
                var localVariable = _reader.GetLocalVariable(localVariableHandle);

                var name = _reader.GetString(localVariable.Name);
                bool compilerGenerated = (localVariable.Attributes & LocalVariableAttributes.DebuggerHidden) != 0;

                variables.Add(new ILLocalVariable(localVariable.Index, name, compilerGenerated));
            }

            var children = localScope.GetChildren();
            while (children.MoveNext())
            {
                ProbeScopeForLocals(variables, children.Current);
            }
        }

        //
        // Recursively scan the scopes for a method stored in a PDB and gather the local slots
        // and names for all of them.  This assumes a CSC-like compiler that doesn't re-use
        // local slots in the same method across scopes.
        //
        public override IEnumerable<ILLocalVariable> GetLocalVariableNamesForMethod(int methodToken)
        {
            var debugInformationHandle = MetadataTokens.MethodDefinitionHandle(methodToken).ToDebugInformationHandle();

            var localScopes = _reader.GetLocalScopes(debugInformationHandle);

            var variables = new List<ILLocalVariable>();
            foreach (var localScopeHandle in localScopes)
            {
                ProbeScopeForLocals(variables, localScopeHandle);
            }
            return variables;
        }
    }
}
