using System;
using System.Collections;
using System.Collections.Generic;

class Program {
    static void Main() {
        IDictionary dict = new Dictionary<int, string>();
        Console.WriteLine(dict[1] == null ? "NULL" : "FOUND");
    }
}
