//
// Tracer.cs
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

using Mono.Cecil;
using Mono.Linker.Steps;
using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Mono.Linker
{
	/// <summary>
	/// Class which implements IDependencyRecorder and writes the dependencies into an XML file.
	/// </summary>
	public class XmlDependencyRecorder : IDependencyRecorder, IDisposable
	{
		public const string DefaultDependenciesFileName = "linker-dependencies.xml.gz";

		private readonly LinkContext context;
		private XmlWriter writer;
		private Stream stream;

		public XmlDependencyRecorder (LinkContext context, string fileName = null)
		{
			this.context = context;

			XmlWriterSettings settings = new XmlWriterSettings {
				Indent = true,
				IndentChars = "\t"
			};

			if (fileName == null)
				fileName = DefaultDependenciesFileName;

			if (string.IsNullOrEmpty (Path.GetDirectoryName (fileName)) && !string.IsNullOrEmpty (context.OutputDirectory)) {
				fileName = Path.Combine (context.OutputDirectory, fileName);
				Directory.CreateDirectory (context.OutputDirectory);
			}

			var depsFile = File.OpenWrite (fileName);

			if (Path.GetExtension (fileName) == ".xml")
				stream = depsFile;
			else
				stream = new GZipStream (depsFile, CompressionMode.Compress);

			writer = XmlWriter.Create (stream, settings);
			writer.WriteStartDocument ();
			writer.WriteStartElement ("dependencies");
			writer.WriteStartAttribute ("version");
			writer.WriteString ("1.2");
			writer.WriteEndAttribute ();
		}

		public void Dispose ()
		{
			if (writer == null)
				return;

			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Flush ();
			writer.Dispose ();
			stream.Dispose ();
			writer = null;
			stream = null;
		}

		public void RecordDependency (object source, object target, bool marked)
		{
			if (!ShouldRecord (source) && !ShouldRecord (target))
				return;

			// This is a hack to work around a quirk of MarkStep that results in outputting ~6k edges even with the above ShouldRecord checks.
			// What happens is that due to the method queueing in MarkStep, the dependency chain is broken in many cases.  And in these cases
			// we end up adding an edge for MarkStep -> <queued Method>
			// This isn't particularly useful information since it's incomplete, but it's especially not useful in ReducedTracing mode when there is one of these for
			// every class library method that was queued.
			if (context.EnableReducedTracing && source is MarkStep && !ShouldRecord (target))
				return;

			// This is another hack to prevent useless information from being logged.  With the introduction of interface sweeping there are a lot of edges such as
			// `e="InterfaceImpl:Mono.Cecil.InterfaceImplementation"` which are useless information.  Ideally we would format the interface implementation into a meaningful format
			// however I don't think that is worth the effort at the moment.
			if (target is InterfaceImplementation)
				return;

			if (source != target) {
				writer.WriteStartElement ("edge");
				if (marked)
					writer.WriteAttributeString ("mark", "1");
				writer.WriteAttributeString ("b", TokenString (source));
				writer.WriteAttributeString ("e", TokenString (target));
				writer.WriteEndElement ();
			}
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

		bool WillAssemblyBeModified (AssemblyDefinition assembly)
		{
			switch (context.Annotations.GetAction (assembly)) {
				case AssemblyAction.Link:
				case AssemblyAction.AddBypassNGen:
				case AssemblyAction.AddBypassNGenUsed:
					return true;
				default:
					return false;
			}
		}

		bool ShouldRecord (object o)
		{
			if (!context.EnableReducedTracing)
				return true;

			if (o is TypeDefinition t)
				return WillAssemblyBeModified (t.Module.Assembly);

			if (o is IMemberDefinition m)
				return WillAssemblyBeModified (m.DeclaringType.Module.Assembly);

			if (o is TypeReference typeRef) {
				var resolved = typeRef.Resolve ();

				// Err on the side of caution if we can't resolve
				if (resolved == null)
					return true;

				return WillAssemblyBeModified (resolved.Module.Assembly);
			}

			if (o is MemberReference mRef) {
				var resolved = mRef.Resolve ();

				// Err on the side of caution if we can't resolve
				if (resolved == null)
					return true;

				return WillAssemblyBeModified (resolved.DeclaringType.Module.Assembly);
			}

			if (o is ModuleDefinition module)
				return WillAssemblyBeModified (module.Assembly);

			if (o is AssemblyDefinition assembly)
				return WillAssemblyBeModified (assembly);

			if (o is ParameterDefinition parameter) {
				if (parameter.Method is MethodDefinition parameterMethodDefinition)
					return WillAssemblyBeModified (parameterMethodDefinition.DeclaringType.Module.Assembly);
			}

			return true;
		}
	}
}
