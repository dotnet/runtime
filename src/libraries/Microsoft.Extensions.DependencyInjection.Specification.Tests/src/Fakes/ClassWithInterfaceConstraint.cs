// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithInterfaceConstraint<T> : IFakeOpenGenericService<T>
        where T : IEnumerable
    {
        public ClassWithInterfaceConstraint(T value) => Value = value;

        public T Value { get; }
    }
}
