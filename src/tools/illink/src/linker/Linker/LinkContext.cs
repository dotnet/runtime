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

	public class UnintializedContextFactory {
		virtual public AnnotationStore CreateAnnotationStore (LinkContext context) => new AnnotationStore (context);
		virtual public MarkingHelpers CreateMarkingHelpers (LinkContext context) => new MarkingHelpers (context);
		virtual public Tracer CreateTracer (LinkContext context) => new Tracer (context);
	}

	public class LinkContext : IDisposable {

		Pipeline _pipeline;
		AssemblyAction _coreAction;
		AssemblyAction _userAction;
		Dictionary<string, AssemblyAction> _actions;
		string _outputDirectory;
		readonly Dictionary<string, string> _parameters;
		bool _linkSymbols;
		bool _keepTypeForwarderOnlyAssemblies;
		bool _keepMembersForDebugger;
		bool _ignoreUnresolved;

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

		public AssemblyAction UserAction {
			get { return _userAction; }
			set { _userAction = value; }
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

		public bool KeepMembersForDebugger
		{
			get { return _keepMembersForDebugger; }
			set { _keepMembersForDebugger = value; }
		}

		public bool IgnoreUnresolved
		{
			get { return _ignoreUnresolved; }
			set { _ignoreUnresolved = value; }
		}

		public bool EnableReducedTracing { get; set; }

		public bool KeepUsedAttributeTypesOnly { get; set; }

		public bool StripResources { get; set; }

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

		public bool LogMessages { get; set; }

		public ILogger Logger { get; set; } = new ConsoleLogger ();

		public MarkingHelpers MarkingHelpers { get; private set; }

		public KnownMembers MarkedKnownMembers { get; private set; }

		public Tracer Tracer { get; private set; }

		public string[] ExcludedFeatures { get; set; }

		public CodeOptimizations DisabledOptimizations { get; set; }

		public bool AddReflectionAnnotations { get; set; }

		public LinkContext (Pipeline pipeline)
			: this (pipeline, new AssemblyResolver ())
		{
		}

		public LinkContext (Pipeline pipeline, AssemblyResolver resolver)
			: this(pipeline, resolver, new ReaderParameters
			{
				AssemblyResolver = resolver
			}, new UnintializedContextFactory ())
		{
		}

		public LinkContext (Pipeline pipeline, AssemblyResolver resolver, ReaderParameters readerParameters, UnintializedContextFactory factory)
		{
			_pipeline = pipeline;
			_resolver = resolver;
			_resolver.Context = this;
			_actions = new Dictionary<string, AssemblyAction> ();
			_parameters = new Dictionary<string, string> ();
			_readerParameters = readerParameters;
			
			SymbolReaderProvider = new DefaultSymbolReaderProvider (false);

			if (factory == null)
				throw new ArgumentNullException (nameof (factory));

			_annotations = factory.CreateAnnotationStore (this);
			MarkingHelpers = factory.CreateMarkingHelpers (this);
			Tracer = factory.CreateTracer (this);
			MarkedKnownMembers = new KnownMembers ();
			StripResources = true;
		}

		public TypeDefinition GetType (string fullName)
		{
			int pos = fullName.IndexOf (",");
			fullName = TypeReferenceExtensions.ToCecilName (fullName);
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
				try {
					AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly (name, _readerParameters);
					return _resolver.CacheAssembly (assembly);
				} catch (Exception e) {
					throw new AssemblyResolutionException (new AssemblyNameReference (name, new Version ()), e);
				}
			}

			return Resolve (new AssemblyNameReference (name, new Version ()));
		}

		public AssemblyDefinition Resolve (IMetadataScope scope)
		{
			AssemblyNameReference reference = GetReference (scope);
			try {
				AssemblyDefinition assembly = _resolver.Resolve (reference, _readerParameters);

				if (assembly != null)
					RegisterAssembly (assembly);

				return assembly;
			}
			catch (Exception e) {
				throw new AssemblyResolutionException (reference, e);
			}
		}

		public void RegisterAssembly (AssemblyDefinition assembly)
		{
			if (SeenFirstTime (assembly)) {
				SafeReadSymbols (assembly);
				SetDefaultAction (assembly);
			}
		}

		protected bool SeenFirstTime (AssemblyDefinition assembly)
		{
			return !_annotations.HasAction (assembly);
		}

		public virtual void SafeReadSymbols (AssemblyDefinition assembly)
		{
			if (assembly.MainModule.HasSymbols)
				return;

			if (_symbolReaderProvider == null)
				throw new ArgumentNullException (nameof (_symbolReaderProvider));

			try {
				var symbolReader = _symbolReaderProvider.GetSymbolReader (
					assembly.MainModule,
					assembly.MainModule.FileName);

				if (symbolReader == null)
					return;

				try {
					assembly.MainModule.ReadSymbols (symbolReader);
				} catch {
					symbolReader.Dispose ();
					return;
				}

				// Add symbol reader to annotations only if we have successfully read it
				_annotations.AddSymbolReader (assembly, symbolReader);
			} catch { }
		}

		public virtual ICollection<AssemblyDefinition> ResolveReferences (AssemblyDefinition assembly)
		{
			List<AssemblyDefinition> references = new List<AssemblyDefinition> ();
			foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences) {
				AssemblyDefinition definition = Resolve (reference);
				if (definition != null)
					references.Add (definition);
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
		
		public void SetAction (AssemblyDefinition assembly, AssemblyAction defaultAction)
		{
			RegisterAssembly (assembly);

			if (!_actions.TryGetValue (assembly.Name.Name, out AssemblyAction action))
				action = defaultAction;

			Annotations.SetAction (assembly, action);
		}

		protected void SetDefaultAction (AssemblyDefinition assembly)
		{
			AssemblyAction action;

			AssemblyNameDefinition name = assembly.Name;

			if (_actions.TryGetValue (name.Name, out action)) {
			} else if (IsCore (name)) {
				action = _coreAction;
			} else {
				action = _userAction;
			}

			_annotations.SetAction (assembly, action);
		}

		public static bool IsCore (AssemblyNameReference name)
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
			case "netstandard":
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

		public bool IsFeatureExcluded (string featureName)
		{
			return ExcludedFeatures != null && Array.IndexOf (ExcludedFeatures, featureName) >= 0;
		}

		public bool IsOptimizationEnabled (CodeOptimizations optimization)
		{
			return (DisabledOptimizations & optimization) == 0;
		}

		public void LogMessage (string message, params object[] values)
		{
			LogMessage (MessageImportance.Normal, message, values);
		}

		public void LogMessage (MessageImportance importance, string message, params object [] values)
		{
			if (LogMessages && Logger != null)
				Logger.LogMessage (importance, message, values);
		}
	}

	[Flags]
	public enum CodeOptimizations
	{
		BeforeFieldInit = 1 << 0,
		
		/// <summary>
		/// Option to disable removal of overrides of virtual methods when a type is never instantiated
		///
		/// Being able to disable this optimization is helpful when trying to troubleshoot problems caused by types created via reflection or from native
		/// that do not get an instance constructor marked.
		/// </summary>
		OverrideRemoval = 1 << 1,
		
		/// <summary>
		/// Option to disable delaying marking of instance methods until an instance of that type could exist
		/// </summary>
		UnreachableBodies = 1 << 2
	}
}
