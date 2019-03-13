// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
}
