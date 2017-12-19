//
// Tracer.cs
//
// Author:
//  Radek Doulik <radou@microsoft.com>
//
// Copyright (C) 2017 Microsoft Corporation (http://www.microsoft.com)
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

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using Mono.Cecil;

namespace Mono.Linker
{
	public class Tracer {

		public string DependenciesFileName { get; set; } = "linker-dependencies.xml.gz";

		Stack<object> dependency_stack;
		System.Xml.XmlWriter writer;
		GZipStream zipStream;

		public void Start ()
		{
			dependency_stack = new Stack<object> ();
			System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings {
				Indent = true,
				IndentChars = "\t"
			};
			var depsFile = File.OpenWrite (DependenciesFileName);
			zipStream = new GZipStream (depsFile, CompressionMode.Compress);

			writer = System.Xml.XmlWriter.Create (zipStream, settings);
			writer.WriteStartDocument ();
			writer.WriteStartElement ("dependencies");
			writer.WriteStartAttribute ("version");
			writer.WriteString ("1.2");
			writer.WriteEndAttribute ();
		}

		public void Finish ()
		{
			if (writer == null)
				return;

			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Flush ();
			writer.Dispose ();
			zipStream.Dispose ();
			writer = null;
			zipStream = null;
			dependency_stack = null;
		}

		public void Push (object o, bool addDependency = true)
		{
			if (writer == null)
				return;

			if (addDependency && dependency_stack.Count > 0)
				AddDependency (o);

			dependency_stack.Push (o);
		}

		public void Pop ()
		{
			if (writer == null)
				return;

			dependency_stack.Pop ();
		}

		static bool IsAssemblyBound (TypeDefinition td)
		{
			do {
				if (td.IsNestedPrivate || td.IsNestedAssembly || td.IsNestedFamilyAndAssembly)
					return true;

				td = td.DeclaringType;
			} while (td != null);

			return false;
		}

		string TokenString (object o)
		{
			if (o == null)
				return "N:null";

			if (o is TypeReference t) {
				bool addAssembly = true;
				var td = t as TypeDefinition ?? t.Resolve ();

				if (td != null) {
					addAssembly = td.IsNotPublic || IsAssemblyBound (td);
					t = td;
				}

				var addition = addAssembly ? $":{t.Module}" : "";

				return $"{(o as IMetadataTokenProvider).MetadataToken.TokenType}:{o}{addition}";
			}

			if (o is IMetadataTokenProvider)
				return (o as IMetadataTokenProvider).MetadataToken.TokenType + ":" + o;

			return "Other:" + o;
		}

		public void AddDirectDependency (object b, object e)
		{
			if (writer == null)
				return;

			writer.WriteStartElement ("edge");
			writer.WriteAttributeString ("b", TokenString (b));
			writer.WriteAttributeString ("e", TokenString (e));
			writer.WriteEndElement ();
		}

		public void AddDependency (object o, bool marked = false)
		{
			if (writer == null)
				return;

			KeyValuePair<object, object> pair = new KeyValuePair<object, object> (dependency_stack.Count > 0 ? dependency_stack.Peek () : null, o);
			if (pair.Key != pair.Value) {
				writer.WriteStartElement ("edge");
				if (marked)
					writer.WriteAttributeString ("mark", "1");
				writer.WriteAttributeString ("b", TokenString (pair.Key));
				writer.WriteAttributeString ("e", TokenString (pair.Value));
				writer.WriteEndElement ();
			}
		}
	}
}
