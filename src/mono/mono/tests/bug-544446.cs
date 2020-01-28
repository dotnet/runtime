using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Collections.Generic;

class MyProxy : RealProxy {
	readonly MarshalByRefObject target;

	public MyProxy (MarshalByRefObject target) : base (target.GetType())
	{
		this.target = target;
	}

	public override IMessage Invoke (IMessage request) {
		IMethodCallMessage call = (IMethodCallMessage)request;
		return RemotingServices.ExecuteMessage (target, call);
	}
}

class R1 : MarshalByRefObject {

	public void foo (out  Dictionary<string, int> paramAssignmentStatus) {

		paramAssignmentStatus = new Dictionary<string, int> ();
		paramAssignmentStatus.Add ("One", 1);
	}
}

class Test {
	static int Main () {
		MyProxy real_proxy = new MyProxy (new R1 ());
		R1 o = (R1)real_proxy.GetTransparentProxy ();

		Dictionary<string, int> i;
		o.foo (out i);
		if (1 == i["One"])
			return 0;
		return 1;
	}
}
