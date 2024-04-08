// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TrimmingArgumentBuilder
	{
		private readonly List<string> _arguments = new List<string> ();
		private readonly TestCaseMetadataProvider _metadataProvider;

		public TrimmingArgumentBuilder (TestCaseMetadataProvider metadataProvider)
		{
			_metadataProvider = metadataProvider;
		}

		public virtual void AddSearchDirectory (NPath directory)
		{
			Append ("-d");
			Append (directory.ToString ());
		}

		public virtual void AddReference (NPath path)
		{
			Append ("-reference");
			Append (path.ToString ());
		}

		public virtual void AddOutputDirectory (NPath directory)
		{
			Append ("-o");
			Append (directory.ToString ());
		}

		public virtual void AddLinkXmlFile (string file)
		{
			Append ("-x");
			Append (file);
		}

		public virtual void AddResponseFile (NPath path)
		{
			Append ($"@{path}");
		}

		public virtual void AddTrimMode (string value)
		{
			Append ("--trim-mode");
			Append (value);
		}

		public virtual void AddDefaultAction (string value)
		{
			Append ("--action");
			Append (value);
		}

		public virtual void RootAssemblyEntryPoint (string fileName)
		{
			Append ("-a");
			Append (fileName);
			Append ("entrypoint");
		}

		public virtual void RootAssemblyVisible (string fileName)
		{
#if NETCOREAPP
			Append ("-a");
			Append (fileName);
			Append ("visible");
#else
			Append ("-r");
			Append (fileName);
#endif
		}

		public virtual void RootAssembly (string fileName)
		{
			Append ("-a");
			Append (fileName);
		}

		public virtual void IgnoreDescriptors (bool value)
		{
			Append ("--ignore-descriptors");
			Append (value ? "true" : "false");
		}

		public virtual void IgnoreSubstitutions (bool value)
		{
			Append ("--ignore-substitutions");
			Append (value ? "true" : "false");
		}

		public virtual void IgnoreLinkAttributes (bool value)
		{
			Append ("--ignore-link-attributes");
			Append (value ? "true" : "false");
		}

		public virtual void AddIl8n (string value)
		{
			Append ("-l");
			Append (value);
		}

		public virtual void AddLinkSymbols (string value)
		{
			Append ("-b");
			Append (value);
		}

		public virtual void AddAssemblyAction (string action, string assembly)
		{
			Append ("--action");
			Append (action);
			Append (assembly);
		}

		public virtual void AddSkipUnresolved (bool skipUnresolved)
		{
			if (skipUnresolved) {
				Append ("--skip-unresolved");
				Append ("true");
			}
		}

		public virtual void AddStripDescriptors (bool stripDescriptors)
		{
			if (!stripDescriptors) {
				Append ("--strip-descriptors");
				Append ("false");
			}
		}

		public virtual void AddStripSubstitutions (bool stripSubstitutions)
		{
			if (!stripSubstitutions) {
				Append ("--strip-substitutions");
				Append ("false");
			}
		}

		public virtual void AddStripLinkAttributes (bool stripLinkAttributes)
		{
			if (!stripLinkAttributes) {
				Append ("--strip-link-attributes");
				Append ("false");
			}
		}

		public virtual void AddSubstitutions (string file)
		{
			Append ("--substitutions");
			Append (file);
		}

		public virtual void AddLinkAttributes (string file)
		{
			Append ("--link-attributes");
			Append (file);
		}

		public string[] Build ()
		{
			return _arguments.ToArray ();
		}

		protected void Append (string arg)
		{
			_arguments.Add (arg);
		}

		public virtual void AddAdditionalArgument (string flag, string[] values)
		{
			Append (flag);
			if (values != null) {
				foreach (var val in values)
					Append (val);
			}
		}

		public virtual void ProcessTestInputAssembly (NPath inputAssemblyPath)
		{
			if (_metadataProvider.LinkPublicAndFamily ())
				RootAssemblyVisible (inputAssemblyPath.ToString ());
			else if (_metadataProvider.LinkAll ())
				RootAssembly (inputAssemblyPath.ToString ());
			else
				RootAssemblyEntryPoint (inputAssemblyPath.ToString ());
		}

		public virtual void ProcessOptions (TestCaseLinkerOptions options)
		{
			if (options.TrimMode != null)
				AddTrimMode (options.TrimMode);

			if (options.DefaultAssembliesAction != null)
				AddDefaultAction (options.DefaultAssembliesAction);

			if (options.AssembliesAction != null) {
				foreach (var entry in options.AssembliesAction)
					AddAssemblyAction (entry.Key, entry.Value);
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
		}
	}
}