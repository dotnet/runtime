// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace ILCompiler.DependencyAnalysisFramework;

public interface IDependencySink<DependencyContextType>
{
    void Add(DependencyNodeCore<DependencyContextType> node, string reason);
    void Add(object node, string reason);
    void Add(DependencyNodeCore<DependencyContextType>.DependencyListEntry dependency);
    void AddRange(params ReadOnlySpan<DependencyNodeCore<DependencyContextType>.DependencyListEntry> dependencies);
}

public interface IConditionalDependencySink<DependencyContextType>
{
    void Add(DependencyNodeCore<DependencyContextType>.CombinedDependencyListEntry dependency);
}
