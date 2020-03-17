using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;


class Driver {
	static void AppDomainMethod () {
		Console.WriteLine ("two");
		var socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
		socket.Bind (ep);
		socket.Listen (10);
		socket.BeginAccept ( delegate {
			Console.WriteLine ("Delegate should not be called!");
			Environment.Exit (1);
		}, socket);
	}

	static int Main () {
		var da = AppDomain.CreateDomain ("le domain");
		da.DoCallBack (delegate { AppDomainMethod ();});
		Console.WriteLine ("unloading");
		AppDomain.Unload (da);
		Console.WriteLine ("done");
		return 0;
	}
}
