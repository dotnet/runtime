// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class MemoryPressure
{
	private long iterations = 0;

	public static void Usage()
	{
		Console.WriteLine("Usage");
		Console.WriteLine("MemoryPressure.exe <iterations> <add|remove>");
	}

	public static void Main(string[] args)                                                                           
	{
		if (args.Length!=2)
		{
			Usage();
			return;
		}
		long iterations = 0;
		if (!long.TryParse(args[0], out iterations))
		{
			Usage();
			return;
		}

		if(iterations == 0)
		{
			iterations=200;
		}

		MemoryPressure mp = new MemoryPressure(iterations);

		switch (args[1].ToLower())
		{
			case "add":
				mp.AddMemoryPressure();
				break;
			case "remove":
				mp.RemoveMemoryPressure();
				break;
			default:
				Usage();
				return;
		}
	}

	public MemoryPressure(long iters)
	{
		iterations = iters;
	}

	public void RemoveMemoryPressure()
	{
		for(long i = 0; i < iterations; i++)
		{
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
		}

		for(long i = 0; i < iterations; i++)
		{
			GC.RemoveMemoryPressure(Int32.MaxValue);
			GC.RemoveMemoryPressure(Int32.MaxValue);
			GC.RemoveMemoryPressure(Int32.MaxValue);
			GC.RemoveMemoryPressure(Int32.MaxValue);
			GC.RemoveMemoryPressure(Int32.MaxValue);
			GC.RemoveMemoryPressure(Int32.MaxValue);
			GC.RemoveMemoryPressure(Int32.MaxValue);
			GC.RemoveMemoryPressure(Int32.MaxValue);
			GC.RemoveMemoryPressure(Int32.MaxValue);
			GC.RemoveMemoryPressure(Int32.MaxValue);
		}
	}

	public void AddMemoryPressure()
	{

		GC.AddMemoryPressure(Int32.MaxValue);

		for(long i = 0; i < iterations; i++)
		{
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
			GC.AddMemoryPressure(Int32.MaxValue);
		}
	}
}
