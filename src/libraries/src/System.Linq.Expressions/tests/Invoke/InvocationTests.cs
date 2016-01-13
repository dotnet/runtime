// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class InvocationTests
    {
        public delegate void X(X a);

        [Fact] // [Issue(3224, "https://github.com/dotnet/corefx/issues/3224")]
        public static void SelfApplication()
        {
            // Expression<X> f = x => {};
            Expression<X> f = Expression.Lambda<X>(Expression.Empty(), Expression.Parameter(typeof(X)));
            var a = Expression.Lambda(Expression.Invoke(f, f));

            a.Compile().DynamicInvoke();

            var it = Expression.Parameter(f.Type);
            var b = Expression.Lambda(Expression.Invoke(Expression.Lambda(Expression.Invoke(it, it), it), f));

            b.Compile().DynamicInvoke();
        }
    }
}