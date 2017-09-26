// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public static class BinaryFormatterHelpers
    {
        internal static T Clone<T>(T obj,
            ISerializationSurrogate surrogate = null,
            FormatterAssemblyStyle assemblyFormat = FormatterAssemblyStyle.Full,
            TypeFilterLevel filterLevel = TypeFilterLevel.Full,
            FormatterTypeStyle typeFormat = FormatterTypeStyle.TypesAlways)
        {
            BinaryFormatter f;
            if (surrogate == null)
            {
                f = new BinaryFormatter();
            }
            else
            {
                var c = new StreamingContext();
                var s = new SurrogateSelector();
                s.AddSurrogate(obj.GetType(), c, surrogate);
                f = new BinaryFormatter(s, c);
            }
            f.AssemblyFormat = assemblyFormat;
            f.FilterLevel = filterLevel;
            f.TypeFormat = typeFormat;

            using (var s = new MemoryStream())
            {
                f.Serialize(s, obj);
                Assert.NotEqual(0, s.Position);
                s.Position = 0;
                return (T)f.Deserialize(s);
            }
        }

        public static void AssertExceptionDeserializationFails<T>() where T : Exception
        {
            // .NET Core and .NET Native throw PlatformNotSupportedExceptions when deserializing many exceptions.
            // The .NET Framework supports full deserialization.
            if (PlatformDetection.IsFullFramework)
            {
                return;
            }

            // Construct a valid serialization payload. This is necessary as most constructors call
            // the base constructor before throwing a PlatformNotSupportedException, and the base
            // constructor validates the SerializationInfo passed.
            var info = new SerializationInfo(typeof(T), new FormatterConverter());
            info.AddValue("ClassName", "ClassName");
            info.AddValue("Message", "Message");
            info.AddValue("InnerException", null);
            info.AddValue("HelpURL", null);
            info.AddValue("StackTraceString", null);
            info.AddValue("RemoteStackTraceString", null);
            info.AddValue("RemoteStackIndex", 5);
            info.AddValue("HResult", 5);
            info.AddValue("Source", null);
            info.AddValue("ExceptionMethod", null);

            // Serialization constructors are of the form .ctor(SerializationInfo, StreamingContext).
            ConstructorInfo constructor = null;
            foreach (ConstructorInfo c in typeof(T).GetTypeInfo().DeclaredConstructors)
            {
                ParameterInfo[] parameters = c.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(SerializationInfo) && parameters[1].ParameterType == typeof(StreamingContext))
                {
                    constructor = c;
                    break;
                }
            };

            // .NET Native prevents reflection on private constructors on non-serializable types.
            if (constructor == null)
            {
                return;
            }

            Exception ex = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(new object[] { info, new StreamingContext() }));
            Assert.IsType<PlatformNotSupportedException>(ex.InnerException);
        }

        public static byte[] ToByteArray(object obj, 
            FormatterAssemblyStyle assemblyStyle = FormatterAssemblyStyle.Full)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.AssemblyFormat = assemblyStyle;
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static string ToBase64String(object obj, 
            FormatterAssemblyStyle assemblyStyle = FormatterAssemblyStyle.Full)
        {
            byte[] raw = ToByteArray(obj, assemblyStyle);
            return Convert.ToBase64String(raw);
        }

        public static object FromByteArray(byte[] raw, 
            FormatterAssemblyStyle assemblyStyle = FormatterAssemblyStyle.Full)
        {
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.AssemblyFormat = assemblyStyle;
            using (var serializedStream = new MemoryStream(raw))
            {
                return binaryFormatter.Deserialize(serializedStream);
            }
        }

        public static object FromBase64String(string base64Str, 
            FormatterAssemblyStyle assemblyStyle = FormatterAssemblyStyle.Full)
        {
            byte[] raw = Convert.FromBase64String(base64Str);
            return FromByteArray(raw, assemblyStyle);
        }
    }
}
