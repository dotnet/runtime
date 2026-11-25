// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using ILCompiler.DependencyAnalysis;
#if READYTORUN
using ILCompiler.DependencyAnalysis.ReadyToRun;
#endif
using ILCompiler.DependencyAnalysisFramework;
using Internal.Runtime;
using Internal.TypeSystem;

namespace ILCompiler
{

    /// <summary>
    /// This class is responsible for managing emitted data for type maps.
    /// </summary>
    public abstract class TypeMapManager : ICompilationRootProvider
    {
        public virtual ModuleDesc AssociatedModule => null;

        public virtual void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
        }

        internal abstract IEnumerable<IExternalTypeMapNode> GetExternalTypeMaps();

        internal abstract IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps();

        public abstract void AddCompilationRoots(IRootingServiceProvider rootProvider);

        protected abstract bool IsEmpty { get; }

        /// <summary>
        /// Gets a value that indicates whether the manager can have type maps to emit even if it is empty when the header of the image is created.
        /// </summary>
        protected virtual bool CanBecomeNonEmpty => false;

        /// <summary>
        /// Gets a value that indicates whether there are no type maps to emit once the program is analyzed.
        /// </summary>
        internal virtual bool IsEmptyAtEmission => IsEmpty;

        public virtual void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, INativeFormatTypeReferenceProvider commonFixupsTableNode)
        {
            if (IsEmpty && !CanBecomeNonEmpty)
            {
                return; // No type maps to emit
            }

            header.Add(ReadyToRunSectionType.ExternalTypeMaps, new ExternalTypeMapObjectNode(this, commonFixupsTableNode));
            header.Add(ReadyToRunSectionType.ProxyTypeMaps, new ProxyTypeMapObjectNode(this, commonFixupsTableNode));
        }
    }
}
