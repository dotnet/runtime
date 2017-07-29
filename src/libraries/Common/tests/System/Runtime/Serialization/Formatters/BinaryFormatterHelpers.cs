// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public static class BinaryFormatterHelpers
    {
        internal static T Clone<T>(T obj)
        {
            var f = new BinaryFormatter();
            using (var s = new MemoryStream())
            {
                f.Serialize(s, obj);
                s.Position = 0;
                return (T)f.Deserialize(s);
            }
        }

        public static void AssertRoundtrips<T>(T expected, params Func<T, object>[] additionalGetters)
            where T : Exception
        {
            for (int i = 0; i < 2; i++)
            {
                if (i > 0) // first time without stack trace, second time with
                {
                    try { throw expected; }
                    catch { }
                }

                // Serialize/deserialize the exception
                T actual = Clone(expected);

                // Verify core state
                if (!PlatformDetection.IsFullFramework) // On full framework, line number may be method body start
                {
                    Assert.Equal(expected.StackTrace, actual.StackTrace);
                    Assert.Equal(expected.ToString(), actual.ToString()); // includes stack trace
                }
                Assert.Equal(expected.Data, actual.Data);
                Assert.Equal(expected.Message, actual.Message);
                Assert.Equal(expected.Source, actual.Source);
                Assert.Equal(expected.HResult, actual.HResult);

                // Verify optional additional state
                foreach (Func<T, object> getter in additionalGetters)
                {
                    Assert.Equal(getter(expected), getter(actual));
                }
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
    }
}
