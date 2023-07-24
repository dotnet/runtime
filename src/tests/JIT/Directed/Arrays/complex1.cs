// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

// Do a complex 5 dimensional Jagged array.
struct Complex
{
  public int a,b,c;
  public void mul_em()
  {
	c = a * b;
  }
};

public class Complex_Array_Test
{
	[Fact]
	public static int TestEntryPoint()
	{
		Console.WriteLine("Starting...");
		int SIZE = 10;

	//Create an array that is jagged.
	// in last 2d, the array looks like:
	//  Complex
	//  Complex Complex 
	//  Complex Complex Complex 
	//  Complex Complex Complex Complex 
	//  Complex Complex Complex Complex Complex 
	//

		Complex [][][][][] foo = new Complex[SIZE][][][][];
		int i,j,k,l,m;
                Int64 sum=0;

		for(i=0;i<SIZE;i++)
		{
			foo[i] = new Complex[i][][][];
			for(j=0;j<i;j++)
			{
				foo[i][j] = new Complex[j][][];
				for(k=0;k<j;k++)
				{
					foo[i][j][k] = new Complex[k][];
					for(l=0;l<k;l++)
					{
						foo[i][j][k][l] = new Complex[l];
						for(m=0;m<l;m++)
						{
							foo[i][j][k][l][m].a = i*j;
							foo[i][j][k][l][m].b = k*l*m;
							foo[i][j][k][l][m].mul_em();
						}
					}
				}
			}
		}

		for(i=0;i<SIZE;i++)
			for(j=0;j<i;j++)
				for(k=0;k<j;k++)
					for(l=0;l<k;l++)
					 for(m=0;m<l;m++)
					 {
                                          sum+=foo[i][j][k][l][m].c;
					  //Console.Write(" "+foo[i][j][k][l][m].c.ToString());
					 }


		if(sum==269325)	
		{
			Console.WriteLine("Everything Worked!");
			return 100;
		}
 		else
		{
			Console.WriteLine("Something is broken!");
			return 1;
		}
	}
}
