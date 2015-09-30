using System;

class Test
{
    static int Main()
    {
        Lazy<int> l = new Lazy<int>(() => 100);
        Console.WriteLine(l.Value);
        return l.Value;
    }
}