// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class CallSiteCachingTests
    {
        [Fact]
        public void InlineCache()
        {
            var callSite = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "A", typeof(CallSiteCachingTests), new CSharpArgumentInfo[1]
            {
                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
            }));

            var initialTarget = callSite.Target;
            Assert.Equal((object)initialTarget, callSite.Update);

            object newExpando = CallSiteCachingTests.GetNewExpando(123);
            callSite.Target(callSite, newExpando);

            var newTarget = callSite.Target;

            for (int i = 0; i < 10; i++)
            {
                callSite.Target(callSite, newExpando);

                // rule should not be changing
                Assert.Equal((object)newTarget, callSite.Target);
            }
        }

        [Fact]
        public void L1Cache()
        {
            var callSite = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "A", typeof(CallSiteCachingTests), new CSharpArgumentInfo[1]
                            {
                                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                            }));

            ObjAndRule[] t = new ObjAndRule[200];

            for (int i = 0; i < 10; i++)
            {
                object newExpando = CallSiteCachingTests.GetNewExpando(i);
                callSite.Target(callSite, newExpando);

                t[i].obj = newExpando;
                t[i].rule = callSite.Target;

                if (i > 0)
                {
                    // must not reuse rules for new expandos
                    Assert.NotEqual((object)t[i].rule, t[i - 1].rule);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                var L1 = CallSiteOps.GetRules((dynamic)callSite);

                // L1 must contain rules
                Assert.Equal((object)t[9 - i].rule, L1[i]);
            }
        }

        [Fact]
        public void L2Cache()
        {
            var callSite = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "A", typeof(CallSiteCachingTests), new CSharpArgumentInfo[1]
                            {
                                 CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                            }));

            ObjAndRule[] t = new ObjAndRule[200];

            for (int i = 0; i < 100; i++)
            {
                object newExpando = CallSiteCachingTests.GetNewExpando(i);
                callSite.Target(callSite, newExpando);

                t[i].obj = newExpando;
                t[i].rule = callSite.Target;

                if (i > 0)
                {
                    // must not reuse rules for new expandos
                    Assert.NotEqual((object)t[i].rule, t[i - 1].rule);
                }
            }

            for (int i = 0; i < 100; i++)
            {
                object newExpando = CallSiteCachingTests.GetNewExpando(i);
                callSite.Target(callSite, newExpando);

                // must reuse rules from L2 cache
                Assert.Equal((object)t[i].rule, callSite.Target);
            }
        }

        private static dynamic GetNewExpando(int i)
        {
            dynamic e = new ExpandoObject();
            e.A = i;

            var d = e as IDictionary<string, Object>;
            d.Add(i.ToString(), i);

            return e;
        }

        private struct ObjAndRule
        {
            public object obj;
            public object rule;
        }
    }
}
