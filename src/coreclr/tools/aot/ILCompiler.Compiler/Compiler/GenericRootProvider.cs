// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler
{
    public class GenericRootProvider<TState> : ICompilationRootProvider
    {
        private readonly TState _state;

        private readonly Action<TState, IRootingServiceProvider> _addRoots;

        public GenericRootProvider(TState state, Action<TState, IRootingServiceProvider> addRootsMethod) =>
            (_state, _addRoots) = (state, addRootsMethod);

        public void AddCompilationRoots(IRootingServiceProvider rootProvider) => _addRoots(_state, rootProvider);
    }
}
