// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public static class DesigntimeLicenseContextSerializerTests
    {
        private const string enableBinaryFormatterInTypeConverter = "System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization";
        private const string enableBinaryFormatter = "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization";

        public static bool AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform => PlatformDetection.IsBinaryFormatterSupported && RemoteExecutor.IsSupported;

        private static void VerifyStreamFormatting(Stream stream)
        {
            AppContext.TryGetSwitch(enableBinaryFormatterInTypeConverter, out bool binaryFormatterUsageInTypeConverterIsEnabled);
            long position = stream.Position;
            int firstByte = stream.ReadByte();
            if (binaryFormatterUsageInTypeConverterIsEnabled)
            {
                Assert.Equal(0, firstByte);
            }
            else
            {
                Assert.Equal(255, firstByte);
            }
            stream.Seek(position, SeekOrigin.Begin);
        }

        [ConditionalTheory(nameof(AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform))]
        [InlineData(false, "key")]
        [InlineData(true, "key")]
        [InlineData(false, "")]
        [InlineData(true, "")]
        public static void SerializeAndDeserialize(bool useBinaryFormatter, string key)
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            if (useBinaryFormatter)
            {
                options.RuntimeConfigurationOptions.Add(enableBinaryFormatterInTypeConverter, bool.TrueString);
            }
            RemoteExecutor.Invoke((key) =>
            {
                var context = new DesigntimeLicenseContext();
                context.SetSavedLicenseKey(typeof(int), key);
                var assembly = typeof(DesigntimeLicenseContextSerializer).Assembly;
                Type runtimeLicenseContextType = assembly.GetType("System.ComponentModel.Design.RuntimeLicenseContext");
                Assert.NotNull(runtimeLicenseContextType);
                object runtimeLicenseContext = Activator.CreateInstance(runtimeLicenseContextType);
                FieldInfo _savedLicenseKeys = runtimeLicenseContextType.GetField("_savedLicenseKeys", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(_savedLicenseKeys);
                _savedLicenseKeys.SetValue(runtimeLicenseContext, new Hashtable());
                Assert.NotNull(runtimeLicenseContext);

                Type designtimeLicenseContextSerializer = assembly.GetType("System.ComponentModel.Design.DesigntimeLicenseContextSerializer");
                Assert.NotNull(designtimeLicenseContextSerializer);
                MethodInfo deserializeMethod = designtimeLicenseContextSerializer.GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Static);

                using (MemoryStream stream = new MemoryStream())
                {
                    long position = stream.Position;
                    DesigntimeLicenseContextSerializer.Serialize(stream, key, context);
                    stream.Seek(position, SeekOrigin.Begin);
                    VerifyStreamFormatting(stream);
                    deserializeMethod.Invoke(null, new object[] { stream, key, runtimeLicenseContext });
                    Hashtable savedLicenseKeys = runtimeLicenseContext.GetType().GetField("_savedLicenseKeys", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runtimeLicenseContext) as Hashtable;
                    Assert.NotNull(savedLicenseKeys);
                    var value = savedLicenseKeys[typeof(int).AssemblyQualifiedName];
                    Assert.True(value is string);
                    Assert.Equal(key, value);
                }
            }, key, options).Dispose();
        }

        [ConditionalTheory(nameof(AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform))]
        [InlineData("key")]
        [InlineData("")]
        public static void SerializeWithBinaryFormatter_DeserializeWithBinaryWriter(string key)
        {
            AppContext.SetSwitch(enableBinaryFormatter, true);
            AppContext.SetSwitch(enableBinaryFormatterInTypeConverter, true);
            var context = new DesigntimeLicenseContext();
            context.SetSavedLicenseKey(typeof(int), key);
            string tempPath = Path.GetTempPath();
            try
            {

                using (MemoryStream stream = new MemoryStream())
                {
                    long position = stream.Position;
                    DesigntimeLicenseContextSerializer.Serialize(stream, key, context);
                    stream.Seek(position, SeekOrigin.Begin);
                    VerifyStreamFormatting(stream);

                    using (FileStream outStream = File.Create(Path.Combine(tempPath, "_temp_SerializeWithBinaryFormatter_DeserializeWithBinaryWriter")))
                    {
                        stream.Seek(position, SeekOrigin.Begin);
                        stream.CopyTo(outStream);
                    }
                }

                RemoteInvokeHandle handle = RemoteExecutor.Invoke((key) =>
                {
                    var assembly = typeof(DesigntimeLicenseContextSerializer).Assembly;
                    Type runtimeLicenseContextType = assembly.GetType("System.ComponentModel.Design.RuntimeLicenseContext");
                    Assert.NotNull(runtimeLicenseContextType);
                    object runtimeLicenseContext = Activator.CreateInstance(runtimeLicenseContextType);
                    Assert.NotNull(runtimeLicenseContext);
                    FieldInfo _savedLicenseKeys = runtimeLicenseContextType.GetField("_savedLicenseKeys", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(_savedLicenseKeys);
                    _savedLicenseKeys.SetValue(runtimeLicenseContext, new Hashtable());

                    Type designtimeLicenseContextSerializer = assembly.GetType("System.ComponentModel.Design.DesigntimeLicenseContextSerializer");
                    Assert.NotNull(designtimeLicenseContextSerializer);
                    MethodInfo deserializeMethod = designtimeLicenseContextSerializer.GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.NotNull(deserializeMethod);

                    string tempPath = Path.GetTempPath();
                    using (FileStream stream = File.Open(Path.Combine(tempPath, "_temp_SerializeWithBinaryFormatter_DeserializeWithBinaryWriter"), FileMode.Open))
                    {
                        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => deserializeMethod.Invoke(null, new object[] { stream, key, runtimeLicenseContext }));
                        Assert.IsType<NotSupportedException>(exception.InnerException);
                    }
                }, key);

                handle.Process.WaitForExit();
                handle.Dispose();
            }
            finally
            {
                File.Delete(Path.Combine(tempPath, "_temp_SerializeWithBinaryFormatter_DeserializeWithBinaryWriter"));
            }
        }
    }
}
