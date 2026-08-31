using System;
using System.Diagnostics;
using System.Reflection;


namespace ToManyOpenHandles
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length >= 1)
			{
				WriteStuffMode(args[0]);
				return;
			}

			RunStuffMode();

			Console.WriteLine("Success!!!!");
		}

		private static void WriteStuffMode(string counter)
		{
			Console.WriteLine("Run {0} {1}", counter, Console.ReadLine ());
		}

		private static void RunStuffMode()
		{
			for (int i = 0; i < 100; i++)
			{
				Execute(Assembly.GetExecutingAssembly().Location, i.ToString());
			}
		}

		public static void Execute(string exe, string exeArgs)
		{
			using (var p = NewProcess(exe, exeArgs))
			{
				p.OutputDataReceived += (sender, args) =>
				{
					if (args.Data != null)
						Console.WriteLine(args.Data);
				};
				p.ErrorDataReceived += (sender, args) =>
				{
					if (args.Data != null)
						Console.Error.WriteLine(args.Data);
				};

				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();

				p.StandardInput.WriteLine ("hello");

				p.WaitForExit();
				p.CancelErrorRead();
				p.CancelOutputRead();

				GC.Collect ();
			}
		}

		public static Process StartProcess(string filename, string arguments)
		{
			var p = NewProcess(filename, arguments);

			p.Start();
			return p;
		}

		static Process NewProcess(string exe, string args)
		{
			var p = new Process
			{
				StartInfo =
				{
					Arguments = args,
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					FileName = exe,
				}
			};

			return p;
		}
	}
}