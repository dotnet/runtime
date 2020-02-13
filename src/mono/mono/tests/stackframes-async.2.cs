using System;
using System.Net;
using System.Diagnostics;

	class MainClass
	{
		static int frame_count = 0;
		public static int Main(string[] args)
		{
			AsyncCallback cback = new AsyncCallback(ResolveCallback);
			IAsyncResult res = Dns.BeginGetHostEntry("localhost", cback, null);
			for (int i = 0; i < 100; ++i) {
				if (frame_count != 0)
					break;
				System.Threading.Thread.Sleep(100);
			}
			/*
			 * seems to be broken
			while (!res.IsCompleted) {
				System.Threading.Thread.Sleep(20);
			};
			IPHostEntry ip = Dns.EndGetHostEntry (res);
			Console.WriteLine (ip);*/
			if (frame_count < 1)
				return 1;

			// A test for #444383
			AppDomain.CreateDomain("1").CreateInstance(typeof (Class1).Assembly.GetName ().Name, "Class1");

			return 0;
		}
		
		public static void ResolveCallback(IAsyncResult ar)
		{
		    Console.WriteLine("ResolveCallback()");
		    StackTrace st = new StackTrace();
		    frame_count = st.FrameCount;
	            for(int i = 0; i < st.FrameCount; i++) {
	                StackFrame sf = st.GetFrame(i);
        	        Console.WriteLine("method: {0}", sf.GetMethod());
	            }
        	    Console.WriteLine("ResolveCallback() complete");
		}
	}

public class Class1
{
	public Class1 () {
		AppDomain.CreateDomain("2").CreateInstance(typeof (Class1).Assembly.GetName ().Name, "Class2");
	}
}

public class Class2
{
	public Class2 () {
		new StackTrace(true);
	}
}
