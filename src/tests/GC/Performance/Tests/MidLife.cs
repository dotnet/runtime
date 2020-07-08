// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

public class AppendTest
{
    public static string[] _strings=null;
    public static int _numStrings = 10000000;
    public static Random rand = new Random(1);
    const int NumIterations = 5000;
                                                                               
    public static string CreateString(int num)
    {
        int length = rand.Next(1, 20);
        char[] ch = new char[length];
        for(int i = 0; i<length; i++) 
        {
            ch[i] = (char) rand.Next(32, 127);
        }
        _strings[num] = new String(ch);
        return _strings[num];
    }
    
    public static void CreateTable()
    {
        // Creates an array of character arrays, and an array of strings
        // corresponding to those char arrays.
        _strings = new String[_numStrings];
        for(int i=0; i<_numStrings; i++) 
        {
            string str = CreateString(i);
        }
    }

    public static void AppendString(long iterations)
    {        
        for (long i=0; i<iterations; i++)
        {
            StringBuilder sb = new StringBuilder();
            for (int j=0; j<10; j++)
            {
                sb.Append(_strings[(i+j)%_numStrings]);
            }
        }
    }
    

    public static void Main(string [] real_args)
    {    
        CreateTable();
        
        // warmup
        AppendString(100);
        AppendString(NumIterations);    
    }
}
