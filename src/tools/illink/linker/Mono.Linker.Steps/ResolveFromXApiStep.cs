//
// ResolveFromXApiStep.cs
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

using System.Xml.XPath;

using Mono.Linker;

using Mono.Cecil;

namespace Mono.Linker.Steps {

	public class ResolveFromXApiStep : ResolveStep, IXApiVisitor {

		static readonly string _name = "name";
		static readonly string _ns = string.Empty;

		XPathDocument _document;

		public ResolveFromXApiStep (XPathDocument document)
		{
			_document = document;
		}

		protected override void Process ()
		{
			XApiReader reader = new XApiReader (_document, this);
			reader.Process (Context);
		}

		public void OnAssembly (XPathNavigator nav, AssemblyDefinition assembly)
		{
		}

		public void OnAttribute (XPathNavigator nav)
		{
			string name = GetName (nav);

			TypeDefinition type = Context.GetType (name);
			if (type != null)
				MarkType (type);
		}

		public void OnClass (XPathNavigator nav, TypeDefinition type)
		{
			MarkType (type);
		}

		public void OnInterface (XPathNavigator nav, TypeDefinition type)
		{
			MarkType (type);
		}

		public void OnField (XPathNavigator nav, FieldDefinition field)
		{
			MarkField (field);
		}

		public void OnMethod (XPathNavigator nav, MethodDefinition method)
		{
			MarkMethod (method);
		}

		public void OnConstructor (XPathNavigator nav, MethodDefinition method)
		{
			MarkMethod (method);
		}

		public void OnProperty (XPathNavigator nav, PropertyDefinition property)
		{
		}

		public void OnEvent (XPathNavigator nav, EventDefinition evt)
		{
			if (evt.AddMethod != null)
				MarkMethod (evt.AddMethod);
			if (evt.InvokeMethod != null)
				MarkMethod (evt.InvokeMethod);
			if (evt.RemoveMethod != null)
				MarkMethod (evt.RemoveMethod);
		}

		static string GetName (XPathNavigator nav)
		{
			return GetAttribute (nav, _name);
		}

		static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, _ns);
		}

		void MarkType (TypeDefinition type)
		{
			InternalMark (type);
		}

		void MarkField (FieldDefinition field)
		{
			InternalMark (field);
		}

		void InternalMark (IMetadataTokenProvider provider)
		{
			Annotations.Mark (provider);
			Annotations.SetPublic (provider);
		}

		void MarkMethod (MethodDefinition method)
		{
			InternalMark (method);
			Annotations.SetAction (method, MethodAction.Parse);
		}
	}
}
