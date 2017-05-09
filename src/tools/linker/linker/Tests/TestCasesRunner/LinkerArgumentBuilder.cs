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

		public string [] ToArgs ()
		{
			return _arguments.ToArray ();
		}

		protected void Append (string arg)
		{
			_arguments.Add (arg);
		}
	}
}