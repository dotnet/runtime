//
// MoonlightA11yDescriptorGenerator.cs
//
// Author:
//   Andrés G. Aragoneses (aaragoneses@novell.com)
//
// (C) 2009 Novell, Inc.
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
using System.Xml;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class MoonlightA11yDescriptorGenerator : BaseStep {

		XmlTextWriter writer = null;
		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (writer == null) {
				writer = new XmlTextWriter (System.Console.Out);
				writer.Formatting = Formatting.Indented;
				writer.WriteStartElement("linker");
			}

			IDictionary types = ScanAssembly (assembly);
			if (types != null && types.Count > 0) {
				writer.WriteStartElement("assembly");
				writer.WriteAttributeString ("fullname", assembly.Name.Name);

				foreach (TypeDefinition type in types.Keys) {
					IList members = (IList)types [type];
					if (members != null && members.Count > 0) {
						writer.WriteStartElement("type");
						writer.WriteAttributeString ("fullname", type.FullName);

						foreach (IAnnotationProvider member in members) {
							MethodDefinition method = member as MethodDefinition;
							if (method != null) {
								writer.WriteStartElement("method");
								writer.WriteAttributeString ("signature", 
								                             method.ReturnType.ReturnType.FullName + " " +
								                             method.Name + GetMethodParams (method));
								writer.WriteEndElement ();
								continue;
							}

							FieldDefinition field = member as FieldDefinition;
							if (field != null) {
								writer.WriteStartElement("field");
								writer.WriteAttributeString ("signature", field.DeclaringType.FullName + " " + field.Name);
								writer.WriteEndElement ();
							}
						}
						writer.WriteEndElement ();
					}
				}
				
				writer.WriteEndElement ();
			}
			
		}

		protected override void EndProcess ()
		{
			if (writer != null) {
				writer.WriteEndElement ();
				writer.Close ();
				writer = null;
			}
		}

		//this is almost the ToString method of MethodDefinition...
		private string GetMethodParams (MethodDefinition method)
		{
			string @params = "(";
			if (method.HasParameters) {
				@params += method.Parameters [0].ParameterType.FullName;
				for (int i = 0; i < method.Parameters.Count; i++) {
					if (i > 0)
						@params += ",";

					@params += method.Parameters [i].ParameterType.FullName;
				}
			}
			@params += ")";
			return @params;
		}

		Hashtable /*Dictionary<TypeDefinition,List<IAnnotationProvider>*/ ScanAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return null;

			Hashtable members_used = new Hashtable ();
			foreach (TypeDefinition type in assembly.MainModule.Types) {
				if (Annotations.IsMarked (type)) {
					members_used [type] = ScanType (type);
					continue;
				}
			}
			return members_used;
		}

		static IList ScanType (TypeDefinition type)
		{
			return ExtractUsedProviders (type.Methods, type.Constructors, type.Fields);
		}

		static IList /*List<IAnnotationProvider>*/ ExtractUsedProviders (IList methods, IList ctors, IList fields)
		{
			IList used = new ArrayList ();
			ExtractUsedProviders (methods, used);
			ExtractUsedProviders (ctors, used);
			ExtractUsedProviders (fields, used);
			return used;
		}

		static void ExtractUsedProviders (IList members, IList result)
		{
			foreach (IAnnotationProvider provider in members)
				if (Annotations.IsMarked (provider))
					result.Add (provider);
		}

	}
}
