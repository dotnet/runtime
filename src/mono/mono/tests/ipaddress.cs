using System.Net;
using System.Net.Sockets;
using System.IO;
using System;

namespace T {
	public class T {

		public static int Main () {
			IPAddress hostadd = new IPAddress(0x0100007f);
			Console.WriteLine("address is " + hostadd.ToString());
			if (hostadd.ToString() != "127.0.0.1")
				return 1;

			return 0;
		}
	}
}

