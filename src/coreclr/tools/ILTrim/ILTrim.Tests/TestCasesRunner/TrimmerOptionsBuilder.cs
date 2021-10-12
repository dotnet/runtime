using System.Collections.Generic;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TrimmerOptionsBuilder
	{
        public TrimmerOptions Options { get; } = new();
		private readonly TestCaseMetadataProvider _metadataProvider;

		public TrimmerOptionsBuilder (TestCaseMetadataProvider metadataProvider)
		{
			_metadataProvider = metadataProvider;
		}

		public virtual void AddSearchDirectory (NPath directory)
		{
		}

		public virtual void AddReference (NPath path)
		{
		}

		public virtual void AddOutputDirectory (NPath directory)
		{
            Options.OutputDirectory = directory.ToString();
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

		public virtual void LinkFromAssembly (string fileName)
		{
            Options.InputPath = fileName;
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
	}
}
