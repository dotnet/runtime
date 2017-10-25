// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CodeDom;
using System.IO;
using System.Management;
using Xunit;

namespace System.Management.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "WMI not supported via UAP")]
    public class ManagementClassTests
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Get_CodeTypeDeclaration_For_Win32_LogicalDisk(bool includeSystemClassInClassDef, bool systemPropertyClass)
        {
            using (var managementClass = new ManagementClass(null, "Win32_LogicalDisk", null))
            {
                CodeTypeDeclaration classDom = managementClass.GetStronglyTypedClassCode(includeSystemClassInClassDef, systemPropertyClass);
                Assert.Equal(systemPropertyClass ? "ManagementSystemProperties" : "LogicalDisk", classDom.Name);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(CodeLanguage.CSharp)]
        [InlineData(CodeLanguage.VB)]
        public void Get_SourceFile_For_Win32_Processor(CodeLanguage lang)
        {
            var tempFilePath = Path.GetTempFileName();
            var passed = false;
            try 
            {
                using (var managementClass = new ManagementClass(null, "Win32_Processor", null))
                    Assert.True(managementClass.GetStronglyTypedClassCode(lang, tempFilePath, "Wmi.Test.CoreFx"));

                passed = true;
            }
            finally
            {
                if (passed && tempFilePath != null)
                    File.Delete(tempFilePath);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ClassMembers_For_Win32_LogicalDisk()
        {
            var managementClass = new ManagementClass(new ManagementPath("Win32_LogicalDisk"));

            MethodDataCollection methods = managementClass.Methods;
            Assert.True(methods.Count > 0);
            foreach (MethodData method in methods)
                Assert.False(string.IsNullOrWhiteSpace(method.Name));

            PropertyDataCollection properties = managementClass.Properties;
            Assert.True(properties.Count > 0);
            foreach (PropertyData property in properties)
                Assert.False(string.IsNullOrWhiteSpace(property.Name));

            QualifierDataCollection qualifiers = managementClass.Qualifiers;
            foreach (QualifierData qualifier in qualifiers)
                Assert.False(string.IsNullOrWhiteSpace(qualifier.Name));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void EnumerateInstances_For_Win32_LogicalDisk()
        {
            using (var managementClass = new ManagementClass(new ManagementPath("Win32_LogicalDisk")))
            using (ManagementObjectCollection instances = managementClass.GetInstances())
            using (ManagementObjectCollection.ManagementObjectEnumerator instancesEnumerator = instances.GetEnumerator())
            {
                while (instancesEnumerator.MoveNext())
                {
                    ManagementObject instance = (ManagementObject)instancesEnumerator.Current;
                    Assert.NotNull(instance);
                    var clone = instance.Clone();
                    Assert.NotNull(clone);
                    Assert.False(ReferenceEquals(instance, clone));
                }
            }
        }
    }
}
