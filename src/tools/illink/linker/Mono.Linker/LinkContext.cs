//
// LinkContext.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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

using System.Collections;
using System.IO;
using Mono.Cecil;

namespace Mono.Linker {

	public class LinkContext {

		Pipeline _pipeline;
		AssemblyAction _coreAction;
		Hashtable _actions;
		string _outputDirectory;
		Hashtable _parameters;
		bool _linkSymbols;

		AssemblyResolver _resolver;

		public Pipeline Pipeline {
			get { return _pipeline; }
		}

		public string OutputDirectory {
			get { return _outputDirectory; }
			set { _outputDirectory = value; }
		}

		public AssemblyAction CoreAction {
			get { return _coreAction; }
			set { _coreAction = value; }
		}

		public bool LinkSymbols {
			get { return _linkSymbols; }
			set { _linkSymbols = value; }
		}

		public IDictionary Actions {
			get { return _actions; }
		}

		public AssemblyResolver Resolver {
			get { return _resolver; }
		}

		public LinkContext (Pipeline pipeline)
			: this (pipeline, new AssemblyResolver ())
		{
		}

		public LinkContext (Pipeline pipeline, AssemblyResolver resolver)
		{
			_pipeline = pipeline;
			_resolver = resolver;
			_actions = new Hashtable ();
			_parameters = new Hashtable ();
		}

		public TypeDefinition GetType (string type)
		{
			int pos = type.IndexOf (",");
			type = type.Replace ("+", "/");
			if (pos == -1) {
				foreach (AssemblyDefinition asm in GetAssemblies ())
					if (asm.MainModule.Types.Contains (type))
						return asm.MainModule.Types [type];

				return null;
			}

			string asmname = type.Substring (pos + 1);
			type = type.Substring (0, pos);
			AssemblyDefinition assembly = Resolve (AssemblyNameReference.Parse (asmname));
			return assembly.MainModule.Types [type];
		}

		public AssemblyDefinition Resolve (string name)
		{
			if (File.Exists (name)) {
				AssemblyDefinition assembly = AssemblyFactory.GetAssembly (name);
				_resolver.CacheAssembly (assembly);
				SafeLoadSymbols (assembly);
				return assembly;
			} else {
				AssemblyNameReference reference = new AssemblyNameReference ();
				reference.Name = name;
				return Resolve (reference);
			}
		}

		public AssemblyDefinition Resolve (IMetadataScope scope)
		{
			AssemblyNameReference reference = GetReference (scope);

			AssemblyDefinition assembly = _resolver.Resolve (reference);

			if (SeenFirstTime (assembly)) {
				SetAction (assembly);
				SafeLoadSymbols (assembly);
			}

			return assembly;
		}

		public void SafeLoadSymbols (AssemblyDefinition assembly)
		{
			if (!_linkSymbols)
				return;

			try {
				assembly.MainModule.LoadSymbols ();
				Annotations.SetHasSymbols (assembly);
			} catch {
				return; // resharper loves this
			}
		}

		static bool SeenFirstTime (AssemblyDefinition assembly)
		{
			return !Annotations.HasAction (assembly);
		}

		static AssemblyNameReference GetReference (IMetadataScope scope)
		{
			AssemblyNameReference reference;
			if (scope is ModuleDefinition) {
				AssemblyDefinition asm = ((ModuleDefinition) scope).Assembly;
				reference = asm.Name;
			} else
				reference = (AssemblyNameReference) scope;

			return reference;
		}

		void SetAction (AssemblyDefinition assembly)
		{
			AssemblyAction action = AssemblyAction.Link;

			AssemblyNameDefinition name = assembly.Name;

			if (_actions.Contains (name.Name))
				action = (AssemblyAction) _actions [name.Name];
			else if (IsCore (name))
				action = _coreAction;

			Annotations.SetAction (assembly, action);
		}

		static bool IsCore (AssemblyNameReference name)
		{
			switch (name.Name) {
			case "mscorlib":
			case "Accessibility":
			case "Mono.Security":
				return true;
			default:
				return name.Name.StartsWith ("System")
					|| name.Name.StartsWith ("Microsoft");
			}
		}

		public AssemblyDefinition [] GetAssemblies ()
		{
			IDictionary cache = _resolver.AssemblyCache;
			AssemblyDefinition [] asms = new AssemblyDefinition [cache.Count];
			cache.Values.CopyTo (asms, 0);
			return asms;
		}

		public void SetParameter (string key, string value)
		{
			_parameters [key] = value;
		}

		public bool HasParameter (string key)
		{
			return _parameters.Contains (key);
		}

		public string GetParameter (string key)
		{
			return (string) _parameters [key];
		}
	}
}
