/*
Tool #1:

	compare file1.dll file2.dll annotations.xml

	file1.dll: This is an assembly created by Microsoft.

	file2.dll: This is a Mono assembly (currently we have none
	that build).

	annotations.xml: contains comments about a class:

	<class name="System.Object">
		<maintainer>
			<email>miguel@ximian.com</email>
			<name>Miguel de Icaza</name>
		</maintainer>
		<status test-suite="no" percent="XX">
	</class>

	That would generate an XML file with all the classes that are
	implemented in the second library.  If there is nothing for a
	given class, it should generate an emtpy group:

	<class name="System.Object">
	</class>

Tool #2:

	Using a Perl script that can grok XML, generate HTML pages
	that we can put on the web site:

		Per assembly status.
		Per maintainer status.
		Per Percent status.

*/

using System;
using System.Collections;
using System.Reflection;
using System.Xml;

namespace Mapper
{
	public class Mapper
	{
                Assembly ms, mono;
                XmlDocument annotations, output;

		public Mapper(string ms_lib, string mono_lib, string annotation)
		{
			Assembly ms = Assembly.LoadFrom (ms_lib);
                        Assembly mono = Assembly.LoadFrom (mono_lib);
                        annotations = new XmlDocument ();
                        annotations.Load (annotation);
                        output = new XmlDocument ();
		}

		void DumpMember (MemberInfo mi)
		{
			string kind;
			string more="";

			switch (mi.MemberType)
				{
				case MemberTypes.Field:
					kind = "field";
					break;
				case MemberTypes.Method:
					if (((MethodInfo)mi).IsSpecialName) {
						return;
					}
					kind = "method";
					more = " signature='" + mi.ToString() +"'";
					break;
				case MemberTypes.Event:
					kind = "event";
					break;
				case MemberTypes.Property:
					kind = "property";
					break;
				default:
					kind = "***UNKOWN***";
					break;
				}
		}

		void DumpType (Type t)
		{
			string kind, name, attrs = "";

			name = t.Name;

			if (t.IsClass) {
				kind = "class";
			} else if (t.IsInterface) {
				kind = "interface";
			} else if (t.IsValueType) {
				kind = "valueType";
			} else if (t.IsEnum) {
				kind = "enum";
			} else return;

			if (t.IsAbstract) {
				attrs += "abstract='true'";
			} else if (t.IsSealed) {
				attrs += "sealed='true'";
			} else if (t.IsCOMObject) {
				attrs += "comobject='true'";
			}

			foreach (Type type in t.GetNestedTypes ()) {
					DumpType (type);
			}

			foreach (FieldInfo field in t.GetFields ()) {
					DumpMember (field);
			}

			foreach (MethodInfo method in t.GetMethods ()) {
				DumpMember (method);
			}

		}
	
		void LoadTypeList (Type [] types)
		{
			foreach (Type t in types) {
			}
		}
	
		public void Map ()
		{
			Type [] types;
			Module [] modules;
                        string name;

			name = ms.GetName ().Name;
			types = ms.GetExportedTypes ();
			modules = ms.GetModules ();

			DumpTypeList (types);
                }

		public static int Main(string[] args)
		{
			Mapper m;
			string basedir = "c:\\WINDOWS\\Microsoft.NET\\Framework\\v1.0.2914\\";

			if (args.Length != 3) {
                                Console.WriteLine ("usage: compare ms_lib.dll mono_lib.dll annotations.xml");
                        }
			try {
        			m = new Mapper (args[0], args[1], args[2]);
				m.Map ();
	        	} catch (Exception e) {
				Console.WriteLine("Error: " + e.ToString ());
			}		
			return 0;
		}
	}
}

