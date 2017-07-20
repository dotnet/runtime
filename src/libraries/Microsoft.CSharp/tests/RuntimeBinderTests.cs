// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// IVT to "Microsoft.CSharp.RuntimeBinder.Binder", just to use IVT in a test (see: InternalsVisibleToTest below)
[assembly: InternalsVisibleTo("Microsoft.CSharp, PublicKey = 002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")]

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class RuntimeBinderTests
    {
        [Fact]
        public void MultipleUseOfSameLocalInSameScope()
        {
            dynamic d0 = 23;
            dynamic d1 = 14;
            if (d0 == 23)
            {
                dynamic d2 = 19;
                d0 = d0 - d1 + d2;
                Assert.Equal(28, new string(' ', d0).Length);
            }
            dynamic dr = d0 * d1 + d0 + d0 + d0 / d1 - Math.Pow(d1, 2);
            Assert.Equal(254, dr);
        }

        private class Value<T>
        {
            public T Quantity { get; set; }
        }

        private class Holder
        {
            private object _value;

            public void Assign<T>(T value) => _value = value;

            public T Value<T>() => (T)_value;
        }

        [Fact]
        public void GenericNameMatchesPredefined()
        {
            dynamic d = 3;
            dynamic v = new Value<int> {Quantity = d};
            dynamic r = v.Quantity;
            Assert.Equal(3, r);
            dynamic h = new Holder();
            h.Assign<int>(1);
            Assert.Equal(1, h.Value<int>());
            h.Assign(2);
            Assert.Equal(2, h.Value<int>());
        }

        private static class MySite
        {
            public static CallSite<Action<CallSite, object>> mySite;
        }

        public class Class1
        {
            public static string Result = null;

            internal void Foo()
            {
                Result += "CALLED";
            }
        }

        // https://github.com/dotnet/coreclr/issues/7103
        [Fact]
        public void InternalsVisibleToTest()
        {
            Class1 typed = new Class1();

            // make a callsite as if it is contained inside "Microsoft.CSharp.RuntimeBinder.RuntimeBinderException"
            MySite.mySite = CallSite<Action<CallSite, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(
                CSharpBinderFlags.ResultDiscarded,
                "Foo",
                null,
                typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException),
                new CSharpArgumentInfo[]
                {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                }));

            MySite.mySite.Target(MySite.mySite, typed);

            // call should suceed becasue of the IVT to Microsoft.CSharp
            Assert.Equal("CALLED", Class1.Result);

            // make a callsite as if it is contained inside "System.Exception"
            MySite.mySite = CallSite<Action<CallSite, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(
                CSharpBinderFlags.ResultDiscarded,
                "Foo",
                null,
                typeof(System.Exception),
                new CSharpArgumentInfo[]
                {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                }));

            // call should fail because "Foo" is internal to the calling context.
            Assert.Throws<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(
                                                            () => MySite.mySite.Target(MySite.mySite, typed)
                                                         );
        }

        public class OuterType<T>
        {
            public class MyEntity
            {
                public int Id { get; set; }

                public string Name { get; set; }
            }
        }

        [Fact]
        public void AccessMemberOfNonGenericNestedInGeneric()
        {
            Func<dynamic, int> dynamicDelegate = e => e.Id;
            var dto = new OuterType<int>.MyEntity { Id = 1, Name = "Foo" };
            Assert.Equal(1, dynamicDelegate(dto));
        }

    }
}
