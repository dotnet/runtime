// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ILCompilerOptionsBuilder
	{
		//public TrimmerOptions Options { get; } = new();
		private readonly TestCaseMetadataProvider _metadataProvider;

		public readonly ILCompilerOptions Options;

		public ILCompilerOptionsBuilder (TestCaseMetadataProvider metadataProvider)
		{
			Options = new ILCompilerOptions ();
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
			Options.FeatureSwitches.Add ("System.Linq.Expressions.CanCompileToIL", false);
			Options.FeatureSwitches.Add ("System.Linq.Expressions.CanEmitObjectArrayDelegate", false);
			Options.FeatureSwitches.Add ("System.Linq.Expressions.CanCreateArbitraryDelegates", false);
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
			Options.TrimAssemblies.Add (fileName);
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

		public virtual void AddKeepDebugMembers (string value)
		{
		}

		public virtual void AddAssemblyAction (string action, string assembly)
		{
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
		}

		public virtual void AddLinkAttributes (string file)
		{
		}

		public virtual void AddAdditionalArgument (string flag, string[] values)
		{
			if (flag == "--feature") {
				Options.FeatureSwitches.Add (values[0], bool.Parse (values[1]));
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

			if (!string.IsNullOrEmpty (options.KeepDebugMembers))
				AddKeepDebugMembers (options.KeepDebugMembers);

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
		}

		static void AppendExpandedPaths (Dictionary<string, string> dictionary, string pattern)
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
	}
}
