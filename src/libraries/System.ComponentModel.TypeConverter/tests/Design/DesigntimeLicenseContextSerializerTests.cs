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
        private static string enableBinaryFormatterInTypeConverter = "System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization";
        private static string enableBinaryFormatter = "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization";

        public static bool AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform => PlatformDetection.IsBinaryFormatterSupported && RemoteExecutor.IsSupported;

        [ConditionalFact(nameof(AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform))]
        public static void SerializeAndDeserialize()
        {
            RemoteExecutor.Invoke(() =>
            {
                foreach (var (useBinaryFormatter, key) in new System.Collections.Generic.List<Tuple<bool, string>>() {
                    new Tuple<bool, string>(false, "key" ),
                    new Tuple<bool, string>(true, "key" ),
                    new Tuple<bool, string>(false, "" ),
                    new Tuple<bool, string>(true, "" ),
            })
                {
                    {
                        AppContext.SetSwitch("TestSwitch.LocalAppContext.DisableCaching", true);
                        if (!useBinaryFormatter)
                        {
                            AppContext.SetSwitch(enableBinaryFormatterInTypeConverter, false);
                        }
                        else
                        {
                            AppContext.SetSwitch(enableBinaryFormatterInTypeConverter, true);
                        }
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
                            deserializeMethod.Invoke(null, new object[] { stream, key, runtimeLicenseContext });
                            Hashtable savedLicenseKeys = runtimeLicenseContext.GetType().GetField("_savedLicenseKeys", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runtimeLicenseContext) as Hashtable;
                            Assert.NotNull(savedLicenseKeys);
                            var value = savedLicenseKeys[typeof(int).AssemblyQualifiedName];
                            Assert.True(value is string);
                            Assert.Equal(key, value);
                        }
                    }
                }
            }).Dispose();
        }

        [ConditionalFact(nameof(AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform))]
        public static void SerializeWithBinaryFormatter_DeserializeWithBinaryWriter()
        {
            RemoteExecutor.Invoke(() =>
            {
                foreach (var key in new System.Collections.Generic.List<string>() { "key", "" })
                {
                    AppContext.SetSwitch("TestSwitch.LocalAppContext.DisableCaching", true);
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
