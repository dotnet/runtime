// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassImplementingIEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }
}
