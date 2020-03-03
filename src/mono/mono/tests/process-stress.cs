using System;
using System.Diagnostics;

public class Tests
{
	static int iterations = 512;

	public static void Main(string[] args) {
		if (args.Length > 0)
			iterations = Int32.Parse (args [0]);

		// Spawn threads without waiting for them to exit
		for (int i = 0; i < iterations; i++) {
			Console.Write (".");
			//Console.WriteLine("Starting: " + i.ToString());
			using (var p = System.Diagnostics.Process.Start("echo -n")) {
				System.Threading.Thread.Sleep(10);
			}
		}

		// Spawn threads and wait for them to exit
		for (int i = 0; i < iterations; i++) {
			Console.Write (".");
			//Console.WriteLine("Starting: " + i.ToString());
			using (var p = System.Diagnostics.Process.Start("echo -n")) {
				p.WaitForExit ();
            }
        }

		Console.WriteLine ();
	}
}
