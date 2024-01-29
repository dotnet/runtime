using System;
using System.Diagnostics;

/// <summary>
/// Tests that types used by DebuggerVisualizerAttribute are not trimmed out
/// when Debugger.IsSupported is true (the default).
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        MyClass myClass = new MyClass() { Name = "trimmed" };
        MyClassWithVisualizerString myClassWithString = new MyClassWithVisualizerString() { Name = "trimmed" };

        Type[] allTypes = typeof(MyClass).Assembly.GetTypes();
        bool foundDebuggerVisualizer = false;
        bool foundDebuggerVisualizer2 = false;
        bool foundDebuggerVisualizerObjectSource = false;
        bool foundStringDebuggerVisualizer = false;
        bool foundStringDebuggerVisualizer2 = false;
        bool foundStringDebuggerVisualizerObjectSource = false;
        for (int i = 0; i < allTypes.Length; i++)
        {
            Type currentType = allTypes[i];
            if (currentType.FullName == "Program+MyClass+DebuggerVisualizer" &&
                currentType.GetProperties().Length == 1 &&
                currentType.GetConstructors().Length == 1)
            {
                foundDebuggerVisualizer = true;
            }
            else if (currentType.FullName == "Program+MyClass+DebuggerVisualizer2" &&
                currentType.GetProperties().Length == 1 &&
                currentType.GetConstructors().Length == 1)
            {
                foundDebuggerVisualizer2 = true;
            }
            else if (currentType.FullName == "Program+MyClass+DebuggerVisualizerObjectSource" &&
                currentType.GetProperties().Length == 1 &&
                currentType.GetConstructors().Length == 1)
            {
                foundDebuggerVisualizerObjectSource = true;
            }
            else if (currentType.FullName == "Program+MyClassWithVisualizerStringVisualizer" &&
                currentType.GetProperties().Length == 1 &&
                currentType.GetConstructors().Length == 1)
            {
                foundStringDebuggerVisualizer = true;
            }
            else if (currentType.FullName == "Program+MyClassWithVisualizerStringVisualizer2" &&
                currentType.GetProperties().Length == 1 &&
                currentType.GetConstructors().Length == 1)
            {
                foundStringDebuggerVisualizer2 = true;
            }
            else if (currentType.FullName == "Program+MyClassWithVisualizerStringVisualizerObjectSource" &&
                currentType.GetProperties().Length == 1 &&
                currentType.GetConstructors().Length == 1)
            {
                foundStringDebuggerVisualizerObjectSource = true;
            }
        }

        if (!foundDebuggerVisualizer) return -1;
        if (!foundDebuggerVisualizer2) return -2;
        if (!foundDebuggerVisualizerObjectSource) return -3;
        if (!foundStringDebuggerVisualizer) return -4;
        if (!foundStringDebuggerVisualizer2) return -5;
        if (!foundStringDebuggerVisualizerObjectSource) return -6;

        return 100;
    }

    [DebuggerVisualizer(typeof(DebuggerVisualizer))]
    [DebuggerVisualizer(typeof(DebuggerVisualizer2), typeof(DebuggerVisualizerObjectSource))]
    public class MyClass
    {
        public string Name { get; set; }

        private class DebuggerVisualizer
        {
            private MyClass _instance;

            public DebuggerVisualizer(MyClass instance)
            {
                _instance = instance;
            }

            public string DebuggerName => _instance.Name + " Visualizer";
        }

        private class DebuggerVisualizer2
        {
            private MyClass _instance;

            public DebuggerVisualizer2(MyClass instance)
            {
                _instance = instance;
            }

            public string DebuggerName => _instance.Name + " Visualizer";
        }

        private class DebuggerVisualizerObjectSource
        {
            private MyClass _instance;

            public DebuggerVisualizerObjectSource(MyClass instance)
            {
                _instance = instance;
            }

            public string DebuggerName => _instance.Name + " Visualizer";
        }
    }

    [DebuggerVisualizer("Program+MyClassWithVisualizerStringVisualizer")]
    [DebuggerVisualizer("Program+MyClassWithVisualizerStringVisualizer2", "Program+MyClassWithVisualizerStringVisualizerObjectSource")]
    public class MyClassWithVisualizerString
    {
        public string Name { get; set; }
    }

    internal class MyClassWithVisualizerStringVisualizer
    {
        private MyClassWithVisualizerString _instance;

        public MyClassWithVisualizerStringVisualizer(MyClassWithVisualizerString instance)
        {
            _instance = instance;
        }

        public string DebuggerName => _instance.Name + " StringVisualizer";
    }

    internal class MyClassWithVisualizerStringVisualizer2
    {
        private MyClassWithVisualizerString _instance;

        public MyClassWithVisualizerStringVisualizer2(MyClassWithVisualizerString instance)
        {
            _instance = instance;
        }

        public string DebuggerName => _instance.Name + " StringVisualizer";
    }

    internal class MyClassWithVisualizerStringVisualizerObjectSource
    {
        private MyClassWithVisualizerString _instance;

        public MyClassWithVisualizerStringVisualizerObjectSource(MyClassWithVisualizerString instance)
        {
            _instance = instance;
        }

        public string DebuggerName => _instance.Name + " StringVisualizer";
    }
}
