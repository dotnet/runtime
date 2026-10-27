// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace ILCompiler.DependencyAnalysisFramework;

public abstract partial class DependencyNodeCore<DependencyContextType>
{
    public interface IDependencySink
    {
        void Add(DependencyNodeCore<DependencyContextType> node, string reason);
        void Add(object node, string reason);
        void Add(DependencyNodeCore<DependencyContextType>.DependencyListEntry dependency);
        void AddRange(params ReadOnlySpan<DependencyNodeCore<DependencyContextType>.DependencyListEntry> dependencies);
    }

    public interface IConditionalDependencySink
    {
        void Add(DependencyNodeCore<DependencyContextType>.CombinedDependencyListEntry dependency);
    }
}
