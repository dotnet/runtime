// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal readonly struct ILEmitCallSiteAnalysisResult
    {
        public ILEmitCallSiteAnalysisResult(int size) : this()
        {
            Size = size;
        }

        public ILEmitCallSiteAnalysisResult(int size, bool hasScope)
        {
            Size = size;
            HasScope = hasScope;
        }

        public readonly int Size;

        public readonly bool HasScope;

        public ILEmitCallSiteAnalysisResult Add(in ILEmitCallSiteAnalysisResult other) =>
            new ILEmitCallSiteAnalysisResult(Size + other.Size, HasScope | other.HasScope);
    }
}
