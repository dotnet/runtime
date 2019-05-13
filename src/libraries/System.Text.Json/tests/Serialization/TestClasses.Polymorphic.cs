// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class Person : ITestClass
    {
        public string Name { get; set; }
        public Address Address { get; set; }

        public Person()
        {
            Address = new Address();
        }

        public virtual void Initialize()
        {
            Name = "MyName";

            Address = new Address();
            Address.Initialize();
        }

        public virtual void Verify()
        {
            Assert.Equal("MyName", Name);
            Address.Verify();
        }

        public void VerifyNonVirtual()
        {
            Assert.Equal("MyName", Name);
            Address.VerifyNonVirtual();
        }
    }

    public class Address : ITestClass
    {
        public string City { get; set; }

        public virtual void Initialize()
        {
            City = "MyCity";
        }

        public virtual void Verify()
        {
            Assert.Equal("MyCity", City);
        }

        public void VerifyNonVirtual()
        {
            Assert.Equal("MyCity", City);
        }
    }

    public class Customer : Person, ITestClass
    {
        public decimal CreditLimit { get; set; }

        public override void Initialize()
        {
            CreditLimit = 500;
            base.Initialize();
        }

        public override void Verify()
        {
            Assert.Equal(500, CreditLimit);
            base.Verify();
        }

        new public void VerifyNonVirtual()
        {
            Assert.Equal(500, CreditLimit);
        }
    }

    public class UsaCustomer : Customer, ITestClass
    {
        public UsaCustomer() : base()
        {
            Address = new UsaAddress();
        }
    }

    public class UsaAddress : Address, ITestClass
    {
        public string State { get; set; }

        public override void Initialize()
        {
            State = "MyState";
            base.Initialize();
        }

        public override void Verify()
        {
            Assert.Equal("MyState", State);
            base.Verify();
        }
    }

    public class ObjectWithObjectProperties
    {
        public object /*Address*/ Address { get; set; }
        public object /*List<string>*/ List { get; set; }
        public object /*string[]*/ Array { get; set; }
        public object /*IEnumerable<string>*/ IEnumerableT { get; set; }
        public object /*IList<string>*/ IListT { get; set; }
        public object /*ICollection<string>*/ ICollectionT { get; set; }
        public object /*IReadOnlyCollection<string>*/ IReadOnlyCollectionT { get; set; }
        public object /*IReadOnlyList<string>*/ IReadOnlyListT { get; set; }
        public object /*Stack<string>*/ StackT { get; set; }
        public object /*Queue<string>*/ QueueT { get; set; }
        public object /*HashSet<string>*/ HashSetT { get; set; }
        public object /*LinkedList<string>*/ LinkedListT { get; set; }
        public object /*SortedSet<string>*/ SortedSetT { get; set; }
        public object /*IImmutableList<string>*/ IImmutableListT { get; set; }
        public object /*IImmutableStack<string>*/ IImmutableStackT { get; set; }
        public object /*IImmutableQueue<string>*/ IImmutableQueueT { get; set; }
        public object /*IImmutableSet<string>*/ IImmutableSetT { get; set; }
        public object /*ImmutableHashSet<string>*/ ImmutableHashSetT { get; set; }
        public object /*ImmutableList<string>*/ ImmutableListT { get; set; }
        public object /*ImmutableStack<string>*/ ImmutableStackT { get; set; }
        public object /*ImmutableQueue<string>*/ ImmutableQueueT { get; set; }
        public object /*ImmutableSortedSet<string>*/ ImmutableSortedSetT { get; set; }
        public object /*int?*/ NullableInt { get; set; }
        public object /*object*/ Object { get; set; }
        public object /*int?[]*/ NullableIntArray { get; set; }

        public ObjectWithObjectProperties()
        {
            Address = new Address();
            ((Address)Address).Initialize();

            List = new List<string> { "Hello", "World" };
            Array = new string[] { "Hello", "Again" };
            IEnumerableT = new List<string> { "Hello", "World" };
            IListT = new List<string> { "Hello", "World" };
            ICollectionT = new List<string> { "Hello", "World" };
            IReadOnlyCollectionT = new List<string> { "Hello", "World" };
            IReadOnlyListT = new List<string> { "Hello", "World" };
            StackT = new Stack<string>(new List<string> { "Hello", "World" });
            QueueT = new Queue<string>(new List<string> { "Hello", "World" });
            HashSetT = new HashSet<string>(new List<string> { "Hello", "World" });
            LinkedListT = new LinkedList<string>(new List<string> { "Hello", "World" });
            SortedSetT = new SortedSet<string>(new List<string> { "Hello", "World" });
            IImmutableListT = ImmutableList.CreateRange(new List<string> { "Hello", "World" });
            IImmutableStackT = ImmutableStack.CreateRange(new List<string> { "Hello", "World" });
            IImmutableQueueT = ImmutableQueue.CreateRange(new List<string> { "Hello", "World" });
            IImmutableSetT = ImmutableHashSet.CreateRange(new List<string> { "Hello", "World" });
            ImmutableHashSetT = ImmutableHashSet.CreateRange(new List<string> { "Hello", "World" });
            ImmutableListT = ImmutableList.CreateRange(new List<string> { "Hello", "World" });
            ImmutableStackT = ImmutableStack.CreateRange(new List<string> { "Hello", "World" });
            ImmutableQueueT = ImmutableQueue.CreateRange(new List<string> { "Hello", "World" });
            ImmutableSortedSetT = ImmutableSortedSet.CreateRange(new List<string> { "Hello", "World" });

            NullableInt = new int?(42);
            Object = new object();
            NullableIntArray = new int?[] { null, 42, null };
        }
    }
}
