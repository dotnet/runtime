
// reference both versions of gactestlib via extern aliases.

// N.B. the order of the aliases is important - the compiler will emit
// .assembly declarations in the IL file in order, and Mono will try to load the declarations in the same order.
// The test relies on V1 being loaded first and then V2 being tried from V1's MONO_PATH directory.
extern alias V1;
extern alias V2;

using System;

public class AppBoth {
	public static int Main (string[] args) {
		// regression test that references two strongly named
		// assemblies that have the same name but different versions.
		V1.OnlyInV1.M ();
		V2.OnlyInV2.M ();
		if (typeof (V1.X).Assembly.GetName ().Version == typeof (V2.X).Assembly.GetName ().Version)
			return 1;

		return 0;
	}
}
