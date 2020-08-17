// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassImplementingIEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }
}
