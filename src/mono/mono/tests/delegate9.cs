using System;

// Regression test for bug #428054

namespace RemotingTest
{
	class Program : MarshalByRefObject
	{
		static int Main (string[] args)
		{
			Program p = new Program ();
			Client client = Client.CreateInNewAppDomain ();
			client.Completed += p.CompletedHandler;
			if (client.Run () != AppDomain.CurrentDomain.FriendlyName)
				return 1;
			else
				return 0;
		}

		public string CompletedHandler ()
		{
			return AppDomain.CurrentDomain.FriendlyName;
		}
	}

	class Client : MarshalByRefObject
	{
		public delegate string StringDel ();
		public event StringDel Completed;

		public static Client CreateInNewAppDomain ()
		{
			AppDomain clientDomain = AppDomain.CreateDomain ("client");
			return (Client) clientDomain.CreateInstanceAndUnwrap (
				typeof(Client).Assembly.FullName, typeof(Client).FullName);
		}

		public string Run ()
		{
			return Completed ();
		}
	}
}
