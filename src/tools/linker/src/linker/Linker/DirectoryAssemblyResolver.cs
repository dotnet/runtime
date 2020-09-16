// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using Mono.Collections.Generic;
using Mono.Cecil;

#if FEATURE_ILLINK
namespace Mono.Linker {

	public abstract class DirectoryAssemblyResolver : IAssemblyResolver {

		readonly Collection<string> directories;

		protected readonly Dictionary<AssemblyDefinition, string> assemblyToPath = new Dictionary<AssemblyDefinition, string> ();

		readonly List<MemoryMappedViewStream> viewStreams = new List<MemoryMappedViewStream> ();

		readonly ReaderParameters defaultReaderParameters;

		public void AddSearchDirectory (string directory)
		{
			directories.Add (directory);
		}

		protected DirectoryAssemblyResolver ()
		{
			defaultReaderParameters = new ReaderParameters ();
			defaultReaderParameters.AssemblyResolver = this;
			directories = new Collection<string> (2) { "." };
		}

		protected AssemblyDefinition GetAssembly (string file, ReaderParameters parameters)
		{
			if (parameters.AssemblyResolver == null)
				parameters.AssemblyResolver = this;

			MemoryMappedViewStream viewStream = null;
			try {
				// Create stream because CreateFromFile(string, ...) uses FileShare.None which is too strict
				using var fileStream = new FileStream (file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false);
				using var mappedFile = MemoryMappedFile.CreateFromFile (
					fileStream, null, fileStream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
				viewStream = mappedFile.CreateViewStream (0, 0, MemoryMappedFileAccess.Read);

				AssemblyDefinition result = ModuleDefinition.ReadModule (viewStream, parameters).Assembly;

				assemblyToPath.Add (result, file);

				viewStreams.Add (viewStream);

				// We transferred the ownership of the viewStream to the collection.
				viewStream = null;

				return result;
			} finally {
				if (viewStream != null)
					viewStream.Dispose ();
			}
		}

		public virtual AssemblyDefinition Resolve (AssemblyNameReference name)
		{
			return Resolve (name, defaultReaderParameters);
		}

		public virtual AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (parameters == null)
				throw new ArgumentNullException ("parameters");

			var assembly = SearchDirectory (name, directories, parameters);
			if (assembly != null)
				return assembly;

			throw new AssemblyResolutionException (name, new FileNotFoundException ($"Unable to find '{name.Name}.dll' or '{name.Name}.exe' file"));
		}

		AssemblyDefinition SearchDirectory (AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
		{
			var extensions = new [] { ".dll", ".exe" };
			foreach (var directory in directories) {
				foreach (var extension in extensions) {
					string file = Path.Combine (directory, name.Name + extension);
					if (!File.Exists (file))
						continue;
					try {
						return GetAssembly (file, parameters);
					} catch (BadImageFormatException) {
						continue;
					}
				}
			}

			return null;
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				foreach (var viewStream in viewStreams) {
					viewStream.Dispose ();
				}

				viewStreams.Clear ();
			}
		}
	}
}
#endif
