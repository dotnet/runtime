// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker
{
	internal sealed class CustomSymbolWriterProvider : ISymbolWriterProvider
	{
		readonly DefaultSymbolWriterProvider _defaultProvider = new DefaultSymbolWriterProvider ();
		readonly bool _preserveSymbolPaths;

		public CustomSymbolWriterProvider (bool preserveSymbolPaths) => this._preserveSymbolPaths = preserveSymbolPaths;

		public ISymbolWriter GetSymbolWriter (ModuleDefinition module, string fileName)
			=> new CustomSymbolWriter (_defaultProvider.GetSymbolWriter (module, fileName), module, _preserveSymbolPaths);

		public ISymbolWriter GetSymbolWriter (ModuleDefinition module, Stream symbolStream)
			=> new CustomSymbolWriter (_defaultProvider.GetSymbolWriter (module, symbolStream), module, _preserveSymbolPaths);
	}

	internal sealed class CustomSymbolWriter : ISymbolWriter
	{
		// ASCII "RSDS": https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#codeview-debug-directory-entry-type-2
		const int CodeViewSignature = 0x53445352;

		readonly ISymbolWriter _symbolWriter;
		readonly ModuleDefinition _module;
		readonly bool _preserveSymbolPaths;

		internal CustomSymbolWriter (ISymbolWriter defaultWriter, ModuleDefinition module, bool preserveSymbolPaths)
		{
			_symbolWriter = defaultWriter;
			_module = module;
			_preserveSymbolPaths = preserveSymbolPaths;
		}

		public ImageDebugHeader GetDebugHeader ()
		{
			var header = _symbolWriter.GetDebugHeader ();
			if (!_preserveSymbolPaths)
				return header;

			if (!header.HasEntries)
				return header;

			for (int i = 0; i < header.Entries.Length; i++) {
				header.Entries [i] = ProcessEntry (header.Entries [i]);
			}

			return header;
		}

		ImageDebugHeaderEntry ProcessEntry (ImageDebugHeaderEntry entry)
		{
			if (entry.Directory.Type != ImageDebugType.CodeView)
				return entry;

			var reader = new BinaryReader (new MemoryStream (entry.Data));
			var newDataStream = new MemoryStream ();
			var writer = new BinaryWriter (newDataStream);

			var sig = reader.ReadUInt32 ();
			if (sig != CodeViewSignature)
				return entry;

			writer.Write (sig);
			writer.Write (reader.ReadBytes (16)); // MVID
			writer.Write (reader.ReadUInt32 ()); // Age

			writer.Write (Encoding.UTF8.GetBytes (GetOriginalPdbPath ()));
			writer.Write ((byte) 0);

			var newData = newDataStream.ToArray ();

			var directory = entry.Directory;
			directory.SizeOfData = newData.Length;

			return new ImageDebugHeaderEntry (directory, newData);
		}

		string GetOriginalPdbPath ()
		{
			if (!_module.HasDebugHeader)
				return string.Empty;

			var debugHeader = _module.GetDebugHeader ();
			foreach (var entry in debugHeader.Entries) {
				if (entry.Directory.Type != ImageDebugType.CodeView)
					continue;

				var reader = new BinaryReader (new MemoryStream (entry.Data));
				var sig = reader.ReadUInt32 ();
				if (sig != CodeViewSignature)
					return string.Empty;

				var stream = reader.BaseStream;
				stream.Seek (16 + 4, SeekOrigin.Current); // MVID and Age
				// Pdb path is NUL-terminated path at offset 24.
				// https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#codeview-debug-directory-entry-type-2
				return Encoding.UTF8.GetString (
					reader.ReadBytes ((int) (stream.Length - stream.Position - 1))); // remaining length - ending \0
			}

			return string.Empty;
		}

		public ISymbolReaderProvider GetReaderProvider () => _symbolWriter.GetReaderProvider ();

		public void Write (MethodDebugInformation info) => _symbolWriter.Write (info);

		public void Write () => _symbolWriter.Write ();

		public void Dispose () => _symbolWriter.Dispose ();
	}
}
