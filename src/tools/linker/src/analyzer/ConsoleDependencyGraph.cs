// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// ConsoleDependencyGraph.cs: text output related code for dependency graph
//
// Author:
//   Radek Doulik (rodo@xamarin.com)
//
// Copyright 2015 Xamarin Inc (http://www.xamarin.com).
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LinkerAnalyzer.Core;

namespace LinkerAnalyzer
{
	public class ConsoleDependencyGraph : DependencyGraph
	{
		public bool Tree;
		public bool FlatDeps;

		public void ShowDependencies (string raw, List<VertexData> verticesList, string searchString)
		{
			VertexData vertex = Vertex (raw);
			if (vertex == null) {
				Regex regex = new Regex (searchString);
				int count = 0;

				foreach (var v in verticesList) {
					if (regex.Match (v.value) != Match.Empty) {
						ShowDependencies (v);
						count++;
					}
				}

				if (count == 0)
					Console.WriteLine ("\nUnable to find vertex: {0}", raw);
				else
					Console.WriteLine ("\nFound {0} matches", count);
			} else
				ShowDependencies (vertex);
		}

		void ShowFlatDependencies (VertexData vertex)
		{
			bool first = true;
			var flatDeps = GetAllDependencies (vertex);

			Console.WriteLine ();

			foreach (var d in flatDeps) {
				var dSize = SpaceAnalyzer == null ? 0 : SpaceAnalyzer.GetSize (d.Item1);
				if (first) {
					var sizeStr = dSize > 0 ? $" [size: {dSize}]" : "";
					Console.WriteLine ($"Distance | {d.Item1.value} [total deps: {flatDeps.Count}]{sizeStr}");
					Line ();
					first = false;
					continue;
				}
				var sizeStr2 = dSize > 0 ? $" [size: {dSize}]" : "";
				Console.WriteLine ($"{string.Format ("{0,8}", d.Item2)} | {d.Item1.value}{d.Item1.DepsCount}{sizeStr2}");
			}
		}

		string SizeString (VertexData vertex)
		{
			return SpaceAnalyzer == null ?
				"" : string.Format (" size: {0}", SpaceAnalyzer.GetSize (vertex));
		}

		public void ShowDependencies (VertexData vertex)
		{
			if (FlatDeps) {
				ShowFlatDependencies (vertex);

				return;
			}

			Header ("{0} dependencies", vertex.value);
			if (vertex.parentIndexes == null) {
				Console.WriteLine ("Root dependency");
			} else {
				int i = 0;
				var visited = new HashSet<int> ();

				foreach (int index in vertex.parentIndexes) {
					Console.WriteLine ("Dependency #{0}", ++i);
					Console.WriteLine ($"\t{vertex.value}{SizeString (vertex)}");

					var childVertex = Vertex (index);
					Console.WriteLine ("\t| {0}{1}", childVertex.value, childVertex.DepsCount);

					visited.Clear ();
					visited.Add (index);

					while (childVertex.parentIndexes != null) {
						int pi = 0, childIdx;

						do {
							childIdx = childVertex.parentIndexes[pi];
							pi++;
						} while (visited.Contains (childIdx) && pi < childVertex.parentIndexes.Count);

						childVertex = Vertex (childIdx);

						if (visited.Contains (childIdx)) {
							Console.WriteLine ($"\twarning: loop to {childVertex.value}");
							break;
						}

						visited.Add (childIdx);
						Console.WriteLine ("\t| {0}{1}", childVertex.value, childVertex.DepsCount);
					}

					if (Tree)
						break;
				}
			}
		}

		public void ShowAllDependencies ()
		{
			Header ("All dependencies");
			Console.WriteLine ("Types count: {0}", vertices.Count);
			foreach (var vertex in vertices)
				ShowDependencies (vertex);
		}

		public void ShowTypesDependencies ()
		{
			Header ("All types dependencies");
			Console.WriteLine ("Deps count: {0}", Types.Count);
			foreach (var type in Types)
				ShowDependencies (type);
		}

		static string Tabs (string key)
		{
			int count = Math.Max (1, 2 - key.Length / 8);

			if (count == 1)
				return "\t";
			else
				return "\t\t";
		}

		public void ShowStat (bool verbose = false)
		{
			Header ("Statistics");
			if (verbose) {
				foreach (var key in counts.Keys)
					Console.WriteLine ("Vertex type:\t{0}{1}count:{2}", key, Tabs (key), counts[key]);
			} else {
				Console.WriteLine ("Assemblies:\t{0}", counts["Assembly"]);
				Console.WriteLine ("Modules:\t{0}", counts["Module"]);
				Console.WriteLine ("Types:\t\t{0}", counts["TypeDef"]);
				Console.WriteLine ("Fields:\t\t{0}", counts["Field"]);
				Console.WriteLine ("Methods:\t{0}", counts["Method"]);
			}

			Console.WriteLine ();
			Console.WriteLine ("Total vertices: {0}", vertices.Count);
		}

		public void ShowRoots ()
		{
			Header ("Root vertices");

			int count = 0;
			foreach (var vertex in vertices) {
				if (vertex.parentIndexes == null) {
					Console.WriteLine ("{0}", vertex.value);
					count++;
				}
			}

			Console.WriteLine ();
			Console.WriteLine ("Total root vertices: {0}", count);
		}

		public void ShowRawDependencies (string raw)
		{
			Header ("Raw dependencies: '{0}'", raw);
			ShowDependencies (raw, vertices, raw);
		}

		public void ShowTypeDependencies (string raw)
		{
			Header ("Type dependencies: '{0}'", raw);
			ShowDependencies ("TypeDef:" + raw, Types, raw);
		}

		static readonly string line = new string ('-', 72);

		static void Line ()
		{
			Console.Write (line);
			Console.WriteLine ();
		}

		public static void Header (string header, params object[] values)
		{
			string formatted = string.Format (header, values);
			Console.WriteLine ();
			Console.WriteLine ($"--- {formatted} {new string ('-', Math.Max (3, 67 - formatted.Length))}");
		}
	}
}
