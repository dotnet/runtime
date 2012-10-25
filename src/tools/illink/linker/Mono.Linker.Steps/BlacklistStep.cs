//
// Blacklist.cs
//
// Author:
//   Jb Evain (jb@nurv.fr)
//
// (C) 2007 Novell Inc.
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

using System.Collections;
using System.IO;
using System.Reflection;
using System.Xml.XPath;

using Mono.Cecil;

namespace Mono.Linker.Steps {

	public class BlacklistStep : BaseStep {

		protected override bool ConditionToProcess()
		{
			return Context.CoreAction == AssemblyAction.Link;
		}

		protected override void Process ()
		{
			foreach (string name in Assembly.GetExecutingAssembly ().GetManifestResourceNames ()) {
				if (!IsReferenced (GetAssemblyName (name)))
					continue;

				Context.Pipeline.AddStepAfter (typeof (TypeMapStep), GetResolveStep (name));
			}
		}

		static string GetAssemblyName (string descriptor)
		{
			int pos = descriptor.LastIndexOf (".");
			if (pos == -1)
				return descriptor;

			return descriptor.Substring (0, pos);
		}

		bool IsReferenced (string name)
		{
			foreach (AssemblyDefinition assembly in Context.GetAssemblies ())
				if (assembly.Name.Name == name)
					return true;

			return false;
		}

		static ResolveFromXmlStep GetResolveStep (string descriptor)
		{
			return new ResolveFromXmlStep (GetDescriptor (descriptor));
		}

		static XPathDocument GetDescriptor (string descriptor)
		{
			using (StreamReader sr = new StreamReader (GetResource (descriptor))) {
				return new XPathDocument (new StringReader (sr.ReadToEnd ()));
			}
		}

		static Stream GetResource (string descriptor)
		{
			return Assembly.GetExecutingAssembly ().GetManifestResourceStream (descriptor);
		}
	}
}
