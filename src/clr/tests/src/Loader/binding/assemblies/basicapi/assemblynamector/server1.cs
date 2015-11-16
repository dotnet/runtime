using System;
using System.Reflection;

//[assembly: AssemblyKeyFile("..\\..\\compatkey.dat")]


public class server1// : MarshalByRefObject 
{
  public int trivial()
  {
	Console.WriteLine ("server1.trivial");
	Console.WriteLine ("simple named");
	return 1;
  }
}