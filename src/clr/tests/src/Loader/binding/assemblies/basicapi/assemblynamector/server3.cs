using System;
using System.Reflection;

[assembly:	   AssemblyVersionAttribute("1.0.0.0")]

#if DESKTOP
public class server3 : MarshalByRefObject
#else
       public class server3 
#endif
{
  public int trivial()
  {
	Console.WriteLine ("server3.trivial");
	return 3;
  }
}