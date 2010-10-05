//
// XApiReader.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
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
using System.Linq;
using System.Text;
using System.Xml.XPath;

using Mono.Cecil;

namespace Mono.Linker {

	public class XApiReader {

		static readonly string _name = "name";
		static readonly string _ns = string.Empty;

		LinkContext _context;
		XPathDocument _document;
		IXApiVisitor _visitor;

		AssemblyDefinition _assembly;
		string _namespace;
		Stack _types = new Stack ();
		StringBuilder _signature;

		public XApiReader (XPathDocument document, IXApiVisitor visitor)
		{
			_document = document;
			_visitor = visitor;
		}

		public void Process (LinkContext context)
		{
			_context = context;
			ProcessAssemblies (_document.CreateNavigator ());
		}

		void OnAssembly (XPathNavigator nav)
		{
			_assembly = GetAssembly (nav);

			_visitor.OnAssembly (nav, _assembly);

			ProcessAttributes (nav);
			ProcessNamespaces (nav);
		}

		AssemblyDefinition GetAssembly (XPathNavigator nav)
		{
			AssemblyNameReference name = new AssemblyNameReference (
				GetName (nav),
				new Version (GetAttribute (nav, "version")));

			AssemblyDefinition assembly = _context.Resolve (name);
			ProcessReferences (assembly);
			return assembly;
		}

		void ProcessReferences (AssemblyDefinition assembly)
		{
			foreach (AssemblyNameReference name in assembly.MainModule.AssemblyReferences)
				_context.Resolve (name);
		}

		void OnAttribute (XPathNavigator nav)
		{
			_visitor.OnAttribute (nav);
		}

		void PushType (TypeDefinition type)
		{
			_types.Push (type);
		}

		TypeDefinition PeekType ()
		{
			return (TypeDefinition) _types.Peek ();
		}

		TypeDefinition PopType ()
		{
			return (TypeDefinition) _types.Pop ();
		}

		void OnNamespace (XPathNavigator nav)
		{
			_namespace = GetName (nav);

			ProcessClasses (nav);
		}

		void OnClass (XPathNavigator nav)
		{
			string name = GetClassName (nav);

			TypeDefinition type = _assembly.MainModule.GetType (name);
			if (type == null)
				return;

			_visitor.OnClass (nav, type);

			PushType (type);

			ProcessAttributes (nav);
			ProcessInterfaces (nav);
			ProcessFields (nav);
			ProcessMethods (nav);
			ProcessConstructors (nav);
			ProcessProperties (nav);
			ProcessEvents (nav);
			ProcessClasses (nav);

			PopType ();
		}

		string GetClassName (XPathNavigator nav)
		{
			if (IsNestedClass ())
				return PeekType ().FullName + "/" + GetName (nav);

			return _namespace + "." + GetName (nav);
		}

		bool IsNestedClass ()
		{
			return _types.Count > 0;
		}

		void OnField (XPathNavigator nav)
		{
			TypeDefinition declaring = PeekType ();

			FieldDefinition field = declaring.Fields.FirstOrDefault (f => f.Name == GetName (nav));
			if (field != null)
				_visitor.OnField (nav, field);

			ProcessAttributes (nav);
		}

		void OnInterface (XPathNavigator nav)
		{
			string name = GetName (nav);

			TypeDefinition type = _context.GetType (GetTypeName (name));
			if (type != null)
				_visitor.OnInterface (nav, type);
		}

		void OnMethod (XPathNavigator nav)
		{
			InitMethodSignature (nav);

			ProcessParameters (nav);

			string signature = GetMethodSignature ();

			MethodDefinition method = GetMethod (signature);
			if (method != null)
				_visitor.OnMethod (nav, method);

			ProcessAttributes (nav);
		}

		MethodDefinition GetMethod (string signature)
		{
			return GetMethod (PeekType ().Methods, signature);
		}

		static MethodDefinition GetMethod (ICollection methods, string signature)
		{
			foreach (MethodDefinition method in methods)
				if (signature == GetSignature (method))
					return method;

			return null;
		}

		static string GetSignature (MethodDefinition method)
		{
			return method.ToString ().Replace ("<", "[").Replace (">", "]");
		}

		string GetMethodSignature ()
		{
			_signature.Append (")");
			return _signature.ToString ();
		}

		void InitMethodSignature (XPathNavigator nav)
		{
			_signature = new StringBuilder ();

			string returntype = GetAttribute (nav, "returntype");
			if (returntype == null || returntype.Length == 0)
				returntype = "System.Void";

			_signature.Append (NormalizeTypeName (returntype));
			_signature.Append (" ");
			_signature.Append (PeekType ().FullName);
			_signature.Append ("::");

			string name = GetName (nav);
			_signature.Append (GetMethodName (name));

			_signature.Append ("(");
		}

		static string GetMethodName (string name)
		{
			return GetStringBefore (name, "(");
		}

		static string NormalizeTypeName (string name)
		{
			return name.Replace ("+", "/").Replace ("<", "[").Replace (">", "]");
		}

		static string GetTypeName (string name)
		{
			return GetStringBefore (NormalizeTypeName (name), "[");
		}

		static string GetStringBefore (string str, string marker)
		{
			int pos = str.IndexOf (marker);
			if (pos == -1)
				return str;

			return str.Substring (0, pos);
		}

		void OnParameter (XPathNavigator nav)
		{
			string type = GetAttribute (nav, "type");
			int pos = int.Parse (GetAttribute (nav, "position"));

			if (pos > 0)
				_signature.Append (",");
			_signature.Append (NormalizeTypeName (type));
		}

		void OnConstructor (XPathNavigator nav)
		{
			InitMethodSignature (nav);

			ProcessParameters (nav);

			string signature = GetMethodSignature ();

			MethodDefinition ctor = GetMethod (signature);
			if (ctor != null)
				_visitor.OnConstructor (nav, ctor);

			ProcessAttributes (nav);
		}

		void OnProperty (XPathNavigator nav)
		{
			string name = GetName (nav);
			TypeDefinition type = PeekType ();

			var property = type.Properties.FirstOrDefault (p => p.Name == name);
			if (property != null)
				_visitor.OnProperty (nav, property);

			ProcessAttributes (nav);
			ProcessMethods (nav);
		}

		void OnEvent (XPathNavigator nav)
		{
			string name = GetName (nav);
			TypeDefinition type = PeekType ();

			EventDefinition evt = type.Events.FirstOrDefault (e => e.Name == name);
			if (evt != null)
				_visitor.OnEvent (nav, evt);

			ProcessAttributes (nav);
		}

		void ProcessAssemblies (XPathNavigator nav)
		{
			ProcessChildren (nav, "assemblies//assembly", new OnChildren (OnAssembly));
		}

		void ProcessAttributes (XPathNavigator nav)
		{
			ProcessChildren (nav, "attributes//attribute", new OnChildren (OnAttribute));
		}

		void ProcessNamespaces (XPathNavigator nav)
		{
			ProcessChildren (nav, "namespaces//namespace", new OnChildren (OnNamespace));
		}

		void ProcessClasses (XPathNavigator nav)
		{
			ProcessChildren (nav, "classes//class", new OnChildren (OnClass));
		}

		void ProcessInterfaces (XPathNavigator nav)
		{
			ProcessChildren (nav, "intefaces//interface", new OnChildren (OnInterface));
		}

		void ProcessFields (XPathNavigator nav)
		{
			ProcessChildren (nav, "fields//field", new OnChildren (OnField));
		}

		void ProcessMethods (XPathNavigator nav)
		{
			ProcessChildren (nav, "methods//method", new OnChildren (OnMethod));
		}

		void ProcessConstructors (XPathNavigator nav)
		{
			ProcessChildren (nav, "constructors//constructor", new OnChildren (OnConstructor));
		}

		void ProcessParameters (XPathNavigator nav)
		{
			ProcessChildren (nav, "parameters//parameter", new OnChildren (OnParameter));
		}

		void ProcessProperties (XPathNavigator nav)
		{
			ProcessChildren (nav, "properties//property", new OnChildren (OnProperty));
		}

		void ProcessEvents (XPathNavigator nav)
		{
			ProcessChildren (nav, "events//event", new OnChildren (OnEvent));
		}

		static void ProcessChildren (XPathNavigator nav, string children, OnChildren action)
		{
			XPathNodeIterator iterator = nav.Select (children);
			while (iterator.MoveNext ())
				action (iterator.Current);
		}

		delegate void OnChildren (XPathNavigator nav);

		static string GetName (XPathNavigator nav)
		{
			return GetAttribute (nav, _name);
		}

		static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, _ns);
		}
	}
}
