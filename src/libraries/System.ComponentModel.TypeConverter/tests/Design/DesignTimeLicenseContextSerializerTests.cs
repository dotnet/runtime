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
        [Theory]
        [InlineData(false, "key")]
        [InlineData(true, "key")]
        [InlineData(false, "")]
        [InlineData(true, "")]
        public void SerializeAndDeserialize(bool useBinaryFormatter, string key)
        {
            AppContext.SetSwitch("TestSwitch.LocalAppContext.DisableCaching", true);
            if (!useBinaryFormatter)
            {
                AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", false);
            }
            else
            {
                AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
            }
            var context = new DesigntimeLicenseContext();
            context.SetSavedLicenseKey(typeof(int), key);
            var assembly = typeof(DesigntimeLicenseContextSerializer).Assembly;
            Type runtimeLicenseContextType = assembly.GetType("System.ComponentModel.Design.RuntimeLicenseContext");
            Assert.NotNull(runtimeLicenseContextType);
            object runtimeLicenseContext = Activator.CreateInstance(runtimeLicenseContextType);
            FieldInfo _savedLicenseKeys = runtimeLicenseContextType.GetField("_savedLicenseKeys");
            Assert.NotNull(_savedLicenseKeys);
            _savedLicenseKeys.SetValue(runtimeLicenseContext, new Hashtable());
            Assert.NotNull(runtimeLicenseContext);

            Type designtimeLicenseContextSerializer = assembly.GetType("System.ComponentModel.Design.DesigntimeLicenseContextSerializer");
            Assert.NotNull(designtimeLicenseContextSerializer);
            Reflection.MethodInfo deserializeMethod = designtimeLicenseContextSerializer.GetMethod("Deserialize", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Static);

            using (MemoryStream stream = new MemoryStream())
            {
                long position = stream.Position;
                System.ComponentModel.Design.DesigntimeLicenseContextSerializer.Serialize(stream, key, context);
                stream.Seek(position, SeekOrigin.Begin);
                deserializeMethod.Invoke(null, new object[] { stream, key, runtimeLicenseContext });
                Hashtable savedLicenseKeys = runtimeLicenseContext.GetType().GetField("_savedLicenseKeys").GetValue(runtimeLicenseContext) as Hashtable;
                Assert.NotNull(savedLicenseKeys);
                var value = savedLicenseKeys[typeof(int).AssemblyQualifiedName];
                Assert.True(value is string);
                Assert.Equal(key, value);
            }
        }

        [Theory]
        [InlineData("key")]
        [InlineData("")]
        public void SerializeWithBinaryFormatter_DeserializeWithBinaryWriter(string key)
        {
            AppContext.SetSwitch("TestSwitch.LocalAppContext.DisableCaching", true);
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
            var context = new DesigntimeLicenseContext();
            context.SetSavedLicenseKey(typeof(int), key);
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
                System.ComponentModel.Design.DesigntimeLicenseContextSerializer.Serialize(stream, key, context);
                AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", false);
                // Cannot deserialize anymore
                stream.Seek(position, SeekOrigin.Begin);
                try
                {
                    deserializeMethod.Invoke(null, new object[] {stream, key, runtimeLicenseContext});
                }
                catch (System.Reflection.TargetInvocationException exception)
                {
                    var baseException = exception.GetBaseException();
                    Assert.True(baseException is System.NotSupportedException);
                }
                catch (System.Exception catchAll)
                {
                    throw catchAll;
                }
            }
        }
    }
}
