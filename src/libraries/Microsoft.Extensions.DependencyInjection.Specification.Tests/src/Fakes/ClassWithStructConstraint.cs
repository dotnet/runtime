// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithStructConstraint<T> : IFakeOpenGenericService<T>
        where T : struct
    {
        public T Value { get; }
    }
}
