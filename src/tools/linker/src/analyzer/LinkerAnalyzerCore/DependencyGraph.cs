// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// DependencyGraph.cs: linker dependencies graph
//
// Author:
//   Radek Doulik (rodo@xamarin.com)
//
// Copyright 2015 Xamarin Inc (http://www.xamarin.com).
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace LinkerAnalyzer.Core
{
	public class VertexData
	{
		public string value;
		public List<int> parentIndexes;
		public int index;

		public string DepsCount {
			get {
				if (parentIndexes == null || parentIndexes.Count < 1)
					return "";
				return string.Format (" [{0} deps]", parentIndexes.Count);
			}
		}
	}

	public class DependencyGraph
	{
		protected List<VertexData> vertices = new List<VertexData> ();
		public List<VertexData> Types = new List<VertexData> ();
		readonly Dictionary<string, int> indexes = new Dictionary<string, int> ();
		protected Dictionary<string, int> counts = new Dictionary<string, int> ();
		internal SpaceAnalyzer SpaceAnalyzer { get; set; }

		public void Load (string filename)
		{
			Console.WriteLine ("Loading dependency tree from: {0}", filename);

			try {
				using (var fileStream = File.OpenRead (filename)) {
					Load (fileStream);
				}
			} catch (Exception) {
				Console.WriteLine ("Unable to open and read the dependencies.");
				Environment.Exit (1);
			}
		}

		void Load (FileStream fileStream)
		{
			using (XmlReader reader = XmlReader.Create (fileStream)) {
				while (reader.Read ()) {
					switch (reader.NodeType) {
					case XmlNodeType.Element:
						// Console.WriteLine (reader.Name);
						if (reader.Name == "edge" && reader.IsStartElement ()) {
							string b = reader.GetAttribute ("b");
							string e = reader.GetAttribute ("e");
							//Console.WriteLine ("edge value " + b + "  -->  " + e);

							if (e != b) {
								VertexData begin = Vertex (b, true);
								VertexData end = Vertex (e, true);

								end.parentIndexes ??= new List<int> ();
								if (!end.parentIndexes.Contains (begin.index)) {
									end.parentIndexes.Add (begin.index);
									//Console.WriteLine (" end parent index: {0}", end.parentIndexes);
								}
							}
						}
						break;
					default:
						//Console.WriteLine ("node: " + reader.NodeType);
						break;
					}
				}
			}
		}

		public VertexData Vertex (string vertexName, bool create = false)
		{
			if (indexes.TryGetValue (vertexName, out int index))
				return vertices[index];

			if (!create)
				return null;

			index = vertices.Count;
			var vertex = new VertexData () { value = vertexName, index = index };
			vertices.Add (vertex);
			indexes.Add (vertexName, index);
			string prefix = vertexName.Substring (0, vertexName.IndexOf (':'));
			if (counts.TryGetValue (prefix, out var count))
				counts[prefix] = count + 1;
			else
				counts[prefix] = 1;
			//Console.WriteLine ("prefix " + prefix + " count " + counts[prefix]);
			if (prefix == "TypeDef") {
				Types.Add (vertex);
			}

			return vertex;
		}

		public VertexData Vertex (int index)
		{
			return vertices[index];
		}

		IEnumerable<Tuple<VertexData, int>> AddDependencies (VertexData vertex, HashSet<int> reachedVertices, int depth)
		{
			reachedVertices.Add (vertex.index);
			yield return new Tuple<VertexData, int> (vertex, depth);

			if (vertex.parentIndexes == null)
				yield break;

			foreach (var pi in vertex.parentIndexes) {
				var parent = Vertex (pi);
				if (reachedVertices.Contains (parent.index))
					continue;

				foreach (var d in AddDependencies (parent, reachedVertices, depth + 1))
					yield return d;
			}
		}

		public List<Tuple<VertexData, int>> GetAllDependencies (VertexData vertex)
		{
			return new List<Tuple<VertexData, int>> (AddDependencies (vertex, new HashSet<int> (), 0));
		}
	}
}
