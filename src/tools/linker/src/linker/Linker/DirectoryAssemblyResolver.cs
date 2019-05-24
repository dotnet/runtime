using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Mono.Collections.Generic;
using Mono.Cecil;

#if FEATURE_ILLINK
namespace Mono.Linker {

	public abstract class DirectoryAssemblyResolver : IAssemblyResolver {

		readonly Collection<string> directories;

		public void AddSearchDirectory (string directory)
		{
			directories.Add (directory);
		}

		public void RemoveSearchDirectory (string directory)
		{
			directories.Remove (directory);
		}

		public string [] GetSearchDirectories ()
		{
			return this.directories.ToArray ();
		}

		protected DirectoryAssemblyResolver ()
		{
			directories = new Collection<string> (2) { "." };
		}

		protected AssemblyDefinition GetAssembly (string file, ReaderParameters parameters)
		{
			if (parameters.AssemblyResolver == null)
				parameters.AssemblyResolver = this;

			return ModuleDefinition.ReadModule (file, parameters).Assembly;
		}

		public virtual AssemblyDefinition Resolve (AssemblyNameReference name)
		{
			return Resolve (name, new ReaderParameters ());
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

			throw new AssemblyResolutionException (name);
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
					} catch (System.BadImageFormatException) {
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
		}
	}
}
#endif
