// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Cecil;

namespace TLens.Analyzers
{
	abstract class Analyzer
	{
		protected virtual bool RequiredMethodBody => true;

		public void ProcessAssembly (AssemblyDefinition assembly)
		{
			foreach (var type in assembly.MainModule.Types) {
				WalkType (type);
			}
		}

		public abstract void PrintResults (int maxCount);

		void WalkType (TypeDefinition type)
		{
			foreach (var method in type.Methods) {
				if (RequiredMethodBody && !method.HasBody)
					continue;

				ProcessMethod (method);
			}

			if (type.HasNestedTypes) {
				foreach (var nt in type.NestedTypes) {
					WalkType (nt);
				}
			}

			ProcessType (type);
		}

		protected virtual void ProcessMethod (MethodDefinition method)
		{
		}

		protected virtual void ProcessType (TypeDefinition type)
		{
		}

		protected static void PrintHeader (string header)
		{
			var str = new string ('=', header.Length);
			Console.WriteLine (str);
			Console.WriteLine (header);
			Console.WriteLine (str);
			Console.WriteLine ();
		}
	}
}

