// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TrimmingArgumentBuilder
	{
		private readonly TestCaseMetadataProvider _metadataProvider;

		private ILCompilerOptions? _options;
		private ILCompilerOptions Options => _options ?? throw new InvalidOperationException ("Invalid state: Build() was already called");

		public TrimmingArgumentBuilder (TestCaseMetadataProvider metadataProvider)
		{
			_options = new ILCompilerOptions ();
			_metadataProvider = metadataProvider;

			string runtimeBinDir = (string) AppContext.GetData ("Mono.Linker.Tests.RuntimeBinDirectory")!;
			AppendExpandedPaths (Options.ReferenceFilePaths, Path.Combine (runtimeBinDir, "aotsdk", "*.dll"));

			string runtimePackDir = (string) AppContext.GetData ("Mono.Linker.Tests.MicrosoftNetCoreAppRuntimePackDirectory")!;
			if (!Directory.Exists (runtimePackDir) && runtimePackDir.Contains ("Debug")) {
				// Frequently we'll have a Debug runtime and Release libraries, which actually produces a Release runtime pack
				// but from within VS we're see Debug everything. So if the runtime pack directory doesn't exist
				// try the Release path (simple string replace)
				string candidate = runtimePackDir.Replace ("Debug", "Release");
				if (Directory.Exists (candidate))
					runtimePackDir = candidate;
			}
			AppendExpandedPaths (Options.ReferenceFilePaths, Path.Combine (runtimePackDir, "*.dll"));

			Options.InitAssemblies.Add ("System.Private.CoreLib");
			Options.InitAssemblies.Add ("System.Private.StackTraceMetadata");
			Options.InitAssemblies.Add ("System.Private.TypeLoader");
			Options.InitAssemblies.Add ("System.Private.Reflection.Execution");

			Options.FeatureSwitches.Add ("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", false);
			Options.FeatureSwitches.Add ("System.Resources.ResourceManager.AllowCustomResourceTypes", false);
			Options.FeatureSwitches.Add ("System.Linq.Expressions.CanEmitObjectArrayDelegate", false);
			Options.FeatureSwitches.Add ("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false);
			Options.FeatureSwitches.Add ("System.Diagnostics.Debugger.IsSupported", false);
			Options.FeatureSwitches.Add ("System.Text.Encoding.EnableUnsafeUTF7Encoding", false);
			Options.FeatureSwitches.Add ("System.Diagnostics.Tracing.EventSource.IsSupported", false);
			Options.FeatureSwitches.Add ("System.Globalization.Invariant", true);
			Options.FeatureSwitches.Add ("System.Resources.UseSystemResourceKeys", true);

			Options.FrameworkCompilation = false;
		}

		public virtual void AddSearchDirectory (NPath directory)
		{
		}

		public virtual void AddReference (NPath path)
		{
			AppendExpandedPaths (Options.ReferenceFilePaths, path.ToString ());
		}

		public virtual void AddOutputDirectory (NPath directory)
		{
		}

		public virtual void AddLinkXmlFile (string file)
		{
			Options.Descriptors.Add (file);
		}

		public virtual void AddResponseFile (NPath path)
		{
		}

		public virtual void AddTrimMode (string value)
		{
		}

		public virtual void AddDefaultAction (string value)
		{
		}

		public virtual void AddLinkAssembly (string fileName)
		{
			Options.TrimAssemblies.Add (Path.GetFileNameWithoutExtension(fileName));
		}

		public virtual void LinkFromAssembly (string fileName)
		{
			AppendExpandedPaths (Options.InputFilePaths, fileName);
		}

		public virtual void LinkFromPublicAndFamily (string fileName)
		{
		}

		public virtual void IgnoreDescriptors (bool value)
		{
		}

		public virtual void IgnoreSubstitutions (bool value)
		{
		}

		public virtual void IgnoreLinkAttributes (bool value)
		{
		}

		public virtual void AddIl8n (string value)
		{
		}

		public virtual void AddKeepTypeForwarderOnlyAssemblies (string value)
		{
		}

		public virtual void AddLinkSymbols (string value)
		{
		}

		public virtual void AddAssemblyAction (string action, string assembly)
		{
			switch (action) {
			case "copy":
				Options.AdditionalRootAssemblies.Add (assembly);
				break;
			}
		}

		public virtual void AddSkipUnresolved (bool skipUnresolved)
		{
		}

		public virtual void AddStripDescriptors (bool stripDescriptors)
		{
		}

		public virtual void AddStripSubstitutions (bool stripSubstitutions)
		{
		}

		public virtual void AddStripLinkAttributes (bool stripLinkAttributes)
		{
		}

		public virtual void AddSubstitutions (string file)
		{
			Options.SubstitutionFiles.Add (file);
		}

		public virtual void AddLinkAttributes (string file)
		{
		}

		public virtual void AddAdditionalArgument (string flag, string[] values)
		{
			if (flag == "--feature") {
				Options.FeatureSwitches.Add (values[0], bool.Parse (values[1]));
			}
			else if (flag == "--singlewarn") {
				Options.SingleWarn = true;
			}
			else if (flag.StartsWith("--warnaserror"))
			{
				if (flag == "--warnaserror" || flag == "--warnaserror+")
				{
					if (values.Length == 0)
						Options.TreatWarningsAsErrors = true;
					else
					{
						foreach (int warning in ProcessWarningCodes(values))
							Options.WarningsAsErrors[warning] = true;
					}

				}
				else if (flag == "--warnaserror-")
				{
					if (values.Length == 0)
						Options.TreatWarningsAsErrors = false;
					else
					{
						foreach (int warning in ProcessWarningCodes(values))
							Options.WarningsAsErrors[warning] = false;
					}
				}
			}
		}

		public virtual void ProcessTestInputAssembly (NPath inputAssemblyPath)
		{
			if (_metadataProvider.LinkPublicAndFamily ())
				LinkFromPublicAndFamily (inputAssemblyPath.ToString ());
			else
				LinkFromAssembly (inputAssemblyPath.ToString ());
		}

		public virtual void ProcessOptions (TestCaseLinkerOptions options)
		{
			if (options.TrimMode != null)
				AddTrimMode (options.TrimMode);

			if (options.DefaultAssembliesAction != null)
				AddDefaultAction (options.DefaultAssembliesAction);

			if (options.AssembliesAction != null) {
				foreach (var (action, assembly) in options.AssembliesAction)
					AddAssemblyAction (action, assembly);
			}

			// Honoring descriptors causes a ton of stuff to be preserved.  That's good for normal use cases, but for
			// our test cases that pollutes the results
			IgnoreDescriptors (options.IgnoreDescriptors);

			IgnoreSubstitutions (options.IgnoreSubstitutions);

			IgnoreLinkAttributes (options.IgnoreLinkAttributes);

#if !NETCOREAPP
			if (!string.IsNullOrEmpty (options.Il8n))
				AddIl8n (options.Il8n);
#endif

			if (!string.IsNullOrEmpty (options.KeepTypeForwarderOnlyAssemblies))
				AddKeepTypeForwarderOnlyAssemblies (options.KeepTypeForwarderOnlyAssemblies);

			if (!string.IsNullOrEmpty (options.LinkSymbols))
				AddLinkSymbols (options.LinkSymbols);

			AddSkipUnresolved (options.SkipUnresolved);

			AddStripDescriptors (options.StripDescriptors);

			AddStripSubstitutions (options.StripSubstitutions);

			AddStripLinkAttributes (options.StripLinkAttributes);

			foreach (var descriptor in options.Descriptors)
				AddLinkXmlFile (descriptor);

			foreach (var substitutions in options.Substitutions)
				AddSubstitutions (substitutions);

			foreach (var attributeDefinition in options.LinkAttributes)
				AddLinkAttributes (attributeDefinition);

			// A list of expensive optimizations which should not run by default
			AddAdditionalArgument ("--disable-opt", new[] { "ipconstprop" });

			// Unity uses different argument format and needs to be able to translate to their format.  In order to make that easier
			// we keep the information in flag + values format for as long as we can so that this information doesn't have to be parsed out of a single string
			foreach (var additionalArgument in options.AdditionalArguments)
				AddAdditionalArgument (additionalArgument.Key, additionalArgument.Value);

			if (options.IlcFrameworkCompilation)
				Options.FrameworkCompilation = true;
		}

		private static void AppendExpandedPaths (Dictionary<string, string> dictionary, string pattern)
		{
			bool empty = true;

			string directoryName = Path.GetDirectoryName (pattern)!;
			string searchPattern = Path.GetFileName (pattern);

			if (directoryName == "")
				directoryName = ".";

			if (Directory.Exists (directoryName)) {
				foreach (string fileName in Directory.EnumerateFiles (directoryName, searchPattern)) {
					string fullFileName = Path.GetFullPath (fileName);

					string simpleName = Path.GetFileNameWithoutExtension (fileName);

					if (!dictionary.ContainsKey (simpleName)) {
						dictionary.Add (simpleName, fullFileName);
					}

					empty = false;
				}
			}

			if (empty) {
				throw new Exception ("No files matching " + pattern);
			}
		}

		private static readonly char[] s_separator = new char[] { ',', ';', ' ' };

		private static IEnumerable<int> ProcessWarningCodes(IEnumerable<string> warningCodes)
		{
			foreach (string value in warningCodes)
			{
				string[] values = value.Split(s_separator, StringSplitOptions.RemoveEmptyEntries);
				foreach (string id in values)
				{
					if (!id.StartsWith("IL", StringComparison.Ordinal) || !ushort.TryParse(id.AsSpan(2), out ushort code))
						continue;

					yield return code;
				}
			}
		}

		public ILCompilerOptions Build ()
		{
			var options = Options;
			_options = null;
			return options;
		}
	}
}
