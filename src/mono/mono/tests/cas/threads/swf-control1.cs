//
// swf-control1.cs (based on swf-begininvoke.cs)
//
// Authors:
//	Jackson Harper (jackson@ximian.com)
//	Sebastien Pouliot <sebastien@ximian.com>
//
// Copyright (c) 2004-2005 Novell, Inc (http://www.novell.com)
//

using System;
using System.Diagnostics;
using System.Drawing;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;

namespace System.Windows.Forms {

	public class BeginInvokeDemo : Form {

		private Label label;

		public BeginInvokeDemo ()
		{
			label = new Label ();
			label.Dock = DockStyle.Fill;
			label.TextAlign = ContentAlignment.MiddleCenter;
			Controls.Add (label);
		}

		private void InfoUpdaterAllow ()
		{
			string s = String.Format ("SecurityManager.SecurityEnabled: {0}{1}{2}{1}",
				SecurityManager.SecurityEnabled, Environment.NewLine, DateTime.Now.ToLongTimeString ());
			try {
				s += String.Format ("Wake up {0}!", Environment.UserName);
			}
			catch (SecurityException) {
				s += "SecurityException";
			}
			label.Text = s;
			label.Refresh ();

			if (debug)
				Console.WriteLine ("Delegate: {0}\n{1}", Thread.CurrentThread.Name, new StackTrace ().ToString ());
		}

		private delegate void updater ();

		private void InfoUpdaterDeny ()
		{
			InfoUpdaterAllow ();
		}

		private void UpdateLabelAllow ()
		{
			if (debug)
				Console.WriteLine ("Allow: {0}", Thread.CurrentThread.Name);

			while (counter++ < 10) {
				lock (this) {
					label.BeginInvoke (new updater (InfoUpdaterAllow));
				}
				Thread.Sleep (500);
			}

			if (debug)
				Console.WriteLine ("Application.Exit ();");
			Application.Exit ();
		}

		[EnvironmentPermission (SecurityAction.Deny, Read = "USERNAME")]
		private void UpdateLabelDeny ()
		{
			if (debug)
				Console.WriteLine ("Deny: {0}", Thread.CurrentThread.Name);

			while (counter++ < 10) {
				lock (this) {
					label.BeginInvoke (new updater (InfoUpdaterDeny));
				}
				Thread.Sleep (500);
			}

			if (debug)
				Console.WriteLine ("Application.Exit ();");
			Application.Exit ();
		}

		static bool debug = false;
		static int counter = 0;

		public static void Main (string[] args)
		{
			Thread.CurrentThread.Name = "Main";
			BeginInvokeDemo demo = new BeginInvokeDemo ();
			demo.CreateHandle ();

			ThreadStart thread_start = new ThreadStart (demo.UpdateLabelDeny);
			if (args.Length > 0) {
				debug = true;
				if (args [0] == "allow") {
					thread_start = new ThreadStart (demo.UpdateLabelAllow);
				}
			}
			Thread worker = new Thread (thread_start);
			worker.Name = "Updater";
			worker.IsBackground = true;
			worker.Start ();

			Application.Run (demo);
		}
	}
}
