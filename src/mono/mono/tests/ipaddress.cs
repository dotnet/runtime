using System.Net;
using System.Net.Sockets;
using System.IO;
using System;

namespace T {
	public class T {

		public static int Main () {

			/*
			Console.WriteLine ("address is " + IPAddress.NetworkToHostOrder (0x0100007f).ToString("X"));
			*/
			
			IPAddress testadd = IPAddress.Parse ("127.0.0.1");
			Console.WriteLine("address is " + testadd.Address.ToString ("X"));
			if (testadd.Address != 0x0100007f)
				return 1;

			
			IPAddress hostadd = new IPAddress(0x0100007f);
			Console.WriteLine("address is " + hostadd.ToString());
			if (hostadd.ToString() != "127.0.0.1")
				return 1;

			return 0;
		}
	}
}

