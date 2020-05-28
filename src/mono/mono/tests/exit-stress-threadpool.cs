// https://bugzilla.novell.com/show_bug.cgi?id=593955
using System;
using System.Threading;

public class Tests
{
	public static void Main (String[] args) {
		new Thread (delegate () {
				Thread.Sleep (100);
				Environment.Exit (0);
		}).Start ();

		while (true) {
			Action a = delegate () {
			};
			a.BeginInvoke (null, null);
		}
	}
}
