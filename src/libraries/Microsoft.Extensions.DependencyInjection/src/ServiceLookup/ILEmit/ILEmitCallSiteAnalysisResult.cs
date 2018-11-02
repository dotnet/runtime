// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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