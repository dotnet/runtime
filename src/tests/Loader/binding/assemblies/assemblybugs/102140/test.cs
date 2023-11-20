// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using Xunit;

public class test
{
    private static double numtests = 4.0;

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("Test1: new AssemblyName(\"server2\")");
        AssemblyName asmN1 = new AssemblyName("server2");
        int ret = Check(asmN1);
        Console.WriteLine("Test2: new AssemblyName(\"server2, Version=0.0.0.0\")");
        AssemblyName asmN2 = new AssemblyName("server2, Version=0.0.0.0");
        ret = ret + Check(asmN2);
        Console.WriteLine("Test3: new AssemblyName(\"server2, Culture=neutral\")");
        AssemblyName asmN3 = new AssemblyName("server2, Culture=neutral");
        ret = ret + Check(asmN3);
        Console.WriteLine("Test4: new AssemblyName(\"server2, Culture=neutral, Version=0.0.0.0\")");
        AssemblyName asmN4 = new AssemblyName("server2, Culture=neutral, Version=0.0.0.0");
        ret = ret + Check(asmN4);
        if(ret/numtests==100.0){
            Console.WriteLine("All Passed!");
            return 100;
        }else{
            Console.WriteLine("Failed!");
            return ret;
        }
    }

    public static int Check(AssemblyName asmN)
    {
        String strVersion = asmN.ToString();
        int index = strVersion.ToLower().IndexOf("version=");
        if(asmN.Version==null){
            if(index==-1){
                Console.WriteLine("Passed: both asmName.ToString() version and asmName.Version are null.");
                return 100;
            }else{
                Console.WriteLine("Failed: asmName.Version != asmName.ToString() Version");
                Console.WriteLine ("\tasmName.Version = \"{0}\"", asmN.Version);
                Console.WriteLine ("\tasmName.ToString() = \"{0}\"", strVersion);
                return 101;
            }
        }else{
            strVersion = strVersion.Substring(index+8,7);
            if(strVersion.Equals(asmN.Version.ToString())){
                Console.WriteLine("Passed: asmName.Version == asmName.ToString() Version");
                return 100;
            }else{
                Console.WriteLine("Failed: asmName.Version != asmName.ToString() Version");
                Console.WriteLine ("\tasmName.Version = \"{0}\"", asmN.Version);
                Console.WriteLine ("\tasmName.ToString() = \"{0}\"", strVersion);
                return 101;
            }
        }
    }
}
