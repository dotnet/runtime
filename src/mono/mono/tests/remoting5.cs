using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

class MyProxy : RealProxy {
	readonly MarshalByRefObject target;

	public MyProxy (MarshalByRefObject target) : base (target.GetType())
	{
		this.target = target;
	}

	public override IMessage Invoke (IMessage request) {
		IMethodCallMessage call = (IMethodCallMessage)request;
		Console.WriteLine ("Invoke " + call.MethodName);

		Console.Write ("ARGS(");
		for (int i = 0; i < call.ArgCount; i++) {
			if (i != 0)
				Console.Write (", ");
			Console.Write (call.GetArgName (i) +  " " +
				       call.GetArg (i));
		}
		Console.WriteLine (")");
		Console.Write ("INARGS(");
		for (int i = 0; i < call.InArgCount; i++) {
			if (i != 0)
				Console.Write (", ");
			Console.Write (call.GetInArgName (i) +  " " +
				       call.GetInArg (i));
		}
		Console.WriteLine (")");

		((R1)target).test_field = 1;
		
		IMethodReturnMessage res = RemotingServices.ExecuteMessage (target, call);

		Console.Write ("RESARGS(");
		for (int i = 0; i < res.ArgCount; i++) {
			if (i != 0)
				Console.Write (", ");
			Console.Write (res.GetArgName (i) +  " " +
				       res.GetArg (i));
		}
		Console.WriteLine (")");		
	
		Console.Write ("RESOUTARGS(");
		for (int i = 0; i < res.OutArgCount; i++) {
			if (i != 0)
				Console.Write (", ");
			Console.Write (res.GetOutArgName (i) +  " " +
				       res.GetOutArg (i));
		}
		Console.WriteLine (")");		
	 
		return res;
	}
}

public struct TestStruct {
  public int F;
}
	
class R1 : MarshalByRefObject {

	public TestStruct S;

	public int test_field = 5;
	
	public virtual int ldfield_test () {

		MyProxy real_proxy = new MyProxy (this);
		R1 o = (R1)real_proxy.GetTransparentProxy ();

		if (o.test_field != 1)
			return 1;

		if (test_field != 1)
			return 1;

		return 0;
	}
}

class Test {
	
	static int Main () {
		R1 myobj = new R1 ();

		// Test ldflda on MarshalByRefObjects
		myobj.S.F = -1;
		if (myobj.S.F != -1)
			return 1;

		return myobj.ldfield_test ();
	}
}
