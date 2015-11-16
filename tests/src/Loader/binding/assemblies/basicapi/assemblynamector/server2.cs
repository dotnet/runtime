using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyVersion("0.0.0.0")]


#if DESKTOP
public class server2 : MarshalByRefObject
#else
       public class server2 
#endif
{
  public int trivial()
  {
	Console.WriteLine ("server2.trivial");
	Console.WriteLine ("strongly named");
	return 2;
  }
}