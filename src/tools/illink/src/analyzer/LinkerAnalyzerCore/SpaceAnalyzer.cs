// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// SpaceAnalyzer.cs
//
// Author:
//  Radek Doulik <radou@microsoft.com>
//
// Copyright (C) 2018 Microsoft Corporation (http://www.microsoft.com)
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
using System.Collections.Generic;
using Mono.Cecil;

namespace LinkerAnalyzer.Core
{
	public class SpaceAnalyzer
	{
		readonly string assembliesDirectory;
		readonly List<AssemblyDefinition> assemblies = new List<AssemblyDefinition> ();
		readonly Dictionary<string, int> sizes = new Dictionary<string, int> ();

		public SpaceAnalyzer (string assembliesDirectory)
		{
			this.assembliesDirectory = assembliesDirectory;
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

		static string GetTypeKey (TypeDefinition td)
		{
			if (td == null)
				return "";

			var addAssembly = td.IsNotPublic || IsAssemblyBound (td);

			var addition = addAssembly ? $":{td.Module}" : "";
			return $"{td.MetadataToken.TokenType}:{td}{addition}";
		}

		static string GetKey (MethodDefinition method)
		{
			return $"{method.MetadataToken.TokenType}:{method}";
		}

		int GetMethodSize (MethodDefinition method)
		{
			var key = GetKey (method);
			int msize;

			if (sizes.TryGetValue (key, out msize))
				return msize;

			msize = method.Body.CodeSize;
			msize += method.Name.Length;

			sizes.Add (key, msize);

			return msize;
		}

		int ProcessType (TypeDefinition type)
		{
			int size = type.Name.Length;

			foreach (var field in type.Fields)
				size += field.Name.Length;

			foreach (var method in type.Methods) {
				method.Resolve ();
				if (method.Body != null)
					size += GetMethodSize (method);
			}

			type.Resolve ();
			try {
				sizes.Add (GetTypeKey (type), size);
			} catch (ArgumentException e) {
				Console.WriteLine ($"\nWarning: duplicated type '{type}' scope '{type.Scope}'\n{e}");
			}
			return size;
		}

		public void LoadAssemblies (bool verbose = true)
		{
			if (verbose) {
				ConsoleDependencyGraph.Header ("Space analyzer");
				Console.WriteLine ("Load assemblies from {0}", assembliesDirectory);
			} else
				Console.Write ("Analyzing assemblies .");

			var resolver = new DefaultAssemblyResolver ();
			resolver.AddSearchDirectory (assembliesDirectory);

			int totalSize = 0;
			foreach (var file in System.IO.Directory.GetFiles (assembliesDirectory, "*.dll")) {
				if (verbose)
					Console.WriteLine ($"Analyzing {file}");
				else
					Console.Write (".");

				ReaderParameters parameters = new ReaderParameters () { ReadingMode = ReadingMode.Immediate, AssemblyResolver = resolver };
				var assembly = AssemblyDefinition.ReadAssembly (file, parameters);
				assemblies.Add (assembly);
				foreach (var module in assembly.Modules) {
					foreach (var type in module.Types) {
						totalSize += ProcessType (type);
						foreach (var child in type.NestedTypes)
							totalSize += ProcessType (child);
					}
				}
			}

			if (verbose)
				Console.WriteLine ("Total known size: {0}", totalSize);
			else
				System.Console.WriteLine ();
		}

		public int GetSize (VertexData vertex)
		{
			return sizes.TryGetValue (vertex.value, out var size) ? size : 0;
		}
	}
}
