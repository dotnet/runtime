// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

namespace ILAssembler.Tests
{
    /// <summary>
    /// Reads an image and its Portable PDB together, from a compilation or from the files the command line writes,
    /// so that tests can look up a method's MethodDebugInformation by method name and read document names and the
    /// sequence points blob.
    /// </summary>
    internal sealed class PortablePdbTestReader : IDisposable
    {
        private readonly PEReader _image;
        private readonly MetadataReaderProvider _pdbProvider;

        /// <summary>Opens the image and the Portable PDB of a compilation that produced a PDB.</summary>
        public PortablePdbTestReader(CompilationResult result)
            : this(new PEReader(DocumentCompilerTestHelpers.Serialize(result)), DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result))
        {
        }

        private PortablePdbTestReader(PEReader image, MetadataReaderProvider pdbProvider)
        {
            _image = image;
            _pdbProvider = pdbProvider;
            Image = _image.GetMetadataReader();
            Pdb = _pdbProvider.GetMetadataReader();
        }

        /// <summary>Compiles a single source named <c>test.il</c> and opens its image and PDB.</summary>
        public static PortablePdbTestReader Compile(string source, Options? options = null)
            => new(DocumentCompilerTestHelpers.CompileAndGetResult(source, options ?? new Options { Debug = true }));

        /// <summary>
        /// Opens an image file and a Portable PDB file, such as the ones the ilasm command line writes. Both files
        /// are read into memory, so they are not held open.
        /// </summary>
        public static PortablePdbTestReader Open(string imagePath, string pdbPath)
            => new(
                new PEReader(ImmutableArray.Create(File.ReadAllBytes(imagePath))),
                MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(File.ReadAllBytes(pdbPath))));

        /// <summary>Gets the image's metadata.</summary>
        public MetadataReader Image { get; }

        /// <summary>Gets the Portable PDB's metadata.</summary>
        public MetadataReader Pdb { get; }

        /// <summary>Gets the names of the PDB's documents, in Document table order.</summary>
        public string[] DocumentNames => Pdb.Documents.Select(GetDocumentName).ToArray();

        /// <summary>Gets the name of a PDB document.</summary>
        public string GetDocumentName(DocumentHandle handle) => Pdb.GetString(Pdb.GetDocument(handle).Name);

        /// <summary>Gets the language GUID of the only document with this name.</summary>
        public Guid GetDocumentLanguage(string name)
            => Pdb.GetGuid(Pdb.GetDocument(Pdb.Documents.Single(handle => GetDocumentName(handle) == name)).Language);

        /// <summary>Gets the MethodDef handle of the only method with this name.</summary>
        public MethodDefinitionHandle GetMethodHandle(string methodName)
            => Image.MethodDefinitions.Single(handle => Image.GetString(Image.GetMethodDefinition(handle).Name) == methodName);

        /// <summary>Gets the MethodDebugInformation row of the only method with this name.</summary>
        public MethodDebugInformation GetDebugInformation(string methodName)
            => Pdb.GetMethodDebugInformation(GetMethodHandle(methodName));

        /// <summary>Gets the name of the document that the method's MethodDebugInformation row names, or null when it is nil.</summary>
        public string? GetMethodDocumentName(string methodName)
        {
            DocumentHandle document = GetDebugInformation(methodName).Document;
            return document.IsNil ? null : GetDocumentName(document);
        }

        /// <summary>Gets the method's sequence points as a reader decodes them, with their documents resolved.</summary>
        public SequencePoint[] GetSequencePoints(string methodName)
            => GetDebugInformation(methodName).GetSequencePoints().ToArray();

        /// <summary>Gets the name of each sequence point's document, as a reader resolves it from the blob.</summary>
        public string[] GetSequencePointDocumentNames(string methodName)
            => GetSequencePoints(methodName).Select(point => GetDocumentName(point.Document)).ToArray();

        /// <summary>
        /// Reads the header of the method's sequence points blob: the LocalSignature row number and, when the
        /// row's document is nil, the InitialDocument row number. Asserts that the method has a blob.
        /// </summary>
        public (int LocalSignature, int? InitialDocument) ReadBlobHeader(string methodName)
        {
            MethodDebugInformation debugInformation = GetDebugInformation(methodName);
            Assert.False(debugInformation.SequencePointsBlob.IsNil);
            BlobReader blob = Pdb.GetBlobReader(debugInformation.SequencePointsBlob);
            int localSignature = blob.ReadCompressedInteger();
            int? initialDocument = debugInformation.Document.IsNil ? blob.ReadCompressedInteger() : null;
            return (localSignature, initialDocument);
        }

        /// <summary>
        /// Asserts that the method has a sequence points blob and that no record after its header is a
        /// document-record: a zero IL offset delta after the first record, followed by a Document row number, which
        /// makes the following points belong to that document (docs/design/specs/PortablePdb-Metadata.md,
        /// "Sequence Points Blob").
        /// </summary>
        /// <remarks>
        /// <see cref="GetSequencePoints"/> cannot tell this: a reader applies a document-record that names the
        /// document that is already current without any visible effect, so the points it returns are the same with
        /// or without the record. On failure, the message lists the records as written: <c>point@&lt;offset&gt;</c>
        /// for a sequence-point-record, <c>hidden@&lt;offset&gt;</c> for a hidden-sequence-point-record and
        /// <c>document#&lt;row&gt;</c> for a document-record.
        /// </remarks>
        public void AssertNoDocumentRecordInSequencePointsBlob(string methodName)
        {
            MethodDebugInformation debugInformation = GetDebugInformation(methodName);
            Assert.False(debugInformation.SequencePointsBlob.IsNil);
            BlobReader blob = Pdb.GetBlobReader(debugInformation.SequencePointsBlob);
            blob.ReadCompressedInteger();
            if (debugInformation.Document.IsNil)
            {
                blob.ReadCompressedInteger();
            }

            var records = new List<string>();
            bool hasDocumentRecord = false;
            int offset = 0;
            bool first = true;
            bool afterNonHiddenPoint = false;
            while (blob.RemainingBytes > 0)
            {
                int offsetDelta = blob.ReadCompressedInteger();
                if (offsetDelta == 0 && !first)
                {
                    records.Add($"document#{blob.ReadCompressedInteger()}");
                    hasDocumentRecord = true;
                    continue;
                }

                offset = first ? offsetDelta : offset + offsetDelta;
                first = false;
                int deltaLines = blob.ReadCompressedInteger();
                int deltaColumns = deltaLines == 0 ? blob.ReadCompressedInteger() : blob.ReadCompressedSignedInteger();
                if (deltaLines == 0 && deltaColumns == 0)
                {
                    records.Add($"hidden@{offset}");
                    continue;
                }

                if (afterNonHiddenPoint)
                {
                    blob.ReadCompressedSignedInteger();
                    blob.ReadCompressedSignedInteger();
                }
                else
                {
                    blob.ReadCompressedInteger();
                    blob.ReadCompressedInteger();
                    afterNonHiddenPoint = true;
                }

                records.Add($"point@{offset}");
            }

            if (hasDocumentRecord)
            {
                Assert.Fail($"The sequence points blob of {methodName} has a document-record: {string.Join(", ", records)}");
            }
        }

        /// <summary>Gets the Document row number of the document with this name.</summary>
        public int GetDocumentRowNumber(string name)
            => MetadataTokens.GetRowNumber(Pdb.Documents.Single(handle => GetDocumentName(handle) == name));

        /// <summary>
        /// Gets the StandAloneSig row number of the local signature that the method's body references, 0 when the
        /// body has no locals, or <see langword="null"/> when the method has no body (RVA 0: abstract,
        /// <c>pinvokeimpl</c> or runtime-implemented).
        /// </summary>
        public int? GetBodyLocalSignatureRowNumber(string methodName)
        {
            MethodDefinition method = Image.GetMethodDefinition(GetMethodHandle(methodName));
            return method.RelativeVirtualAddress == 0
                ? null
                : MetadataTokens.GetRowNumber(_image.GetMethodBody(method.RelativeVirtualAddress).LocalSignature);
        }

        /// <summary>Releases the image and PDB readers.</summary>
        public void Dispose()
        {
            _pdbProvider.Dispose();
            _image.Dispose();
        }
    }
}
