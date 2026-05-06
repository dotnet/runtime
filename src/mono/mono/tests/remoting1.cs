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

public class EmptyProxy : RealProxy
{
	public EmptyProxy ( Type type ) : base( type ) 
	{ 
	}

	public override IMessage Invoke( IMessage msg )
	{
		IMethodCallMessage call = (IMethodCallMessage)msg;

		return new ReturnMessage( null, null, 0, null, call );
	}
}

public struct MyStruct {
	public int a;
	public int b;
	public int c;
}

interface R2 {
}
	
class R1 : MarshalByRefObject, R2 {

	public int test_field = 5;
	public object null_test_field;
	
	public virtual MyStruct Add (int a, out int c, int b) {
		Console.WriteLine ("ADD");
		c = a + b;

		MyStruct res = new MyStruct ();

		res.a = a;
		res.b = b;
		res.c = c;
		
		return res;
	}

	public long nonvirtual_Add (int a, int b) {
		Console.WriteLine ("nonvirtual_Add " + a + " + " + b);
		return a + b;
	}
}

class R3 : MarshalByRefObject {
	public object anObject;
}

class Test {

	delegate MyStruct RemoteDelegate1 (int a, out int c, int b);
	delegate long RemoteDelegate2 (int a, int b);

	static long test_call (R1 o)
	{
		return o.nonvirtual_Add (2, 3);
	}
	
	static int Main () {
		R1 myobj = new R1 ();
		int res = 0;
		long lres;
		
		MyProxy real_proxy = new MyProxy (myobj);

		R1 o = (R1)real_proxy.GetTransparentProxy ();

		if (RemotingServices.IsTransparentProxy (null))
			return 1;
		
		if (!RemotingServices.IsTransparentProxy (o))
			return 2;

		Console.WriteLine ("XXXXXXXXXXXX: " + RemotingServices.GetRealProxy (o));

		if (o.GetType () != myobj.GetType ())
			return 3;

		MyStruct myres = o.Add (2, out res, 3);

		Console.WriteLine ("Result: " + myres.a + " " +
				   myres.b + " " + myres.c +  " " + res);

		if (myres.a != 2)
			return 4;
		
		if (myres.b != 3)
			return 5;
		
		if (myres.c != 5)
			return 6;

		if (res != 5)
			return 7;

		R1 o2 = new R1 ();
		
		lres = test_call (o2);
		
		lres = test_call (o);

		Console.WriteLine ("Result: " + lres);
		if (lres != 5)
			return 8;
		
		lres = test_call (o);

		o.test_field = 2;
		
		int i = o.test_field;  // copy to local variable necessary to avoid CS1690: "Accessing a member on 'member' may cause a runtime exception because it is a field of a marshal-by-reference class"
		Console.WriteLine ("test_field: " + i);
		if (i != 2)
			return 9;

		RemoteDelegate1 d1 = new RemoteDelegate1 (o.Add);
		MyStruct myres2 = d1 (2, out res, 3);

		Console.WriteLine ("Result: " + myres2.a + " " +
				   myres2.b + " " + myres2.c +  " " + res);

		if (myres2.a != 2)
			return 10;
		
		if (myres2.b != 3)
			return 11;
		
		if (myres2.c != 5)
			return 12;

		if (res != 5)
			return 13;

		RemoteDelegate2 d2 = new RemoteDelegate2 (o.nonvirtual_Add);
		d2 (6, 7);

		if (!(real_proxy.GetTransparentProxy () is R2))
			return 14;

		/* Test what happens if the proxy doesn't return the required information */
		EmptyProxy handler = new EmptyProxy ( typeof (R3) );
		R3 o3 = (R3)handler.GetTransparentProxy();

		if (o3.anObject != null)
			return 15;

		if (o.null_test_field != null)
			return 16;

		return 0;
	}
}
