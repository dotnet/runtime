using System;
using System.Collections.Generic;

class Test
{
    static void Main()
	{
		List<string> in1 = new List<string>();
		in1.Add("A");
		List<string> in2 = new List<string>();
		in2.Add("B");
		SetRequiredInputTypes(new List<string>[] { in1, in2 });
    }

	static public void SetRequiredInputTypes(IEnumerable<IEnumerable<string>> enum_enum_strings)
	{
		foreach (IEnumerable<string> enum_strings in enum_enum_strings)
		{
			foreach (string s in enum_strings)
				Console.WriteLine(s);
		}
	}
}
