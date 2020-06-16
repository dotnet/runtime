using System;
using System.ComponentModel;

class Program
{
    static int Main(string[] args)
    {
        Type type = TypeDescriptor.ComObjectType;
        Activator.CreateInstance(type);
        return 100;
    }
}
