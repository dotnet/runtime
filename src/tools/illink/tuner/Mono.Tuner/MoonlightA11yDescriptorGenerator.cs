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

using System;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Text.RegularExpressions;
using System.Text;

using System.Xml;
using System.Xml.XPath;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class MoonlightA11yDescriptorGenerator : BaseStep {

		XmlTextWriter writer = null;
		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (assembly.Name.Name == "MoonAtkBridge" || assembly.Name.Name == "System.Windows" ||
			    assembly.Name.Name.Contains ("Dummy"))
				return;

			if (writer == null) {
				if (!Directory.Exists (Context.OutputDirectory))
					Directory.CreateDirectory (Context.OutputDirectory);

				string file_name = "descriptors.xml";
				string file_path = Path.Combine (Context.OutputDirectory, file_name);
				if (File.Exists (file_path))
					File.Delete (file_path);
				FileStream xml_file = new FileStream (file_path, FileMode.OpenOrCreate);
				Console.WriteLine ("Created file {0}", file_name);
				Console.Write ("Writing contents...");

				writer = new XmlTextWriter (xml_file, System.Text.Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				writer.WriteStartElement("linker");
			}

			SortedDictionary <TypeDefinition, IList> types = ScanAssembly (assembly);
			if (types != null && types.Count > 0) {
				writer.WriteStartElement("assembly");
				writer.WriteAttributeString ("fullname", assembly.Name.Name);

				foreach (TypeDefinition type in types.Keys) {
					IList members = types [type];
					if (members != null && members.Count > 0) {
						writer.WriteStartElement("type");
						writer.WriteAttributeString ("fullname", type.FullName);

						foreach (IMetadataTokenProvider member in members) {
							MethodDefinition method = member as MethodDefinition;
							if (method != null) {
								writer.WriteStartElement("method");
								writer.WriteAttributeString ("signature",
								                             method.ReturnType.FullName + " " +
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
				Console.WriteLine ();
			}

		}

		protected override void EndProcess ()
		{
			Console.WriteLine ();

			foreach (FileStream stream in streams)
				stream.Close ();

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
				for (int i = 0; i < method.Parameters.Count; i++) {
					if (i > 0)
						@params += ",";

					@params += method.Parameters [i].ParameterType.FullName;
				}
			}
			@params += ")";
			return @params;
		}

		SortedDictionary<TypeDefinition, IList> /*,List<IAnnotationProvider>>*/ ScanAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return null;

			SortedDictionary<TypeDefinition, IList> members_used = new SortedDictionary<TypeDefinition, IList> (new TypeComparer ());
			foreach (TypeDefinition type in assembly.MainModule.Types) {
				IList used_providers = FilterPublicMembers (ScanType (type));
				if (used_providers.Count > 0)
					members_used [type] = used_providers;
				else if (IsInternal (type, true) &&
				         Annotations.IsMarked (type))
					throw new NotSupportedException (String.Format ("The type {0} is used while its API is not", type.ToString ()));
			}
			return members_used;
		}

		IList ScanType (TypeDefinition type)
		{
			return ExtractUsedProviders (type.Methods, type.Fields);
		}

		static IList FilterPublicMembers (IList members)
		{
			IList new_list = new ArrayList ();
			foreach (MemberReference item in members)
				if (IsInternal (item, true))
					new_list.Add (item);

			return new_list;
		}

		static string [] master_infos = Directory.GetFiles (Environment.CurrentDirectory, "*.info");

		static string FindMasterInfoFile (string name)
		{
			if (master_infos.Length == 0)
				throw new Exception ("No masterinfo files found in current directory");

			foreach (string file in master_infos) {
				if (file.EndsWith (name + ".info"))
					return file;
			}

			return null;
		}

		const string xpath_init = "assemblies/assembly/namespaces/namespace[@name='{0}']/classes/class[@name='{1}']";

		static string GetXPathSearchForType (TypeDefinition type)
		{
			TypeDefinition parent_type = type;
			string xpath = String.Empty;
			while (parent_type.DeclaringType != null) {
				xpath = String.Format ("/classes/class[@name='{0}']", parent_type.Name) + xpath;
				parent_type = parent_type.DeclaringType;
			}
			return String.Format (xpath_init, parent_type.Namespace, parent_type.Name) + xpath;
		}

		static bool IsInternal (MemberReference member, bool master_info)
		{
			TypeDefinition type = null;
			string master_info_file = null;

			if (member is TypeDefinition) {
				type = member as TypeDefinition;
				if (!master_info)
					return (!type.IsNested && !type.IsPublic) ||
					       (type.IsNested && (!type.IsNestedPublic || IsInternal (type.DeclaringType, false)));

				master_info_file = FindMasterInfoFile (type.Module.Assembly.Name.Name);
				if (master_info_file == null)
					return IsInternal (member, false);

				return !NodeExists (master_info_file, GetXPathSearchForType (type));
			}

			type = member.DeclaringType.Resolve ();

			if (IsInternal (type, master_info))
				return true;

			MethodDefinition method = member as MethodDefinition;
			FieldDefinition field = member as FieldDefinition;

			if (field == null && method == null)
				throw new System.NotSupportedException ("Members to scan should be methods or fields");

			if (!master_info) {

				if (method != null)
					return !method.IsPublic;

				return !field.IsPublic;
			}

			master_info_file = FindMasterInfoFile (type.Module.Assembly.Name.Name);
			if (master_info_file == null)
				return IsInternal (member, false);

			string xpath_type = GetXPathSearchForType (type);
			string name;
			if (field != null)
				name = field.Name;
			else {
				name = method.ToString ();

				//lame, I know...
				name = WackyOutArgs (WackyCommas (name.Substring (name.IndexOf ("::") + 2)
				                    .Replace ("/", "+") // nested classes
				                    .Replace ('<', '[').Replace ('>', ']'))); //generic params
			}

			if (field != null || !IsPropertyMethod (method))
				return !NodeExists (master_info_file, xpath_type + String.Format ("/*/*[@name='{0}']", name));

			return !NodeExists (master_info_file, xpath_type + String.Format ("/properties/*/*/*[@name='{0}']", name));
		}

		//at some point I want to get rid of this method and ask cecil's maintainer to spew commas in a uniform way...
		static string WackyCommas (string method)
		{
			string outstring = String.Empty;
			bool square_bracket = false;
			foreach (char c in method) {
				if (c == '[')
					square_bracket = true;
				else if (c == ']')
					square_bracket = false;

				outstring = outstring + c;

				if (c == ',' && !square_bracket)
					outstring = outstring + " ";
			}
			return outstring;
		}

		//ToString() spews & but not 'out' keyword
		static string WackyOutArgs (string method)
		{
			return Regex.Replace (method, @"\w+&", delegate (Match m) { return "out " + m.ToString (); });
		}

		//copied from MarkStep (violating DRY unless I can put this in a better place... Cecil?)
		static bool IsPropertyMethod (MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.Getter) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Setter) != 0;
		}

		static Dictionary<string, XPathNavigator> navs = new Dictionary<string, XPathNavigator> ();
		static List<FileStream> streams = new List<FileStream> ();

		static bool NodeExists (string file, string xpath)
		{
			Console.Write (".");
			//Console.WriteLine ("Looking for node {0} in file {1}", xpath, file.Substring (file.LastIndexOf ("/") + 1));

			XPathNavigator nav = null;
			if (!navs.TryGetValue (file, out nav)) {
				FileStream stream = new FileStream (file, FileMode.Open);
				XPathDocument document = new XPathDocument (stream);
				nav = document.CreateNavigator ();
				streams.Add (stream);
				navs [file] = nav;
			}
			return nav.SelectSingleNode (xpath) != null;
		}

		IList /*List<IAnnotationProvider>*/ ExtractUsedProviders (params IList[] members)
		{
			IList used = new ArrayList ();
			if (members == null || members.Length == 0)
				return used;

			foreach (IList members_list in members)
				foreach (IMetadataTokenProvider provider in members_list)
					if (Annotations.IsMarked (provider))
						used.Add (provider);

			return used;
		}

		class TypeComparer : IComparer <TypeDefinition> {

			public int Compare (TypeDefinition x, TypeDefinition y)
			{
				return string.Compare (x.ToString (), y.ToString ());
			}

		}

	}
}
