// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace System.Linq.Expressions.Tests
{
    internal class InlinePerCompilationTypeAttribute : DataAttribute
    {
        private static readonly object[] s_boxedBooleans = {false, true};

        private readonly object[] _data;

        public InlinePerCompilationTypeAttribute(params object[] data)
        {
            _data = data;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            // Re-using the arrays would be a nice optimization, and safe since this is internal and we could
            // just not do the sort of uses that would break that, but xUnit pre-loads GetData() results and
            // we'd therefore end up with multiple copies of the last result.
            foreach (object compilationType in s_boxedBooleans)
            {
                object[] withType = new object[_data.Length + 1];
                _data.CopyTo(withType, 0);
                withType[withType.Length - 1] = compilationType;
                yield return withType;
            }
        }
    }
}
