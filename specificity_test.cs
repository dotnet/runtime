using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(IsMoreSpecific("App*", "App"));
        Console.WriteLine(IsMoreSpecific("A*", "AB"));
    }

    static bool IsMoreSpecific(string ruleSource, string bestSource)
    {
        if (ruleSource.Length != bestSource.Length)
        {
            return ruleSource.Length > bestSource.Length;
        }
        return true; // fallthrough in actual code returns true eventually if others equal
    }
}
