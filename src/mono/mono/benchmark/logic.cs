/* this code is part of the pnetmark benchmark - i have only done some small
 * modification
 */

/*
 * LogicBenchmark.cs - Implementation of the "LogicBenchmark" class.
 *
 * Copyright (C) 2001  Southern Storm Software, Pty Ltd.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

using System;

public class Tests {

	public static void logic_run ()
	{
		int iter;

		// Initialize.
		bool flag1 = true;
		bool flag2 = true;
		bool flag3 = true;
		bool flag4 = true;
		bool flag5 = true;
		bool flag6 = true;
		bool flag7 = true;
		bool flag8 = true;
		bool flag9 = true;
		bool flag10 = true;
		bool flag11 = true;
		bool flag12 = true;
		bool flag13 = true;

		// First set of tests.
		for(iter = 0; iter < 2000000; ++iter) {
			if((flag1 || flag2) && (flag3 || flag4) &&
			   (flag5 || flag6 || flag7))
				{
				flag8 = !flag8;
				flag9 = !flag9;
				flag10 = !flag10;
				flag11 = !flag11;
				flag12 = !flag12;
				flag13 = !flag13;
				flag1 = !flag1;
				flag2 = !flag2;
				flag3 = !flag3;
				flag4 = !flag4;
				flag5 = !flag5;
				flag6 = !flag6;
				flag1 = !flag1;
				flag2 = !flag2;
				flag3 = !flag3;
				flag4 = !flag4;
				flag5 = !flag5;
				flag6 = !flag6;
			}
		}
	}
	
	public static int Main (string[] args) {
		int repeat = 1;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < (repeat * 50); i++)
			logic_run ();
		
		return 0;
	}
}


