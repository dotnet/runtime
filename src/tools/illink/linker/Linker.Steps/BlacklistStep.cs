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

		protected override void Process ()
		{
			foreach (string name in Assembly.GetExecutingAssembly ().GetManifestResourceNames ()) {
				if (!name.EndsWith (".xml", StringComparison.OrdinalIgnoreCase) || !ShouldProcessAssemblyResource (GetAssemblyName (name)))
					continue;

				try {
					Context.LogMessage ("Processing resource linker descriptor: {0}", name);
					AddToPipeline (GetResolveStep (name));
				} catch (XmlException ex) {
					/* This could happen if some broken XML file is included. */
					Context.LogMessage ("Error processing {0}: {1}", name, ex);
				}
			}

			foreach (var asm in Context.GetAssemblies ()) {
				foreach (var rsc in asm.Modules
									.SelectMany (mod => mod.Resources)
									.Where (res => res.ResourceType == ResourceType.Embedded)
									.Where (res => res.Name.EndsWith (".xml", StringComparison.OrdinalIgnoreCase))
									.Where (res => ShouldProcessAssemblyResource (GetAssemblyName (res.Name)))
									.Cast<EmbeddedResource> ()) {
					try {
						Context.LogMessage ("Processing embedded resource linker descriptor: {0}", rsc.Name);

						AddToPipeline (GetExternalResolveStep (rsc, asm));
					} catch (XmlException ex) {
						/* This could happen if some broken XML file is embedded. */
						Context.LogMessage ("Error processing {0}: {1}", rsc.Name, ex);
					}
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

		bool ShouldProcessAssemblyResource (string name)
		{
			AssemblyDefinition assembly = GetAssemblyIfReferenced (name);

			if (assembly == null)
				return false;

			switch (Annotations.GetAction (assembly)) {
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
				return true;
			default:
				return false;
			}
		}

		AssemblyDefinition GetAssemblyIfReferenced (string name)
		{
			foreach (AssemblyDefinition assembly in Context.GetAssemblies ())
				if (assembly.Name.Name == name)
					return assembly;

			return null;
		}

		protected virtual void AddToPipeline (IStep resolveStep)
		{
			Context.Pipeline.AddStepAfter (typeof (BlacklistStep), resolveStep);
		}

		protected virtual IStep GetExternalResolveStep (EmbeddedResource resource, AssemblyDefinition assembly)
		{
			return new ResolveFromXmlStep (GetExternalDescriptor (resource), resource.Name, assembly, "resource " + resource.Name + " in " + assembly.FullName);
		}

		static ResolveFromXmlStep GetResolveStep (string descriptor)
		{
			return new ResolveFromXmlStep (GetDescriptor (descriptor), "descriptor " + descriptor + " from " + Assembly.GetExecutingAssembly ().FullName);
		}

		protected static XPathDocument GetExternalDescriptor (EmbeddedResource resource)
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
