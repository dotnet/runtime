// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace TLens
{
	class AssemlyReferenceResolver : IAssemblyResolver
	{
		readonly string[] additionalFolders;
		readonly Dictionary<string, AssemblyDefinition> resolved = new ();

		public AssemlyReferenceResolver (string[] additionalFolders)
		{
			this.additionalFolders = additionalFolders;
			ReaderParameters = new ReaderParameters (ReadingMode.Deferred) {
				AssemblyResolver = this
			};
		}

		public ReaderParameters ReaderParameters { get; }

		public void Dispose ()
		{
		}

		public AssemblyDefinition Resolve (AssemblyNameReference name)
		{
			if (resolved.TryGetValue (name.Name, out AssemblyDefinition assembly))
				return assembly;

			string fileName = name.Name + ".dll";

			foreach (var folder in additionalFolders) {
				string file = Path.Combine (folder, fileName);
				if (File.Exists (file)) {
					assembly = AssemblyDefinition.ReadAssembly (file, ReaderParameters);
					if (assembly != null) {
						resolved.Add (name.Name, assembly);
						return assembly;
					}
				}
			}

			Console.WriteLine ($"The file for assembly reference '{name.Name}' could not be located.");
			return null;
		}

		public AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			throw new NotImplementedException ();
		}
	}
}
