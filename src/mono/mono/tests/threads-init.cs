using System;
using System.Threading;

class Driver
{
	public static void Main ()
	{
		Thread t1 = new Thread(() => {
			for (int i = 0; i < 10; ++i) {
				Thread t2 = new Thread(() => {
					while (true) {
						Thread t3 = new Thread(() => {});
						t3.IsBackground = true;
						t3.Start();
						t3.Join();
					}
				});

				t2.IsBackground = true;
				t2.Start ();
			}
		});

		t1.IsBackground = true;
		t1.Start ();

		Thread.Sleep (100);
	}
}
