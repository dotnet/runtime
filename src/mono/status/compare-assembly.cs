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
namespace Mapper
{
    using System;
	using System.Collections;
	using System.Reflection;

    /// <summary>
    ///    Summary description for Class1.
    /// </summary>
    public class Mapper
    {
		Assembly a;
		Hashtable nshash = new Hashtable();
		int indent = 0;

        public Mapper(string name)
		{
			a = Assembly.LoadFrom (name);
		}

		void o (string s)
		{
			Console.WriteLine (s.PadLeft (s.Length + indent, ' '));
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

			o ("<" + kind + " name='" + mi.Name + "'" + more + "/>");
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

			o ("<" + kind + " name='" + name + (attrs == "" ? "'" : "' ") + attrs + ">");

			indent += 4;

			/*o ("<maintainer></maintainer>");
			o ("<description></description>");*/

			foreach (Type type in t.GetNestedTypes ())
			{
				DumpType(type);
			}

			foreach (FieldInfo field in t.GetFields ())
			{
				DumpMember (field);
			}

			foreach (MethodInfo method in t.GetMethods ())
			{
				DumpMember (method);
			}

			indent -= 4;

			o ("</" + kind + ">");
		}
	
		void LoadTypeList (Type [] types)
		{
			foreach (Type t in types)
			{
				ArrayList list = (ArrayList) nshash [t.Namespace];
				if (list == null)
				{
					list = new ArrayList ();
					nshash.Add (t.Namespace, list);
				}
				list.Add (t);
			}
		}
	
		void DumpTypeList (Type [] types)
		{
			LoadTypeList (types);

			foreach (string ns in nshash.Keys)
			{
				o ("<namespace " + "name='" + ns + "'>");

				indent += 4;

				foreach (Type t in (ArrayList) nshash [ns])
				{
					DumpType (t);
				}

				indent -= 4;

				o ("</namespace>");
			}
		}

		public void Map ()
		{
			string name;
			Type [] types;
			Module [] modules;

			name = a.GetName ().Name;
			types = a.GetExportedTypes ();
			modules = a.GetModules ();

			o ("<assembly name='" + name + "'>");

			indent += 4;

			/*o ("<maintainer></maintainer>");
			o ("<description></description>");*/

			DumpTypeList (types);

			indent -= 4;

			o ("</assembly>");
		}

		public static int Main(string[] args)
        {
			Mapper m;
			string basedir = "c:\\WINDOWS\\Microsoft.NET\\Framework\\v1.0.2914\\";

			if (args.Length > 0) {
				foreach (string s in args){
					try {
						m = new Mapper (s);
						m.Map ();
					} catch (Exception e) {
						Console.WriteLine("Error: "+e.ToString());
					}
				}
			} else {
					try {
				m = new Mapper (basedir + "mscorlib.dll");
				m.Map ();
					} catch (Exception e) {
						Console.WriteLine("Error: "+e.ToString());
					}
			}

            return 0;
        }
    }
}

