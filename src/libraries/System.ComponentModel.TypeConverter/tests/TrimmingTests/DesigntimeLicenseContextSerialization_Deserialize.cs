// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;

/// <summary>
/// Tests that the System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization
/// property works as expected when used in a trimmed application.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        AppContext.SetSwitch("System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization", true);

        using (var stream = new MemoryStream(TrimmingTests.SampleStream.ASampleStream))
        {
            var assembly = typeof(DesigntimeLicenseContextSerializer).Assembly;
            Type runtimeLicenseContextType = assembly.GetType("System.ComponentModel.Design.RuntimeLicenseContext");
            object runtimeLicenseContext = Activator.CreateInstance(runtimeLicenseContextType);
            FieldInfo _savedLicenseKeys = runtimeLicenseContextType.GetField("_savedLicenseKeys", BindingFlags.NonPublic | BindingFlags.Instance);

            Type designtimeLicenseContextSerializer = assembly.GetType("System.ComponentModel.Design.DesigntimeLicenseContextSerializer");
            MethodInfo deserializeMethod = designtimeLicenseContextSerializer.GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Static);

            deserializeMethod.Invoke(null, new object[] { stream, "key", runtimeLicenseContext });
        }

        return 100;
    }
}
