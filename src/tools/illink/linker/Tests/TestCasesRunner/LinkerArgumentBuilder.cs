using System.Collections.Generic;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class LinkerArgumentBuilder {
		private readonly List<string> _arguments = new List<string> ();
		private readonly TestCaseMetadaProvider _metadaProvider;

		public LinkerArgumentBuilder (TestCaseMetadaProvider metadaProvider)
		{
			_metadaProvider = metadaProvider;
		}

		public virtual void AddSearchDirectory (NPath directory)
		{
			Append ("-d");
			Append (directory.ToString ());
		}

		public virtual void AddOutputDirectory (NPath directory)
		{
			Append ("-o");
			Append (directory.ToString ());
		}

		public virtual void AddLinkXmlFile (NPath path)
		{
			Append ("-x");
			Append (path.ToString ());
		}

		public virtual void AddResponseFile (NPath path)
		{
			Append ($"@{path}");
		}

		public virtual void AddCoreLink (string value)
		{
			Append ("-c");
			Append (value);
		}

		public virtual void AddUserLink (string value)
		{
			Append ("-u");
			Append (value);
		}

		public virtual void LinkFromAssembly (string fileName)
		{
			Append ("-a");
			Append (fileName);
		}
		
		public virtual void LinkFromPublicAndFamily (string fileName)
		{
			Append ("-r");
			Append (fileName);
		}

		public virtual void IncludeBlacklist (bool value)
		{
			Append ("-z");
			Append (value ? "true" : "false");
		}

		public virtual void AddIl8n (string value)
		{
			Append ("-l");
			Append (value);
		}

		public virtual void AddKeepTypeForwarderOnlyAssemblies (string value)
		{
			if (bool.Parse (value))
				Append ("-t");
		}
		
		public virtual void AddLinkSymbols (string value)
		{
			Append ("-b");
			Append (value);
		}
		
		public virtual void AddKeepDebugMembers (string value)
		{
			Append ("-v");
			Append (value);
		}

		public virtual void AddAssemblyAction (string action, string assembly)
		{
			Append ("-p");
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

		public virtual void AddStripResources (bool stripResources)
		{
			if (!stripResources) {
				Append ("--strip-resources");
				Append ("false");
			}
		}

		public string [] ToArgs ()
		{
			return _arguments.ToArray ();
		}

		protected void Append (string arg)
		{
			_arguments.Add (arg);
		}

		public virtual void AddAdditionalArgument (string flag, string [] values)
		{
			Append (flag);
			if (values != null) {
				foreach (var val in values)
					Append (val);
			}
		}
		
		public virtual void ProcessTestInputAssembly (NPath inputAssemblyPath)
		{
			if (_metadaProvider.LinkPublicAndFamily ())
				LinkFromPublicAndFamily (inputAssemblyPath.ToString ());
			else
				LinkFromAssembly (inputAssemblyPath.ToString ());
		}

		public virtual void ProcessOptions (TestCaseLinkerOptions options)
		{
			if (options.CoreAssembliesAction != null)
				AddCoreLink (options.CoreAssembliesAction);

			if (options.UserAssembliesAction != null)
				AddUserLink (options.UserAssembliesAction);

			if (options.AssembliesAction != null) {
				foreach (var entry in options.AssembliesAction)
					AddAssemblyAction (entry.Key, entry.Value);
			}

			// Running the blacklist step causes a ton of stuff to be preserved.  That's good for normal use cases, but for
			// our test cases that pollutes the results
			IncludeBlacklist (options.IncludeBlacklistStep);

			if (!string.IsNullOrEmpty (options.Il8n))
				AddIl8n (options.Il8n);

			if (!string.IsNullOrEmpty (options.KeepTypeForwarderOnlyAssemblies))
				AddKeepTypeForwarderOnlyAssemblies (options.KeepTypeForwarderOnlyAssemblies);
			
			if (!string.IsNullOrEmpty (options.LinkSymbols))
				AddLinkSymbols (options.LinkSymbols);
			
			if (!string.IsNullOrEmpty (options.KeepDebugMembers))
				AddKeepDebugMembers (options.KeepDebugMembers);

			AddSkipUnresolved (options.SkipUnresolved);

			AddStripResources (options.StripResources);

			// Unity uses different argument format and needs to be able to translate to their format.  In order to make that easier
			// we keep the information in flag + values format for as long as we can so that this information doesn't have to be parsed out of a single string
			foreach (var additionalArgument in options.AdditionalArguments)
				AddAdditionalArgument (additionalArgument.Key, additionalArgument.Value);
		}
	}
}