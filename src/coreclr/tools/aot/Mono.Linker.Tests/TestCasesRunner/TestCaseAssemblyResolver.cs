// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestCaseAssemblyResolver : DefaultAssemblyResolver
	{
		private readonly HashSet<IDisposable> itemsToDispose;

		public TestCaseAssemblyResolver ()
		{
			itemsToDispose = new HashSet<IDisposable> ();
		}

		public override AssemblyDefinition? Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			var assembly = base.Resolve (name, parameters);

			if (assembly == null)
				return null;

			// Don't do any caching because the reader parameters could be different each time
			// but we still want to track items that need to be disposed for easy clean up
			itemsToDispose.Add (assembly);

			if (assembly.MainModule.SymbolReader != null)
				itemsToDispose.Add (assembly.MainModule.SymbolReader);
			return assembly;
		}

		protected override void Dispose (bool disposing)
		{
			foreach (var item in itemsToDispose)
				item.Dispose ();

			base.Dispose (disposing);
		}
	}
}
