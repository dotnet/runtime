// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define TEST_DEFINITION

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
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
    }
}
