// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define TEST_DEFINITION

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class BindingErrors
    {
        private class TypeWithConditional
        {
            [Conditional("TEST_DEFINITION")]
            public virtual void DoNothing()
            {
            }
        }

        private class DerivedTypeWithConditional : TypeWithConditional
        {
            public override void DoNothing()
            {
            }
        }

        private class TypeWithEvent
        {
            public event Action<object, EventArgs> Event;

            protected virtual void OnTrigger(EventArgs e)
            {
                Event?.Invoke(this, e);
            }
        }

        private class TypeWithOverloads
        {
            public void DoNothing(int x, long y)
            {
            }

            public void DoNothing(long x, int y)
            {
            }
        }

        // So the static binder can't decide some cases are/are not static at compilation stage
        class StaticAndInstanceSameName
        {
            public void DoSomething(double d)
            {
            }

            public static void DoSomething(int i)
            {
            }
        }

        private class AmbiguousNumClass
        {
            private readonly int _num;

            public AmbiguousNumClass(int num) => _num = num;

            public static implicit operator double(AmbiguousNumClass nc) => nc._num;

            public static implicit operator decimal(AmbiguousNumClass nc) => nc._num;
        }

        public static T ReturnRef<T>(T item) where T : class
        {
            return item;
        }

        public static T ReturnVal<T>(T item) where T : struct
        {
            return item;
        }

        [Fact]
        public void CannotBindToConditional()
        {
            var obj = new TypeWithConditional();
            obj.DoNothing(); // Confirm can bind statically.
            dynamic d = obj;
            Assert.Throws<RuntimeBinderException>(() => d.DoNothing());
        }

        [Fact]
        public void CannotBindToOverriddenConditional()
        {
            var obj = new DerivedTypeWithConditional();
            obj.DoNothing(); // Confirm can bind statically.
            dynamic d = obj;
            Assert.Throws<RuntimeBinderException>(() => d.DoNothing());
        }

        [Fact]
        public void CannotBindToEventAsProperty()
        {
            dynamic d = new TypeWithEvent();
            Assert.Throws<RuntimeBinderException>(() => d.Event = 3);
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int x = d.Event;
            });
        }

        [Fact]
        public void CannotBindToEventAsInvokable()
        {
            dynamic d = new TypeWithEvent();
            Assert.Throws<RuntimeBinderException>(() => d.Event(new EventArgs()));
        }

        [Fact]
        public void MethodOnNullReference()
        {
            dynamic d = null;
            Assert.Throws<RuntimeBinderException>(() => d.DoStuff());
        }

        [Fact]
        public void PropertyOrFieldOnNullReference()
        {
            dynamic d = null;
            Assert.Throws<RuntimeBinderException>(() => d.Value = 3);
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int x = d.Value;
            });
        }

        [Fact]
        public void CannotConvertVoid()
        {
            dynamic d = new List<int>();
            Assert.Throws<RuntimeBinderException>(() => d.Add(1).ToString());
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int i = d.Add(1);
            });
        }

        [Fact]
        public void MethodGroupLikeProperty()
        {
            dynamic d = new List<int>();
            Assert.Throws<RuntimeBinderException>(() => d.Add = 42);
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int i = d.Add;
            });
        }

        [Fact]
        public void PropertyLikeMethod()
        {
            dynamic d = new List<int>();
            Assert.Throws<RuntimeBinderException>(() => d.Add = 42);
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int i = d.Add;
            });
        }

        [Fact]
        public void AmbiguousOverloadAsTarget()
        {
            dynamic d = new TypeWithOverloads();
            Assert.Throws<RuntimeBinderException>(() => d.DoNothing(1, 2));
        }

        [Fact]
        public void AmbiguousOverloadAsArgument()
        {
            TypeWithOverloads target = new TypeWithOverloads();
            dynamic d = 2;
            Assert.Throws<RuntimeBinderException>(() => target.DoNothing(1, d));
        }

        [Fact]
        public void RefConstraintMethod()
        {
            dynamic d = 3;
            Assert.Throws<RuntimeBinderException>(() => ReturnRef(d));
        }

        [Fact]
        public void ValConstraintMethod()
        {
            dynamic d = "abc";
            dynamic dThis = this; // Or else the fact that d is really object means this can't compile
            Assert.Throws<RuntimeBinderException>(() => dThis.ReturnVal(d));
        }

        [Fact]
        public void ExplicitlyCallOperator()
        {
            dynamic d = 23m;
            Assert.Throws<RuntimeBinderException>(() => d.op_Increment());
        }

        [Fact]
        public void ExplicitlyCallPropertyAccessor()
        {
            dynamic d = "abc";
            Assert.Throws<RuntimeBinderException>(() => d.get_Length());
        }

        [Fact]
        public void CastToStatic()
        {
            CallSite<Func<CallSite, object, object>> site = CallSite<Func<CallSite, object, object>>.Create(
                Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(Binder), GetType()));
            Func<CallSite, object, object> targ = site.Target;
            Assert.Throws<RuntimeBinderException>(() => targ(site, "abc"));
        }

        [Fact]
        public void AmbigousUnaryOp()
        {
            dynamic d = new AmbiguousNumClass(7);
            Assert.Throws<RuntimeBinderException>(() => -d);
        }

        [Fact]
        public void StaticCallOnInstance()
        {
            dynamic d = new StaticAndInstanceSameName();
            d.DoSomething(2.0); // No exception
            Assert.Throws<RuntimeBinderException>(() => d.DoSomething(2));
            d = 2.0;
            new StaticAndInstanceSameName().DoSomething(d); // No exception
            d = 2;
            Assert.Throws<RuntimeBinderException>(() => new StaticAndInstanceSameName().DoSomething(d));
        }

        [Fact]
        public void InstanceCallOnType()
        {
            dynamic d = 2;
            StaticAndInstanceSameName.DoSomething(d); // No exception
            d = 2.0;
            Assert.Throws<RuntimeBinderException>(() => StaticAndInstanceSameName.DoSomething(d));
        }
    }
}
