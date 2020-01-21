using System;
using System.Reflection;
using System.Threading;

namespace UnhandledExceptionTest
{
	class TestConfiguration {
		private bool useDifferentThread = false;
		public bool DT {
			get {
				return useDifferentThread;
			}
		}
		private bool useDifferentApplicationDomain = false;
		public bool DA {
			get {
				return useDifferentApplicationDomain;
			}
		}
		private bool differentConfigurationIsLegacy;
		public bool DCIL {
			get {
				return differentConfigurationIsLegacy;
			}
		}
		private bool useDifferentThreadInDifferentApplicationDomain = false;
		public bool DTDA {
			get {
				return useDifferentThreadInDifferentApplicationDomain;
			}
		}
		private bool addHandlerToRootApplicationDomain = false;
		public bool HRA {
			get {
				return addHandlerToRootApplicationDomain;
			}
		}
		private bool addHandlerToDifferentApplicationDomain = false;
		public bool HDA {
			get {
				return addHandlerToDifferentApplicationDomain;
			}
		}
		
		private static bool ParseArgumentValue (string value) {
			if ((value.Length == 1)) {
				switch (value [0]) {
				case 'T':
					return true;
				case 'F':
					return false;
				default:
					Console.WriteLine ("Invalid argument value {0}", value);
					throw new ApplicationException ("Invalid argument value " + value);
				}
			} else {
				Console.WriteLine ("Invalid argument value {0}", value);
				throw new ApplicationException ("Invalid argument value " + value);
			}
		}
		
		public TestConfiguration (string configuration) {
			string [] arguments = configuration.Split (',');
			foreach (string argument in arguments) {
				string [] components = argument.Split (':');
				if (components.Length == 2) {
					switch (components [0]) {
					case "DT":
						useDifferentThread = ParseArgumentValue (components [1]);
						break;
					case "DA":
						useDifferentApplicationDomain = ParseArgumentValue (components [1]);
						break;
					case "DCIL":
						differentConfigurationIsLegacy = ParseArgumentValue (components [1]);
						break;
					case "DTDA":
						useDifferentThreadInDifferentApplicationDomain = ParseArgumentValue (components [1]);
						break;
					case "HRA":
						addHandlerToRootApplicationDomain = ParseArgumentValue (components [1]);
						break;
					case "HDA":
						addHandlerToDifferentApplicationDomain = ParseArgumentValue (components [1]);
						break;
					default:
						Console.WriteLine ("Invalid argument {0}", argument);
						throw new ApplicationException ("Invalid argument " + argument);
					}
				} else {
					Console.WriteLine ("Invalid argument {0}", argument);
					throw new ApplicationException ("Invalid argument " + argument);
				}
			}
		}
		private static string BoolToString (bool value) {
			return value ? "T" : "F";
		}
		public void Print () {
			Console.WriteLine ("Configuration: DT={0},DA={1},DCIL={2},DTDA={3},HRA={4},HDA={5}",
					BoolToString (useDifferentThread),
					BoolToString (useDifferentApplicationDomain),
					BoolToString (differentConfigurationIsLegacy),
					BoolToString (useDifferentThreadInDifferentApplicationDomain),
					BoolToString (addHandlerToRootApplicationDomain),
					BoolToString (addHandlerToDifferentApplicationDomain));
		}
	}
	
	class Test {
		private string configurationDescription;
		private TestConfiguration configuration;
		public Test (string configurationDescription) {
			this.configurationDescription = configurationDescription;
			this.configuration = new TestConfiguration (configurationDescription);
		}
		
		private AppDomain CreateDiffrentAppDomain () {
			AppDomainSetup ads = new AppDomainSetup();
        		ads.ApplicationBase = System.Environment.CurrentDirectory;
        		ads.DisallowBindingRedirects = false;
        		ads.DisallowCodeDownload = true;
        		ads.ConfigurationFile = System.Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar +
					(configuration.DCIL ? "unhandled-exception-legacy-configuration.config" : "unhandled-exception-base-configuration.config");
			return AppDomain.CreateDomain("DifferentAppDomain", null, ads);
		}
		
		public void RunTest () {
			if (configuration.DA) {
				AppDomain differentAppDomain = CreateDiffrentAppDomain ();
				if (configuration.HDA) {
					differentAppDomain.UnhandledException += new UnhandledExceptionEventHandler (DifferentDomainUnhandledExceptionHandler);
				}
				DifferentDomainActor dda = (DifferentDomainActor) differentAppDomain.CreateInstanceAndUnwrap (
            					Assembly.GetEntryAssembly().FullName, typeof (DifferentDomainActor).FullName);
				dda.Act (configurationDescription);
			} else {
				if (configuration.DT) {
					Console.WriteLine ("Throwing ApplicationException in different thread");
				} else {
					Console.WriteLine ("Throwing ApplicationException in main thread");
				}
				throw new ApplicationException ("This exception is unhandled");
			}
			if (configuration.DT) {
				Console.WriteLine ("Continuing in different thread after the exception was thrown");
			}
		}
		
		static void Main (string [] args) {
			if (args.Length != 1) {
				Console.WriteLine ("Invalid arguments (number of) {0}", args.Length);
				throw new ApplicationException ("Invalid arguments (number of) " + args.Length);
			}
			Test test = new Test (args [0]);
			test.Act ();
		}
		public void Act () {
			configuration.Print ();
			Console.WriteLine ("Running under version {0}", Environment.Version);
			
			if (configuration.HRA) {
				AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler (RootDomainUnhandledExceptionHandler);
			}
			
			if (configuration.DT) {
				Thread thread = new Thread (new ThreadStart (this.RunTest));
				thread.Start ();
				thread.Join ();
			} else {
				RunTest ();
			}
			
			Console.WriteLine ("Continuing in main thread after the exception was thrown");
			Console.WriteLine ("Continuing in root AppDomain after the exception was thrown");
			Console.WriteLine ("MARKER-CONT");
		}

		public static void PrintUnhandledException (string caller, object sender, UnhandledExceptionEventArgs e) {
			Exception ex = (Exception)e.ExceptionObject;

			Console.WriteLine ("Running {0}", caller);
			Console.WriteLine ("Handling exception type {0}", ex.GetType().Name);
			Console.WriteLine ("Message is {0}", ex.Message);
			Console.WriteLine ("IsTerminating is set to {0}", e.IsTerminating);
		}
		public static void RootDomainUnhandledExceptionHandler (object sender, UnhandledExceptionEventArgs e) {
			Console.WriteLine ("MARKER-RDUE");
			PrintUnhandledException ("RootDomainUnhandledExceptionHandler", sender, e);
		}
		public static void DifferentDomainUnhandledExceptionHandler (object sender, UnhandledExceptionEventArgs e) {
			Console.WriteLine ("MARKER-DDUE");
			PrintUnhandledException ("DifferentDomainUnhandledExceptionHandler", sender, e);
		}
	}
	
	public class DifferentDomainActor : MarshalByRefObject {
		//private string configurationDescription = null;
		private TestConfiguration configuration = null;
		
		public void RunTest () {
			if (configuration.DTDA) {
				Console.WriteLine ("Throwing ApplicationException in new thread (different appdomain)");
			} else if (configuration.DT) {
				Console.WriteLine ("Throwing ApplicationException in different thread (different appdomain)");
			} else {
				Console.WriteLine ("Throwing ApplicationException in main thread (different appdomain)");
			}
			throw new ApplicationException ("This exception is unhandled");
		}
		
		//  Call this method via a proxy.
		public void Act (string configurationDescription) {
			//this.configurationDescription = configurationDescription;
			this.configuration = new TestConfiguration (configurationDescription);
			
			if (configuration.DTDA) {
				Thread thread = new Thread (new ThreadStart (this.RunTest));
				thread.Start ();
				thread.Join ();
			} else {
				RunTest ();
			}
		}
	}
}
