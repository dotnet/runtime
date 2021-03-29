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

        /*
        AFAICT, it is not straightforward to call System.ComponentModel.Design.DesigntimeLicenseContextSerializer.Deserialize because:
            1. It is not public
            2. RuntimeLicenseContext is internal. Even if we created this somehow, RuntimeLicenseContext._savedLicenseKeys is internal, so we'd have to initialize the HashTable in this test (which would defeat the purpose of having this test)
            3. The public API that exercises the deserialize method requires the use of a *.lic file that needs to be embedded in some component. This would likely mean checking in some component with a license file and including it in this test. All the examples I've seen use Windows Forms components and add licensing to them. Not sure it is worth the effort here when we can test the same behavior with reflection.
        */
        using (MemoryStream stream = new MemoryStream(TrimmingTests.DesigntimeLicenseContextSerialization_Stream.ASampleStream))
        {
            Type runtimeLicenseContextType = Type.GetType("System.ComponentModel.Design.RuntimeLicenseContext, System.ComponentModel.TypeConverter");
            object runtimeLicenseContext = Activator.CreateInstance(runtimeLicenseContextType, true);
            FieldInfo _savedLicenseKeys = runtimeLicenseContextType.GetField("_savedLicenseKeys", BindingFlags.NonPublic | BindingFlags.Instance);

            Type designtimeLicenseContextSerializer = Type.GetType("System.ComponentModel.Design.DesigntimeLicenseContextSerializer,System.ComponentModel.TypeConverter");
            MethodInfo deserializeMethod = designtimeLicenseContextSerializer.GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Static);

            deserializeMethod.Invoke(null, new object[] { stream, "key", runtimeLicenseContext });
        }

        return 100;
    }
}
