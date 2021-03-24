// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public static class DesigntimeLicenseContextSerializerTests
    {
        private static string enableBinaryFormatterInTypeConverter = "System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization";
        private static string enableBinaryFormatter = "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization";

        public static bool AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform => PlatformDetection.IsBinaryFormatterSupported && RemoteExecutor.IsSupported;

        private static void VerifyStreamFormatting(Stream stream)
        {
            AppContext.TryGetSwitch(enableBinaryFormatterInTypeConverter, out bool binaryFormatterUsageInTypeConverterIsEnabled);
            long position = stream.Position;
            int firstByte = stream.ReadByte();
            if (binaryFormatterUsageInTypeConverterIsEnabled)
            {
                if (firstByte != 0)
                {
                    Assert.False(true, "Expected this stream to have used BinaryFormatter");
                }
            }
            else
            {
                if (firstByte != 255)
                {
                    Assert.False(true, "Expected this stream to have used BinaryWriter");
                }
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
            {
                RemoteInvokeOptions options = new RemoteInvokeOptions();
                if (useBinaryFormatter)
                {
                    options.RuntimeConfigurationOptions.Add(enableBinaryFormatterInTypeConverter, bool.TrueString);
                }
                RemoteExecutor.Invoke((key) =>
                {
                    Debugger.Launch();
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
                        Reflection.MethodInfo deserializeMethod = designtimeLicenseContextSerializer.GetMethod("Deserialize", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Static);

                        using (MemoryStream stream = new MemoryStream())
                        {
                            long position = stream.Position;
                            System.ComponentModel.Design.DesigntimeLicenseContextSerializer.Serialize(stream, key, context);
                            stream.Seek(position, SeekOrigin.Begin);
                            VerifyStreamFormatting(stream);
                            deserializeMethod.Invoke(null, new object[] { stream, key, runtimeLicenseContext });
                            Hashtable savedLicenseKeys = runtimeLicenseContext.GetType().GetField("_savedLicenseKeys", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runtimeLicenseContext) as Hashtable;
                            Assert.NotNull(savedLicenseKeys);
                            var value = savedLicenseKeys[typeof(int).AssemblyQualifiedName];
                            Assert.True(value is string);
                            Assert.Equal(key, value);
                        }
                    }
                }, key, options).Dispose();
            }
        }

        [ConditionalFact(nameof(AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform))]
        public static void SerializeWithBinaryFormatter_DeserializeWithBinaryWriter()
        {
            RemoteExecutor.Invoke(() =>
            {
                foreach (var key in new System.Collections.Generic.List<string>() { "key", "" })
                {
                    AppContext.SetSwitch(enableBinaryFormatterInTypeConverter, true);
                    AppContext.SetSwitch(enableBinaryFormatter, true);
                    var context = new DesigntimeLicenseContext();
                    context.SetSavedLicenseKey(typeof(int), key);
                    var assembly = typeof(DesigntimeLicenseContextSerializer).Assembly;
                    Type runtimeLicenseContextType = assembly.GetType("System.ComponentModel.Design.RuntimeLicenseContext");
                    Assert.NotNull(runtimeLicenseContextType);
                    object runtimeLicenseContext = Activator.CreateInstance(runtimeLicenseContextType);
                    Assert.NotNull(runtimeLicenseContext);

                    Type designtimeLicenseContextSerializer = assembly.GetType("System.ComponentModel.Design.DesigntimeLicenseContextSerializer");
                    Assert.NotNull(designtimeLicenseContextSerializer);

                    using (MemoryStream stream = new MemoryStream())
                    {
                        long position = stream.Position;
                        System.ComponentModel.Design.DesigntimeLicenseContextSerializer.Serialize(stream, key, context);
                        AppContext.SetSwitch(enableBinaryFormatter, false);
                        Reflection.MethodInfo deserializeMethod = designtimeLicenseContextSerializer.GetMethod("Deserialize", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Static);
                        stream.Seek(position, SeekOrigin.Begin);
                        try
                        {
                            deserializeMethod.Invoke(null, new object[] { stream, key, runtimeLicenseContext });
                        }
                        catch (System.Reflection.TargetInvocationException exception)
                        {
                            var baseException = exception.GetBaseException();
                            Assert.IsType<NotSupportedException>(baseException);
                        }
                    }
                }
            }).Dispose();
        }
    }
}
