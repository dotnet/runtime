// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.IO;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesigntimeLicenseContextSerializerTests
    {
        [Fact]
        public void SerializeAndDeserialize()
        {
            var context = new DesigntimeLicenseContext();
            context.SetSavedLicenseKey(typeof(int), "key");
            var assembly = typeof(DesigntimeLicenseContextSerializer).Assembly;
            Type runtimeLicenseContextType = assembly.GetType("System.ComponentModel.Design.RuntimeLicenseContext");
            Assert.NotNull(runtimeLicenseContextType);
            object runtimeLicenseContext = Activator.CreateInstance(runtimeLicenseContextType);
            Assert.NotNull(runtimeLicenseContext);

            Type designtimeLicenseContextSerializer = assembly.GetType("System.ComponentModel.Design.DesigntimeLicenseContextSerializer");
            Assert.NotNull(designtimeLicenseContextSerializer);
            Reflection.MethodInfo deserializeMethod = designtimeLicenseContextSerializer.GetMethod("Deserialize", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Static);

            using (MemoryStream stream = new MemoryStream())
            {
                long position = stream.Position;
                System.ComponentModel.Design.DesigntimeLicenseContextSerializer.Serialize(stream, "crypto", context);
                stream.Seek(position, SeekOrigin.Begin);
                deserializeMethod.Invoke(null, new object[] { stream, "crypto", runtimeLicenseContext });
                Hashtable savedLicenseKeys = runtimeLicenseContext.GetType().GetField("_savedLicenseKeys").GetValue(runtimeLicenseContext) as Hashtable;
                Assert.NotNull(savedLicenseKeys);
                var value = savedLicenseKeys[typeof(int).AssemblyQualifiedName];
                Assert.True(value is string);
                Assert.Equal("key", value);
            }
        }
    }
}
