// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// An abstract assembly stub node whose instances are discoverable by string key.
    /// All instances present in the dependency graph are collected and emitted as a single
    /// READYTORUN_FIXUP_InjectStringThunks eager fixup, mapping each LookupString to
    /// the code address of the stub in the R2R image.
    /// </summary>
    public abstract class StringDiscoverableAssemblyStubNode : AssemblyStubNode
    {
        /// <summary>
        /// The string key used to look up this stub at runtime via LookupPregeneratedThunkByString.
        /// Must be non-empty and must not contain embedded null characters.
        /// </summary>
        public abstract string LookupString { get; }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();
            dependencies.Add(factory.InjectStringThunksImport, "StringDiscoverableAssemblyStubNode requires InjectStringThunks fixup");

            return dependencies;
        }

        protected override void OnMarked(NodeFactory factory)
        {
            Debug.Assert(!string.IsNullOrEmpty(LookupString), "LookupString must be non-empty");
            Debug.Assert(!LookupString.Contains('\0'), "LookupString must not contain embedded null characters");
            factory.RegisterStringDiscoverableStub(this);
        }
    }
}
