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
		public static Dictionary<int, string> Original = new Dictionary<int, string>()
		{
			{0, "CommonComponents.Infra.Zero.Uc, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"},
			{1, "CommonComponents.Infra.Zero.Uc"},
			{2, "CommonComponents.Infra.Zero.Uc"},
			{3, "System.ServiceModel2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"},
			{4, "System.ServiceModel2, Version=4.0.0.0, Culture=neutral"},
			{5, "System.ServiceModel2, Version=4.0.0.0, Culture=neutral"},
			{6, "System.ServiceModel2, Version=4.0.0.0"},
			{7, "System.ServiceModel2, Version=4.0.0.0"},
			{8, "System.ServiceModel2, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089"},
			{9, "System.ServiceModel2, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089"},
			{10, "System.ServiceModel2, Culture=neutral"},
			{11, "System.ServiceModel2, Culture=neutral"},
			{12, "System.ServiceModel2, Culture=neutral2"},
			{13, "System.ServiceModel2, Version=4.0.0.0"},
			{14, "System.ServiceModel2"},
			{15, "System.ServiceModel2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"},
			{16, "System.ServiceModel2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"},
			{17, "System.Core2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"},
			{18, "System.Core2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null"},
		};

		static int I = 0;
		static int ExcCount = 0;
		static int Main(string[] args)
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;

			currentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);

			//We try to load assembly in different case

			var t0 = Type.GetType("CommonComponents.Infra.Zero.Uc.Deployment.ClientSoftwareUpdateStrategy, CommonComponents.Infra.Zero.Uc, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Console.WriteLine("*** Type 0 ***");

			var t = Type.GetType("CommonComponents.Infra.Zero.Uc.Deployment.ClientSoftwareUpdateStrategy, CommonComponents.Infra.Zero.Uc");
			Console.WriteLine("*** Type 1 ***");

			var t2 = Type.GetType("CommonComponents.Infra.Zero.Uc.Deployment.ClientSoftwareUpdateStrategy, CommonComponents.Infra.Zero.Uc ");
			Console.WriteLine("*** Type 2 ***");

			var t3 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Console.WriteLine("*** Type 3 ***");

			var t4 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Version=4.0.0.0, Culture=neutral");
			Console.WriteLine("*** Type 4 ***");

			var t5 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Version=4.0.0.0, Culture=neutral ");
			Console.WriteLine("*** Type 5 ***");

			var t6 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Version=4.0.0.0");
			Console.WriteLine("*** Type 6 ***");

			var t7 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Version=4.0.0.0 ");
			Console.WriteLine("*** Type 7 ***");

			var t8 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089");
			Console.WriteLine("*** Type 8 ***");

			var t9 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089 ");
			Console.WriteLine("*** Type 9 ***");

			var t10 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Culture=neutral");
			Console.WriteLine("*** Type 10 ***");

			var t11 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Culture=neutral ");
			Console.WriteLine("*** Type 11 ***");

			var t12 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Culture=neutral2");
			Console.WriteLine("*** Type 12 ***");

			var t13 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Version=4.0.0.0, PublicKeyToke2n=b77a5c561934e089");
			Console.WriteLine("*** Type 13 ***");

			var t14 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, Vedrsion=4.0.0.0, PublicKeyToke2n=b77a5c561934e089");
			Console.WriteLine("*** Type 14 ***");

			var t15 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.ServiceModel2, PublicKeyToken=b77a5c561934e089, Version=4.0.0.0, Culture=neutral");
			Console.WriteLine("*** Type 15 ***");

			var t16 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection    , System.ServiceModel2   , PublicKeyToken=b77a5c561934e089  , Version=4.0.0.0  , Culture=neutral    ");
			Console.WriteLine("*** Type 16 ***");

			var t17 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.Core2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Console.WriteLine("*** Type 17 ***");

			var t18 = Type.GetType("System.ServiceModel.Configuration.DiagnosticSection, System.Core2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null");
			Console.WriteLine("*** Type 18 ***");

			Console.WriteLine("Test is OK");
			if (ExcCount>0)
				  throw new Exception("Resolving assembly parametrs and original parametrs are not equal in AssemblyResolveTest7");
			return 0;
		}

		private static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
		{
			Console.WriteLine($" Resolving assembly name: {args.Name}/");
			if (Original.TryGetValue(I, out string  value1))
			{
				Console.WriteLine("value1 :" + value1);
				if (value1.Equals(args.Name)) {
					Console.WriteLine($"Equals : {args.Name} and {value1}");
						if (I < Original.Count - 1)
							I++;
				return null;
				}
			}
			ExcCount++;
			return null;
			//bug: An exception is not raised!
			//throw new Exception("Resolving assembly parametrs and original parametrs are not equal");
		}
	}
}
