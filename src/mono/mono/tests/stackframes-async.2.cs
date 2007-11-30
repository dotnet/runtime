using System;
using System.Net;
using System.Diagnostics;

namespace stacktracetest
{
	class MainClass
	{
		static int frame_count = 0;
		public static int Main(string[] args)
		{
			AsyncCallback cback = new AsyncCallback(ResolveCallback);
			IAsyncResult res = Dns.BeginGetHostEntry("localhost", cback, null);
			System.Threading.Thread.Sleep(2000);
			/*
			 * seems to be broken
			while (!res.IsCompleted) {
				System.Threading.Thread.Sleep(20);
			};
			IPHostEntry ip = Dns.EndGetHostEntry (res);
			Console.WriteLine (ip);*/
			if (frame_count >= 1)
				return 0;
			return 1;
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
}
