// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithThrowingEmptyCtor
    {
        public ClassWithThrowingEmptyCtor()
        {
            throw new Exception(nameof(ClassWithThrowingEmptyCtor));
        }
    }
}
