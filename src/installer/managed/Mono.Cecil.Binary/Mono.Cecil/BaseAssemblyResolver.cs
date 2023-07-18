//
// BaseAssemblyResolver.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil {

	using System;
	using System.Collections;
	using System.IO;
	using SR = System.Reflection;
	using System.Text;

	internal abstract class BaseAssemblyResolver : IAssemblyResolver {

		ArrayList m_directories;
		string[] m_monoGacPaths;

		public void AddSearchDirectory (string directory)
		{
			m_directories.Add (directory);
		}

		public void RemoveSearchDirectory (string directory)
		{
			m_directories.Remove (directory);
		}

		public string [] GetSearchDirectories ()
		{
			return (string []) m_directories.ToArray (typeof (string));
		}

		public virtual AssemblyDefinition Resolve (string fullName)
		{
			return Resolve (AssemblyNameReference.Parse (fullName));
		}

		public BaseAssemblyResolver ()
		{
			m_directories = new ArrayList ();
			m_directories.Add (".");
			m_directories.Add ("bin");
		}

		public virtual AssemblyDefinition Resolve (AssemblyNameReference name)
		{
			AssemblyDefinition assembly;
			string frameworkdir = Path.GetDirectoryName (typeof (object).Module.FullyQualifiedName);

			assembly = SearchDirectory (name, m_directories);
			if (assembly != null)
				return assembly;

			if (IsZero (name.Version)) {
				assembly = SearchDirectory (name, new string [] {frameworkdir});
				if (assembly != null)
					return assembly;
			}

#if !CF_1_0 && !CF_2_0 && !NO_SYSTEM_DLL
			if (name.Name == "mscorlib") {
				assembly = GetCorlib (name);
				if (assembly != null)
					return assembly;
			}

			assembly = GetAssemblyInGac (name);
			if (assembly != null)
				return assembly;
#endif

			assembly = SearchDirectory (name, new string [] {frameworkdir});
			if (assembly != null)
				return assembly;

			throw new FileNotFoundException ("Could not resolve: " + name);
		}

		static readonly string [] _extentions = new string [] { ".dll", ".exe" };

		static AssemblyDefinition SearchDirectory (AssemblyNameReference name, IEnumerable directories)
		{
			foreach (string dir in directories) {
				foreach (string ext in _extentions) {
					string file = Path.Combine (dir, name.Name + ext);
					if (File.Exists (file))
						return AssemblyFactory.GetAssembly (file);
				}
			}

			return null;
		}

		static bool IsZero (Version version)
		{
			return version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0;
		}

#if !CF_1_0 && !CF_2_0 && !NO_SYSTEM_DLL
		static AssemblyDefinition GetCorlib (AssemblyNameReference reference)
		{
			SR.AssemblyName corlib = typeof (object).Assembly.GetName ();
			if (corlib.Version == reference.Version || IsZero (reference.Version))
				return AssemblyFactory.GetAssembly (typeof (object).Module.FullyQualifiedName);

			string path = Directory.GetParent (
				Directory.GetParent (
					typeof (object).Module.FullyQualifiedName).FullName
				).FullName;

			string runtime_path = null;
			if (OnMono ()) {
				if (reference.Version.Major == 1)
					runtime_path = "1.0";
				else if (reference.Version.Major == 2) {
					if (reference.Version.Minor == 1)
						runtime_path = "2.1";
					else
						runtime_path = "2.0";
				} else if (reference.Version.Major == 4)
					runtime_path = "4.0";
			} else {
				switch (reference.Version.ToString ()) {
				case "1.0.3300.0":
					runtime_path = "v1.0.3705";
					break;
				case "1.0.5000.0":
					runtime_path = "v1.1.4322";
					break;
				case "2.0.0.0":
					runtime_path = "v2.0.50727";
					break;
				case "4.0.0.0":
					runtime_path = "v4.0.30319";
					break;
				}
			}

			if (runtime_path == null)
				throw new NotSupportedException ("Version not supported: " + reference.Version);

			path = Path.Combine (path, runtime_path);

			if (File.Exists (Path.Combine (path, "mscorlib.dll")))
				return AssemblyFactory.GetAssembly (Path.Combine (path, "mscorlib.dll"));

			return null;
		}

		public static bool OnMono ()
		{
			return typeof (object).Assembly.GetType ("System.MonoType", false) != null;
		}

		string[] MonoGacPaths {
			get {
				if (m_monoGacPaths == null)
					m_monoGacPaths = GetDefaultMonoGacPaths ();
				return m_monoGacPaths;
			}
		}

		static string[] GetDefaultMonoGacPaths ()
		{
			ArrayList paths = new ArrayList ();
			string s = GetCurrentGacPath ();
			if (s != null)
				paths.Add (s);
			string gacPathsEnv = Environment.GetEnvironmentVariable ("MONO_GAC_PREFIX");
			if (gacPathsEnv != null && gacPathsEnv.Length > 0) {
				string[] gacPrefixes = gacPathsEnv.Split (Path.PathSeparator);
				foreach (string gacPrefix in gacPrefixes) {
					if (gacPrefix != null && gacPrefix.Length > 0) {
						string gac = Path.Combine (Path.Combine (Path.Combine (gacPrefix, "lib"), "mono"), "gac");
						if (Directory.Exists (gac) && !paths.Contains (gac))
							paths.Add (gac);
					}
				}
			}
			return (string[]) paths.ToArray (typeof (String));
		}

		AssemblyDefinition GetAssemblyInGac (AssemblyNameReference reference)
		{
			if (reference.PublicKeyToken == null || reference.PublicKeyToken.Length == 0)
				return null;

			if (OnMono ()) {
				foreach (string gacpath in MonoGacPaths) {
					string s = GetAssemblyFile (reference, gacpath);
					if (File.Exists (s))
						return AssemblyFactory.GetAssembly (s);
				}
			} else {
				string currentGac = GetCurrentGacPath ();
				if (currentGac == null)
					return null;

				string [] gacs = new string [] {"GAC_MSIL", "GAC_32", "GAC"};
				for (int i = 0; i < gacs.Length; i++) {
					string gac = Path.Combine (Directory.GetParent (currentGac).FullName, gacs [i]);
					string asm = GetAssemblyFile (reference, gac);
					if (Directory.Exists (gac) && File.Exists (asm))
						return AssemblyFactory.GetAssembly (asm);
				}
			}

			return null;
		}

		static string GetAssemblyFile (AssemblyNameReference reference, string gac)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (reference.Version);
			sb.Append ("__");
			for (int i = 0; i < reference.PublicKeyToken.Length; i++)
				sb.Append (reference.PublicKeyToken [i].ToString ("x2"));

			return Path.Combine (
				Path.Combine (
					Path.Combine (gac, reference.Name), sb.ToString ()),
					string.Concat (reference.Name, ".dll"));
		}

		static string GetCurrentGacPath ()
		{
			string file = typeof (Uri).Module.FullyQualifiedName;
			if (!File.Exists (file))
				return null;

			return Directory.GetParent (
				Directory.GetParent (
					Path.GetDirectoryName (
						file)
					).FullName
				).FullName;
		}
#endif
	}
}
