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

        Type[] allTypes = typeof(MyClass).Assembly.GetTypes();
        for (int i = 0; i < allTypes.Length; i++)
        {
            if (allTypes[i].Name.Contains("DebuggerProxy"))
            {
                Type proxyType = allTypes[i];
                if (proxyType.GetProperties().Length == 1 &&
                    proxyType.GetConstructors().Length == 1)
                {
                    return 100;
                }
            }
        }

        // didn't find the proxy type, or it wasn't preserved correctly
        return -1;
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

            public string DebuggerName => _instance.Name;
        }
    }
}
