// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// Driver.cs
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
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using ILLink.Shared;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker
{

	public partial class Driver : IDisposable
	{
		const string resolvers = "-a|-x";
		const string _linker = "IL Linker";

		public static int Main (string[] args)
		{
			LinkerEventSource.Log.LinkerStart (string.Join ("; ", args));
			if (args.Length == 0) {
				Console.Error.WriteLine ("No parameters specified");
				LinkerEventSource.Log.LinkerStop ();
				return 1;
			}

			if (!ProcessResponseFile (args, out var arguments)) {
				LinkerEventSource.Log.LinkerStop ();
				return 1;
			}

			try {
				using (Driver driver = new Driver (arguments)) {
					return driver.Run ();
				}
			} catch {
				Console.Error.WriteLine ("Fatal error in {0}", _linker);
				throw;
			} finally {
				LinkerEventSource.Log.LinkerStop ();
			}
		}

		readonly Queue<string> arguments;
		bool _needAddBypassNGenStep;
		LinkContext? context;
		protected LinkContext Context {
			get {
				Debug.Assert (context != null);
				return context;
			}
			set {
				Debug.Assert (context == null);
				context = value;
			}
		}

		private static readonly char[] s_separators = new char[] { ',', ';', ' ' };

		public Driver (Queue<string> arguments)
		{
			this.arguments = arguments;
		}

		public static bool ProcessResponseFile (string[] args, out Queue<string> result)
		{
			result = new Queue<string> ();
			foreach (string arg in args) {
				if (arg.StartsWith ("@")) {
					try {
						string responseFileName = arg.Substring (1);
						using (var responseFileText = new StreamReader (responseFileName))
							ParseResponseFile (responseFileText, result);
					} catch (Exception e) when (e is IOException or ObjectDisposedException) {
						Console.Error.WriteLine ("Cannot read response file due to '{0}'", e.Message);
						return false;
					}
				} else {
					result.Enqueue (arg);
				}
			}

			return true;
		}

		public static void ParseResponseFile (TextReader reader, Queue<string> result)
		{
			int cur;
			while ((cur = reader.Read ()) >= 0) {
				// skip whitespace
				while (char.IsWhiteSpace ((char) cur)) {
					if ((cur = reader.Read ()) < 0)
						break;
				}
				if (cur < 0) // EOF
					break;

				StringBuilder argBuilder = new StringBuilder ();
				bool inquote = false;

				// build up an argument one character at a time
				while (true) {
					bool copyChar = true;
					int numBackslash = 0;

					// count backslashes
					while (cur == '\\') {
						numBackslash++;
						if ((cur = reader.Read ()) < 0)
							break;
					}
					if (cur == '"') {
						if ((numBackslash % 2) == 0) {
							if (inquote && (reader.Peek () == '"')) {
								// handle "" escape sequence in a quote
								cur = reader.Read ();
							} else {
								// unescaped " begins/ends a quote
								copyChar = false;
								inquote = !inquote;
							}
						}
						// treat backslashes before " as escapes
						numBackslash /= 2;
					}
					if (numBackslash > 0)
						argBuilder.Append (new string ('\\', numBackslash));
					if (cur < 0 || (!inquote && char.IsWhiteSpace ((char) cur)))
						break;
					if (copyChar)
						argBuilder.Append ((char) cur);
					cur = reader.Read ();
				}
				result.Enqueue (argBuilder.ToString ());
			}
		}

		void ErrorMissingArgument (string optionName)
		{
			Context.LogError (null, DiagnosticId.MissingArgumentForCommanLineOptionName, optionName);
		}

		public enum DependenciesFileFormat
		{
			Xml,
			Dgml
		};

		// Perform setup of the LinkContext and parse the arguments.
		// Return values:
		// 0 => successfully set up context with all arguments
		// 1 => argument processing stopped early without errors
		// -1 => error setting up context
		protected int SetupContext (ILogger? customLogger = null)
		{
			Pipeline p = GetStandardPipeline ();
			context = GetDefaultContext (p, customLogger);

			var body_substituter_steps = new Stack<string> ();
			var xml_custom_attribute_steps = new Stack<string> ();
			var custom_steps = new List<string> ();
			var set_optimizations = new List<(CodeOptimizations, string?, bool)> ();
			bool dumpDependencies = false;
			string? dependenciesFileName = null;
			context.StripSecurity = true;
			bool new_mvid_used = false;
			bool deterministic_used = false;
			bool keepCompilersResources = false;
			MetadataTrimming metadataTrimming = MetadataTrimming.Any;
			DependenciesFileFormat fileType = DependenciesFileFormat.Xml;

			List<BaseStep> inputs = CreateDefaultResolvers ();

			while (arguments.Count > 0) {
				string token = arguments.Dequeue ();
				if (token.Length < 2) {
					context.LogError (null, DiagnosticId.UnrecognizedCommandLineOption, token);
					return -1;
				}

				//
				// Handling of --value like options
				//
				if (token[0] == '-' && token[1] == '-') {
					switch (token) {
					case "--help":
						Usage ();
						return 1;

					case "--skip-unresolved":
						if (!GetBoolParam (token, l => context.IgnoreUnresolved = l))
							return -1;

						continue;

					case "--verbose":
						context.LogMessages = true;
						continue;

					case "--dependencies-file":
						if (!GetStringParam (token, out dependenciesFileName))
							return -1;

						continue;

					case "--dump-dependencies":
						dumpDependencies = true;
						continue;

					case "--dependencies-file-format":
						if (!GetStringParam (token, out var dependenciesFileFormat))
							return -1;

						if (!Enum.TryParse (dependenciesFileFormat, ignoreCase: true, out fileType)) {
							context.LogError (null, DiagnosticId.InvalidDependenciesFileFormat);
							return -1;
						}
						continue;

					case "--reduced-tracing":
						if (!GetBoolParam (token, l => context.EnableReducedTracing = l))
							return -1;

						continue;

					case "--used-attrs-only":
						if (!GetBoolParam (token, l => context.KeepUsedAttributeTypesOnly = l))
							return -1;

						continue;

					case "--strip-security":
						if (!GetBoolParam (token, l => context.StripSecurity = l))
							return -1;

						continue;

					case "--strip-descriptors":
						if (!GetBoolParam (token, l => set_optimizations.Add ((CodeOptimizations.RemoveDescriptors, null, l))))
							return -1;

						continue;

					case "--strip-substitutions":
						if (!GetBoolParam (token, l => set_optimizations.Add ((CodeOptimizations.RemoveSubstitutions, null, l))))
							return -1;

						continue;

					case "--strip-link-attributes":
						if (!GetBoolParam (token, l => set_optimizations.Add ((CodeOptimizations.RemoveLinkAttributes, null, l))))
							return -1;

						continue;

					case "--substitutions":
						if (arguments.Count < 1) {
							ErrorMissingArgument (token);
							return -1;
						}

						if (!GetStringParam (token, out string? substitutionFile))
							return -1;

						body_substituter_steps.Push (substitutionFile);

						continue;
					case "--explicit-reflection":
						if (!GetBoolParam (token, l => context.AddReflectionAnnotations = l))
							return -1;

						continue;

					case "--action": {
							if (!GetStringParam (token, out string? actionString))
								return -1;

							AssemblyAction? action = ParseAssemblyAction (actionString);
							if (action == null)
								return -1;

							string? assemblyName = GetNextStringValue ();
							if (assemblyName == null) {
								context.DefaultAction = action.Value;
								continue;
							}

							if (!IsValidAssemblyName (assemblyName)) {
								context.LogError (null, DiagnosticId.InvalidAssemblyName, assemblyName);
								return -1;
							}

							context.RegisterAssemblyAction (assemblyName, action.Value);
							continue;
						}
					case "--trim-mode": {
							if (!GetStringParam (token, out string? actionString))
								return -1;

							AssemblyAction? action = ParseAssemblyAction (actionString);
							if (action == null)
								return -1;

							context.TrimAction = action.Value;
							continue;
						}
					case "--custom-step":
						if (!GetStringParam (token, out string? custom_step))
							return -1;

						custom_steps.Add (custom_step);

						continue;

					case "--custom-data":
						if (arguments.Count < 1) {
							ErrorMissingArgument (token);
							return -1;
						}

						var arg = arguments.Dequeue ();
						string[] values = arg.Split ('=');
						if (values?.Length != 2) {
							context.LogError (null, DiagnosticId.CustomDataFormatIsInvalid);
							return -1;
						}

						context.SetCustomData (values[0], values[1]);
						continue;

					case "--keep-compilers-resources":
						if (!GetBoolParam (token, l => keepCompilersResources = l))
							return -1;

						continue;

					case "--keep-dep-attributes":
						if (!GetBoolParam (token, l => set_optimizations.Add ((CodeOptimizations.RemoveDynamicDependencyAttribute, null, !l))))
							return -1;

						continue;

					case "--keep-metadata": {
							if (!GetStringParam (token, out string? mname))
								return -1;

							if (!TryGetMetadataTrimming (mname, out var type))
								return -1;

							metadataTrimming &= ~type;
							continue;
						}

					case "--enable-serialization-discovery":
						if (!GetBoolParam (token, l => context.EnableSerializationDiscovery = l))
							return -1;

						continue;

					case "--disable-operator-discovery":
						if (!GetBoolParam (token, l => context.DisableOperatorDiscovery = l))
							return -1;

						continue;

					case "--ignore-descriptors":
						if (!GetBoolParam (token, l => context.IgnoreDescriptors = l))
							return -1;

						continue;

					case "--ignore-substitutions":
						if (!GetBoolParam (token, l => context.IgnoreSubstitutions = l))
							return -1;

						continue;

					case "--ignore-link-attributes":
						if (!GetBoolParam (token, l => context.IgnoreLinkAttributes = l))
							return -1;

						continue;

					case "--disable-opt": {
							if (!GetStringParam (token, out string? optName))
								return -1;

							if (!GetOptimizationName (optName, out var opt))
								return -1;

							string? assemblyName = GetNextStringValue ();
							set_optimizations.Add ((opt, assemblyName, false));

							continue;
						}
					case "--enable-opt": {
							if (!GetStringParam (token, out string? optName))
								return -1;

							if (!GetOptimizationName (optName, out var opt))
								return -1;

							string? assemblyName = GetNextStringValue ();
							set_optimizations.Add ((opt, assemblyName, true));

							continue;
						}

					case "--feature": {
							if (!GetStringParam (token, out string? featureName))
								return -1;

							if (!GetBoolParam (token, value => {
								context.SetFeatureValue (featureName, value);
							}))
								return -1;

							continue;
						}
					case "--new-mvid":
						//
						// This is not same as --deterministic which calculates MVID
						// from stable assembly content. This option creates a new random
						// mvid or uses mvid of the source assembly.
						//
						if (!GetBoolParam (token, l => {
							if (!l)
								p.RemoveStep (typeof (RegenerateGuidStep));
						}))
							return -1;

						new_mvid_used = true;
						continue;

					case "--deterministic":
						if (!GetBoolParam (token, l => context.DeterministicOutput = l))
							return -1;

						deterministic_used = true;
						continue;

					case "--output-assemblylist":
						if (!GetStringParam (token, out string? assemblyListFile))
							return -1;

						context.AssemblyListFile = assemblyListFile;

						continue;

					case "--output-pinvokes":
						if (!GetStringParam (token, out string? pinvokesListFile))
							return -1;

						context.PInvokesListFile = pinvokesListFile;

						continue;

					case "--link-attributes":
						if (arguments.Count < 1) {
							ErrorMissingArgument (token);
							return -1;
						}

						if (!GetStringParam (token, out string? fileList))
							return -1;

						foreach (string file in GetFiles (fileList))
							xml_custom_attribute_steps.Push (file);

						continue;

					case "--generate-warning-suppressions":
						if (!GetStringParam (token, out string? generateWarningSuppressionsArgument))
							return -1;

						if (!GetWarningSuppressionWriterFileOutputKind (generateWarningSuppressionsArgument, out var fileOutputKind)) {
							context.LogError (null, DiagnosticId.InvalidGenerateWarningSuppressionsValue, generateWarningSuppressionsArgument);
							return -1;
						}

						context.WarningSuppressionWriter = new WarningSuppressionWriter (context, fileOutputKind);
						continue;

					case "--notrimwarn":
						context.NoTrimWarn = true;
						continue;

					case "--nowarn":
						if (!GetStringParam (token, out string? noWarnArgument))
							return -1;

						context.NoWarn.UnionWith (ProcessWarningCodes (noWarnArgument));
						continue;

					case "--warnaserror":
					case "--warnaserror+":
						var warningList = GetNextStringValue ();
						if (!string.IsNullOrEmpty (warningList)) {
							foreach (var warning in ProcessWarningCodes (warningList))
								context.WarnAsError[warning] = true;

						} else {
							context.GeneralWarnAsError = true;
							context.WarnAsError.Clear ();
						}

						continue;

					case "--warnaserror-":
						warningList = GetNextStringValue ();
						if (!string.IsNullOrEmpty (warningList)) {
							foreach (var warning in ProcessWarningCodes (warningList))
								context.WarnAsError[warning] = false;

						} else {
							context.GeneralWarnAsError = false;
							context.WarnAsError.Clear ();
						}

						continue;

					case "--warn":
						if (!GetStringParam (token, out string? warnVersionArgument))
							return -1;

						if (!GetWarnVersion (warnVersionArgument, out WarnVersion version))
							return -1;

						context.WarnVersion = version;

						continue;

					case "--singlewarn":
					case "--singlewarn+": {
							string? assemblyName = GetNextStringValue ();
							if (assemblyName != null) {
								if (!IsValidAssemblyName (assemblyName)) {
									context.LogError (null, DiagnosticId.InvalidAssemblyName, assemblyName);
									return -1;
								}

								context.SingleWarn[assemblyName] = true;
							} else {
								context.GeneralSingleWarn = true;
								context.SingleWarn.Clear ();
							}

							continue;
						}

					case "--singlewarn-": {
							string? assemblyName = GetNextStringValue ();
							if (assemblyName != null) {
								if (!IsValidAssemblyName (assemblyName)) {
									context.LogError (null, DiagnosticId.InvalidAssemblyName, assemblyName);
									return -1;
								}

								context.SingleWarn[assemblyName] = false;
							} else {
								context.GeneralSingleWarn = false;
								context.SingleWarn.Clear ();
							}

							continue;
						}

					case "--version":
						Version ();
						return 1;

					case "--about":
						About ();
						return 1;
					}
				}

				if (token[0] == '-' || token[1] == '/') {

					switch (token.Substring (1)) {
					case "d":
						if (!GetStringParam (token, out string? directory))
							return -1;

						DirectoryInfo info = new DirectoryInfo (directory);
						context.Resolver.AddSearchDirectory (info.FullName);

						continue;
					case "o":
					case "out":
						if (!GetStringParam (token, out string? outputDirectory))
							return -1;

						context.OutputDirectory = outputDirectory;

						continue;
					case "x": {
							if (!GetStringParam (token, out string? xmlFile))
								return -1;

							if (!File.Exists (xmlFile)) {
								context.LogError (null, DiagnosticId.XmlDescriptorCouldNotBeFound, xmlFile);
								return -1;
							}

							inputs.Add (new ResolveFromXmlStep (File.OpenRead (xmlFile), xmlFile));
							continue;
						}
					case "a": {
							if (!GetStringParam (token, out string? assemblyFile))
								return -1;

							if (!File.Exists (assemblyFile) && assemblyFile.EndsWith (".dll", StringComparison.InvariantCultureIgnoreCase)) {
								context.LogError (null, DiagnosticId.RootAssemblyCouldNotBeFound, assemblyFile);
								return -1;
							}

							AssemblyRootMode rmode = AssemblyRootMode.AllMembers;
							var rootMode = GetNextStringValue ();
							if (rootMode != null) {
								var parsed_rmode = ParseAssemblyRootMode (rootMode);
								if (parsed_rmode is null)
									return -1;

								rmode = parsed_rmode.Value;
							}

							inputs.Add (new RootAssemblyInput (assemblyFile, rmode));
							continue;
						}
					case "b":
						if (!GetBoolParam (token, l => context.LinkSymbols = l))
							return -1;

						continue;
					case "g":
						if (!GetBoolParam (token, l => context.DeterministicOutput = !l))
							return -1;

						continue;
					case "z":
						if (!GetBoolParam (token, l => context.IgnoreDescriptors = !l))
							return -1;

						continue;
					case "?":
					case "h":
					case "help":
						Usage ();
						return 1;

					case "reference":
						if (!GetStringParam (token, out string? reference))
							return -1;

						context.Resolver.AddReferenceAssembly (reference);

						continue;
					}
				}

				context.LogError (null, DiagnosticId.UnrecognizedCommandLineOption, token);
				return -1;
			}

			if (inputs.Count == 0) {
				context.LogError (null, DiagnosticId.NoFilesToLinkSpecified, resolvers);
				return -1;
			}

			if (new_mvid_used && deterministic_used) {
				context.LogError (null, DiagnosticId.NewMvidAndDeterministicCannotBeUsedAtSameTime);
				return -1;
			}

			context.MetadataTrimming = metadataTrimming;

			// Default to deterministic output
			if (!new_mvid_used && !deterministic_used) {
				context.DeterministicOutput = true;
			}
			if (dumpDependencies) {
				switch (fileType) {
				case DependenciesFileFormat.Xml:
					AddXmlDependencyRecorder (context, dependenciesFileName);
					break;
				case DependenciesFileFormat.Dgml:
					AddDgmlDependencyRecorder (context, dependenciesFileName);
					break;
				default:
					context.LogError (null, DiagnosticId.InvalidDependenciesFileFormat);
					break;
				}
			}


			if (set_optimizations.Count > 0) {
				foreach (var (opt, assemblyName, enable) in set_optimizations) {
					if (enable)
						context.Optimizations.Enable (opt, assemblyName);
					else
						context.Optimizations.Disable (opt, assemblyName);
				}
			}

			//
			// Modify the default pipeline
			//

			for (int i = inputs.Count; i != 0; --i)
				p.PrependStep (inputs[i - 1]);

			foreach (var file in xml_custom_attribute_steps)
				AddLinkAttributesStep (p, file);

			foreach (var file in body_substituter_steps)
				AddBodySubstituterStep (p, file);

			if (context.DeterministicOutput)
				p.RemoveStep (typeof (RegenerateGuidStep));

			if (context.AddReflectionAnnotations)
				p.AddStepAfter (typeof (MarkStep), new ReflectionBlockedStep ());

			if (_needAddBypassNGenStep)
				p.AddStepAfter (typeof (SweepStep), new AddBypassNGenStep ());

			if (keepCompilersResources) {
				p.RemoveStep (typeof (RemoveResourcesStep));
			}

			p.AddStepBefore (typeof (OutputStep), new SealerStep ());

			//
			// Pipeline setup with all steps enabled
			//
			// RootAssemblyInputStep or ResolveFromXmlStep [at least one of them]
			// LinkAttributesStep [optional, possibly many]
			// BodySubstituterStep [optional]
			// MarkStep
			// ReflectionBlockedStep [optional]
			// RemoveResourcesStep [optional]
			// ProcessWarningsStep
			// OutputWarningSuppressions
			// SweepStep
			// AddBypassNGenStep [optional]
			// CodeRewriterStep
			// CleanStep
			// RegenerateGuidStep [optional]
			// SealerStep
			// OutputStep

			if (context.EnableSerializationDiscovery)
				p.MarkHandlers.Add (new DiscoverSerializationHandler ());

			if (!context.DisableOperatorDiscovery)
				p.MarkHandlers.Add (new DiscoverOperatorsHandler ());

			foreach (string custom_step in custom_steps) {
				if (!AddCustomStep (p, custom_step))
					return -1;
			}

			return 0;
		}

		// Returns the exit code of the process. 0 indicates success.
		// Known non-recoverable errors (LinkerFatalErrorException) set the exit code
		// to the error code.
		// May propagate exceptions, which will result in the process getting an
		// exit code determined by dotnet.
		public int Run (ILogger? customLogger = null)
		{
			int setupStatus = SetupContext (customLogger);
			if (setupStatus > 0)
				return 0;
			if (setupStatus < 0)
				return 1;

			Pipeline p = Context.Pipeline;
			PreProcessPipeline (p);

			try {
				p.Process (Context);
			} catch (Exception e) when (LogFatalError (e)) {
				// Unreachable
				throw;
			}

			Context.FlushCachedWarnings ();
			Context.Tracer.Finish ();
			return Context.ErrorsCount > 0 ? 1 : 0;
		}

		/// <summary>
		/// This method is called in the exception filter for unexpected exceptions.
		/// Prints error messages and returns false to avoid catching in the exception filter.
		/// </summary>
		bool LogFatalError (Exception e)
		{
			switch (e) {
			case LinkerFatalErrorException lex:
				Context.LogMessage (lex.MessageContainer);
				Debug.Assert (lex.MessageContainer.Category == MessageCategory.Error);
				Debug.Assert (lex.MessageContainer.Code != null);
				Debug.Assert (lex.MessageContainer.Code.Value != 0);
				break;
			case ResolutionException re:
				Context.LogError (null, DiagnosticId.FailedToResolveMetadataElement, re.Message);
				break;
			default:
				Context.LogError (null, DiagnosticId.LinkerUnexpectedError);
				break;
			}
			return false;
		}

		partial void PreProcessPipeline (Pipeline pipeline);

		private static IEnumerable<int> ProcessWarningCodes (string value)
		{
			static string Unquote (string arg)
			{
				if (arg.Length > 1 && arg[0] == '"' && arg[arg.Length - 1] == '"')
					return arg.Substring (1, arg.Length - 2);

				return arg;
			}

			value = Unquote (value);
			string[] values = value.Split (s_separators, StringSplitOptions.RemoveEmptyEntries);
			foreach (string v in values) {
				var id = v.Trim ();
				if (!id.StartsWith ("IL", StringComparison.Ordinal) || !ushort.TryParse (id.AsSpan (2), out ushort code))
					continue;

				yield return code;
			}
		}

		Assembly? GetCustomAssembly (string arg)
		{
			if (Path.IsPathRooted (arg)) {
				var assemblyPath = Path.GetFullPath (arg);
				if (File.Exists (assemblyPath)) {
					// The CLR will return the already-loaded assembly if the same path is requested multiple times
					// (or even if a different path specifies the "same" assembly, based on the MVID).

					// Ignore warning, since we're just enabling analyzer for dogfooding
#pragma warning disable IL2026
					return AssemblyLoadContext.Default.LoadFromAssemblyPath (assemblyPath);
#pragma warning restore IL2026
				}
				Context.LogError (null, DiagnosticId.AssemblyInCustomStepOptionCouldNotBeFound, arg);
			} else
				Context.LogError (null, DiagnosticId.AssemblyPathInCustomStepMustBeFullyQualified, arg);

			return null;
		}

		protected virtual void AddResolveFromXmlStep (Pipeline pipeline, string file)
		{
			pipeline.PrependStep (new ResolveFromXmlStep (File.OpenRead (file), file));
		}

		protected virtual void AddLinkAttributesStep (Pipeline pipeline, string file)
		{
			pipeline.AddStepBefore (typeof (MarkStep), new LinkAttributesStep (File.OpenRead (file), file));
		}

		protected virtual void AddBodySubstituterStep (Pipeline pipeline, string file)
		{
			pipeline.AddStepBefore (typeof (MarkStep), new BodySubstituterStep (File.OpenRead (file), file));
		}

		protected virtual void AddXmlDependencyRecorder (LinkContext context, string? fileName)
		{
			context.Tracer.AddRecorder (new XmlDependencyRecorder (context, fileName));
		}

		protected virtual void AddDgmlDependencyRecorder (LinkContext context, string? fileName)
		{
			context.Tracer.AddRecorder (new DgmlDependencyRecorder (context, fileName));
		}

		protected bool AddMarkHandler (Pipeline pipeline, string arg)
		{
			if (!TryGetCustomAssembly (ref arg, out Assembly? custom_assembly))
				return false;

			var step = ResolveStep<IMarkHandler> (arg, custom_assembly);
			if (step == null)
				return false;

			pipeline.AppendMarkHandler (step);
			return true;
		}

		bool TryGetCustomAssembly (ref string arg, [NotNullWhen (true)] out Assembly? assembly)
		{
			assembly = null;
			int pos = arg.IndexOf (",");
			if (pos == -1)
				return false;

			assembly = GetCustomAssembly (arg.Substring (pos + 1));
			if (assembly == null)
				return false;

			arg = arg.Substring (0, pos);
			return true;
		}

		protected bool AddCustomStep (Pipeline pipeline, string arg)
		{
			if (!TryGetCustomAssembly (ref arg, out Assembly? custom_assembly))
				return false;

			string customStepName;
			string? targetName = null;
			bool before = false;
			if (!arg.Contains (':')) {
				customStepName = arg;
			} else {
				string[] parts = arg.Split (':');
				if (parts.Length != 2) {
					Context.LogError (null, DiagnosticId.InvalidArgForCustomStep, arg);
					return false;
				}
				customStepName = parts[1];

				if (!parts[0].StartsWith ("-") && !parts[0].StartsWith ("+")) {
					Context.LogError (null, DiagnosticId.ExpectedSignToControlNewStepInsertion);
					return false;
				}

				before = parts[0][0] == '-';
				targetName = parts[0].Substring (1);
			}

			var stepType = ResolveStepType (customStepName, custom_assembly);
			if (stepType == null)
				return false;

			if (typeof (IStep).IsAssignableFrom (stepType)) {

				var customStep = (IStep?) Activator.CreateInstance (stepType) ?? throw new InvalidOperationException ();
				if (targetName == null) {
					pipeline.AppendStep (customStep);
					return true;
				}

				IStep? target = FindStep (pipeline, targetName);
				if (target == null) {
					Context.LogError (null, DiagnosticId.PipelineStepCouldNotBeFound, targetName);
					return false;
				}

				if (before)
					pipeline.AddStepBefore (target, customStep);
				else
					pipeline.AddStepAfter (target, customStep);

				return true;
			}

			if (typeof (IMarkHandler).IsAssignableFrom (stepType)) {

				var customStep = (IMarkHandler?) Activator.CreateInstance (stepType) ?? throw new InvalidOperationException ();
				if (targetName == null) {
					pipeline.AppendMarkHandler (customStep);
					return true;
				}

				IMarkHandler? target = FindMarkHandler (pipeline, targetName);
				if (target == null) {
					Context.LogError (null, DiagnosticId.PipelineStepCouldNotBeFound, targetName);
					return false;
				}

				if (before)
					pipeline.AddMarkHandlerBefore (target, customStep);
				else
					pipeline.AddMarkHandlerAfter (target, customStep);

				return true;
			}

			Context.LogError (null, DiagnosticId.CustomStepTypeIsIncompatibleWithLinkerVersion, stepType.ToString ());
			return false;
		}

		protected virtual IStep? FindStep (Pipeline pipeline, string name)
		{
			foreach (IStep step in pipeline.GetSteps ()) {
				Type t = step.GetType ();
				if (t.Name == name)
					return step;
			}

			return null;
		}

		static IMarkHandler? FindMarkHandler (Pipeline pipeline, string name)
		{
			foreach (IMarkHandler step in pipeline.MarkHandlers) {
				Type t = step.GetType ();
				if (t.Name == name)
					return step;
			}

			return null;
		}

		Type? ResolveStepType (string type, Assembly assembly)
		{
			// Ignore warning, since we're just enabling analyzer for dogfooding
#pragma warning disable IL2026
			Type? step = assembly != null ? assembly.GetType (type) : Type.GetType (type, false);
#pragma warning restore IL2026

			if (step == null) {
				Context.LogError (null, DiagnosticId.CustomStepTypeCouldNotBeFound, type);
				return null;
			}

			return step;
		}

		TStep? ResolveStep<TStep> (string type, Assembly assembly) where TStep : class
		{
			// Ignore warning, since we're just enabling analyzer for dogfooding
#pragma warning disable IL2026
			Type? step = assembly != null ? assembly.GetType (type) : Type.GetType (type, false);
#pragma warning restore IL2026

			if (step == null) {
				Context.LogError (null, DiagnosticId.CustomStepTypeCouldNotBeFound, type);
				return null;
			}

			if (!typeof (TStep).IsAssignableFrom (step)) {
				Context.LogError (null, DiagnosticId.CustomStepTypeIsIncompatibleWithLinkerVersion, type);
				return null;
			}

			return (TStep?) Activator.CreateInstance (step);
		}

		static string[] GetFiles (string param)
		{
			if (param.Length < 1 || param[0] != '@')
				return new string[] { param };

			string file = param.Substring (1);
			return ReadLines (file);
		}

		static string[] ReadLines (string file)
		{
			var lines = new List<string> ();
			using (StreamReader reader = new StreamReader (file)) {
				string? line;
				while ((line = reader.ReadLine ()) != null)
					lines.Add (line);
			}
			return lines.ToArray ();
		}

		AssemblyAction? ParseAssemblyAction (string s)
		{
			switch (s.ToLowerInvariant ()) {
			case "copy":
				return AssemblyAction.Copy;
			case "copyused":
				return AssemblyAction.CopyUsed;
			case "link":
				return AssemblyAction.Link;
			case "skip":
				return AssemblyAction.Skip;

			case "addbypassngen":
				_needAddBypassNGenStep = true;
				return AssemblyAction.AddBypassNGen;
			case "addbypassngenused":
				_needAddBypassNGenStep = true;
				return AssemblyAction.AddBypassNGenUsed;
			}

			Context.LogError (null, DiagnosticId.InvalidAssemblyAction, s);
			return null;
		}

		AssemblyRootMode? ParseAssemblyRootMode (string s)
		{
			switch (s.ToLowerInvariant ()) {
			case "all":
				return AssemblyRootMode.AllMembers;
			case "visible":
				return AssemblyRootMode.VisibleMembers;
			case "entrypoint":
				return AssemblyRootMode.EntryPoint;
			case "library":
				return AssemblyRootMode.Library;
			}

			Context.LogError (null, DiagnosticId.InvalidAssemblyRootMode, s);
			return null;
		}

		bool GetWarnVersion (string text, out WarnVersion version)
		{
			if (int.TryParse (text, out int versionNum)) {
				version = (WarnVersion) versionNum;
				if (version >= WarnVersion.ILLink0 && version <= WarnVersion.Latest)
					return true;
			}

			Context.LogError (null, DiagnosticId.InvalidWarningVersion, text);
			version = 0;
			return false;
		}

		protected bool GetOptimizationName (string text, out CodeOptimizations optimization)
		{
			switch (text.ToLowerInvariant ()) {
			case "beforefieldinit":
				optimization = CodeOptimizations.BeforeFieldInit;
				return true;
			case "overrideremoval":
				optimization = CodeOptimizations.OverrideRemoval;
				return true;
			case "unreachablebodies":
				optimization = CodeOptimizations.UnreachableBodies;
				return true;
			case "unusedinterfaces":
				optimization = CodeOptimizations.UnusedInterfaces;
				return true;
			case "unusedtypechecks":
				optimization = CodeOptimizations.UnusedTypeChecks;
				return true;
			case "ipconstprop":
				optimization = CodeOptimizations.IPConstantPropagation;
				return true;
			case "sealer":
				optimization = CodeOptimizations.Sealer;
				return true;
			}

			Context.LogError (null, DiagnosticId.InvalidOptimizationValue, text);
			optimization = 0;
			return false;
		}

		bool TryGetMetadataTrimming (string text, out MetadataTrimming metadataTrimming)
		{
			switch (text.ToLowerInvariant ()) {
			case "all":
				metadataTrimming = MetadataTrimming.Any;
				return true;
			case "none":
				metadataTrimming = MetadataTrimming.None;
				return true;
			case "parametername":
				metadataTrimming = MetadataTrimming.ParameterName;
				return true;
			}

			Context.LogError (null, DiagnosticId.InvalidMetadataOption, text);
			metadataTrimming = 0;
			return false;
		}

		protected static bool GetWarningSuppressionWriterFileOutputKind (string text, out WarningSuppressionWriter.FileOutputKind fileOutputKind)
		{
			switch (text.ToLowerInvariant ()) {
			case "cs":
				fileOutputKind = WarningSuppressionWriter.FileOutputKind.CSharp;
				return true;

			case "xml":
				fileOutputKind = WarningSuppressionWriter.FileOutputKind.Xml;
				return true;

			default:
				fileOutputKind = WarningSuppressionWriter.FileOutputKind.CSharp;
				return false;
			}
		}

		bool GetBoolParam (string token, Action<bool> action)
		{
			if (arguments.Count == 0) {
				action (true);
				return true;
			}

			var arg = arguments.Peek ();
			if (bool.TryParse (arg.ToLowerInvariant (), out bool value)) {
				arguments.Dequeue ();
				action (value);
				return true;
			}

			if (arg.StartsWith ("-") || arg.StartsWith ("/")) {
				action (true);
				return true;
			}

			Context.LogError (null, DiagnosticId.InvalidArgumentForTokenOption, token);
			return false;
		}

		bool GetStringParam (string token, [NotNullWhen (true)] out string? value)
		{
			value = null;
			if (arguments.Count < 1) {
				ErrorMissingArgument (token);
				return false;
			}

			var arg = arguments.Dequeue ();
			if (!string.IsNullOrEmpty (arg)) {
				value = arg;
				return true;
			}

			ErrorMissingArgument (token);
			return false;
		}

		string? GetNextStringValue ()
		{
			if (arguments.Count < 1)
				return null;

			var arg = arguments.Peek ();
			if (arg.StartsWith ("-") || arg.StartsWith ("/"))
				return null;

			arguments.Dequeue ();
			return arg;
		}

		protected virtual LinkContext GetDefaultContext (Pipeline pipeline, ILogger? logger)
		{
			return new LinkContext (pipeline, logger ?? new ConsoleLogger (), "output") {
				TrimAction = AssemblyAction.Link,
				DefaultAction = AssemblyAction.Link,
			};
		}

		protected virtual List<BaseStep> CreateDefaultResolvers ()
		{
			return new List<BaseStep> ();
		}

		static bool IsValidAssemblyName (string value)
		{
			return !string.IsNullOrEmpty (value);
		}

		static void Usage ()
		{
			Console.WriteLine (_linker);

			Console.WriteLine ($"illink [options] {resolvers}");
			Console.WriteLine ("  -a FILE [MODE]      Assembly file used as root assembly with optional MODE value to alter default root mode");
			Console.WriteLine ("                      Mode can be one of the following values");
			Console.WriteLine ("                        all: Keep all members in root assembly");
			Console.WriteLine ("                        default: Use entry point for applications and all members for libraries");
			Console.WriteLine ("                        entrypoint: Use assembly entry point as only root in the assembly");
			Console.WriteLine ("                        library: All assembly members and data needed for secondary trimming are retained");
			Console.WriteLine ("                        visible: Keep all members and types visible outside of the assembly");
			Console.WriteLine ("  -x FILE             XML descriptor file with members to be kept");

			Console.WriteLine ();
			Console.WriteLine ("Options");
			Console.WriteLine ("  -d PATH             Specify additional directory to search in for assembly references");
			Console.WriteLine ("  -reference FILE     Specify additional file location used to resolve assembly references");
			Console.WriteLine ("  -b                  Update debug symbols for all modified files. Defaults to false");
			Console.WriteLine ("  -out PATH           Specify the output directory. Defaults to 'output'");
			Console.WriteLine ("  -h                  Lists all {0} options", _linker);
			Console.WriteLine ("  @FILE               Read response file for more options");

			Console.WriteLine ();
			Console.WriteLine ("Actions");
			Console.WriteLine ("  --trim-mode ACTION  Sets action for assemblies annotated with IsTrimmable attribute. Defaults to 'link'");
			Console.WriteLine ("                          copy: Analyze whole assembly and save it to the output");
			Console.WriteLine ("                          copyused: Same as copy but only for assemblies which are needed");
			Console.WriteLine ("                          link: Remove any unused IL or metadata and optimizes the assembly");
			Console.WriteLine ("                          skip: Do not process the assembly");
			Console.WriteLine ("                          addbypassngen: Add BypassNGenAttribute to unused methods");
			Console.WriteLine ("                          addbypassngenused: Same as addbypassngen but unused assemblies are removed");
			Console.WriteLine ("  --action ACTION       Sets action for assemblies that have no IsTrimmable attribute. Defaults to 'link'");
			Console.WriteLine ("  --action ACTION ASM   Overrides the default action for specific assembly name");

			Console.WriteLine ();
			Console.WriteLine ("Advanced Options");
			Console.WriteLine ("  --about                   About the {0}", _linker);
			Console.WriteLine ("  --custom-step CFG         Add a custom step <config> to the existing pipeline");
			Console.WriteLine ("                            Step can use one of following configurations");
			Console.WriteLine ("                            TYPE,PATH_TO_ASSEMBLY: Add user defined type as last step to the pipeline");
			Console.WriteLine ("                            -NAME:TYPE,PATH_TO_ASSEMBLY: Inserts step type before existing step with name");
			Console.WriteLine ("                            +NAME:TYPE,PATH_TO_ASSEMBLY: Add step type after existing step");
			Console.WriteLine ("  --custom-data KEY=VALUE   Populates context data set with user specified key-value pair");
			Console.WriteLine ("  --deterministic           Produce a deterministic output for modified assemblies");
			Console.WriteLine ("  --ignore-descriptors      Skips reading embedded descriptors (short -z). Defaults to false");
			Console.WriteLine ("  --skip-unresolved         Ignore unresolved types, methods, and assemblies. Defaults to true");
			Console.WriteLine ("  --output-pinvokes PATH    Output a JSON file with all modules and entry points of the P/Invokes found");
			Console.WriteLine ("  --verbose                 Log messages indicating progress and warnings");
			Console.WriteLine ("  --nowarn WARN             Disable specific warning messages");
			Console.WriteLine ("  --warn VERSION            Only print out warnings with version <= VERSION. Defaults to '9999'");
			Console.WriteLine ("                              VERSION is an integer in the range 0-9999.");
			Console.WriteLine ("  --warnaserror[+|-]        Report all warnings as errors");
			Console.WriteLine ("  --warnaserror[+|-] WARN   Report specific warnings as errors");
			Console.WriteLine ("  --singlewarn[+|-]         Show at most one analysis warning per assembly");
			Console.WriteLine ("  --singlewarn[+|-] ASM     Show at most one analysis warning for a specific assembly");
			Console.WriteLine ("  --version                 Print the version number of the {0}", _linker);

			Console.WriteLine ();
			Console.WriteLine ("Trimming");
			Console.WriteLine ("  --disable-opt NAME [ASM]   Disable one of the default optimizations globaly or for a specific assembly name");
			Console.WriteLine ("                               beforefieldinit: Unused static fields are removed if there is no static ctor");
			Console.WriteLine ("                               ipconstprop: Interprocedural constant propagation on return values");
			Console.WriteLine ("                               overrideremoval: Overrides of virtual methods on types that are never instantiated are removed");
			Console.WriteLine ("                               unreachablebodies: Instance methods that are marked but not executed are converted to throws");
			Console.WriteLine ("                               unusedinterfaces: Removes interface types from declaration when not used");
			Console.WriteLine ("                               unusedtypechecks: Inlines never successful type checks");
			Console.WriteLine ("  --enable-opt NAME [ASM]    Enable one of the additional optimizations globaly or for a specific assembly name");
			Console.WriteLine ("                               sealer: Any method or type which does not have override is marked as sealed");
			Console.WriteLine ("  --explicit-reflection      Adds to members never used through reflection DisablePrivateReflection attribute. Defaults to false");
			Console.WriteLine ("  --feature FEATURE VALUE    Apply any optimizations defined when this feature setting is a constant known at link time");
			Console.WriteLine ("  --keep-compilers-resources Keep assembly resources used for F# compilation resources. Defaults to false");
			Console.WriteLine ("  --keep-dep-attributes      Keep attributes used for manual dependency tracking. Defaults to false");
			Console.WriteLine ("  --keep-metadata NAME       Keep metadata which would otherwise be removed if not used");
			Console.WriteLine ("                               all: Metadata for any member are all kept");
			Console.WriteLine ("                               parametername: All parameter names are kept");
			Console.WriteLine ("  --new-mvid                 Generate a new guid for each linked assembly (short -g). Defaults to true");
			Console.WriteLine ("  --strip-descriptors        Remove XML descriptor resources for linked assemblies. Defaults to true");
			Console.WriteLine ("  --strip-security           Remove metadata and code related to Code Access Security. Defaults to true");
			Console.WriteLine ("  --substitutions FILE       Configuration file with field or methods substitution rules");
			Console.WriteLine ("  --ignore-substitutions     Skips reading embedded substitutions. Defaults to false");
			Console.WriteLine ("  --strip-substitutions      Remove XML substitution resources for linked assemblies. Defaults to true");
			Console.WriteLine ("  --used-attrs-only          Attribute usage is removed if the attribute type is not used. Defaults to false");
			Console.WriteLine ("  --link-attributes FILE     Supplementary custom attribute definitions for attributes controlling the trimming behavior.");
			Console.WriteLine ("  --ignore-link-attributes   Skips reading embedded attributes. Defaults to false");
			Console.WriteLine ("  --strip-link-attributes    Remove XML link attributes resources for linked assemblies. Defaults to true");

			Console.WriteLine ();
			Console.WriteLine ("Analyzer");
			Console.WriteLine ("  --dependencies-file FILE              Specify the dependencies output. Defaults to 'output/linker-dependencies.xml'");
			Console.WriteLine ("                                        if 'xml' is file format, 'output/linker-dependencies.dgml if 'dgml' is file format");
			Console.WriteLine ("  --dump-dependencies                   Dump dependencies for the ILLink analyzer tool");
			Console.WriteLine ("  --dependencies-file-format FORMAT     Specify output file type. Defaults to 'xml'");
			Console.WriteLine ("                                          xml: outputs an .xml file");
			Console.WriteLine ("                                          dgml: outputs a .dgml file");
			Console.WriteLine ("  --reduced-tracing                     Reduces dependency output related to assemblies that will not be modified");
			Console.WriteLine ("");
		}

		static void Version ()
		{
			Console.WriteLine ("{0} Version {1}",
				_linker,
				System.Reflection.Assembly.GetExecutingAssembly ().GetName ().Version);
		}

		static void About ()
		{
			Console.WriteLine ("For more information, visit the project Web site");
			Console.WriteLine ("   https://github.com/dotnet/runtime/tree/main/src/tools/illink");
		}

		static Pipeline GetStandardPipeline ()
		{
			Pipeline p = new Pipeline ();
			p.AppendStep (new ProcessReferencesStep ());
			p.AppendStep (new MarkStep ());
			p.AppendStep (new RemoveResourcesStep ());
			p.AppendStep (new ValidateVirtualMethodAnnotationsStep ());
			p.AppendStep (new ProcessWarningsStep ());
			p.AppendStep (new OutputWarningSuppressions ());
			p.AppendStep (new SweepStep ());
			p.AppendStep (new CheckSuppressionsDispatcher ());
			p.AppendStep (new CodeRewriterStep ());
			p.AppendStep (new CleanStep ());
			p.AppendStep (new RegenerateGuidStep ());
			p.AppendStep (new OutputStep ());
			return p;
		}

		public void Dispose ()
		{
			context?.Dispose ();
		}
	}
}
