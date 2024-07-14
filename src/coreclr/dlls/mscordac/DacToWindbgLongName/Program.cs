// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

if (args.Length < 6)
{
    Console.Error.WriteLine("usage: <srcfile> <destname> <hostmach> <targmach> <version> <destdir>");
    return 1;
}

string? buildType = Environment.GetEnvironmentVariable("_BuildType");
if (buildType == "dbg" || buildType == "chk")
{
    buildType = $".{buildType}";
}
else
{
    buildType = null;
}

string srcFile = args[0];
string destName = args[1];
string hostMach = args[2];
string targMach = args[3];
string version = args[4];
string destDir = args[5];

string destFile = Path.Combine(destDir, $"{destName}_{hostMach}_{targMach}_{version}{buildType}.dll");

try
{
    File.Copy(srcFile, destFile, overwrite: true);
}
catch
{
    Console.Error.WriteLine($"Error: Unable to copy {srcFile} to {destFile}");
    throw;
}

return 0;
