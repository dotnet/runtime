using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var e = Enumerable.Repeat("test", 1).GetEnumerator();
        try { Console.WriteLine("Before: " + e.Current); } catch (Exception ex) { Console.WriteLine("Before exception: " + ex.GetType().Name); }
        Console.WriteLine("Move1: " + e.MoveNext());
        Console.WriteLine("After1: " + e.Current);
        Console.WriteLine("Move2: " + e.MoveNext());
        Console.WriteLine("After2: " + e.Current);
    }
}
