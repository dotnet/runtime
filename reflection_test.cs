using System;
using System.Reflection;

class Program {
    private class Inner {
        public long MyField = 42;
    }

    static void Main() {
        object obj = new Inner();
        var field = obj.GetType().GetField("MyField");
        Console.WriteLine(field != null ? "FOUND" : "NULL");
    }
}
