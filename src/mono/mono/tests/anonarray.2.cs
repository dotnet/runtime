using System;
using System.Collections.Generic;

class Program {
  public static void Main()
  {
    // this form of initialisation causes a crash when I try
    // to iterate through the items.
    IEnumerable<IEnumerable<string>> table 
      = new string[][] { 
           new string[] { "1a", "1b" },
           new string[] { "2a", "2b" }
        };

    foreach (IEnumerable<string> row in table) {
      foreach (string cell in row) {
        Console.Write("{0}  ", cell);
      }
      Console.WriteLine();
    }
  }
}
