//
// InjectAttributes.cs
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

using System.Xml.XPath;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class InjectAttributes : BaseStep, IXApiVisitor {

		static string _security = "System.Security";
		static string _safe_critical = Concat (_security, "SecuritySafeCriticalAttribute");
		static string _critical = Concat (_security, "SecurityCriticalAttribute");
		static string _transparent = Concat (_security, "SecurityTransparentAttribute");

		static string Concat (string l, string r)
		{
			return l + "." + r;
		}

		AssemblyDefinition _assembly;
		ICustomAttributeProvider _provider;

		MethodDefinition _safe_critical_ctor;
		MethodDefinition _critical_ctor;
		MethodDefinition _transparent_ctor;

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			_assembly = assembly;

			MatchApi ();
			LoadAdjustments ();
			ProcessInternals ();
		}

		/*
			This step is responsible for injecting the security attributes in
			the tuned assemblies. It's a three parts operation:

			- we apply the attributes from we found in the public api, by reading
				the xml-api-info files.

			- we load an external file manually maintained wich details the security attributes
				we know we have to apply on certain metadata elements.

			- we apply the attributes on the internals of the assembly.
		*/

		void MatchApi ()
		{
			XPathDocument xapi = XApiService.GetApiInfoByAssemblyName (_assembly.Name.Name);
			if (xapi == null)
				return;

			XApiReader reader = new XApiReader (xapi, this);
			reader.Process (Context);
		}

		void LoadAdjustments ()
		{
		}

		void ProcessInternals ()
		{
		}

		MethodDefinition GetDefaultConstructor (TypeDefinition type)
		{
			foreach (MethodDefinition ctor in type.Constructors)
				if (ctor.Parameters.Count == 0)
					return ctor;

			return null;
		}

		MethodDefinition GetSafeCriticalCtor ()
		{
			if (_safe_critical_ctor != null)
				return _safe_critical_ctor;

			_safe_critical_ctor = GetDefaultConstructor (Context.GetType (_safe_critical));
			return _safe_critical_ctor;
		}

		MethodDefinition GetCriticalCtor ()
		{
			if (_critical_ctor != null)
				return _critical_ctor;

			_critical_ctor = GetDefaultConstructor (Context.GetType (_critical));
			return _critical_ctor;
		}

		MethodDefinition GetTransparentCtor ()
		{
			if (_transparent_ctor != null)
				return _transparent_ctor;

			_transparent_ctor = GetDefaultConstructor (Context.GetType (_transparent));
			return _transparent_ctor;
		}

		MethodReference Import (MethodDefinition method)
		{
			return _assembly.MainModule.Import (method);
		}

		CustomAttribute CreateSafeCriticalAttribute ()
		{
			return new CustomAttribute (Import (GetSafeCriticalCtor ()));
		}

		CustomAttribute CreateCriticalAttribute ()
		{
			return new CustomAttribute (Import (GetCriticalCtor ()));
		}

		CustomAttribute CreateTransparentAttribute ()
		{
			return new CustomAttribute (Import (GetTransparentCtor ()));
		}

		public void OnAttribute (XPathNavigator nav)
		{
			CustomAttribute attribute = null;
			string name = nav.GetAttribute ("name", string.Empty);

			if (name == _safe_critical)
				attribute = CreateSafeCriticalAttribute ();
			else if (name == _critical)
				attribute = CreateCriticalAttribute ();
			else if (name == _transparent)
				attribute = CreateTransparentAttribute ();

			if (attribute != null)
				_provider.CustomAttributes.Add (attribute);
		}

		public void OnAssembly (XPathNavigator nav, AssemblyDefinition assembly)
		{
			_provider = assembly;
		}

		public void OnClass (XPathNavigator nav, TypeDefinition type)
		{
			_provider = type;
		}

		public void OnInterface (XPathNavigator nav, TypeDefinition type)
		{
		}

		public void OnField (XPathNavigator nav, FieldDefinition field)
		{
			_provider = field;
		}

		public void OnMethod (XPathNavigator nav, MethodDefinition method)
		{
			_provider = method;
		}

		public void OnConstructor (XPathNavigator nav, MethodDefinition method)
		{
			_provider = method;
		}

		public void OnProperty (XPathNavigator nav, PropertyDefinition property)
		{
			_provider = property;
		}

		public void OnEvent (XPathNavigator nav, EventDefinition evt)
		{
			_provider = evt;
		}
	}
}
