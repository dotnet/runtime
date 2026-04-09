using System;
using System.Runtime.CompilerServices;

namespace recursivetype_1238911
{
	public struct DecentStruct1
	{
		public FineStruct2 RadicalValue;
	}

	[Serializable]
	public struct FineStruct2
	{
		public static DecentStruct1 BadValue;
	}

	class Program
	{
		static int Main(string[] args)
		{
			try {
				TestBody();
			} catch (TypeLoadException) {
				return 0;
			}
			return 1;
		}

		[MethodImpl (MethodImplOptions.NoInlining)]
		static void TestBody ()
		{
			Type info = typeof(DecentStruct1);
			TestBody2 (info);
		}

		[MethodImpl (MethodImplOptions.NoInlining)]
		static void TestBody2(Type info)
		{
			FineStruct2 fs = (FineStruct2)info.Assembly.CreateInstance("recursivetype_1238911.FineStruct2");
		}
	}
}
