// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

/// <summary>
/// This class generates a test that can be used to test perf of analyzing
/// compiler-generated code. Run it by copying this file into a console app and
/// calling <see cref="PerfTestGeneratorForCompilerGeneratedCode.Run"/>. A file
/// will be generated in the current directory named GeneratedLinkerTests.cs.
/// Copy this file into another Console app and trim the app to measure the
/// perf.
/// </summary>
static class PerfTestGeneratorForCompilerGeneratedCode
{
	const int FuncNumber = 10000;
	public static void Run ()
	{
		using var fstream = File.Create ("GeneratedLinkerTests.cs");
		using var writer = new StreamWriter (fstream);
		writer.WriteLine ($$"""
class C {
    public static async void Main()
    {
        int x = 0;
        {{string.Join (@"
        ", Enumerable.Range (0, FuncNumber).Select (i => $"x += await N{i}<int>.M();"))}}
        Console.WriteLine(x);
    }
}
""");
		for (int i = 0; i < FuncNumber; i++) {
			writer.WriteLine ($$"""
public static class N{{i}}<T>
{
    public static async ValueTask<int> M()
    {
        Func<int> a = () => 1;
        await Task.Delay(0);
        return a();
    }
}
""");
		}
	}
}