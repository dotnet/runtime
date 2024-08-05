// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Mono.Cecil;

namespace Mono.Linker
{
	/// <summary>
	/// Class which implements IDependencyRecorder and writes the dependencies into an XML file.
	/// </summary>
	public class XmlDependencyRecorder : IDependencyRecorder, IDisposable
	{
		public const string DefaultDependenciesFileName = "linker-dependencies.xml";

		private readonly LinkContext context;
		private XmlWriter? writer;
		private Stream? stream;

		public XmlDependencyRecorder (LinkContext context, string? fileName = null)
		{
			this.context = context;

			XmlWriterSettings settings = new XmlWriterSettings {
				Indent = true,
				IndentChars = "\t"
			};

			fileName ??= DefaultDependenciesFileName;

			if (string.IsNullOrEmpty (Path.GetDirectoryName (fileName)) && !string.IsNullOrEmpty (context.OutputDirectory)) {
				fileName = Path.Combine (context.OutputDirectory, fileName);
				Directory.CreateDirectory (context.OutputDirectory);
			}

			var depsFile = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
			stream = depsFile;

			writer = XmlWriter.Create (stream, settings);
			writer.WriteStartDocument ();
			writer.WriteStartElement ("dependencies");
			writer.WriteStartAttribute ("version");
			writer.WriteString ("1.2");
			writer.WriteEndAttribute ();
		}

		public void FinishRecording ()
		{
			Debug.Assert (writer != null);

			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Flush ();
		}

		public void Dispose ()
		{
			if (writer == null)
				return;

			writer.Dispose ();
			stream?.Dispose ();
			writer = null;
			stream = null;
		}

		public void RecordDependency (object target, in DependencyInfo reason, bool marked)
		{
			if (writer == null)
				throw new InvalidOperationException ();

			if (reason.Kind == DependencyKind.Unspecified)
				return;

			// For now, just report a dependency from source to target without noting the DependencyKind.
			RecordDependency (reason.Source, target, marked);
		}

		public void RecordDependency (object? source, object target, bool marked)
		{
			if (writer == null)
				throw new InvalidOperationException ();

			if (!DependencyRecorderHelper.ShouldRecord (context, source) && !DependencyRecorderHelper.ShouldRecord (context, target))
				return;

			// We use a few hacks to work around MarkStep outputting thousands of edges even
			// with the above ShouldRecord checks. Ideally we would format these into a meaningful format
			// however I don't think that is worth the effort at the moment.

			// Prevent useless logging of attributes like `e="Other:Mono.Cecil.CustomAttribute"`.
			if (source is CustomAttribute || target is CustomAttribute)
				return;

			// Prevent useless logging of interface implementations like `e="InterfaceImpl:Mono.Cecil.InterfaceImplementation"`.
			if (source is InterfaceImplementation || target is InterfaceImplementation)
				return;

			if (source != target) {
				writer.WriteStartElement ("edge");
				if (marked)
					writer.WriteAttributeString ("mark", "1");
				writer.WriteAttributeString ("b", DependencyRecorderHelper.TokenString (context, source));
				writer.WriteAttributeString ("e", DependencyRecorderHelper.TokenString (context, target));
				writer.WriteEndElement ();
			}
		}
	}
}
