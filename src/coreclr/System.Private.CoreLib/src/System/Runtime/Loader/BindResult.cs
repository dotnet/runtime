// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Loader
{
    internal struct BindResult
    {
        public struct AttemptResult
        {
            public BinderAssembly? Assembly;
            public int HResult;
            public bool Attempted;
        }

        public bool IsContextBound { get; private set; }
        public BinderAssembly? Assembly { get; private set; }

        private AttemptResult _inContextAttempt;
        private AttemptResult _applicationAssembliesResult;

        public void SetAttemptResult(int hResult, BinderAssembly? assembly, bool isInContext = false)
        {
            ref AttemptResult result = ref (isInContext ? ref _inContextAttempt : ref _applicationAssembliesResult);
            result.HResult = hResult;
            result.Assembly = assembly;
            result.Attempted = true;
        }

        public AttemptResult? GetAttemptResult(bool isInContext = false)
        {
            AttemptResult result = isInContext ? _inContextAttempt : _applicationAssembliesResult;
            return result.Attempted ? result : null;
        }

        public void SetResult(BinderAssembly assembly, bool isInContext = false)
        {
            Assembly = assembly;
            IsContextBound = isInContext;
        }

        public void SetNoResult()
        {
            Assembly = null;
        }
    }
}
