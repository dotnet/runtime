// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Data.Common;
using System.ComponentModel;
using System.Collections;

namespace DbConnectionStringBuilderTrimmingTests
{
    class Program
    {
        static int Main(string[] args)
        {
            DbConnectionStringBuilder2 dcsb2 = new();
            ICustomTypeDescriptor td = dcsb2;

            if (td.GetClassName() != "DbConnectionStringBuilderTrimmingTests.DbConnectionStringBuilder2")
            {
                throw new Exception("Class name got trimmed");
            }

            if (td.GetComponentName() != "Test Component Name")
            {
                throw new Exception("Component name got trimmed");
            }

            bool foundAttr = false;

            foreach (Attribute attr in td.GetAttributes())
            {
                if (attr.GetType().Name == "TestAttribute")
                {
                    if (attr.ToString() != "Test Attribute Value")
                    {
                        throw new Exception("Test attribute value differs");
                    }

                    if (foundAttr)
                    {
                        throw new Exception("More than one attribute found");
                    }

                    foundAttr = true;
                }
            }

            if (!foundAttr)
            {
                throw new Exception("Attribute not found");
            }

            bool foundEvent = false;
            bool foundEventWithDisplayName = false;

            foreach (EventDescriptor ev in td.GetEvents())
            {
                if (ev.DisplayName == "TestEvent")
                {
                    if (foundEvent)
                    {
                        throw new Exception("More than one event TestEvent found.");
                    }

                    foundEvent = true;
                }

                if (ev.DisplayName == "Event With DisplayName")
                {
                    if (foundEventWithDisplayName)
                    {
                        throw new Exception("More than one event with display name found.");
                    }

                    foundEventWithDisplayName = true;
                }
            }

            if (!foundEvent)
            {
                throw new Exception("Event not found");
            }

            if (!foundEventWithDisplayName)
            {
                throw new Exception("Event with DisplayName not found");
            }

            return 100;
        }
    }

    [Test("Test Attribute Value")]
    class DbConnectionStringBuilder2 : DbConnectionStringBuilder, IComponent
    {
#pragma warning disable CS0067 // The event is never used
        public event EventHandler Disposed;
        public event Action TestEvent;
        [DisplayName("Event With DisplayName")]
        public event Action TestEvent2;
#pragma warning restore CS0067

        public ISite Site { get => new TestSite(); set => throw new NotImplementedException(); }
        public void Dispose() { }
    }

    class TestSite : INestedSite
    {
        public string FullName => null;
        public IComponent Component => throw new NotImplementedException();
        public IContainer Container => throw new NotImplementedException();
        public bool DesignMode => throw new NotImplementedException();
        public string Name { get => "Test Component Name"; set => throw new NotImplementedException(); }
        public object GetService(Type serviceType) => null;
    }

    class TestAttribute : Attribute
    {
        public string Test { get; private set; }

        public TestAttribute(string test)
        {
            Test = test;
        }

        public override string ToString()
        {
            return Test;
        }
    }
}
