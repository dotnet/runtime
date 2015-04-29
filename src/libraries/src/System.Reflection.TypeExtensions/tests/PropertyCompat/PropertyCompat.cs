using System;
using System.Reflection;
using Xunit;

using MyNamespace1;

public class PropertyCompatTests
{
    [Fact]
    public void PropertyCompat()
    {

        Type t = typeof(Derived);
        PropertyInfo pi = t.GetProperty("GetOnly", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        Assert.False(pi.CanWrite);
        Assert.Null(pi.SetMethod);
        Assert.Throws<ArgumentException>( () => pi.SetValue(new Derived(), 5) );

        return;
    }
}

namespace MyNamespace1
{
    public class Base
    {
        public int GetOnly { get; private set; }
    }

    public class Derived : Base
    {
    }
}
