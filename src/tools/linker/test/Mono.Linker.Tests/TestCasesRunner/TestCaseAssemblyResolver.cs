﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TestCaseAssemblyResolver : DefaultAssemblyResolver
    {
        readonly HashSet<IDisposable> itemsToDispose;

        public TestCaseAssemblyResolver()
        {
            itemsToDispose = new HashSet<IDisposable>();
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            var assembly = base.Resolve(name, parameters);

            if (assembly == null)
                return null;

            // Don't do any caching because the reader parameters could be different each time
            // but we still want to track items that need to be disposed for easy clean up
            itemsToDispose.Add(assembly);

            if (assembly.MainModule.SymbolReader != null)
                itemsToDispose.Add(assembly.MainModule.SymbolReader);
            return assembly;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var item in itemsToDispose)
                item.Dispose();

            base.Dispose(disposing);
        }
    }
}
