using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

public class Tests
{
	[DllImport("__Internal")]
	extern static void mono_set_assemblies_path (string path);

	public static void Main (string[] args)
	{
		var ver40 = new Version (4, 0, 0, 0);
		var ver140 = new Version (14, 0, 0, 0);
		var util20 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Utilities, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
		var util35 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Utilities.v3.5, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
		var task20 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Tasks, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
		var task35 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Tasks.v3.5, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
		var engn20 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Engine, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
		var engn35 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Engine, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
		var frwk20 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Framework, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
		var frwk35 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Framework, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

		// when run as part of the test suite, we need to register the xbuild 14.0 path or v14 assembly lookup will fail
		var mono_path = Environment.GetEnvironmentVariable ("MONO_PATH");
		if (!String.IsNullOrEmpty (mono_path)) {
			var xbuild = Path.Combine (new DirectoryInfo (mono_path).Parent.FullName, "xbuild_14");
			mono_path = xbuild + Path.PathSeparator + mono_path;
			Console.WriteLine ("Setting Mono assemblies path to " + mono_path);
			mono_set_assemblies_path (mono_path);
		}

		var engn140 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Engine, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
		var frwk140 = Assembly.ReflectionOnlyLoad ("Microsoft.Build.Framework, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

		if (util20 == null)
			throw new Exception ("#1 assembly couldn't be loaded.");

		if (util35 == null)
			throw new Exception ("#2 assembly couldn't be loaded.");

		if (util20.GetName ().Version != ver40)
			throw new Exception ("#3 expected remap to v4.0.0.0, but got " + util20);

		if (util35.GetName ().Version != ver40)
			throw new Exception ("#4 expected remap to v4.0.0.0, but got " + util35);

		if (task20 == null)
			throw new Exception ("#5 assembly couldn't be loaded.");

		if (task35 == null)
			throw new Exception ("#6 assembly couldn't be loaded.");

		if (task20.GetName ().Version != ver40)
			throw new Exception ("#7 expected remap to v4.0.0.0, but got " + task20);

		if (task35.GetName ().Version != ver40)
			throw new Exception ("#8 expected remap to v4.0.0.0, but got " + task35);

		if (engn20 == null)
			throw new Exception ("#9 assembly couldn't be loaded.");

		if (engn35 == null)
			throw new Exception ("#10 assembly couldn't be loaded.");

		if (engn140 == null)
			throw new Exception ("#11 assembly couldn't be loaded.");

		if (engn20.GetName ().Version != ver40)
			throw new Exception ("#12 expected remap to v4.0.0.0, but got " + engn20);

		if (engn35.GetName ().Version != ver40)
			throw new Exception ("#13 expected remap to v4.0.0.0, but got " + engn35);
	
		if (engn140.GetName ().Version != ver140)
			throw new Exception ("#14 expected v14.0.0.0, but got " + engn140);

		if (frwk20 == null)
			throw new Exception ("#15 assembly couldn't be loaded.");

		if (frwk35 == null)
			throw new Exception ("#16 assembly couldn't be loaded.");

		if (frwk140 == null)
			throw new Exception ("#17 assembly couldn't be loaded.");

		if (frwk20.GetName ().Version != ver40)
			throw new Exception ("#18 expected remap to v4.0.0.0, but got " + frwk20);

		if (frwk35.GetName ().Version != ver40)
			throw new Exception ("#19 expected remap to v4.0.0.0, but got " + frwk35);

		if (frwk140.GetName ().Version != ver140)
			throw new Exception ("#20 expected v14.0.0.0, but got " + frwk140);
	}
}
