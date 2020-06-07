// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

class AA
{
	bool[] m_null = null;
	
	static double[] Alloc()
	{
		return new double[2];
	}

	static void DoThings()
	{
		DoThings2(__arglist());
	}

	static uint[] DoThings2(__arglist)
	{
		return DoThings3(__arglist(new double[2], Alloc()[1], new AA().m_null));
	}
	
	static uint[] DoThings3(__arglist)
	{
		GC.Collect();
		return null;
	}
	
	void CheckHeap()
	{
		GC.Collect();
	}
	
	public static int Main()
	{
		DoThings();
		new AA().CheckHeap();
		return 100;
	}
}
