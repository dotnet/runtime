// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class Program
{
    public static IRuntime s_rt;
    public static ulong s_1;
    public static int Main()
    {
		try
		{
			var vr1 = (uint)((int)M2(ref s_1, 0) % (long)1);
        	M2(ref s_1, vr1);
		}
		catch (System.Exception)
		{
		}

		return 100;
    }

    public static byte M2(ref ulong arg0, uint arg1)
    {
        s_rt.WriteLine(arg0);
        return 0;
    }
}

public interface IRuntime
{
    void WriteLine<T>(T value);
}

public class Runtime : IRuntime
{
    public void WriteLine<T>(T value) => System.Console.WriteLine(value);
}