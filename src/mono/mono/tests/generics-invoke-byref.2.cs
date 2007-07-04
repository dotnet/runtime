using System;
using System.Collections.Generic;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
	  List<string> str = null;
	  
	  object[] methodArgs = new object[] { str };
	  
	  Program p = new Program();
	  p.GetType().GetMethod("TestMethod").Invoke(p, methodArgs);
        }
      
	public Program()
	{
	}

	public void TestMethod(ref List<string> strArg)
	{
	  strArg = new List<string>();
	}
    }
}
