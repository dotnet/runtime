
using System;
using System.Threading.Tasks;
namespace DebuggerTests;

public class GenericCustomAttribute
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomAttribute<TInterface> : Attribute
    {
    }

    [Custom<BadClass>]
    public class BadClass
    {
        public static void PauseInside()
        {
            Console.WriteLine("Test");
        }
    }

    public static void TestCustomAttribute()
    {
        BadClass.PauseInside();
    }
}
