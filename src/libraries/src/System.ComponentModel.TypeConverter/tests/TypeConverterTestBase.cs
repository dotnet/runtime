// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Tests
{
    public abstract class TypeConverterTestBase
    {
        public abstract TypeConverter Converter { get; }
        public virtual IEnumerable<ConvertTest> ConvertToTestData() => Enumerable.Empty<ConvertTest>();
        public virtual IEnumerable<ConvertTest> ConvertFromTestData() => Enumerable.Empty<ConvertTest>();

        public virtual bool PropertiesSupported { get; } = false;
        public virtual bool StandardValuesSupported { get; } = false;
        public virtual bool StandardValuesExclusive { get; } = false;

        [Fact]
        public void ConvertTo_DestinationType_Success()
        {
            Assert.All(ConvertToTestData(), convertTest =>
            {
                // We need to duplicate this test code as RemoteInvoke can't
                // create "this" as the declaring type is an abstract class.
                if (convertTest.RemoteInvokeCulture == null)
                {
                    Assert.Equal(convertTest.CanConvert, Converter.CanConvertTo(convertTest.Context, convertTest.DestinationType));

                    if (convertTest.CanConvert)
                    {
                        object actual = Converter.ConvertTo(convertTest.Context, convertTest.Culture, convertTest.Source, convertTest.DestinationType);
                        AssertEqualInstanceDescriptor(convertTest.Expected, actual);
                    }
                    else
                    {
                        Assert.Throws<NotSupportedException>(() => Converter.ConvertTo(convertTest.Context, convertTest.Culture, convertTest.Source, convertTest.DestinationType));
                    }
                }
                else
                {
                    RemoteExecutor.Invoke((typeName, testString) =>
                    {
                        // Deserialize the current test.
                        TypeConverterTestBase testBase = (TypeConverterTestBase)Activator.CreateInstance(Type.GetType(typeName));
                        ConvertTest test = ConvertTest.FromSerializedString(testString);

                        CultureInfo.CurrentCulture = test.RemoteInvokeCulture;
                        Assert.Equal(test.CanConvert, testBase.Converter.CanConvertTo(test.Context, test.DestinationType));

                        if (test.CanConvert)
                        {
                            object actual = testBase.Converter.ConvertTo(test.Context, test.Culture, test.Source, test.DestinationType);
                            Assert.Equal(test.Expected, actual);
                        }
                        else
                        {
                            Assert.Throws<NotSupportedException>(() => testBase.Converter.ConvertTo(test.Context, test.Culture, test.Source, test.DestinationType));
                        }

                        return RemoteExecutor.SuccessExitCode;
                    }, this.GetType().AssemblyQualifiedName, convertTest.GetSerializedString()).Dispose();
                }
            });
        }

        [Fact]
        public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("destinationType", () => Converter.ConvertTo(TypeConverterTests.s_context, null, "", null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Core fixes some NullReferenceExceptions in CanConvertTo")]
        public void CanConvertTo_NullDestinationType_ReturnsFalse()
        {
            Assert.False(Converter.CanConvertTo(null));
        }

        [Fact]
        public void ConvertFrom_DestinationType_Success()
        {
            Assert.All(ConvertFromTestData(), convertTest =>
            {
                // We need to duplicate this test code as RemoteInvoke can't
                // create "this" as the declaring type is an abstract class.
                if (convertTest.RemoteInvokeCulture == null)
                {
                    if (convertTest.Source != null)
                    {
                        Assert.Equal(convertTest.CanConvert, Converter.CanConvertFrom(convertTest.Context, convertTest.Source.GetType()));
                    }

                    if (convertTest.NetCoreExceptionType == null)
                    {
                        object actual = Converter.ConvertFrom(convertTest.Context, convertTest.Culture, convertTest.Source);
                        Assert.Equal(convertTest.Expected, actual);
                    }
                    else
                    {
                        AssertExtensions.Throws(convertTest.NetCoreExceptionType, convertTest.NetFrameworkExceptionType, () => Converter.ConvertFrom(convertTest.Context, convertTest.Culture, convertTest.Source));
                    }
                }
                else
                {
                    RemoteExecutor.Invoke((typeName, testString) =>
                    {
                        // Deserialize the current test.
                        TypeConverterTestBase testBase = (TypeConverterTestBase)Activator.CreateInstance(Type.GetType(typeName));
                        ConvertTest test = ConvertTest.FromSerializedString(testString);

                        CultureInfo.CurrentCulture = test.RemoteInvokeCulture;
                        if (test.Source != null)
                        {
                            Assert.Equal(test.CanConvert, testBase.Converter.CanConvertFrom(test.Context, test.Source.GetType()));
                        }

                        if (test.NetCoreExceptionType == null)
                        {
                            object actual = testBase.Converter.ConvertFrom(test.Context, test.Culture, test.Source);
                            Assert.Equal(test.Expected, actual);
                        }
                        else
                        {
                            AssertExtensions.Throws(test.NetCoreExceptionType, test.NetFrameworkExceptionType, () => testBase.Converter.ConvertFrom(test.Context, test.Culture, test.Source));
                        }

                        return RemoteExecutor.SuccessExitCode;
                    }, this.GetType().AssemblyQualifiedName, convertTest.GetSerializedString()).Dispose();
                }
            });
        }

        [Fact]
        public void GetPropertiesSupported_Invoke_ReturnsExpected()
        {
            TypeConverter converter = Converter;
            Assert.Equal(PropertiesSupported, converter.GetPropertiesSupported());
        }

        [Fact]
        public void GetStandardValuesSupported_Invoke_ReturnsExpected()
        {
            TypeConverter converter = Converter;
            Assert.Equal(StandardValuesSupported, converter.GetStandardValuesSupported());
        }

        [Fact]
        public void GetStandardValuesExclusive_Invoke_ReturnsExpected()
        {
            TypeConverter converter = Converter;
            Assert.Equal(StandardValuesExclusive, converter.GetStandardValuesExclusive());
        }

        private static void AssertEqualInstanceDescriptor(object expected, object actual)
        {
            if (expected is InstanceDescriptor expectedDescriptor && actual is InstanceDescriptor actualDescriptor)
            {
                Assert.Equal(expectedDescriptor.MemberInfo, actualDescriptor.MemberInfo);
                Assert.Equal(expectedDescriptor.Arguments, actualDescriptor.Arguments);
                Assert.Equal(expectedDescriptor.IsComplete, actualDescriptor.IsComplete);
            }
            else
            {
                Assert.Equal(expected, actual);
            }
        }

        [Serializable]
        public class ConvertTest : ISerializable
        {
            public object Source { get; set; }
            public Type DestinationType { get; set; }
            public object Expected { get; set; }
            public CultureInfo Culture { get; set; }
            public Type NetCoreExceptionType { get; set; }
            public Type NetFrameworkExceptionType { get; set; }
            public ITypeDescriptorContext Context { get; set; } = TypeConverterTests.s_context;
            public CultureInfo RemoteInvokeCulture { get; set; }

            public bool CanConvert { get; set; }
            
            public static ConvertTest Valid(object source, object expected, CultureInfo culture = null)
            {
                return new ConvertTest
                {
                    Source = source,
                    DestinationType = expected?.GetType(),
                    Expected = expected,
                    Culture = culture,
                    CanConvert = true,
                };
            }

            public static ConvertTest Throws<TException>(object source, CultureInfo culture = null) where TException : Exception
            {
                return Throws<TException, TException>(source, culture);
            }

            public static ConvertTest Throws<TNetCoreException, TNetFrameworkException>(object source, CultureInfo culture = null) where TNetCoreException : Exception where TNetFrameworkException : Exception
            {
                return new ConvertTest
                {
                    Source = source,
                    Culture = culture,
                    NetCoreExceptionType = typeof(TNetCoreException),
                    NetFrameworkExceptionType = typeof(TNetFrameworkException),
                    CanConvert = true
                };
            }

            public static ConvertTest CantConvertTo(object source, Type destinationType = null, CultureInfo culture = null)
            {
                return new ConvertTest
                {
                    Source = source,
                    DestinationType = destinationType,
                    Culture = culture,
                    NetCoreExceptionType = typeof(NotSupportedException),
                    NetFrameworkExceptionType = typeof(NotSupportedException),
                    CanConvert = false
                };
            }

            public static ConvertTest CantConvertFrom(object source,  CultureInfo culture = null)
            {
                return new ConvertTest
                {
                    Source = source,
                    Culture = culture,
                    NetCoreExceptionType = typeof(NotSupportedException),
                    NetFrameworkExceptionType = typeof(NotSupportedException),
                    CanConvert = false
                };
            }

            public ConvertTest WithContext(ITypeDescriptorContext context)
            {
                Context = context;
                return this;
            }

            public ConvertTest WithRemoteInvokeCulture(CultureInfo culture)
            {
                RemoteInvokeCulture = culture;
                return this;
            }

            public ConvertTest WithInvariantRemoteInvokeCulture() => WithRemoteInvokeCulture(CultureInfo.InvariantCulture);

            public ConvertTest()
            {
            }
  
            protected ConvertTest(SerializationInfo info, StreamingContext context)
            {
                string sourceType = (string)info.GetValue("SourceType", typeof(string));
                if (sourceType != null)
                {
                    Source = info.GetValue(nameof(Source), Type.GetType(sourceType));
                }

                string destinationType = (string)info.GetValue(nameof(DestinationType), typeof(string));
                if (destinationType != null)
                {
                    DestinationType = Type.GetType(destinationType);
                }

                string culture = (string)info.GetValue(nameof(Culture), typeof(string));
                if (culture != null)
                {
                    Culture = culture == string.Empty ? CultureInfo.InvariantCulture : new CultureInfo(culture);
                }

                string contextType = (string)info.GetValue("ContextType", typeof(string));
                if (contextType != null)
                {
                    Context = (ITypeDescriptorContext)info.GetValue(nameof(Context), Type.GetType(contextType));
                }

                string expectedType = (string)info.GetValue("ExpectedType", typeof(string));
                if (expectedType != null)
                {
                    Expected = info.GetValue(nameof(Expected), Type.GetType(expectedType));
                }

                string netCoreExceptionType = (string)info.GetValue(nameof(NetCoreExceptionType), typeof(string));
                if (netCoreExceptionType != null)
                {
                    NetCoreExceptionType = Type.GetType(netCoreExceptionType);
                }

                string netFrameworkExceptionType = (string)info.GetValue(nameof(NetFrameworkExceptionType), typeof(string));
                if (netFrameworkExceptionType != null)
                {
                    NetFrameworkExceptionType = Type.GetType(netFrameworkExceptionType);
                }

                string remoteInvokeCulture = (string)info.GetValue(nameof(RemoteInvokeCulture), typeof(string));
                if (remoteInvokeCulture != null)
                {
                    RemoteInvokeCulture = culture == string.Empty ? CultureInfo.InvariantCulture : new CultureInfo(remoteInvokeCulture);
                }

                CanConvert = (bool)info.GetValue(nameof(CanConvert), typeof(bool));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(Source), Source);
                info.AddValue("SourceType", Source?.GetType()?.AssemblyQualifiedName);
                info.AddValue(nameof(DestinationType), DestinationType?.AssemblyQualifiedName);
                info.AddValue(nameof(Culture), Culture?.Name);

                info.AddValue(nameof(Context), Context);
                info.AddValue("ContextType", Context?.GetType()?.AssemblyQualifiedName);


                info.AddValue(nameof(Expected), Expected);
                info.AddValue("ExpectedType", Expected?.GetType()?.AssemblyQualifiedName);

                info.AddValue(nameof(NetCoreExceptionType), NetCoreExceptionType?.AssemblyQualifiedName);
                info.AddValue(nameof(NetFrameworkExceptionType), NetFrameworkExceptionType?.AssemblyQualifiedName);

                info.AddValue(nameof(RemoteInvokeCulture), RemoteInvokeCulture?.Name);

                info.AddValue(nameof(CanConvert), CanConvert);
            }
   
            public static ConvertTest FromSerializedString(string s)
            {
                byte[] bytes = Convert.FromBase64String(s);
                using (var stream = new MemoryStream(bytes, 0, bytes.Length))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Position = 0;
                    return (ConvertTest)new BinaryFormatter().Deserialize(stream);
                }
            }

            public string GetSerializedString()
            {
                using (var stream = new MemoryStream())
                {
                    new BinaryFormatter().Serialize(stream, this);         
                    return Convert.ToBase64String(stream.ToArray());
                }
            }
        }
    }
}