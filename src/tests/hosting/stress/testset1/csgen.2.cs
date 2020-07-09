// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections;
using System.Runtime.InteropServices;

#pragma warning disable 1717
#pragma warning disable 0219
#pragma warning disable 1718

public enum TestEnum
{
	red = 1,
	green = 2,
	blue = 4,
}

public class AA
{
	public double[][] m_adblField1;
	public static ulong[] Static1()
	{
		for (App.m_xFwd1=App.m_xFwd1; App.m_bFwd2; App.m_shFwd3-=((short)(121.0)))
		{
			if (App.m_bFwd2)
				do
				{
					try
					{
						uint[,][] local1 = (new uint[88u, 43u][]);
						do
						{
						}
						while(App.m_bFwd2);
					}
					catch (Exception)
					{
						AA local2 = new AA();
					}
					try
					{
						bool local3 = false;
					}
					finally
					{
						char local4 = '\x5d';
					}
				}
				while(App.m_bFwd2);
			throw new DivideByZeroException();
		}
		try
		{
		}
		catch (IndexOutOfRangeException)
		{
			byte local5 = ((byte)(26.0));
		}
		return (new ulong[3u]);
	}
	public static ushort[] Static2()
	{
		for (App.m_ushFwd4*=App.m_ushFwd4; App.m_bFwd2; App.m_dblFwd5-=66.0)
		{
			float local6 = 112.0f;
			do
			{
				goto label1;
			}
			while(App.m_bFwd2);
			local6 = local6;
			label1:
			try
			{
				Array local7 = ((Array)(null));
			}
			catch (Exception)
			{
			}
		}
		try
		{
		}
		catch (IndexOutOfRangeException)
		{
		}
		while (App.m_bFwd2)
		{
		}
		return App.m_aushFwd6;
	}
	public static long[] Static3()
	{
		for (App.m_sbyFwd7*=((sbyte)(70)); (null == new AA()); App.m_dblFwd5+=97.0)
		{
			double local8 = 20.0;
			for (App.m_iFwd8*=119; App.m_bFwd2; App.m_lFwd9--)
			{
				short local9 = App.m_shFwd3;
				return (new long[111u]);
			}
			local8 = local8;
			if (App.m_bFwd2)
				continue;
		}
		return App.m_alFwd10;
	}
	public static short[] Static4()
	{
		byte local10 = ((byte)(55u));
		for (App.m_dblFwd5=App.m_dblFwd5; App.m_bFwd2; App.m_ulFwd11--)
		{
			sbyte[][,,][,] local11 = (new sbyte[91u][,,][,]);
			break;
		}
		while (App.m_bFwd2)
		{
			char local12 = '\x48';
			while ((null != new AA()))
			{
				double local13 = 55.0;
				try
				{
					while ((local10 != local10))
					{
						sbyte[,,] local14 = (new sbyte[97u, 62u, 10u]);
					}
				}
				catch (Exception)
				{
				}
				if (App.m_bFwd2)
					for (App.m_sbyFwd7/=((sbyte)(local10)); App.m_bFwd2; App.m_lFwd9/=App.
						m_lFwd9)
					{
					}
			}
		}
		return (new short[16u]);
	}
	public static long[][] Static5()
	{
		byte[,,] local15 = (new byte[7u, 92u, 57u]);
		return new long[][]{(new long[33u]), (new long[60u]), /*2 REFS*/(new long[61u]
			), /*2 REFS*/(new long[61u]), (new long[22u]) };
	}
	public static TestEnum Static6()
	{
		int local16 = 105;
		do
		{
			ulong local17 = ((ulong)(54));
			for (local16+=(local16 *= local16); App.m_bFwd2; App.m_dblFwd5++)
			{
				int local18 = 90;
				try
				{
					int local19 = 45;
					if ((new AA() == null))
						try
						{
						}
						catch (Exception)
						{
						}
					else
					{
					}
				}
				catch (InvalidOperationException)
				{
				}
				local17 += local17;
				goto label2;
			}
		}
		while(App.m_bFwd2);
		local16 -= ((int)(18u));
		label2:
		return 0;
	}
	public static double Static7()
	{
		for (App.m_sbyFwd7=App.m_sbyFwd7; App.m_bFwd2; App.m_chFwd12++)
		{
			object local20 = null;
			for (App.m_iFwd8++; ((bool)(local20)); local20=local20)
			{
				AA.Static1(  );
			}
		}
		return 82.0;
	}
	public static byte Static8()
	{
		while (App.m_bFwd2)
		{
			short local21 = App.m_shFwd3;
			while (Convert.ToBoolean(local21))
			{
				local21 *= (local21 -= local21);
				goto label3;
			}
			if (App.m_bFwd2)
				local21 /= local21;
			local21 = (local21 += local21);
		}
		if (App.m_bFwd2)
			for (App.m_lFwd9--; App.m_bFwd2; App.m_dblFwd5-=AA.Static7())
			{
				TestEnum[][,,,][,] local22 = (new TestEnum[102u][,,,][,]);
			}
		label3:
		return App.m_byFwd13;
	}
}

[StructLayout(LayoutKind.Sequential)] public class App
{
	public static int Main()
	{
		try
		{
			AA.Static1(  );
		}
		catch (Exception )
		{
		}
		try
		{
			AA.Static2(  );
		}
		catch (Exception )
		{
		}
		try
		{
			AA.Static3(  );
		}
		catch (Exception )
		{
		}
		try
		{
			AA.Static4(  );
		}
		catch (Exception )
		{
		}
		try
		{
			AA.Static5(  );
		}
		catch (Exception )
		{
		}
		try
		{
			AA.Static6(  );
		}
		catch (Exception )
		{
		}
		try
		{
			AA.Static7(  );
		}
		catch (Exception )
		{
		}
		try
		{
			AA.Static8(  );
		}
		catch (Exception )
		{
		}
		return 100;
	}
	public static Array m_xFwd1;
	public static bool m_bFwd2;
	public static short m_shFwd3;
	public static ushort m_ushFwd4;
	public static double m_dblFwd5;
	public static ushort[] m_aushFwd6;
	public static sbyte m_sbyFwd7;
	public static int m_iFwd8;
	public static long m_lFwd9;
	public static long[] m_alFwd10;
	public static ulong m_ulFwd11;
	public static char m_chFwd12;
	public static byte m_byFwd13;
}
