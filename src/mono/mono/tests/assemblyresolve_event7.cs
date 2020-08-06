using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyResolveTest
{
	// This is a testing program for debuging one problem with name of resolving assembly
	class Program
	{
		public static Dictionary<int, string> Original = new Dictionary<int, string>() {
			{0, "System.ServiceModel2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"},
			{1, "System.ServiceModel2, Version=4.0.0.0, Culture=neutral2, PublicKeyToken=b77a5c561934e089"},
			{2, "System.ServiceModel2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"},
			{3, "System.ServiceModel2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"},
			{4, "System.ServiceModel2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"}
		};

		static int I = 0;
		static int ExcCount = 0;

		static int Main(string[] args)
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);
            //We try to load assembly in different case

			var t0 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Console.WriteLine("*** Type 0 ***");

			var t1 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection     , System.ServiceModel2    , Version=4.0.0.0   , Culture=neutral2  , PublicKeyToken=b77a5c561934e089      ");
			Console.WriteLine("*** Type 1 ***");

			var t2 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2  ,   Culture   =    neutral  , Version     =     4.0.0.0, PublicKeyToken  = b77a5c561934e089");
			Console.WriteLine("*** Type 2 ***");

			var t3 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2  , Version     =     4.0.0.0    ,   Culture   =    neutral, PublicKeyToken  = b77a5c561934e089");
			Console.WriteLine("*** Type 3 ***");

			var t4 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection,           System.ServiceModel2         ");
			Console.WriteLine("*** Type 4 ***");

			if (ExcCount> 0)
				throw new Exception("Resolving assembly parametrs and original parametrs are not equal in AssemblyResolveTest7");

			Console.WriteLine("Test is OK");
			return 0;
			}

        private static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
		{
			Console.WriteLine($" Resolving assembly name: {args.Name}/");
			if (Original.TryGetValue(I, out string value)) {
				Console.WriteLine ($"Value is {value}");
				if (value.Equals(args.Name)) {
						Console.WriteLine ($"Value '{value}' and args.name {args.Name}   is Equals !");
					} else
						ExcCount++;
					//bug: An exception is not raised!
					//throw new Exception("Resolving assembly parametrs and original parametrs are not equal in AssemblyResolveTest!!");
				if (I < Original.Count - 1)
					I++;
				return null;
			}
			ExcCount++;
			return null;
			//bug: An exception is not raised!
			//throw new Exception("Resolving assembly parametrs and original parametrs are not equal");
		}
	}
}
