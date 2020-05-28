using System;
using System.Reflection;


internal class NotExportedA {
}

internal class NotExportedB {
}

internal struct NBStruct {
	internal int a;
	internal int b;
}

public struct PublicStruct {
	internal int a;
	internal int b;
}

public class ClassC {
	internal int fld;
	internal static int sfld;
	internal void Test() {}
}


