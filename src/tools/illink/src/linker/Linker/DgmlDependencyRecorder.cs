// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace Mono.Linker
{
	/// <summary>
	/// Class which implements IDependencyRecorder and writes the dependencies into an DGML file.
	/// </summary>
	public class DgmlDependencyRecorder : IDependencyRecorder, IDisposable
	{
		public const string DefaultDependenciesFileName = "linker-dependencies.dgml";
		public Dictionary<string, int> nodeList = new ();
		public HashSet<(string dependent, string dependee, string reason)> linkList = new (); // first element is source, second is target (dependent --> dependee), third is reason

		private readonly LinkContext context;
		private XmlWriter? writer;
		private Stream? stream;

		public DgmlDependencyRecorder (LinkContext context, string? fileName = null)
		{
			this.context = context;

			XmlWriterSettings settings = new XmlWriterSettings {
				Indent = true,
				IndentChars = " "
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
			writer.WriteStartElement ("DirectedGraph", "http://schemas.microsoft.com/vs/2009/dgml");
		}

		public void FinishRecording ()
		{
			Debug.Assert (writer != null);

			writer.WriteStartElement ("Nodes");
			{
				foreach (var pair in nodeList) {
					writer.WriteStartElement ("Node");
					writer.WriteAttributeString ("Id", pair.Value.ToString ());
					writer.WriteAttributeString ("Label", pair.Key);
					writer.WriteEndElement ();
				}
			}
			writer.WriteEndElement ();

			writer.WriteStartElement ("Links");
			{
				foreach (var tup in linkList) {
					writer.WriteStartElement ("Link");
					writer.WriteAttributeString ("Source", nodeList[tup.dependent].ToString ());
					writer.WriteAttributeString ("Target", nodeList[tup.dependee].ToString ());
					writer.WriteAttributeString ("Reason", tup.reason);
					writer.WriteEndElement ();
				}
			}
			writer.WriteEndElement ();

			writer.WriteStartElement ("Properties");
			{
				writer.WriteStartElement ("Property");
				writer.WriteAttributeString ("Id", "Label");
				writer.WriteAttributeString ("Label", "Label");
				writer.WriteAttributeString ("DataType", "String");
				writer.WriteEndElement ();

				writer.WriteStartElement ("Property");
				writer.WriteAttributeString ("Id", "Reason");
				writer.WriteAttributeString ("Label", "Reason");
				writer.WriteAttributeString ("DataType", "String");
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();

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
			RecordDependency (reason.Source, target, reason.Kind);
		}

		public void RecordDependency (object? source, object target, object? reason)
		{
			if (writer == null)
				throw new InvalidOperationException ();

			if (!DependencyRecorderHelper.ShouldRecord (context, source, target))
				return;

			string dependent = DependencyRecorderHelper.TokenString (context, source);
			string dependee = DependencyRecorderHelper.TokenString (context, target);

			// figure out why nodes are sometimes null, are we missing some information in the graph?
			if (!nodeList.ContainsKey (dependent)) AddNode (dependent);
			if (!nodeList.ContainsKey (dependee)) AddNode (dependee);
			if (source != target) {
				AddLink (dependent, dependee, reason);
			}
		}

		private int _nodeIndex;

		void AddNode (string node)
		{
			nodeList.Add (node, _nodeIndex);
			_nodeIndex++;
		}

		void AddLink (string source, string target, object? kind)
		{
			linkList.Add ((source, target, DependencyRecorderHelper.TokenString (context, kind)));
		}

		public void RecordDependency (object source, object target, bool marked)
		{
			throw new NotImplementedException ();
		}
	}
}
