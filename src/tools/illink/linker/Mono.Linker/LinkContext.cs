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

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker {

	public class LinkContext : IDisposable {

		Pipeline _pipeline;
		AssemblyAction _coreAction;
		Dictionary<string, AssemblyAction> _actions;
		string _outputDirectory;
		readonly Dictionary<string, string> _parameters;
		bool _linkSymbols;
		bool _keepTypeForwarderOnlyAssemblies;
		bool _keepMembersForDebuggerAttributes;

		AssemblyResolver _resolver;

		ReaderParameters _readerParameters;
		ISymbolReaderProvider _symbolReaderProvider;
		ISymbolWriterProvider _symbolWriterProvider;

		AnnotationStore _annotations;

		public Pipeline Pipeline {
			get { return _pipeline; }
		}

		public AnnotationStore Annotations {
			get { return _annotations; }
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

		public bool KeepTypeForwarderOnlyAssemblies
		{
			get { return _keepTypeForwarderOnlyAssemblies; }
			set { _keepTypeForwarderOnlyAssemblies = value; }
		}

		public bool KeepMembersForDebuggerAttributes
		{
			get { return _keepMembersForDebuggerAttributes; }
			set { _keepMembersForDebuggerAttributes = value; }
		}

		public System.Collections.IDictionary Actions {
			get { return _actions; }
		}

		public AssemblyResolver Resolver {
			get { return _resolver; }
		}

		public ReaderParameters ReaderParameters {
			get { return _readerParameters; }
		}

		public ISymbolReaderProvider SymbolReaderProvider {
			get { return _symbolReaderProvider; }
			set { _symbolReaderProvider = value; }
		}

		public ISymbolWriterProvider SymbolWriterProvider {
			get { return _symbolWriterProvider; }
			set { _symbolWriterProvider = value; }
		}

		public bool LogInternalExceptions { get; set; } = false;

		public LinkContext (Pipeline pipeline)
			: this (pipeline, new AssemblyResolver ())
		{
		}

		public LinkContext (Pipeline pipeline, AssemblyResolver resolver)
			: this(pipeline, resolver, new ReaderParameters
			{
				AssemblyResolver = resolver
			},
			new AnnotationStore ())
		{
		}

		public LinkContext (Pipeline pipeline, AssemblyResolver resolver, ReaderParameters readerParameters, AnnotationStore annotations)
		{
			_pipeline = pipeline;
			_resolver = resolver;
			_actions = new Dictionary<string, AssemblyAction> ();
			_parameters = new Dictionary<string, string> ();
			_annotations = annotations;
			_readerParameters = readerParameters;
		}

		public TypeDefinition GetType (string fullName)
		{
			int pos = fullName.IndexOf (",");
			fullName = fullName.Replace ("+", "/");
			if (pos == -1) {
				foreach (AssemblyDefinition asm in GetAssemblies ()) {
					var type = asm.MainModule.GetType (fullName);
					if (type != null)
						return type;
				}

				return null;
			}

			string asmname = fullName.Substring (pos + 1);
			fullName = fullName.Substring (0, pos);
			AssemblyDefinition assembly = Resolve (AssemblyNameReference.Parse (asmname));
			return assembly.MainModule.GetType (fullName);
		}

		public AssemblyDefinition Resolve (string name)
		{
			if (File.Exists (name)) {
				AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly (name, _readerParameters);
				return _resolver.CacheAssembly (assembly);
			}

			return Resolve (new AssemblyNameReference (name, new Version ()));
		}

		public AssemblyDefinition Resolve (IMetadataScope scope)
		{
			AssemblyNameReference reference = GetReference (scope);
			try {
				AssemblyDefinition assembly = _resolver.Resolve (reference, _readerParameters);

				if (SeenFirstTime (assembly)) {
					SafeReadSymbols (assembly);
					SetAction (assembly);
				}

				return assembly;
			}
			catch (Exception e) {
				throw new AssemblyResolutionException (reference, e);
			}
		}

		protected bool SeenFirstTime (AssemblyDefinition assembly)
		{
			return !_annotations.HasAction (assembly);
		}

		public virtual void SafeReadSymbols (AssemblyDefinition assembly)
		{
			if (!_linkSymbols)
				return;

			if (assembly.MainModule.HasSymbols)
				return;

			try {
				if (_symbolReaderProvider != null) {
					var symbolReader = _symbolReaderProvider.GetSymbolReader (
						assembly.MainModule,
						assembly.MainModule.FileName);

					_annotations.AddSymbolReader (assembly, symbolReader);
					assembly.MainModule.ReadSymbols (symbolReader);
				} else
					assembly.MainModule.ReadSymbols ();
			} catch {}
		}

		public virtual ICollection<AssemblyDefinition> ResolveReferences (AssemblyDefinition assembly)
		{
			List<AssemblyDefinition> references = new List<AssemblyDefinition> ();
			foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences) {
				try {
					references.Add (Resolve (reference));
				}
				catch (AssemblyResolutionException) {
				}
			}
			return references;
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

		protected void SetAction (AssemblyDefinition assembly)
		{
			AssemblyAction action;

			AssemblyNameDefinition name = assembly.Name;

			if (_actions.TryGetValue (name.Name, out action)) {
			} else if (IsCore (name)) {
				action = _coreAction;
			} else {
				action = AssemblyAction.Link;
			}

			_annotations.SetAction (assembly, action);
		}

		static bool IsCore (AssemblyNameReference name)
		{
			switch (name.Name) {
			case "mscorlib":
			case "Accessibility":
			case "Mono.Security":
				// WPF
			case "PresentationFramework":
			case "PresentationCore":
			case "WindowsBase":
			case "UIAutomationProvider":
			case "UIAutomationTypes":
			case "PresentationUI":
			case "ReachFramework":
				return true;
			default:
				return name.Name.StartsWith ("System")
					|| name.Name.StartsWith ("Microsoft");
			}
		}

		public virtual AssemblyDefinition [] GetAssemblies ()
		{
			var cache = _resolver.AssemblyCache;
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
			return _parameters.ContainsKey (key);
		}

		public string GetParameter (string key)
		{
			string val = null;
			_parameters.TryGetValue (key, out val);
			return val;
		}

		public void Dispose ()
		{
			_resolver.Dispose ();
		}
	}
}
