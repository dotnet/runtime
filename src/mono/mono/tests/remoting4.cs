using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization;

namespace RemotingTest
{
	class MyProxy : RealProxy 
	{
		readonly MarshalByRefObject target;

		public MyProxy (MarshalByRefObject target) : base (target.GetType()) 
		{
			this.target = target;
		}

		public override IMessage Invoke (IMessage request) 
		{
			IMethodCallMessage call = (IMethodCallMessage)request;
			Console.WriteLine ("Invoke " + call.MethodName);

			Console.Write ("ARGS(");
			for (int i = 0; i < call.ArgCount; i++) 
			{
				if (i != 0)
					Console.Write (", ");
				Console.Write (call.GetArgName (i) +  " " +
					call.GetArg (i));
			}
			Console.WriteLine (")");
			Console.Write ("INARGS(");
			for (int i = 0; i < call.InArgCount; i++) 
			{
				if (i != 0)
					Console.Write (", ");
				Console.Write (call.GetInArgName (i) +  " " +
					call.GetInArg (i));
			}
			Console.WriteLine (")");

			IMethodReturnMessage res = RemotingServices.ExecuteMessage (target, call);

			Console.Write ("RESARGS(");
			for (int i = 0; i < res.ArgCount; i++) 
			{
				if (i != 0)
					Console.Write (", ");
				Console.Write (res.GetArgName (i) +  " " +
					res.GetArg (i));
			}
			Console.WriteLine (")");		
		
			Console.Write ("RESOUTARGS(");
			for (int i = 0; i < res.OutArgCount; i++) 
			{
				if (i != 0)
					Console.Write (", ");
				Console.Write (res.GetOutArgName (i) +  " " +
					res.GetOutArg (i));
			}
			Console.WriteLine (")");		
		
			return res;
		}
	}

	class R2 
	{
		string sTest;
		public R2() 
		{
			sTest = "R2";
		}

		public void Print() 
		{
			Console.WriteLine(sTest);
		}
	}

	[Serializable]
	class R2_MBV
	{
		string sTest;
		public R2_MBV() 
		{
			sTest = "R2";
		}

		public string Data
		{
			get 
			{
				return sTest;
			}
		}
	}

	class R1 : MarshalByRefObject 
	{
		public R2 TestMBV() {
			return new R2();
		}
	}

	class Class1
	{
		static int Main(string[] args)
		{
			Console.WriteLine("test " + AppDomain.CurrentDomain.FriendlyName);
			AppDomain app2 = AppDomain.CreateDomain("2");

			if (!RemotingServices.IsTransparentProxy(app2)) 
				return 1;				

			ObjectHandle o = AppDomain.CurrentDomain.CreateInstance(typeof(R1).Assembly.FullName, typeof(R1).FullName);
			R1 myobj = (R1) o.Unwrap();
			
			// should not be a proxy in our domain..
			if (RemotingServices.IsTransparentProxy(myobj)) 
			{
				Console.WriteLine("CreateInstance return TP for in our current domain");
				return 2;				
			}

			o = app2.CreateInstance(typeof(R1).Assembly.FullName, typeof(R1).FullName);

			Console.WriteLine("type: " + o.GetType().ToString());

			myobj = (R1) o.Unwrap();
			if (!RemotingServices.IsTransparentProxy(myobj))
				return 3;

			Console.WriteLine("unwrapped type: " + myobj.GetType().ToString());

			R2 r2 = null;
			bool bSerExc = false;

			// this should crash
			try
			{
				r2 = myobj.TestMBV();
			}		
			catch (SerializationException)
			{
				bSerExc = true;
			}

			if (!bSerExc)
				return 4;
	
			Console.WriteLine("test-ok");
			return 0;
		}
	}
}
