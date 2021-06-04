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
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Xml.XPath;
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
			if (args.Length == 0) {
				Console.Error.WriteLine ("No parameters specified");
				return 1;
			}

			if (!ProcessResponseFile (args, out var arguments))
				return 1;

			try {
				using (Driver driver = new Driver (arguments)) {
					return driver.Run ();
				}
			} catch {
				Console.Error.WriteLine ("Fatal error in {0}", _linker);
				throw;
			}
		}

		readonly Queue<string> arguments;
		bool _needAddBypassNGenStep;
		protected LinkContext context;

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
					} catch (Exception e) {
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
						argBuilder.Append (new String ('\\', numBackslash));
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
			context.LogError ($"Missing argument for '{optionName}' option", 1018);
		}

		// Perform setup of the LinkContext and parse the arguments.
		// Return values:
		// 0 => successfully set up context with all arguments
		// 1 => argument processing stopped early without errors
		// -1 => error setting up context
		protected int SetupContext (ILogger customLogger = null)
		{
			Pipeline p = GetStandardPipeline ();
			context = GetDefaultContext (p, customLogger);

			var body_substituter_steps = new Stack<string> ();
			var xml_custom_attribute_steps = new Stack<string> ();
			var custom_steps = new List<string> ();
			var set_optimizations = new List<(CodeOptimizations, string, bool)> ();
			bool dumpDependencies = false;
			string dependenciesFileName = null;
			context.StripSecurity = true;
			bool new_mvid_used = false;
			bool deterministic_used = false;
			bool keepCompilersResources = false;
			MetadataTrimming metadataTrimming = MetadataTrimming.Any;

			List<BaseStep> inputs = CreateDefaultResolvers ();

			while (arguments.Count > 0) {
				string token = arguments.Dequeue ();
				if (token.Length < 2) {
					context.LogError ($"Unrecognized command-line option: '{token}'", 1015);
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
						if (!GetStringParam (token, l => dependenciesFileName = l))
							return -1;

						continue;

					case "--dump-dependencies":
						dumpDependencies = true;
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

						if (!GetStringParam (token, l => body_substituter_steps.Push (l)))
							return -1;

						continue;
					case "--explicit-reflection":
						if (!GetBoolParam (token, l => context.AddReflectionAnnotations = l))
							return -1;

						continue;

					case "--action": {
							AssemblyAction? action = null;
							if (!GetStringParam (token, l => action = ParseAssemblyAction (l)))
								return -1;

							if (action == null)
								return -1;

							string assemblyName = GetNextStringValue ();
							if (assemblyName == null) {
								context.DefaultAction = action.Value;
								continue;
							}

							if (!IsValidAssemblyName (assemblyName)) {
								context.LogError ($"Invalid assembly name '{assemblyName}'", 1036);
								return -1;
							}

							context.RegisterAssemblyAction (assemblyName, action.Value);
							continue;
						}
					case "--trim-mode": {
							AssemblyAction? action = null;
							if (!GetStringParam (token, l => action = ParseAssemblyAction (l)))
								return -1;

							if (action == null)
								return -1;

							context.TrimAction = action.Value;
							continue;
						}
					case "--custom-step":
						if (!GetStringParam (token, l => custom_steps.Add (l)))
							return -1;

						continue;

					case "--custom-data":
						if (arguments.Count < 1) {
							ErrorMissingArgument (token);
							return -1;
						}

						var arg = arguments.Dequeue ();
						string[] values = arg.Split ('=');
						if (values?.Length != 2) {
							context.LogError ($"Value used with '--custom-data' has to be in the KEY=VALUE format", 1019);
							return -1;
						}

						context.SetCustomData (values[0], values[1]);
						continue;

					case "--keep-compilers-resources":
						if (!GetBoolParam (token, l => keepCompilersResources = l))
							return -1;

						continue;

					case "--keep-facades":
						if (!GetBoolParam (token, l => context.KeepTypeForwarderOnlyAssemblies = l))
							return -1;

						continue;

					case "--keep-dep-attributes":
						if (!GetBoolParam (token, l => set_optimizations.Add ((CodeOptimizations.RemoveDynamicDependencyAttribute, null, !l))))
							return -1;

						continue;

					case "--keep-metadata": {
							string mname = null;
							if (!GetStringParam (token, l => mname = l))
								return -1;

							if (!TryGetMetadataTrimming (mname, out var type))
								return -1;

							metadataTrimming &= ~type;
							continue;
						}

					case "--disable-serialization-discovery":
						if (!GetBoolParam (token, l => context.DisableSerializationDiscovery = l))
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
							string optName = null;
							if (!GetStringParam (token, l => optName = l))
								return -1;

							if (!GetOptimizationName (optName, out var opt))
								return -1;

							string assemblyName = GetNextStringValue ();
							set_optimizations.Add ((opt, assemblyName, false));

							continue;
						}
					case "--enable-opt": {
							string optName = null;
							if (!GetStringParam (token, l => optName = l))
								return -1;

							if (!GetOptimizationName (optName, out var opt))
								return -1;

							string assemblyName = GetNextStringValue ();
							set_optimizations.Add ((opt, assemblyName, true));

							continue;
						}

					case "--feature": {
							string featureName = null;
							if (!GetStringParam (token, l => featureName = l))
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
						if (!GetStringParam (token, l => context.AssemblyListFile = l))
							return -1;

						continue;

					case "--output-pinvokes":
						if (!GetStringParam (token, l => context.PInvokesListFile = l))
							return -1;

						continue;

					case "--link-attributes":
						if (arguments.Count < 1) {
							ErrorMissingArgument (token);
							return -1;
						}

						if (!GetStringParam (token, l => {
							foreach (string file in GetFiles (l))
								xml_custom_attribute_steps.Push (file);
						}))
							return -1;

						continue;

					case "--generate-warning-suppressions":
						string generateWarningSuppressionsArgument = string.Empty;
						if (!GetStringParam (token, l => generateWarningSuppressionsArgument = l))
							return -1;

						if (!GetWarningSuppressionWriterFileOutputKind (generateWarningSuppressionsArgument, out var fileOutputKind)) {
							context.LogError ($"Invalid value '{generateWarningSuppressionsArgument}' for '--generate-warning-suppressions' option", 1017);
							return -1;
						}

						context.WarningSuppressionWriter = new WarningSuppressionWriter (fileOutputKind);
						continue;

					case "--nowarn":
						string noWarnArgument = null;
						if (!GetStringParam (token, l => noWarnArgument = l))
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
						string warnVersionArgument = null;
						if (!GetStringParam (token, l => warnVersionArgument = l))
							return -1;

						if (!GetWarnVersion (warnVersionArgument, out WarnVersion version))
							return -1;

						context.WarnVersion = version;

						continue;

					case "--singlewarn":
					case "--singlewarn+": {
							string assemblyName = GetNextStringValue ();
							if (assemblyName != null) {
								if (!IsValidAssemblyName (assemblyName)) {
									context.LogError ($"Invalid assembly name '{assemblyName}'", 1036);
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
							string assemblyName = GetNextStringValue ();
							if (assemblyName != null) {
								if (!IsValidAssemblyName (assemblyName)) {
									context.LogError ($"Invalid assembly name '{assemblyName}'", 1036);
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
						if (!GetStringParam (token, l => {
							DirectoryInfo info = new DirectoryInfo (l);
							context.Resolver.AddSearchDirectory (info.FullName);
						}))
							return -1;

						continue;
					case "o":
					case "out":
						if (!GetStringParam (token, l => context.OutputDirectory = l))
							return -1;

						continue;
					case "t":
						context.KeepTypeForwarderOnlyAssemblies = true;
						continue;
					case "x": {
							string xmlFile = null;
							if (!GetStringParam (token, l => xmlFile = l))
								return -1;

							if (!File.Exists (xmlFile)) {
								context.LogError ($"XML descriptor file '{xmlFile}' could not be found'", 1033);
								return -1;
							}

							inputs.Add (new ResolveFromXmlStep (File.OpenRead (xmlFile), xmlFile));
							continue;
						}
					case "a": {
							string assemblyFile = null;
							if (!GetStringParam (token, l => assemblyFile = l))
								return -1;

							if (!File.Exists (assemblyFile) && assemblyFile.EndsWith (".dll", StringComparison.InvariantCultureIgnoreCase)) {
								context.LogError ($"Root assembly '{assemblyFile}' could not be found", 1032);
								return -1;
							}

							AssemblyRootMode rmode = AssemblyRootMode.Default;
							var rootMode = GetNextStringValue ();
							if (rootMode != null) {
								var parsed_rmode = ParseAssemblyRootsMode (rootMode);
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
						if (!GetStringParam (token, l => context.Resolver.AddReferenceAssembly (l)))
							return -1;

						continue;
					}
				}

				context.LogError ($"Unrecognized command-line option: '{token}'", 1015);
				return -1;
			}

			if (inputs.Count == 0) {
				context.LogError ($"No input files were specified. Use one of '{resolvers}' options", 1020);
				return -1;
			}

			if (new_mvid_used && deterministic_used) {
				context.LogError ($"Options '--new-mvid' and '--deterministic' cannot be used at the same time", 1021);
				return -1;
			}

			context.MetadataTrimming = metadataTrimming;

			// Default to deterministic output
			if (!new_mvid_used && !deterministic_used) {
				context.DeterministicOutput = true;
			}
			if (dumpDependencies)
				AddXmlDependencyRecorder (context, dependenciesFileName);

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

			if (!context.DisableSerializationDiscovery)
				p.MarkHandlers.Add (new DiscoverSerializationHandler ());

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
		public int Run (ILogger customLogger = null)
		{
			int setupStatus = SetupContext (customLogger);
			if (setupStatus > 0)
				return 0;
			if (setupStatus < 0)
				return 1;

			Pipeline p = context.Pipeline;
			PreProcessPipeline (p);

			try {
				p.Process (context);
			} catch (LinkerFatalErrorException lex) {
				context.LogMessage (lex.MessageContainer);
				Console.Error.WriteLine (lex.ToString ());
				Debug.Assert (lex.MessageContainer.Category == MessageCategory.Error);
				Debug.Assert (lex.MessageContainer.Code != null);
				Debug.Assert (lex.MessageContainer.Code.Value != 0);
				return lex.MessageContainer.Code ?? 1;
			} catch (ResolutionException e) {
				context.LogError ($"{e.Message}", 1040);
			} catch (Exception) {
				// Unhandled exceptions are usually linker bugs. Ask the user to report it.
				context.LogError ($"IL Linker has encountered an unexpected error. Please report the issue at https://github.com/mono/linker/issues", 1012);
				// Don't swallow the exception and exit code - rethrow it and let the surrounding tooling decide what to do.
				// The stack trace will go to stderr, and the MSBuild task will surface it with High importance.
				throw;
			} finally {
				context.FlushCachedWarnings ();
				context.Tracer.Finish ();
			}

			return context.ErrorsCount > 0 ? 1 : 0;
		}

		partial void PreProcessPipeline (Pipeline pipeline);

		private static IEnumerable<int> ProcessWarningCodes (string value)
		{
			string Unquote (string arg)
			{
				if (arg.Length > 1 && arg[0] == '"' && arg[arg.Length - 1] == '"')
					return arg.Substring (1, arg.Length - 2);

				return arg;
			}

			value = Unquote (value);
			string[] values = value.Split (new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string v in values) {
				var id = v.Trim ();
				if (!id.StartsWith ("IL", StringComparison.Ordinal) || !ushort.TryParse (id.Substring (2), out ushort code))
					continue;

				yield return code;
			}
		}

		Assembly GetCustomAssembly (string arg)
		{
			if (Path.IsPathRooted (arg)) {
				var assemblyPath = Path.GetFullPath (arg);
				if (File.Exists (assemblyPath)) {
					// The CLR will return the already-loaded assembly if the same path is requested multiple times
					// (or even if a different path specifies the "same" assembly, based on the MVID).
					return AssemblyLoadContext.Default.LoadFromAssemblyPath (assemblyPath);
				}
				context.LogError ($"The assembly '{arg}' specified for '--custom-step' option could not be found", 1022);
			} else
				context.LogError ($"The path to the assembly '{arg}' specified for '--custom-step' must be fully qualified", 1023);

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

		static void AddBodySubstituterStep (Pipeline pipeline, string file)
		{
			pipeline.AddStepBefore (typeof (MarkStep), new BodySubstituterStep (File.OpenRead (file), file));
		}

		protected virtual void AddXmlDependencyRecorder (LinkContext context, string fileName)
		{
			context.Tracer.AddRecorder (new XmlDependencyRecorder (context, fileName));
		}

		protected bool AddMarkHandler (Pipeline pipeline, string arg)
		{
			if (!TryGetCustomAssembly (ref arg, out Assembly custom_assembly))
				return false;

			var step = ResolveStep<IMarkHandler> (arg, custom_assembly);
			if (step == null)
				return false;

			pipeline.AppendMarkHandler (step);
			return true;
		}

		bool TryGetCustomAssembly (ref string arg, out Assembly assembly)
		{
			assembly = null;
			int pos = arg.IndexOf (",");
			if (pos == -1)
				return true;

			assembly = GetCustomAssembly (arg.Substring (pos + 1));
			if (assembly == null)
				return false;

			arg = arg.Substring (0, pos);
			return true;
		}

		protected bool AddCustomStep (Pipeline pipeline, string arg)
		{
			if (!TryGetCustomAssembly (ref arg, out Assembly custom_assembly))
				return false;

			string customStepName;
			string targetName = null;
			bool before = false;
			if (!arg.Contains (":")) {
				customStepName = arg;
			} else {
				string[] parts = arg.Split (':');
				if (parts.Length != 2) {
					context.LogError ($"Invalid value '{arg}' specified for '--custom-step' option", 1024);
					return false;
				}
				customStepName = parts[1];

				if (!parts[0].StartsWith ("-") && !parts[0].StartsWith ("+")) {
					context.LogError ($"Expected '+' or '-' to control new step insertion", 1025);
					return false;
				}

				before = parts[0][0] == '-';
				targetName = parts[0].Substring (1);
			}

			var stepType = ResolveStepType (customStepName, custom_assembly);
			if (stepType == null)
				return false;

			if (typeof (IStep).IsAssignableFrom (stepType)) {

				var customStep = (IStep) Activator.CreateInstance (stepType);
				if (targetName == null) {
					pipeline.AppendStep (customStep);
					return true;
				}

				IStep target = FindStep (pipeline, targetName);
				if (target == null) {
					context.LogError ($"Pipeline step '{targetName}' could not be found", 1026);
					return false;
				}

				if (before)
					pipeline.AddStepBefore (target, customStep);
				else
					pipeline.AddStepAfter (target, customStep);

				return true;
			}

			if (typeof (IMarkHandler).IsAssignableFrom (stepType)) {

				var customStep = (IMarkHandler) Activator.CreateInstance (stepType);
				if (targetName == null) {
					pipeline.AppendMarkHandler (customStep);
					return true;
				}

				IMarkHandler target = FindMarkHandler (pipeline, targetName);
				if (target == null) {
					context.LogError ($"Pipeline step '{targetName}' could not be found", 1026);
					return false;
				}

				if (before)
					pipeline.AddMarkHandlerBefore (target, customStep);
				else
					pipeline.AddMarkHandlerAfter (target, customStep);

				return true;
			}

			context.LogError ($"Custom step '{stepType}' is incompatible with this linker version", 1028);
			return false;
		}

		static IStep FindStep (Pipeline pipeline, string name)
		{
			foreach (IStep step in pipeline.GetSteps ()) {
				Type t = step.GetType ();
				if (t.Name == name)
					return step;
			}

			return null;
		}

		static IMarkHandler FindMarkHandler (Pipeline pipeline, string name)
		{
			foreach (IMarkHandler step in pipeline.MarkHandlers) {
				Type t = step.GetType ();
				if (t.Name == name)
					return step;
			}

			return null;
		}

		Type ResolveStepType (string type, Assembly assembly)
		{
			Type step = assembly != null ? assembly.GetType (type) : Type.GetType (type, false);

			if (step == null) {
				context.LogError ($"Custom step '{type}' could not be found", 1027);
				return null;
			}

			return step;
		}

		TStep ResolveStep<TStep> (string type, Assembly assembly) where TStep : class
		{
			Type step = assembly != null ? assembly.GetType (type) : Type.GetType (type, false);

			if (step == null) {
				context.LogError ($"Custom step '{type}' could not be found", 1027);
				return null;
			}

			if (!typeof (TStep).IsAssignableFrom (step)) {
				context.LogError ($"Custom step '{type}' is incompatible with this linker version", 1028);
				return null;
			}

			return (TStep) Activator.CreateInstance (step);
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
				string line;
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

			context.LogError ($"Invalid assembly action '{s}'", 1031);
			return null;
		}

		AssemblyRootMode? ParseAssemblyRootsMode (string s)
		{
			switch (s.ToLowerInvariant ()) {
			case "default":
				return AssemblyRootMode.Default;
			case "all":
				return AssemblyRootMode.AllMembers;
			case "visible":
				return AssemblyRootMode.VisibleMembers;
			case "entrypoint":
				return AssemblyRootMode.EntryPoint;
			case "library":
				return AssemblyRootMode.Library;
			}

			context.LogError ($"Invalid assembly root mode '{s}'", 1037);
			return null;
		}

		bool GetWarnVersion (string text, out WarnVersion version)
		{
			if (int.TryParse (text, out int versionNum)) {
				version = (WarnVersion) versionNum;
				if (version >= WarnVersion.ILLink0 && version <= WarnVersion.Latest)
					return true;
			}

			context.LogError ($"Invalid warning version '{text}'", 1016);
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

			context.LogError ($"Invalid optimization value '{text}'", 1029);
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

			context.LogError ($"Invalid metadata value '{text}'", 1046);
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

			context.LogError ($"Invalid argument for '{token}' option", 1030);
			return false;
		}

		bool GetStringParam (string token, Action<string> action)
		{
			if (arguments.Count < 1) {
				ErrorMissingArgument (token);
				return false;
			}

			var arg = arguments.Dequeue ();
			if (!string.IsNullOrEmpty (arg)) {
				action (arg);
				return true;
			}

			ErrorMissingArgument (token);
			return false;
		}

		string GetNextStringValue ()
		{
			if (arguments.Count < 1)
				return null;

			var arg = arguments.Peek ();
			if (arg.StartsWith ("-") || arg.StartsWith ("/"))
				return null;

			arguments.Dequeue ();
			return arg;
		}

		protected virtual LinkContext GetDefaultContext (Pipeline pipeline, ILogger logger)
		{
			return new LinkContext (pipeline, logger ?? new ConsoleLogger ()) {
				TrimAction = AssemblyAction.Link,
				DefaultAction = AssemblyAction.Link,
				OutputDirectory = "output",
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
			Console.WriteLine ("  --keep-facades            Keep assemblies with type-forwarders (short -t). Defaults to false");
			Console.WriteLine ("  --skip-unresolved         Ignore unresolved types, methods, and assemblies. Defaults to false");
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
			Console.WriteLine ("Linking");
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
			Console.WriteLine ("  --link-attributes FILE     Supplementary custom attribute definitions for attributes controlling the linker behavior.");
			Console.WriteLine ("  --ignore-link-attributes   Skips reading embedded attributes. Defaults to false");
			Console.WriteLine ("  --strip-link-attributes    Remove XML link attributes resources for linked assemblies. Defaults to true");

			Console.WriteLine ();
			Console.WriteLine ("Analyzer");
			Console.WriteLine ("  --dependencies-file FILE   Specify the dependencies output. Defaults to 'output/linker-dependencies.xml.gz'");
			Console.WriteLine ("  --dump-dependencies        Dump dependencies for the linker analyzer tool");
			Console.WriteLine ("  --reduced-tracing          Reduces dependency output related to assemblies that will not be modified");
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
			Console.WriteLine ("   https://github.com/mono/linker");
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
			p.AppendStep (new CodeRewriterStep ());
			p.AppendStep (new CleanStep ());
			p.AppendStep (new RegenerateGuidStep ());
			p.AppendStep (new OutputStep ());
			return p;
		}

		public void Dispose ()
		{
			if (context != null)
				context.Dispose ();
		}
	}
}
