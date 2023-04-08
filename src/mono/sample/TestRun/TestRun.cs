using System;
namespace HelloWorld
{
  class Program
  {
    static int test(){
      throw new System.Exception();
    }
    static int Main()
    {
      Console.WriteLine($"IsMono: {Type.GetType("Mono.RuntimeStructs") != null}");
      Console.WriteLine("Hello RISC-V");
      test();
      return 0;
    }
  }
}
