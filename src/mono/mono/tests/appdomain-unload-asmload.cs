using System;
using System.Threading.Tasks;

/* This is a regression test that checks that after an AssemblyLoad event fires
 * in a domain, the domain can be unloaded.  In bug # 56694, a
 * System.Reflection.Assembly object from the unloaded domain was kept alive
 * and crashed the GC.  */
namespace AppDomainUnloadAsmLoad
{
	class Program
	{
		static void Main(string[] args)
		{
			// Need some threads in play
			new Program().Run().Wait();
		}

		private async Task Run()
		{
			var appDomain = AppDomain.CreateDomain("Test subdomain", null, AppDomain.CurrentDomain.SetupInformation);
			try
			{
				var driver = (AppDomainTestDriver)appDomain.CreateInstanceAndUnwrap(typeof(AppDomainTestDriver).Assembly.FullName,
												    typeof(AppDomainTestDriver).FullName);
				driver.Test();
			}
			finally
			{
				AppDomain.Unload(appDomain);
			}
		}
	}

	class AppDomainTestDriver : MarshalByRefObject
	{
		static AppDomainTestDriver()
		{
			// Needs a callback so that the runtime fires the
			// AssemblyLoad event for this domain and materializes a System.Reflection.Assembly
			AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
		}

		private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
		}

		internal void Test()
		{
			/* this can be any class from any assembly that hasn't
			 * already been loaded into the test domain.
			 * System.Xml.dll is good because all the tests link
			 * against it, but it's not otherwise used by this
			 * domain. */
			var foo = default(System.Xml.XmlException);
		}
    }
}
