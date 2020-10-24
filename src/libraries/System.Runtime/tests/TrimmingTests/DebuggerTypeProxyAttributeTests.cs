using System;
using System.Diagnostics;

/// <summary>
/// Tests that types used by DebuggerTypeProxyAttribute are not trimmed out
/// when Debugger.IsSupported is true (the default).
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        MyClass myClass = new MyClass() { Name = "trimmed" };
        MyClassWithProxyString myClassWithString = new MyClassWithProxyString() { Name = "trimmed" };

        Type[] allTypes = typeof(MyClass).Assembly.GetTypes();
        bool foundDebuggerProxy = false;
        bool foundStringDebuggerProxy = false;
        for (int i = 0; i < allTypes.Length; i++)
        {
            Type currentType = allTypes[i];
            if (currentType.FullName == "Program+MyClass+DebuggerProxy" &&
                currentType.GetProperties().Length == 1 &&
                currentType.GetConstructors().Length == 1)
            {
                foundDebuggerProxy = true;
            }
            else if (currentType.FullName == "Program+MyClassWithProxyStringProxy" &&
                currentType.GetProperties().Length == 1 &&
                currentType.GetConstructors().Length == 1)
            {
                foundStringDebuggerProxy = true;
            }
        }

        return foundDebuggerProxy && foundStringDebuggerProxy ? 100 : -1;
    }

    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public class MyClass
    {
        public string Name { get; set; }

        private class DebuggerProxy
        {
            private MyClass _instance;

            public DebuggerProxy(MyClass instance)
            {
                _instance = instance;
            }

            public string DebuggerName => _instance.Name + " Proxy";
        }
    }

    [DebuggerTypeProxy("Program+MyClassWithProxyStringProxy")]
    public class MyClassWithProxyString
    {
        public string Name { get; set; }
    }

    internal class MyClassWithProxyStringProxy
    {
        private MyClassWithProxyString _instance;

        public MyClassWithProxyStringProxy(MyClassWithProxyString instance)
        {
            _instance = instance;
        }

        public string DebuggerName => _instance.Name + " StringProxy";
    }
}
