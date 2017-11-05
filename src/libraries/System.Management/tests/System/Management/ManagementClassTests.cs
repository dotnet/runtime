// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CodeDom;
using System.IO;
using Xunit;

namespace System.Management.Tests
{
    public class ManagementClassTests
    {
        [ConditionalTheory(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
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

        [ConditionalTheory(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
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

        [ConditionalTheory(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
        [InlineData(CodeLanguage.JScript)]
        [InlineData(CodeLanguage.Mcpp)]
        [InlineData(CodeLanguage.VJSharp)]
        public void Throw_On_Unsupported_Languages(CodeLanguage lang)
        {
            // On full framework JScript is supported and no exception raised
            if (lang == CodeLanguage.JScript && PlatformDetection.IsFullFramework)
                return;

            var tempFilePath = Path.GetTempFileName();
            var managementClass = new ManagementClass(null, "Win32_Processor", null);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => managementClass.GetStronglyTypedClassCode(lang, tempFilePath, "Wmi.Test.CoreFx"));
            }
            finally
            {
                managementClass = null;
                GC.Collect(2);
                GC.WaitForPendingFinalizers();
                if (tempFilePath != null)
                    File.Delete(tempFilePath);
            }
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
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

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsWmiSupported))]
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

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsElevatedAndSupportsWmi))]
        public void Create_Delete_Namespace()
        {
            using (var rootNamespace = new ManagementClass("root:__namespace"))
            using (ManagementObject newNamespace = rootNamespace.CreateInstance())
            {
                const string NewNamespace = "CoreFx_Create_Delete_Namespace_Test";
                newNamespace["Name"] = NewNamespace;
                newNamespace.Put();

                ManagementObject targetNamespace = new ManagementObject($"root:__namespace.Name='{NewNamespace}'");
                Assert.Equal(NewNamespace, targetNamespace["Name"]);

                // If any of the steps below fail it is likely that the new namespace was not deleted, likely it will have to
                // be deleted via a tool like wbemtest.
                targetNamespace.Delete();
                ManagementException managementException = Assert.Throws<ManagementException>(() => targetNamespace.Get());
                Assert.Equal(ManagementStatus.NotFound, managementException.ErrorCode);
            }
        }
    }
}
