using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class CustomException : Exception
{
}

class CustomException2 : Exception
{
}

class CrossDomain : MarshalByRefObject
{
	public Action NewDelegateWithTarget ()
	{
		return new Action (Bar);
	}

	public Action NewDelegateWithoutTarget ()
	{
		return () => { throw new CustomException (); };
	}

	public void Bar ()
	{
		throw new CustomException ();
	}
}

class Driver
{
	/* expected exit code: 3 */
	static void Main (string[] args)
	{
		ManualResetEvent mre = new ManualResetEvent (false);

		var cd = (CrossDomain) AppDomain.CreateDomain ("ad").CreateInstanceAndUnwrap (typeof(CrossDomain).Assembly.FullName, "CrossDomain");

		var a = cd.NewDelegateWithTarget ();
		var ares = a.BeginInvoke (delegate { throw new CustomException2 (); }, null);

		try {
			a.EndInvoke (ares);
			Environment.Exit (4);
		} catch (CustomException) {
		} catch (Exception ex) {
			Console.WriteLine (ex);
			Environment.Exit (3);
		}

		Environment.Exit (0);
	}
}
