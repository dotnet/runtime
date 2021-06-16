// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Data.Common;
using System.ComponentModel;
using System.Collections;

class Program
{
    static int Main(string[] args)
    {
        DbConnectionStringBuilder2 dcsb2 = new();
        ICustomTypeDescriptor td = dcsb2;
        if (td.GetClassName() != "DbConnectionStringBuilder2")
        {
            throw new Exception("Class name got trimmed");
        }

        if (td.GetComponentName() != "Test Component Name")
        {
            throw new Exception("Component name got trimmed");
        }

        bool foundAttr = false;
        string attrName = "Test" + ((args != null) ? "Attribute" : "");
        foreach (Attribute attr in td.GetAttributes())
        {
            if (attr.GetType().Name == attrName)
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

        return 100;
    }
}

[Test("Test Attribute Value")]
class DbConnectionStringBuilder2 : DbConnectionStringBuilder, IComponent
{
    public Hashtable GetProperties2()
    {
        Hashtable propertyDescriptors = new Hashtable();
        GetProperties(propertyDescriptors);
        return propertyDescriptors;
    }

#pragma warning disable CS0067 // The event is never used
    public event EventHandler Disposed;
#pragma warning restore CS0067

    public string TestProperty { get; set; }
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
