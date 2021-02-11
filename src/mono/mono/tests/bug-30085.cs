using System;

class Program
{

	static void MissingImage ()
	{
		Type good = System.Type.GetType("System.Nullable`1[[System.Int32, mscorlib]]");
		Type bad = System.Type.GetType("System.Nullable`1[[System.Int32, mscorlibBAD]]");

		if (good.Assembly.FullName.Split (',') [0] != "mscorlib")
			throw new Exception ("Wrong assembly name");

		if (bad != null)
			throw new Exception ("Should not have loaded type");
	}

	static void ProbeCorlib ()
	{
		Type good = System.Type.GetType("System.Nullable`1[[System.Int32, mscorlib]]");
#if MOBILE
		string pubKeyToken = "7cec85d7bea7798e";
#else
		string pubKeyToken = "b77a5c561934e089";
#endif
		string t = String.Format ("System.Nullable`1[[System.IO.MemoryMappedFiles.MemoryMappedFile, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken={0}]]", pubKeyToken);
		Type bad = System.Type.GetType(t);

		if (good.Assembly.FullName.Split (',') [0] != "mscorlib")
			throw new Exception ("Wrong assembly name");

		if (good == null || bad == null)
			throw new Exception ("Missing image did not probe corlib");
	}

	static void Main()
	{
		MissingImage ();
		ProbeCorlib ();
	}
}
