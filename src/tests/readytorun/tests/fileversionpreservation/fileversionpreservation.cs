using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Xunit;

public class Program 
{
    [Fact]
    public static int TestEntryPoint() 
    {
        return RunTest();
    }

    public static bool CheckVersionResourceUsingApi(string filename)
    {
        FileVersionInfo fileVer = FileVersionInfo.GetVersionInfo(filename);

        bool success = fileVer.FileVersion == "2.0.1.9";
        if (!success)
            Console.WriteLine($"{filename} has version \"{fileVer.FileVersion}\"");

        return success;
    }

    public static int RunTest()
    {
        string ilfilename = typeof(Program).Assembly.Location;
        string nifilename = ilfilename.Replace(".exe", ".ni.exe");

        bool success = true;

        if (!CheckVersionResourceUsingApi(ilfilename))
            success = false;

        if (!CheckVersionResourceUsingApi(nifilename))
            success = false;

        return success ? 100 : -1;
    }
}
