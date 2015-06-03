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

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Xml;
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
				if (Path.GetExtension (name) != ".xml" || !IsReferenced (GetAssemblyName (name)))
					continue;

				try {
					if (Context.LogInternalExceptions)
						Console.WriteLine ("Processing resource linker descriptor: {0}", name);
					Context.Pipeline.AddStepAfter (typeof (TypeMapStep), GetResolveStep (name));
				} catch (XmlException ex) {
					/* This could happen if some broken XML file is included. */
					if (Context.LogInternalExceptions)
						Console.WriteLine ("Error processing {0}: {1}", name, ex);
				}
			}

			foreach (var rsc in Context.GetAssemblies ()
								.SelectMany (asm => asm.Modules)
								.SelectMany (mod => mod.Resources)
								.Where (res => res.ResourceType == ResourceType.Embedded)
								.Where (res => Path.GetExtension (res.Name) == ".xml")
								.Where (res => IsReferenced (GetAssemblyName (res.Name)))
								.Cast<EmbeddedResource> ()) {
				try {
					if (Context.LogInternalExceptions)
						Console.WriteLine ("Processing embedded resource linker descriptor: {0}", rsc.Name);

					Context.Pipeline.AddStepAfter (typeof (TypeMapStep), GetExternalResolveStep (rsc));
				} catch (XmlException ex) {
					/* This could happen if some broken XML file is embedded. */
					if (Context.LogInternalExceptions)
						Console.WriteLine ("Error processing {0}: {1}", rsc.Name, ex);
				}
			}
		}

		static string GetAssemblyName (string descriptor)
		{
			int pos = descriptor.LastIndexOf ('.');
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

		static ResolveFromXmlStep GetExternalResolveStep (EmbeddedResource resource)
		{
			return new ResolveFromXmlStep (GetExternalDescriptor (resource));
		}

		static ResolveFromXmlStep GetResolveStep (string descriptor)
		{
			return new ResolveFromXmlStep (GetDescriptor (descriptor));
		}

		static XPathDocument GetExternalDescriptor (EmbeddedResource resource)
		{
			using (var sr = new StreamReader (resource.GetResourceStream ())) {
				return new XPathDocument (new StringReader (sr.ReadToEnd ()));
			}
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
