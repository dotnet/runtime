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
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.XPath;

using Mono.Linker.Steps;

namespace Mono.Linker {

	public partial class Driver {

#if FEATURE_ILLINK
		const string resolvers = "-a|-r|-x";
		static readonly string _linker = "IL Linker";
#else
		const string resolvers = "-a|-i|-r|-x";
		static readonly string _linker = "Mono IL Linker";
#endif

		public static int Main (string [] args)
		{
			return Execute (args);
		}

		public static int Execute (string[] args, ILogger customLogger = null)
		{
			if (args.Length == 0) {
				Console.Error.WriteLine ("No parameters specified");
				return 1;
			}

			if (!ProcessResponseFile (args, out var arguments))
				return 1;

			try {

				Driver driver = new Driver (arguments);
				if (!driver.Run (customLogger))
					return 1;

			} catch {
				Console.Error.WriteLine ("Fatal error in {0}", _linker);
				throw;
			}

			return 0;
		}

		readonly Queue<string> arguments;
		bool _needAddBypassNGenStep;

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
						IEnumerable<string> responseFileLines = File.ReadLines (responseFileName);
						ParseResponseFileLines (responseFileLines, result);
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

		public static void ParseResponseFileLines (IEnumerable<string> responseFileLines, Queue<string> result)
		{
			foreach (var rawResponseFileText in responseFileLines) {
				var responseFileText = rawResponseFileText.Trim ();
				int idx = 0;
				while (idx < responseFileText.Length) {
					while (idx < responseFileText.Length && char.IsWhiteSpace (responseFileText [idx])) {
						idx++;
					}
					if (idx == responseFileText.Length) {
						break;
					}
					StringBuilder argBuilder = new StringBuilder ();
					bool inquote = false;
					while (true) {
						bool copyChar = true;
						int numBackslash = 0;
						while (idx < responseFileText.Length && responseFileText [idx] == '\\') {
							numBackslash++;
							idx++;
						}
						if (idx < responseFileText.Length && responseFileText [idx] == '"') {
							if ( (numBackslash % 2) == 0) {
								if (inquote && (idx + 1) < responseFileText.Length && responseFileText [idx + 1] == '"') {
									idx++;
								} else {
									copyChar = false;
									inquote = !inquote;
								}
							}
							numBackslash /= 2;
						}
						argBuilder.Append (new String ('\\', numBackslash));
						if (idx == responseFileText.Length || (!inquote && Char.IsWhiteSpace (responseFileText [idx]))) {
							break;
						}
						if (copyChar) {
							argBuilder.Append (responseFileText [idx]);
						}
						idx++;
					}
					result.Enqueue (argBuilder.ToString ());
				}
			}
		}

		static void ErrorUnrecognizedOption (string optionName)
		{
			Console.WriteLine ($"Unrecognized command-line option: '{optionName}'");
		}

		static void ErrorMissingArgument (string optionName)
		{
			Console.WriteLine ($"Missing argument for '{optionName}' option");
		}

		public bool Run (ILogger customLogger = null)
		{
			Pipeline p = GetStandardPipeline ();
			using (LinkContext context = GetDefaultContext (p)) {
				if (customLogger != null)
					context.Logger = customLogger;

#if !FEATURE_ILLINK
				I18nAssemblies assemblies = I18nAssemblies.All;
#endif
				var custom_steps = new List<string> ();
				var excluded_features = new HashSet<string> (StringComparer.Ordinal);
				var set_optimizations = new List<(CodeOptimizations, string, bool)> ();
				bool dumpDependencies = false;
				string dependenciesFileName = null;
				bool ignoreDescriptors = false;
				bool removeCAS = true;
				bool new_mvid_used = false;
				bool deterministic_used = false;

				bool resolver = false;
				while (arguments.Count > 0) {
					string token = arguments.Dequeue ();
					if (token.Length < 2) {
						ErrorUnrecognizedOption (token);
						return false;
					}

					//
					// Handling of --value like options
					//
					if (token [0] == '-' && token [1] == '-') {
						switch (token) {
						case "--skip-unresolved":
							if (!GetBoolParam (token, l => context.IgnoreUnresolved = context.Resolver.IgnoreUnresolved = l))
								return false;

							continue;

						case "--verbose":
							context.LogMessages = true;
							continue;

						case "--dependencies-file":
							if (!GetStringParam (token, l => dependenciesFileName = l))
								return false;

							continue;

						case "--dump-dependencies":
							dumpDependencies = true;
							continue;

						case "--reduced-tracing":
							if (!GetBoolParam (token, l => context.EnableReducedTracing = l))
								return false;

							continue;

						case "--used-attrs-only":
							if (!GetBoolParam (token, l => context.KeepUsedAttributeTypesOnly = l))
								return false;

							continue;

						case "--strip-security":
							if (!GetBoolParam (token, l => removeCAS = l))
								return false;

							continue;

						case "--strip-resources":
							if (!GetBoolParam (token, l => context.StripResources = l))
								return false;

							continue;

						case "--substitutions":
							if (arguments.Count < 1) {
								ErrorMissingArgument (token);
								return false;
							}

							if (!GetStringParam (token, l => context.AddSubstitutionFile (l)))
								return false;

							continue;

						case "--exclude-feature":
							if (arguments.Count < 1) {
								ErrorMissingArgument (token);
								return false;
							}

							if (!GetStringParam (token, l => {
								foreach (var feature in l.Split (',')) {
									if (!excluded_features.Contains (feature))
										excluded_features.Add (feature);
								}
							}))
								return false;

							continue;

						case "--explicit-reflection":
							if (!GetBoolParam (token, l => context.AddReflectionAnnotations = l))
								return false;

							continue;

						case "--custom-step":
							if (!GetStringParam (token, l => custom_steps.Add (l)))
								return false;

							continue;

						case "--keep-facades":
							if (!GetBoolParam (token, l => context.KeepTypeForwarderOnlyAssemblies = l))
								return false;

							continue;

						case "--keep-dep-attributes":
							if (!GetBoolParam (token, l => context.KeepDependencyAttributes = l))
								return false;

							continue;

						case "--ignore-descriptors":
							if (!GetBoolParam (token, l => ignoreDescriptors = l))
								return false;

							continue;

						case "--disable-opt":
							if (!GetStringParam (token, l => {
								if (!GetOptimizationName (l, out var opt))
									return;

								string assemblyName = GetNextStringValue ();
								set_optimizations.Add ((opt, assemblyName, false));
							}))
								return false;

							continue;

						case "--enable-opt":
							if (!GetStringParam (token, l => {
								if (!GetOptimizationName (l, out var opt))
									return;

								string assemblyName = GetNextStringValue ();
								set_optimizations.Add ((opt, assemblyName, true));
							}))
								return false;

							continue;

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
								return false;

							new_mvid_used = true;
							continue;

						case "--deterministic":
							if (!GetBoolParam (token, l => context.DeterministicOutput = l))
								return false;

							deterministic_used = true;
							continue;

						case "--output-assemblylist":
							if (!GetStringParam (token, l => context.AssemblyListFile = l))
								return false;

							continue;

						case "--output-pinvokes":
							if (!GetStringParam (token, l => context.PInvokesListFile = l))
								return false;

							continue;

						case "--version":
							Version ();
							return true;

						case "--about":
							About ();
							return true;
						}
					}

					if (token [0] == '-' || token [1] == '/') {

						switch (token.Substring (1)) {
						case "d":
							if (!GetStringParam (token, l => {
								DirectoryInfo info = new DirectoryInfo (l);
								context.Resolver.AddSearchDirectory (info.FullName);
							}))
								return false;

							continue;
						case "o":
						case "out":
							if (!GetStringParam (token, l => context.OutputDirectory = l))
								return false;

							continue;
						case "c":
							if (!GetStringParam (token, l => context.CoreAction = ParseAssemblyAction (l)))
								return false;

							continue;
						case "u":
							if (!GetStringParam (token, l => context.UserAction = ParseAssemblyAction (l)))
								return false;

							continue;
						case "p":
							if (arguments.Count < 2) {
								ErrorMissingArgument (token);
								return false;
							}

							AssemblyAction action = ParseAssemblyAction (arguments.Dequeue ());
							context.Actions [arguments.Dequeue ()] = action;
							continue;
						case "t":
							context.KeepTypeForwarderOnlyAssemblies = true;
							continue;
						case "x":
							if (!GetStringParam (token, l => {
								foreach (string file in GetFiles (l))
									p.PrependStep (new ResolveFromXmlStep (new XPathDocument (file)));

								}))
								return false;

							resolver = true;
							continue;
						case "r":
						case "a":
							if (!GetStringParam (token, l => {

								var rootVisibility = (token [1] == 'r')
									? ResolveFromAssemblyStep.RootVisibility.PublicAndFamily
									: ResolveFromAssemblyStep.RootVisibility.Any;
								foreach (string file in GetFiles (l))
									p.PrependStep (new ResolveFromAssemblyStep (file, rootVisibility));
							}))
								return false;

							resolver = true;
							continue;
#if !FEATURE_ILLINK
						case "i":
							if (!GetStringParam (token, l => {
								foreach (string file in GetFiles (l))
									p.PrependStep (new ResolveFromXApiStep (new XPathDocument (file)));
								}))
								return false;

							resolver = true;
							continue;
						case "l":
							if (!GetStringParam (token, l => assemblies = ParseI18n (l)))
								return false;

							continue;
#endif
						case "m":
							if (arguments.Count < 2) {
								ErrorMissingArgument (token);
								return false;
							}

							context.SetParameter (arguments.Dequeue (), arguments.Dequeue ());
							continue;
						case "b":
							if (!GetBoolParam (token, l => context.LinkSymbols = l))
								return false;

							continue;
						case "g":
							if (!GetBoolParam (token, l => context.DeterministicOutput = !l))
								return false;

							continue;
						case "z":
							if (!GetBoolParam (token, l => ignoreDescriptors = !l))
								return false;

							continue;
						case "v":
							if (!GetBoolParam (token, l => context.KeepMembersForDebugger = l))
								return false;

							continue;
						case "?":
						case "help":
							Usage ();
							return true;

						case "reference":
							if (!GetStringParam (token, l => context.Resolver.AddReferenceAssembly (l)))
								return false;

							continue;
						}
					}

					ErrorUnrecognizedOption (token);
					return false;
				}

				if (!resolver) {
					Console.WriteLine ($"No files to link were specified. Use one of '{resolvers}' options");
					return false;
				}

				if (new_mvid_used && deterministic_used) {
					Console.WriteLine ($"Options '--new-mvid' and '--deterministic' cannot be used at the same time");
					return false;
				}

				if (dumpDependencies)
					context.Tracer.AddRecorder (new XmlDependencyRecorder (context, dependenciesFileName));

				if (set_optimizations.Count > 0) {
					foreach (var item in set_optimizations) {
						if (item.Item3)
							context.Optimizations.Enable (item.Item1, item.Item2);
						else
							context.Optimizations.Disable (item.Item1, item.Item2);
					}
				}

				//
				// Modify the default pipeline
				//
				if (ignoreDescriptors)
					p.RemoveStep (typeof (BlacklistStep));

				if (context.DeterministicOutput)
					p.RemoveStep (typeof (RegenerateGuidStep));

				if (context.AddReflectionAnnotations)
					p.AddStepAfter (typeof (MarkStep), new ReflectionBlockedStep ());

#if !FEATURE_ILLINK
				p.AddStepAfter (typeof (LoadReferencesStep), new LoadI18nAssemblies (assemblies));

				if (assemblies != I18nAssemblies.None) {
					p.AddStepAfter (typeof (PreserveDependencyLookupStep), new PreserveCalendarsStep (assemblies));
				}
#endif

				if (_needAddBypassNGenStep) {
					p.AddStepAfter (typeof (SweepStep), new AddBypassNGenStep ());
				}

				p.AddStepBefore (typeof (MarkStep), new BodySubstituterStep ());

				if (removeCAS)
					p.AddStepBefore (typeof (MarkStep), new RemoveSecurityStep ());

				if (excluded_features.Count > 0) {
					p.AddStepBefore (typeof (MarkStep), new RemoveFeaturesStep () {
						FeatureCOM = excluded_features.Contains ("com"),
						FeatureETW = excluded_features.Contains ("etw"),
						FeatureSRE = excluded_features.Contains ("sre"),
						FeatureGlobalization = excluded_features.Contains ("globalization")
					});

					var excluded = new string [excluded_features.Count];
					excluded_features.CopyTo (excluded);
					context.ExcludedFeatures = excluded;
				}

				p.AddStepBefore (typeof (MarkStep), new RemoveUnreachableBlocksStep ());
				p.AddStepBefore (typeof (OutputStep), new ClearInitLocalsStep ());
				p.AddStepBefore (typeof (OutputStep), new SealerStep ());

				//
				// Pipeline setup with all steps enabled
				//
				// LoadReferencesStep
				// BlacklistStep [optional]
				// PreserveDependencyLookupStep
				// TypeMapStep
				// BodySubstituterStep [optional]
				// RemoveSecurityStep [optional]
				// RemoveFeaturesStep [optional]
				// RemoveUnreachableBlocksStep [optional]
				// MarkStep
				// ReflectionBlockedStep [optional]
				// SweepStep
				// AddBypassNGenStep [optional]
				// CodeRewriterStep
				// CleanStep
				// RegenerateGuidStep [optional]
				// ClearInitLocalsStep
				// OutputStep
				//

				foreach (string custom_step in custom_steps) {
					if (!AddCustomStep (p, custom_step))
						return false;
				}

				PreProcessPipeline (p);

				try {
					p.Process (context);
				} finally {
					context.Tracer.Finish ();
				}

				return true;
			}
		}

		partial void PreProcessPipeline (Pipeline pipeline);

		private static Assembly GetCustomAssembly (string arg) {
			if (Path.IsPathRooted (arg)) {
				var assemblyPath = Path.GetFullPath (arg);
				if (File.Exists (assemblyPath))
					return Assembly.Load (File.ReadAllBytes (assemblyPath));
				Console.WriteLine ($"The assembly '{arg}' specified for '--custom-step' option could not be found");
			}
			else
				Console.WriteLine ($"The path to the assembly '{arg}' specified for '--custom-step' must be fully qualified");

			return null;
		}

		protected static bool AddCustomStep (Pipeline pipeline, string arg)
		{
			Assembly custom_assembly = null;
			int pos = arg.IndexOf (",");
			if (pos != -1) {
				custom_assembly = GetCustomAssembly (arg.Substring (pos + 1));
				if (custom_assembly == null)
					return false;
				arg = arg.Substring (0, pos);
			}

			pos = arg.IndexOf (":");
			if (pos == -1) {
				var step = ResolveStep (arg, custom_assembly);
				if (step == null)
					return false;

				pipeline.AppendStep (step);
				return true;
			}

			string[] parts = arg.Split (':');
			if (parts.Length != 2) {
				Console.WriteLine ($"Invalid value '{arg}' specified for '--custom-step' option");
				return false;
			}

			if (!parts[0].StartsWith ("-") && !parts [0].StartsWith ("+")) {
				Console.WriteLine ($"Expected '+' or '-' to control new step insertion");
				return false;
			}

			bool before = parts [0][0] == '-';
			string name = parts [0].Substring (1);

			IStep target = FindStep (pipeline, name);
			if (target == null) {
				Console.WriteLine ($"Pipeline step '{name}' could not be found");
				return false;
			}

			IStep newStep = ResolveStep (parts [1], custom_assembly);
			if (newStep == null)
				return false;

			if (before)
				pipeline.AddStepBefore (target, newStep);
			else
				pipeline.AddStepAfter (target, newStep);

			return true;
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

		static IStep ResolveStep (string type, Assembly assembly)
		{
			Type step = assembly != null ? assembly.GetType(type) : Type.GetType (type, false);

			if (step == null) {
				Console.WriteLine ($"Custom step '{type}' could not be found");
				return null;
			}

			if (!typeof (IStep).IsAssignableFrom (step)) {
				Console.WriteLine ($"Custom step '{type}' is incompatible with this linker version");
				return null;
			}

			return (IStep) Activator.CreateInstance (step);
		}

		static string [] GetFiles (string param)
		{
			if (param.Length < 1 || param [0] != '@')
				return new string [] {param};

			string file = param.Substring (1);
			return ReadLines (file);
		}

		static string [] ReadLines (string file)
		{
			var lines = new List<string> ();
			using (StreamReader reader = new StreamReader (file)) {
				string line;
				while ((line = reader.ReadLine ()) != null)
					lines.Add (line);
			}
			return lines.ToArray ();
		}

#if !FEATURE_ILLINK
		protected static I18nAssemblies ParseI18n (string str)
		{
			I18nAssemblies assemblies = I18nAssemblies.None;
			string [] parts = str.Split (',');
			foreach (string part in parts)
				assemblies |= (I18nAssemblies) Enum.Parse (typeof (I18nAssemblies), part.Trim (), true);

			return assemblies;
		}
#endif

		AssemblyAction ParseAssemblyAction (string s)
		{
			var assemblyAction = (AssemblyAction)Enum.Parse(typeof(AssemblyAction), s, true);
			// The AddBypassNGenStep is necessary if any actions (default or per-assembly) are AddBypassNGen(Used).
			// We enable this step as soon as we see such an action. Even if subsequent parameters change an action we have
			// already seen, the step will only operate on assemblies with a final action AddBypassNGen(Used).
			if ((assemblyAction == AssemblyAction.AddBypassNGen) || (assemblyAction == AssemblyAction.AddBypassNGenUsed)) {
				_needAddBypassNGenStep = true;
			}
			return assemblyAction;
		}

		static bool GetOptimizationName (string text, out CodeOptimizations optimization)
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
			case "clearinitlocals":
				optimization = CodeOptimizations.ClearInitLocals;
				return true;
			case "unusedinterfaces":
				optimization = CodeOptimizations.UnusedInterfaces;
				return true;
			case "ipconstprop":
				optimization = CodeOptimizations.IPConstantPropagation;
				return true;
			case "sealer":
				optimization = CodeOptimizations.Sealer;
				return true;
			}

			Console.WriteLine ($"Invalid optimization value '{text}'");
			optimization = 0;
			return false;
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

			Console.WriteLine ($"Invalid argument for '{token}' option");
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

		protected virtual LinkContext GetDefaultContext (Pipeline pipeline)
		{
			LinkContext context = new LinkContext (pipeline) {
				CoreAction = AssemblyAction.Skip,
				UserAction = AssemblyAction.Link,
				OutputDirectory = "output",
				StripResources = true
			};
			return context;
		}

		static void Usage ()
		{
			Console.WriteLine (_linker);

#if FEATURE_ILLINK
			Console.WriteLine ($"illink [options] {resolvers} file");
#else
			Console.WriteLine ($"monolinker [options] {resolvers} file");
#endif

			Console.WriteLine ("  -a                  Link from a list of assemblies");
#if !FEATURE_ILLINK
			Console.WriteLine ("  -i                  Link from an mono-api-info descriptor");
#endif
			Console.WriteLine ("  -r                  Link from a list of assemblies using roots visible outside of the assembly");
			Console.WriteLine ("  -x                  Link from XML descriptor");
			Console.WriteLine ("  -d PATH             Specify additional directories to search in for references");
			Console.WriteLine ("  -reference FILE     Specify additional assemblies to use as references");
			Console.WriteLine ("  -b                  Update debug symbols for each linked module. Defaults to false");
			Console.WriteLine ("  -v                  Keep members and types used by debugger. Defaults to false");
#if !FEATURE_ILLINK
			Console.WriteLine ("  -l <name>,<name>    List of i18n assemblies to copy to the output directory. Defaults to 'all'");
			Console.WriteLine ("                        Valid names are 'none', 'all', 'cjk', 'mideast', 'other', 'rare', 'west'");
#endif
			Console.WriteLine ("  -out PATH           Specify the output directory. Defaults to 'output'");
			Console.WriteLine ("  --about             About the {0}", _linker);
			Console.WriteLine ("  --verbose           Log messages indicating progress and warnings");
			Console.WriteLine ("  --version           Print the version number of the {0}", _linker);
			Console.WriteLine ("  -help               Lists all linker options");
			Console.WriteLine ("  @FILE               Read response file for more options");

			Console.WriteLine ();
			Console.WriteLine ("Actions");
			Console.WriteLine ("  -c ACTION           Action on the framework assemblies. Defaults to 'skip'");
			Console.WriteLine ("                        copy: Copy the assembly into the output (it can be updated when any of its dependencies is removed)");
			Console.WriteLine ("                        copyused: Same as copy but only for assemblies which are needed");
			Console.WriteLine ("                        link: Remove any ununsed code or metadata from the assembly");
			Console.WriteLine ("                        skip: Do not process the assembly");
			Console.WriteLine ("                        addbypassngen: Add BypassNGenAttribute to unused methods");
			Console.WriteLine ("                        addbypassngenused: Same as addbypassngen but unused assemblies are removed");
			Console.WriteLine ("  -u ACTION           Action on the user assemblies. Defaults to 'link'");
			Console.WriteLine ("  -p ACTION ASM       Overrides the default action for an assembly");

			Console.WriteLine ();
			Console.WriteLine ("Advanced");
			Console.WriteLine ("  --custom-step CFG         Add a custom step <config> to the existing pipeline");
			Console.WriteLine ("                            Step can use one of following configurations");
			Console.WriteLine ("                            TYPE,PATH_TO_ASSEMBLY: Add user defined type as last step to the pipeline");
			Console.WriteLine ("                            +NAME:TYPE,PATH_TO_ASSEMBLY: Inserts step type before existing step with name");
			Console.WriteLine ("                            -NAME:TYPE,PATH_TO_ASSEMBLY: Add step type after existing step");
			Console.WriteLine ("  --ignore-descriptors      Skips reading embedded descriptors (short -z). Defaults to false");
			Console.WriteLine ("  --keep-facades            Keep assemblies with type-forwarders (short -t). Defaults to false");
			Console.WriteLine ("  --skip-unresolved         Ignore unresolved types, methods, and assemblies. Defaults to false");
			Console.WriteLine ("  --output-pinvokes PATH    Output a JSON file with all modules and entry points of the P/Invokes found");

			Console.WriteLine ();
			Console.WriteLine ("Linking");
			Console.WriteLine ("  --deterministic           Produce a deterministic output for linked assemblies");
			Console.WriteLine ("  --disable-opt NAME [ASM]  Disable one of the default optimizations globaly or for a specific assembly name");
			Console.WriteLine ("                              beforefieldinit: Unused static fields are removed if there is no static ctor");
			Console.WriteLine ("                              ipconstprop: Interprocedural constant propagation on return values");
			Console.WriteLine ("                              overrideremoval: Overrides of virtual methods on types that are never instantiated are removed");
			Console.WriteLine ("                              unreachablebodies: Instance methods that are marked but not executed are converted to throws");
			Console.WriteLine ("                              unusedinterfaces: Removes interface types from declaration when not used");
			Console.WriteLine ("  --enable-opt NAME [ASM]   Enable one of the additional optimizations globaly or for a specific assembly name");
			Console.WriteLine ("                              clearinitlocals: Remove initlocals");
			Console.WriteLine ("                              sealer: Any method or type which does not have override is marked as sealed");
			Console.WriteLine ("  --exclude-feature NAME    Any code which has a feature <name> in linked assemblies will be removed");
			Console.WriteLine ("                              com: Support for COM Interop");
			Console.WriteLine ("                              etw: Event Tracing for Windows");
#if !FEATURE_ILLINK
			Console.WriteLine ("                              remoting: .NET Remoting dependencies");
#endif
			Console.WriteLine ("                              sre: System.Reflection.Emit namespace");
			Console.WriteLine ("                              globalization: Globalization data and globalization behavior");
			Console.WriteLine ("  --explicit-reflection     Adds to members never used through reflection DisablePrivateReflection attribute. Defaults to false");
			Console.WriteLine ("  --keep-dep-attributes     Keep attributes used for manual dependency tracking. Defaults to false");
			Console.WriteLine ("  --new-mvid                Generate a new guid for each linked assembly (short -g). Defaults to true");
			Console.WriteLine ("  --strip-resources         Remove XML descriptor resources for linked assemblies. Defaults to true");
			Console.WriteLine ("  --strip-security          Remove metadata and code related to Code Access Security. Defaults to true");
			Console.WriteLine ("  --substitutions FILE      Configuration file with field or methods substitution rules");
			Console.WriteLine ("  --used-attrs-only         Attribute usage is removed if the attribute type is not used. Defaults to false");

			Console.WriteLine ();
			Console.WriteLine ("Analyzer");
			Console.WriteLine ("  --dependencies-file PATH   Specify the dependencies output. Defaults to 'output/linker-dependencies.xml.gz'");
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
			Console.WriteLine ("   http://www.mono-project.com/");
		}

		static Pipeline GetStandardPipeline ()
		{
			Pipeline p = new Pipeline ();
			p.AppendStep (new LoadReferencesStep ());
			p.AppendStep (new BlacklistStep ());
			p.AppendStep (new PreserveDependencyLookupStep ());
			p.AppendStep (new TypeMapStep ());
			p.AppendStep (new MarkStep ());
			p.AppendStep (new SweepStep ());
			p.AppendStep (new CodeRewriterStep ());
			p.AppendStep (new CleanStep ());
			p.AppendStep (new RegenerateGuidStep ());
			p.AppendStep (new OutputStep ());
			return p;
		}
	}
}
