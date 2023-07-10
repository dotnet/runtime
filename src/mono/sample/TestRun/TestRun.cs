using System;
namespace HelloWorld
{
  class Program
  {
    static string test(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third){
      return string.Concat(first,"-",second,"+",third);
    }

    static int Main()
    {
      string str = test("Hello", "RISCV", "Mono");
      Console.WriteLine($"Data about {str}");
      return 0;
    }
  }
}
