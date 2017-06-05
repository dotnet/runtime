using System.Collections.Generic;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class LinkerArgumentBuilder {
		private readonly List<string> _arguments = new List<string> ();

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

		public virtual void AddCoreLink (string value)
		{
			Append ("-c");
			Append (value);
		}

		public virtual void IncludeBlacklist (string value)
		{
			Append ("-z");
			Append (value);
		}

		public virtual void AddIl8n (string value)
		{
			Append ("-l");
			Append (value);
		}

		public string [] ToArgs ()
		{
			return _arguments.ToArray ();
		}

		protected void Append (string arg)
		{
			_arguments.Add (arg);
		}

		public virtual void ProcessOptions (TestCaseLinkerOptions options)
		{
			AddCoreLink (options.CoreLink);

			// Running the blacklist step causes a ton of stuff to be preserved.  That's good for normal use cases, but for
			// our test cases that pollutes the results
			if (!string.IsNullOrEmpty (options.IncludeBlacklistStep))
				IncludeBlacklist (options.IncludeBlacklistStep);

			// Internationalization assemblies pollute our test case results as well so disable them
			if (!string.IsNullOrEmpty (options.Il8n))
				AddIl8n (options.Il8n);
		}
	}
}