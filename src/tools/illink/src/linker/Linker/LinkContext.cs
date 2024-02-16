// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILLink.Shared;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Dataflow;
using Mono.Linker.Steps;

namespace Mono.Linker
{

	public class UnintializedContextFactory
	{
		public virtual AnnotationStore CreateAnnotationStore (LinkContext context) => new AnnotationStore (context);
		public virtual MarkingHelpers CreateMarkingHelpers (LinkContext context) => new MarkingHelpers (context);
		public virtual Tracer CreateTracer (LinkContext context) => new Tracer (context);
		public virtual EmbeddedXmlInfo CreateEmbeddedXmlInfo () => new ();
		public virtual AssemblyResolver CreateResolver (LinkContext context) => new AssemblyResolver (context, new ReaderParameters ());
	}

	public static class TargetRuntimeVersion
	{
		public const int NET5 = 5;
		public const int NET6 = 6;
	}

	public interface ITryResolveMetadata
	{
		MethodDefinition? TryResolve (MethodReference methodReference);
		TypeDefinition? TryResolve (TypeReference typeReference);
	}

	public class LinkContext : IMetadataResolver, ITryResolveMetadata, IDisposable
	{

		readonly Pipeline _pipeline;
		readonly Dictionary<string, AssemblyAction> _actions;
		readonly Dictionary<string, string> _parameters;
		int? _targetRuntime;

		readonly AssemblyResolver _resolver;
		readonly TypeNameResolver _typeNameResolver;

		readonly AnnotationStore _annotations;
		readonly CustomAttributeSource _customAttributes;
		readonly CompilerGeneratedState _compilerGeneratedState;
		readonly List<MessageContainer> _cachedWarningMessageContainers;
		readonly ILogger _logger;
		readonly Dictionary<AssemblyDefinition, bool> _isTrimmable;
		readonly UnreachableBlocksOptimizer _unreachableBlocksOptimizer;

		public Pipeline Pipeline {
			get { return _pipeline; }
		}

		public CustomAttributeSource CustomAttributes => _customAttributes;

		public CompilerGeneratedState CompilerGeneratedState => _compilerGeneratedState;

		public AnnotationStore Annotations => _annotations;

		public bool DeterministicOutput { get; set; }

		public int ErrorsCount { get; private set; }

		public string OutputDirectory { get; set; }

		public MetadataTrimming MetadataTrimming { get; set; }

		public AssemblyAction TrimAction { get; set; }

		public AssemblyAction DefaultAction { get; set; }

		public bool LinkSymbols { get; set; }

		public bool KeepMembersForDebugger { get; set; } = true;

		public bool IgnoreUnresolved { get; set; } = true;

		public bool EnableReducedTracing { get; set; }

		public bool KeepUsedAttributeTypesOnly { get; set; }

		public bool EnableSerializationDiscovery { get; set; }

		public bool DisableOperatorDiscovery { get; set; }

		/// <summary>
		/// Option to not special case EventSource.
		/// Currently, values are hard-coded and does not have a command line option to control
		/// </summary>
		public bool DisableEventSourceSpecialHandling { get; set; }

		public bool IgnoreDescriptors { get; set; }

		public bool IgnoreSubstitutions { get; set; }

		public bool IgnoreLinkAttributes { get; set; }

		public Dictionary<string, bool> FeatureSettings { get; init; }

		public List<PInvokeInfo> PInvokes { get; private set; }

		public string? PInvokesListFile;

		public bool StripSecurity { get; set; }

		public Dictionary<string, AssemblyAction> Actions {
			get { return _actions; }
		}

		public AssemblyResolver Resolver {
			get { return _resolver; }
		}

		internal TypeNameResolver TypeNameResolver {
			get { return _typeNameResolver; }
		}

		public ISymbolReaderProvider SymbolReaderProvider { get; set; }

		public bool LogMessages { get; set; }

		public MarkingHelpers MarkingHelpers { get; private set; }

		public KnownMembers MarkedKnownMembers { get; private set; }

		public WarningSuppressionWriter? WarningSuppressionWriter { get; set; }

		public HashSet<int> NoWarn { get; set; }

		public bool NoTrimWarn { get; set; }

		public Dictionary<int, bool> WarnAsError { get; set; }

		public bool GeneralWarnAsError { get; set; }

		public WarnVersion WarnVersion { get; set; }

		public UnconditionalSuppressMessageAttributeState Suppressions { get; set; }

		public Tracer Tracer { get; private set; }

		public EmbeddedXmlInfo EmbeddedXmlInfo { get; private set; }

		public CodeOptimizationsSettings Optimizations { get; set; }

		public bool AddReflectionAnnotations { get; set; }

		public string? AssemblyListFile { get; set; }

		public List<IMarkHandler> MarkHandlers { get; }

		public Dictionary<string, bool> SingleWarn { get; set; }

		public bool GeneralSingleWarn { get; set; }

		public HashSet<string> AssembliesWithGeneratedSingleWarning { get; set; }

		public SerializationMarker SerializationMarker { get; }

		public LinkContext (Pipeline pipeline, ILogger logger, string outputDirectory)
			: this(pipeline, logger, outputDirectory, new UnintializedContextFactory ())
		{
		}

		protected LinkContext (Pipeline pipeline, ILogger logger, string outputDirectory, UnintializedContextFactory factory)
		{
			_pipeline = pipeline;
			_logger = logger ?? throw new ArgumentNullException (nameof (logger));

			_resolver = factory.CreateResolver (this);
			_typeNameResolver = new TypeNameResolver (this);
			_actions = new Dictionary<string, AssemblyAction> ();
			_parameters = new Dictionary<string, string> (StringComparer.Ordinal);
			_customAttributes = new CustomAttributeSource (this);
			_compilerGeneratedState = new CompilerGeneratedState (this);
			_cachedWarningMessageContainers = new List<MessageContainer> ();
			_isTrimmable = new Dictionary<AssemblyDefinition, bool> ();
			OutputDirectory = outputDirectory;
			FeatureSettings = new Dictionary<string, bool> (StringComparer.Ordinal);

			SymbolReaderProvider = new DefaultSymbolReaderProvider (false);

			_annotations = factory.CreateAnnotationStore (this);
			MarkingHelpers = factory.CreateMarkingHelpers (this);
			SerializationMarker = new SerializationMarker (this);
			Tracer = factory.CreateTracer (this);
			EmbeddedXmlInfo = factory.CreateEmbeddedXmlInfo ();
			MarkedKnownMembers = new KnownMembers ();
			PInvokes = new List<PInvokeInfo> ();
			Suppressions = new UnconditionalSuppressMessageAttributeState (this);
			NoWarn = new HashSet<int> ();
			GeneralWarnAsError = false;
			WarnAsError = new Dictionary<int, bool> ();
			WarnVersion = WarnVersion.Latest;
			MarkHandlers = new List<IMarkHandler> ();
			GeneralSingleWarn = false;
			SingleWarn = new Dictionary<string, bool> ();
			AssembliesWithGeneratedSingleWarning = new HashSet<string> ();
			_unreachableBlocksOptimizer = new UnreachableBlocksOptimizer (this);

			const CodeOptimizations defaultOptimizations =
				CodeOptimizations.BeforeFieldInit |
				CodeOptimizations.OverrideRemoval |
				CodeOptimizations.UnusedInterfaces |
				CodeOptimizations.UnusedTypeChecks |
				CodeOptimizations.IPConstantPropagation |
				CodeOptimizations.UnreachableBodies |
				CodeOptimizations.RemoveDescriptors |
				CodeOptimizations.RemoveLinkAttributes |
				CodeOptimizations.RemoveSubstitutions |
				CodeOptimizations.RemoveDynamicDependencyAttribute |
				CodeOptimizations.OptimizeTypeHierarchyAnnotations |
				CodeOptimizations.SubstituteFeatureChecks;

			DisableEventSourceSpecialHandling = true;

			Optimizations = new CodeOptimizationsSettings (defaultOptimizations);
		}

		public void SetFeatureValue (string feature, bool value)
		{
			Debug.Assert (!string.IsNullOrEmpty (feature));
			FeatureSettings[feature] = value;
		}

		public bool HasFeatureValue (string feature, bool value)
		{
			return FeatureSettings.TryGetValue (feature, out bool fvalue) && value == fvalue;
		}

		public TypeDefinition? GetType (string fullName)
		{
			int pos = fullName.IndexOf (",");
			fullName = TypeReferenceExtensions.ToCecilName (fullName);
			if (pos == -1) {
				foreach (AssemblyDefinition asm in GetReferencedAssemblies ()) {
					var type = asm.MainModule.GetType (fullName);
					if (type != null)
						return type;
				}

				return null;
			}

			string asmname = fullName.Substring (pos + 1);
			fullName = fullName.Substring (0, pos);
			AssemblyDefinition? assembly = Resolve (AssemblyNameReference.Parse (asmname));
			return assembly?.MainModule.GetType (fullName);
		}

		public AssemblyDefinition? TryResolve (string name)
		{
			return TryResolve (new AssemblyNameReference (name, new Version ()));
		}

		public AssemblyDefinition? TryResolve (AssemblyNameReference name)
		{
			return _resolver.Resolve (name, probing: true);
		}

		public AssemblyDefinition? Resolve (IMetadataScope scope)
		{
			AssemblyNameReference reference = GetReference (scope);
			return _resolver.Resolve (reference);
		}

		public AssemblyDefinition? Resolve (AssemblyNameReference name)
		{
			return _resolver.Resolve (name);
		}

		public void RegisterAssembly (AssemblyDefinition assembly)
		{
			if (SeenFirstTime (assembly)) {
				SafeReadSymbols (assembly);
				Annotations.SetAction (assembly, CalculateAssemblyAction (assembly));
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

			if (SymbolReaderProvider == null)
				throw new InvalidOperationException ("Symbol provider is not set");

			try {
				var symbolReader = SymbolReaderProvider.GetSymbolReader (
					assembly.MainModule,
					GetAssemblyLocation (assembly));

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
			if (assembly == null)
				return references;

			foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences) {
				AssemblyDefinition? definition = Resolve (reference);
				if (definition != null)
					references.Add (definition);
			}

			return references;
		}

		static AssemblyNameReference GetReference (IMetadataScope scope)
		{
			AssemblyNameReference reference;
			if (scope is ModuleDefinition moduleDefinition) {
				AssemblyDefinition asm = moduleDefinition.Assembly;
				reference = asm.Name;
			} else
				reference = (AssemblyNameReference) scope;

			return reference;
		}

		public void RegisterAssemblyAction (string assemblyName, AssemblyAction action)
		{
			_actions[assemblyName] = action;
		}

#if !FEATURE_ILLINK
		public void SetAction (AssemblyDefinition assembly, AssemblyAction defaultAction)
		{
			if (!_actions.TryGetValue (assembly.Name.Name, out AssemblyAction action))
				action = defaultAction;

			Annotations.SetAction (assembly, action);
		}
#endif
		public AssemblyAction CalculateAssemblyAction (AssemblyDefinition assembly)
		{
			if (_actions.TryGetValue (assembly.Name.Name, out AssemblyAction action)) {
				if (IsCPPCLIAssembly (assembly.MainModule) && action != AssemblyAction.Copy && action != AssemblyAction.Skip) {
					LogWarning ($"Invalid assembly action '{action}' specified for assembly '{assembly.Name.Name}'. C++/CLI assemblies can only be copied or skipped.", 2106, GetAssemblyLocation (assembly));
					return AssemblyAction.Copy;
				}

				return action;
			}

			if (IsCPPCLIAssembly (assembly.MainModule))
				return DefaultAction == AssemblyAction.Skip ? DefaultAction : AssemblyAction.Copy;

			if (IsTrimmable (assembly))
				return TrimAction;

			return DefaultAction;

			static bool IsCPPCLIAssembly (ModuleDefinition module)
			{
				foreach (var type in module.Types)
					if (type.Namespace == "<CppImplementationDetails>" ||
						type.Namespace == "<CrtImplementationDetails>")
						return true;

				return false;
			}
		}

		public bool IsTrimmable (AssemblyDefinition assembly)
		{
			if (_isTrimmable.TryGetValue (assembly, out bool isTrimmable))
				return isTrimmable;

			if (!assembly.HasCustomAttributes) {
				_isTrimmable.Add (assembly, false);
				return false;
			}

			foreach (var ca in assembly.CustomAttributes) {
				if (!ca.AttributeType.IsTypeOf<AssemblyMetadataAttribute> ())
					continue;

				var args = ca.ConstructorArguments;
				if (args.Count != 2)
					continue;

				if (args[0].Value is not string key || !key.Equals ("IsTrimmable", StringComparison.OrdinalIgnoreCase))
					continue;

				if (args[1].Value is not string value || !value.Equals ("True", StringComparison.OrdinalIgnoreCase)) {
					LogWarning (GetAssemblyLocation (assembly), DiagnosticId.InvalidIsTrimmableValue, args[1].Value.ToString () ?? "", assembly.Name.Name);
					continue;
				}

				isTrimmable = true;
			}

			_isTrimmable.Add (assembly, isTrimmable);
			return isTrimmable;
		}

		public virtual AssemblyDefinition[] GetAssemblies ()
		{
			var cache = _resolver.AssemblyCache;
			AssemblyDefinition[] asms = new AssemblyDefinition[cache.Count];
			cache.Values.CopyTo (asms, 0);
			return asms;
		}

		public AssemblyDefinition? GetLoadedAssembly (string name)
		{
			if (!string.IsNullOrEmpty (name) && _resolver.AssemblyCache.TryGetValue (name, out var ad))
				return ad;

			return null;
		}

		public string GetAssemblyLocation (AssemblyDefinition assembly)
		{
			return Resolver.GetAssemblyLocation (assembly);
		}

		public IEnumerable<AssemblyDefinition> GetReferencedAssemblies ()
		{
			var assemblies = GetAssemblies ();

			foreach (var assembly in assemblies)
				yield return assembly;

			var loaded = new HashSet<AssemblyDefinition> (assemblies);
			var toProcess = new Queue<AssemblyDefinition> (assemblies);

			while (toProcess.Count > 0) {
				var assembly = toProcess.Dequeue ();
				foreach (var reference in ResolveReferences (assembly)) {
					if (!loaded.Add (reference))
						continue;
					yield return reference;
					toProcess.Enqueue (reference);
				}
			}
		}

		public void SetCustomData (string key, string value)
		{
			_parameters[key] = value;
		}

		public bool HasCustomData (string key)
		{
			return _parameters.ContainsKey (key);
		}

		public bool TryGetCustomData (string key, [NotNullWhen (true)] out string? value)
		{
			return _parameters.TryGetValue (key, out value);
		}

		public void Dispose ()
		{
			_resolver.Dispose ();
		}

		public bool IsOptimizationEnabled (CodeOptimizations optimization, MemberReference context)
		{
			return Optimizations.IsEnabled (optimization, context?.Module.Assembly);
		}

		public bool IsOptimizationEnabled (CodeOptimizations optimization, AssemblyDefinition? context)
		{
			return Optimizations.IsEnabled (optimization, context);
		}

		public bool CanApplyOptimization (CodeOptimizations optimization, AssemblyDefinition context)
		{
			return Annotations.GetAction (context) == AssemblyAction.Link &&
				IsOptimizationEnabled (optimization, context);
		}

		public void LogMessage (MessageContainer message)
		{
			if (message == MessageContainer.Empty)
				return;

			if ((message.Category == MessageCategory.Diagnostic ||
				message.Category == MessageCategory.Info) && !LogMessages)
				return;

			if (WarningSuppressionWriter != null &&
				message.IsWarningMessage (out int? code) &&
				message.Origin?.Provider is Mono.Cecil.ICustomAttributeProvider provider)
				WarningSuppressionWriter.AddWarning (code.Value, provider);

			if (message.Category == MessageCategory.Error || message.Category == MessageCategory.WarningAsError)
				ErrorsCount++;

			_logger.LogMessage (message);
		}

		public void LogMessage (string message)
		{
			if (!LogMessages)
				return;

			LogMessage (MessageContainer.CreateInfoMessage (message));
		}

		public void LogDiagnostic (string message)
		{
			if (!LogMessages)
				return;

			LogMessage (MessageContainer.CreateDiagnosticMessage (message));
		}


		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in ILLink, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="text">Humanly readable message describing the warning</param>
		/// <param name="code">Unique warning ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="origin">Filename or member where the warning is coming from</param>
		/// <param name="subcategory">Optionally, further categorize this warning</param>
		public void LogWarning (string text, int code, MessageOrigin origin, string subcategory = MessageSubCategory.None)
		{
			WarnVersion version = GetWarningVersion ();
			MessageContainer warning = MessageContainer.CreateWarningMessage (this, text, code, origin, version, subcategory);
			_cachedWarningMessageContainers.Add (warning);
		}

		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in ILLink, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="origin">Filename or member where the warning is coming from</param>
		/// <param name="id">Unique warning ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="args">Additional arguments to form a humanly readable message describing the warning</param>
		public void LogWarning (MessageOrigin origin, DiagnosticId id, params string[] args)
		{
			WarnVersion version = GetWarningVersion ();
			MessageContainer warning = MessageContainer.CreateWarningMessage (this, origin, id, version, args);
			_cachedWarningMessageContainers.Add (warning);
		}

		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in ILLink, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="text">Humanly readable message describing the warning</param>
		/// <param name="code">Unique warning ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="origin">Type or member where the warning is coming from</param>
		/// <param name="subcategory">Optionally, further categorize this warning</param>
		public void LogWarning (string text, int code, IMemberDefinition origin, int? ilOffset = null, string subcategory = MessageSubCategory.None)
		{
			MessageOrigin _origin = new MessageOrigin (origin, ilOffset);
			LogWarning (text, code, _origin, subcategory);
		}

		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in ILLink, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="origin">Type or member where the warning is coming from</param>
		/// <param name="id">Unique warning ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="args">Additional arguments to form a humanly readable message describing the warning</param>
		public void LogWarning (IMemberDefinition origin, DiagnosticId id, int? ilOffset = null, params string[] args)
		{
			MessageOrigin _origin = new MessageOrigin (origin, ilOffset);
			LogWarning (_origin, id, args);
		}

		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in ILLink, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="origin">Type or member where the warning is coming from</param>
		/// <param name="id">Unique warning ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="args">Additional arguments to form a humanly readable message describing the warning</param>
		public void LogWarning (IMemberDefinition origin, DiagnosticId id, params string[] args)
		{
			MessageOrigin _origin = new MessageOrigin (origin);
			LogWarning (_origin, id, args);
		}

		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in ILLink, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="text">Humanly readable message describing the warning</param>
		/// <param name="code">Unique warning ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="origin">Filename where the warning is coming from</param>
		/// <param name="subcategory">Optionally, further categorize this warning</param>
		public void LogWarning (string text, int code, string origin, string subcategory = MessageSubCategory.None)
		{
			MessageOrigin _origin = new MessageOrigin (origin);
			LogWarning (text, code, _origin, subcategory);
		}

		/// <summary>
		/// Display a warning message to the end user.
		/// This API is used for warnings defined in ILLink, not by custom steps. Warning
		/// versions are inferred from the code, and every warning that we define is versioned.
		/// </summary>
		/// <param name="origin">Filename where the warning is coming from</param>
		/// <param name="id">Unique warning ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md for the list of warnings and possibly add a new one</param>
		/// <param name="args">Additional arguments to form a humanly readable message describing the warning</param>
		public void LogWarning (string origin, DiagnosticId id, params string[] args)
		{
			MessageOrigin _origin = new MessageOrigin (origin);
			LogWarning (_origin, id, args);
		}

		/// <summary>
		/// Display an error message to the end user.
		/// </summary>
		/// <param name="text">Humanly readable message describing the error</param>
		/// <param name="code">Unique error ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md for the list of errors and possibly add a new one</param>
		/// <param name="subcategory">Optionally, further categorize this error</param>
		/// <param name="origin">Filename, line, and column where the error was found</param>
		public void LogError (string text, int code, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
		{
			var error = MessageContainer.CreateErrorMessage (text, code, subcategory, origin);
			LogMessage (error);
		}

		/// <summary>
		/// Display an error message to the end user.
		/// </summary>
		/// <param name="origin">Filename, line, and column where the error was found</param>
		/// <param name="id">Unique error ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md and https://github.com/dotnet/runtime/blob/main/src/tools/illink/src/ILLink.Shared/DiagnosticId.cs for the list of errors and possibly add a new one</param>
		/// <param name="args">Additional arguments to form a humanly readable message describing the warning</param>
		public void LogError (MessageOrigin? origin, DiagnosticId id, params string[] args)
		{
			var error = MessageContainer.CreateErrorMessage (origin, id, args);
			LogMessage (error);
		}

		/// <summary>
		/// Throws a LinkerFatalErrorException
		/// </summary>
		/// <param name="text">Humanly readable message describing the error</param>
		/// <param name="code">Unique error ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md
		/// for the list of errors and possibly add a new one</param>
		/// <param name="subcategory">Optionally, further categorize this error</param>
		/// <param name="origin">Filename, line, and column where the error was found</param>
		public static void FatalError (string text, int code, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
		{
			throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage (text, code, subcategory, origin));
		}

		/// <summary>
		/// Throws a LinkerFatalErrorException
		/// </summary>
		/// <param name="text">Humanly readable message describing the error</param>
		/// <param name="code">Unique error ID. Please see https://github.com/dotnet/runtime/blob/main/docs/tools/illink/error-codes.md
		/// for the list of errors and possibly add a new one</param>
		/// <param name="subcategory">Optionally, further categorize this error</param>
		/// <param name="origin">Filename, line, and column where the error was found</param>
		/// <param name="innerException">Optional, an inner exception</param>
		public static void FatalError (string text, int code, Exception innerException, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
		{
			throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage (text, code, subcategory, origin), innerException);
		}

		public void FlushCachedWarnings ()
		{
			_cachedWarningMessageContainers.Sort ();
			foreach (var warning in _cachedWarningMessageContainers)
				LogMessage (warning);

			_cachedWarningMessageContainers.Clear ();
		}

		public bool IsWarningSuppressed (int warningCode, string subcategory, MessageOrigin origin)
		{
			if (subcategory == MessageSubCategory.TrimAnalysis && NoTrimWarn)
				return true;

			// This warning was turned off by --nowarn.
			if (NoWarn.Contains (warningCode))
				return true;

			if (Suppressions == null)
				return false;

			return Suppressions.IsSuppressed (warningCode, origin, out _);
		}

		public bool IsWarningAsError (int warningCode)
		{
			bool value;
			if (GeneralWarnAsError)
				return !WarnAsError.TryGetValue (warningCode, out value) || value;

			return WarnAsError.TryGetValue (warningCode, out value) && value;
		}

		public bool IsSingleWarn (string assemblyName)
		{
			bool value;
			if (GeneralSingleWarn)
				return !SingleWarn.TryGetValue (assemblyName, out value) || value;

			return SingleWarn.TryGetValue (assemblyName, out value) && value;
		}

		static WarnVersion GetWarningVersion ()
		{
			// This should return an increasing WarnVersion for new warning waves.
			return WarnVersion.ILLink5;
		}

		public int GetTargetRuntimeVersion ()
		{
			if (_targetRuntime != null)
				return _targetRuntime.Value;

			TypeDefinition? objectType = BCL.FindPredefinedType (WellKnownType.System_Object, this);
			_targetRuntime = objectType?.Module.Assembly.Name.Version.Major ?? -1;

			return _targetRuntime.Value;
		}

		readonly Dictionary<MethodReference, MethodDefinition?> methodresolveCache = new ();
		readonly Dictionary<FieldReference, FieldDefinition?> fieldresolveCache = new ();
		readonly Dictionary<TypeReference, TypeDefinition?> typeresolveCache = new ();
		readonly Dictionary<ExportedType, TypeDefinition?> exportedTypeResolveCache = new ();

		/// <summary>
		/// Tries to resolve the MethodReference to a MethodDefinition and logs a warning if it can't
		/// </summary>
		public MethodDefinition? Resolve (MethodReference methodReference)
		{
			if (methodReference is MethodDefinition methodDefinition)
				return methodDefinition;

			if (methodReference is null)
				return null;

			if (methodresolveCache.TryGetValue (methodReference, out MethodDefinition? md))
				return md;

#pragma warning disable RS0030 // Cecil's resolve is banned -- this provides the wrapper
			md = methodReference.Resolve ();
#pragma warning restore RS0030
			if (md == null && !IgnoreUnresolved)
				ReportUnresolved (methodReference);

			methodresolveCache.Add (methodReference, md);
			return md;
		}

		/// <summary>
		/// Tries to resolve the MethodReference to a MethodDefinition and returns null if it can't
		/// </summary>
		public MethodDefinition? TryResolve (MethodReference methodReference)
		{
			if (methodReference is MethodDefinition methodDefinition)
				return methodDefinition;

			if (methodReference is null)
				return null;

			if (methodresolveCache.TryGetValue (methodReference, out MethodDefinition? md))
				return md;

#pragma warning disable RS0030 // Cecil's resolve is banned -- this method provides the wrapper
			md = methodReference.Resolve ();
#pragma warning restore RS0030
			methodresolveCache.Add (methodReference, md);
			return md;
		}

		/// <summary>
		/// Tries to resolve the FieldReference to a FieldDefinition and logs a warning if it can't
		/// </summary>
		public FieldDefinition? Resolve (FieldReference fieldReference)
		{
			if (fieldReference is FieldDefinition fieldDefinition)
				return fieldDefinition;

			if (fieldReference is null)
				return null;

			if (fieldresolveCache.TryGetValue (fieldReference, out FieldDefinition? fd))
				return fd;

			fd = fieldReference.Resolve ();
			if (fd == null && !IgnoreUnresolved)
				ReportUnresolved (fieldReference);

			fieldresolveCache.Add (fieldReference, fd);
			return fd;
		}

		/// <summary>
		/// Tries to resolve the FieldReference to a FieldDefinition and returns null if it can't
		/// </summary>
		public FieldDefinition? TryResolve (FieldReference fieldReference)
		{
			if (fieldReference is FieldDefinition fieldDefinition)
				return fieldDefinition;

			if (fieldReference is null)
				return null;

			if (fieldresolveCache.TryGetValue (fieldReference, out FieldDefinition? fd))
				return fd;

			fd = fieldReference.Resolve ();
			fieldresolveCache.Add (fieldReference, fd);
			return fd;
		}

		/// <summary>
		/// Tries to resolve the TypeReference to a TypeDefinition and logs a warning if it can't
		/// </summary>
		public TypeDefinition? Resolve (TypeReference typeReference)
		{
			if (typeReference is TypeDefinition typeDefinition)
				return typeDefinition;

			if (typeReference is null)
				return null;

			if (typeresolveCache.TryGetValue (typeReference, out TypeDefinition? td))
				return td;

			//
			// Types which never have TypeDefinition or can have ambiguous definition should not be passed in
			//
			if (typeReference is GenericParameter || (typeReference is TypeSpecification && typeReference is not GenericInstanceType))
				throw new NotSupportedException ($"TypeDefinition cannot be resolved from '{typeReference.GetType ()}' type");

#pragma warning disable RS0030
			td = typeReference.Resolve ();
#pragma warning restore RS0030
			if (td == null && !IgnoreUnresolved)
				ReportUnresolved (typeReference);

			typeresolveCache.Add (typeReference, td);
			return td;
		}

		/// <summary>
		/// Tries to resolve the TypeReference to a TypeDefinition and returns null if it can't
		/// </summary>
		public TypeDefinition? TryResolve (TypeReference typeReference)
		{
			if (typeReference is TypeDefinition typeDefinition)
				return typeDefinition;

			if (typeReference is null || typeReference is GenericParameter)
				return null;

			if (typeresolveCache.TryGetValue (typeReference, out TypeDefinition? td))
				return td;

			if (typeReference is TypeSpecification ts) {
				if (typeReference is FunctionPointerType) {
					td = null;
				} else {
					//
					// It returns element-type for arrays and also element type for wrapping types like ByReference, PinnedType, etc
					//
					td = TryResolve (ts.GetElementType ());
				}
			} else {
#pragma warning disable RS0030
				td = typeReference.Resolve ();
#pragma warning restore RS0030
			}

			typeresolveCache.Add (typeReference, td);
			return td;
		}

		/// <summary>
		/// Tries to resolve the ExportedType to a TypeDefinition and logs a warning if it can't
		/// </summary>
		public TypeDefinition? Resolve (ExportedType et)
		{
			if (TryResolve (et) is not TypeDefinition td) {
				ReportUnresolved (et);
				return null;
			}
			return td;
		}

		/// <summary>
		/// Tries to resolve the ExportedType to a TypeDefinition and returns null if it can't
		/// </summary>
		public TypeDefinition? TryResolve (ExportedType et)
		{
			if (exportedTypeResolveCache.TryGetValue (et, out var td)) {
				return td;
			}
#pragma warning disable RS0030 // Cecil's Resolve is banned -- this method provides the wrapper
			td = et.Resolve ();
#pragma warning restore RS0030
			exportedTypeResolveCache.Add (et, td);
			return td;
		}

		public TypeDefinition? TryResolve (AssemblyDefinition assembly, string typeNameString)
		{
			// It could be cached if it shows up on fast path
			return _typeNameResolver.TryResolveTypeName (assembly, typeNameString, out TypeReference? typeReference, out _)
				? TryResolve (typeReference)
				: null;
		}

		readonly HashSet<MethodDefinition> _processed_bodies_for_method = new HashSet<MethodDefinition> (2048);

		/// <summary>
		/// ILLink applies some optimization on method bodies. For example it can remove dead branches of code
		/// based on constant propagation. To avoid overmarking, all code which processes the method's IL
		/// should only view the IL after it's been optimized.
		/// As such typically MethodDefinition.MethodBody should not be accessed directly on the Cecil object model
		/// instead all accesses to method body should go through the ILProvider here
		/// which will make sure the IL of the method is fully optimized before it's handed out.
		/// </summary>
		public MethodIL GetMethodIL (Cecil.Cil.MethodBody methodBody)
			=> GetMethodIL (methodBody.Method);

		public MethodIL GetMethodIL (MethodDefinition method)
		{
			if (_processed_bodies_for_method.Add (method)) {
				_unreachableBlocksOptimizer.ProcessMethod (method);
			}

			return MethodIL.Create (method.Body);
		}

		readonly HashSet<MemberReference> unresolved_reported = new ();

		readonly HashSet<ExportedType> unresolved_exported_types_reported = new ();

		protected virtual void ReportUnresolved (FieldReference fieldReference)
		{
			if (unresolved_reported.Add (fieldReference))
				LogError (string.Format (SharedStrings.FailedToResolveFieldElementMessage, fieldReference.FullName), (int) DiagnosticId.FailedToResolveMetadataElement);
		}

		protected virtual void ReportUnresolved (MethodReference methodReference)
		{
			if (unresolved_reported.Add (methodReference))
				LogError (string.Format (SharedStrings.FailedToResolveMethodElementMessage, methodReference.GetDisplayName ()), (int) DiagnosticId.FailedToResolveMetadataElement);
		}

		protected virtual void ReportUnresolved (TypeReference typeReference)
		{
			if (unresolved_reported.Add (typeReference))
				LogError (string.Format (SharedStrings.FailedToResolveTypeElementMessage, typeReference.GetDisplayName ()), (int) DiagnosticId.FailedToResolveMetadataElement);
		}

		protected virtual void ReportUnresolved (ExportedType et)
		{
			if (unresolved_exported_types_reported.Add (et))
				LogError (string.Format (SharedStrings.FailedToResolveTypeElementMessage, et.Name), (int) DiagnosticId.FailedToResolveMetadataElement);
		}
	}

	public class CodeOptimizationsSettings
	{
		sealed class Pair
		{
			public Pair (CodeOptimizations set, CodeOptimizations values)
			{
				this.Set = set;
				this.Values = values;
			}

			public CodeOptimizations Set;
			public CodeOptimizations Values;
		}

		readonly Dictionary<string, Pair> perAssembly = new ();

		public CodeOptimizationsSettings (CodeOptimizations globalOptimizations)
		{
			Global = globalOptimizations;
		}

		public CodeOptimizations Global { get; private set; }

		internal bool IsEnabled (CodeOptimizations optimizations, AssemblyDefinition? context)
		{
			return IsEnabled (optimizations, context?.Name.Name);
		}

		public bool IsEnabled (CodeOptimizations optimizations, string? assemblyName)
		{
			// Only one bit is set
			Debug.Assert (optimizations != 0 && (optimizations & (optimizations - 1)) == 0);

			if (perAssembly.Count > 0 && assemblyName != null &&
				perAssembly.TryGetValue (assemblyName, out var assemblySetting) &&
				(assemblySetting.Set & optimizations) != 0) {
				return (assemblySetting.Values & optimizations) != 0;
			}

			return (Global & optimizations) != 0;
		}

		public void Enable (CodeOptimizations optimizations, string? assemblyContext = null)
		{
			if (assemblyContext == null) {
				Global |= optimizations;
				return;
			}

			if (!perAssembly.TryGetValue (assemblyContext, out var assemblySetting)) {
				perAssembly.Add (assemblyContext, new Pair (optimizations, optimizations));
				return;
			}

			assemblySetting.Set |= optimizations;
			assemblySetting.Values |= optimizations;
		}

		public void Disable (CodeOptimizations optimizations, string? assemblyContext = null)
		{
			if (assemblyContext == null) {
				Global &= ~optimizations;
				return;
			}

			if (!perAssembly.TryGetValue (assemblyContext, out var assemblySetting)) {
				perAssembly.Add (assemblyContext, new Pair (optimizations, 0));
				return;
			}

			assemblySetting.Set |= optimizations;
			assemblySetting.Values &= ~optimizations;
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
		UnreachableBodies = 1 << 2,

		/// <summary>
		/// Option to remove .interfaceimpl for interface types that are not used
		/// </summary>
		UnusedInterfaces = 1 << 3,

		/// <summary>
		/// Option to do interprocedural constant propagation on return values
		/// </summary>
		IPConstantPropagation = 1 << 4,

		/// <summary>
		/// Devirtualizes methods and seals types
		/// </summary>
		Sealer = 1 << 5,

		/// <summary>
		/// Option to inline typechecks for never instantiated types
		/// </summary>
		UnusedTypeChecks = 1 << 6,


		RemoveDescriptors = 1 << 20,
		RemoveSubstitutions = 1 << 21,
		RemoveLinkAttributes = 1 << 22,
		RemoveDynamicDependencyAttribute = 1 << 23,

		/// <summary>
		/// Option to apply annotations to type heirarchy
		/// Enable type heirarchy apply in library mode to annotate derived types eagerly
		/// Otherwise, type annotation will only be applied with calls to object.GetType()
		/// </summary>
		OptimizeTypeHierarchyAnnotations = 1 << 24,

		/// <summary>
		/// Substitute "featuredefault" settings for substitution XMLs, and
		/// FeatureCheck(typeof(RequiresUnreferencedCodeAttribute)) properties for features not
		/// explicitly given values on the command-line.
		/// </summary>
		SubstituteFeatureChecks = 1 << 25,
	}
}
