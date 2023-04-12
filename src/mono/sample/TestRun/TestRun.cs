using System;
namespace HelloWorld
{
  class Program
  {
    static int test(int c){
      return 1 / c;
    }
    static int Main()
    {
      Console.WriteLine($"IsMono: {Type.GetType("Mono.RuntimeStructs") != null}");
      Console.WriteLine("Hello RISC-V");
      // test(0);
      return 0;
    }
  }
}
