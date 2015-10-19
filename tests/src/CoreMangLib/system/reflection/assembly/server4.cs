using System;
using System.Reflection;

//[assembly: AssemblyKeyFile("..\\..\\compatkey.dat")]


public class server4
{
  public int trivial()
  {
	TestLibrary.Logging.WriteLine ("server.trivial");
	return 4;
  }
}
