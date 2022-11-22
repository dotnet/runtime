// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// AssemblyResolver.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker
{
	public class AssemblyResolver : IAssemblyResolver
	{
		readonly List<string> _references = new ();
		readonly LinkContext _context;
		readonly List<string> _directories = new ();
		readonly Dictionary<AssemblyDefinition, string> _assemblyToPath = new ();
		readonly List<MemoryMappedViewStream> _viewStreams = new ();
		readonly ReaderParameters _defaultReaderParameters;

		HashSet<string>? _unresolvedAssemblies;
		HashSet<string>? _reportedUnresolvedAssemblies;

		public AssemblyResolver (LinkContext context)
		{
			_context = context;
			_defaultReaderParameters = new ReaderParameters () {
				AssemblyResolver = this
			};
		}

		public IDictionary<string, AssemblyDefinition> AssemblyCache { get; } = new Dictionary<string, AssemblyDefinition> (StringComparer.OrdinalIgnoreCase);

		public string GetAssemblyLocation (AssemblyDefinition assembly)
		{
			if (_assemblyToPath.TryGetValue (assembly, out string? path))
				return path;

			throw new InternalErrorException ($"Assembly '{assembly}' was not loaded using linker resolver");
		}

		AssemblyDefinition? ResolveFromReferences (AssemblyNameReference name)
		{
			foreach (var reference in _references) {
				foreach (var extension in Extensions) {
					var fileName = name.Name + extension;
					if (Path.GetFileName (reference) != fileName)
						continue;
					try {
						return GetAssembly (reference);
					} catch (BadImageFormatException) {
						continue;
					}
				}
			}

			return null;
		}

		public AssemblyDefinition? Resolve (AssemblyNameReference name, bool probing)
		{
			if (AssemblyCache.TryGetValue (name.Name, out AssemblyDefinition? asm))
				return asm;

			if (_unresolvedAssemblies?.Contains (name.Name) == true) {
				if (!probing)
					ReportUnresolvedAssembly (name);
				return null;
			}

			// Any full path explicit reference takes precedence over other look up logic
			asm = ResolveFromReferences (name);

			asm ??= SearchDirectory (name);

			if (asm == null) {
				_unresolvedAssemblies ??= new HashSet<string> ();

				if (!probing)
					ReportUnresolvedAssembly (name);

				_unresolvedAssemblies.Add (name.Name);
				return null;
			}

			CacheAssembly (asm);
			return asm;
		}

		void ReportUnresolvedAssembly (AssemblyNameReference reference)
		{
			_reportedUnresolvedAssemblies ??= new HashSet<string> ();

			if (!_reportedUnresolvedAssemblies.Add (reference.Name))
				return;

			if (_context.IgnoreUnresolved)
				_context.LogMessage ($"Ignoring unresolved assembly '{reference.Name}' reference.");
			else
				_context.LogError (null, DiagnosticId.CouldNotFindAssemblyReference, reference.Name);
		}

		public void AddSearchDirectory (string directory)
		{
			_directories.Add (directory);
		}

		public AssemblyDefinition GetAssembly (string file)
		{
			MemoryMappedViewStream? viewStream = null;
			try {
				// Create stream because CreateFromFile(string, ...) uses FileShare.None which is too strict
				using var fileStream = new FileStream (file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false);
				using var mappedFile = MemoryMappedFile.CreateFromFile (
					fileStream, null, fileStream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
				viewStream = mappedFile.CreateViewStream (0, 0, MemoryMappedFileAccess.Read);

				AssemblyDefinition result = ModuleDefinition.ReadModule (viewStream, _defaultReaderParameters).Assembly;

				_assemblyToPath.Add (result, file);

				_viewStreams.Add (viewStream);

				// We transferred the ownership of the viewStream to the collection.
				viewStream = null;

				return result;
			} finally {
				viewStream?.Dispose ();
			}
		}

		public AssemblyDefinition? Resolve (AssemblyNameReference name)
		{
			return Resolve (name, probing: false);
		}

		AssemblyDefinition IAssemblyResolver.Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			// This is never used by cecil in linker context
			throw new NotSupportedException ();
		}

		static readonly string[] Extensions = new[] { ".dll", ".exe" };

		AssemblyDefinition? SearchDirectory (AssemblyNameReference name)
		{
			foreach (var directory in _directories) {
				foreach (var extension in Extensions) {
					string file = Path.Combine (directory, name.Name + extension);
					if (!File.Exists (file))
						continue;
					try {
						return GetAssembly (file);
					} catch (BadImageFormatException) {
						continue;
					}
				}
			}

			return null;
		}

		public void CacheAssembly (AssemblyDefinition assembly)
		{
			AssemblyCache[assembly.Name.Name] = assembly;
			_context.RegisterAssembly (assembly);
		}

		public void AddReferenceAssembly (string referencePath)
		{
			_references.Add (referencePath);
		}

		public List<string> GetReferencePaths ()
		{
			return _references;
		}

		public void Dispose ()
		{
			Dispose (true);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing)
				return;

			foreach (var asm in AssemblyCache.Values) {
				asm.Dispose ();
			}

			AssemblyCache.Clear ();
			_unresolvedAssemblies?.Clear ();

			_reportedUnresolvedAssemblies?.Clear ();

			foreach (var viewStream in _viewStreams) {
				viewStream.Dispose ();
			}

			_viewStreams.Clear ();
		}
	}
}
