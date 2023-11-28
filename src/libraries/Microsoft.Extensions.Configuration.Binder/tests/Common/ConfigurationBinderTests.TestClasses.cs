// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions
#if BUILDING_SOURCE_GENERATOR_TESTS
    .SourceGeneration
#endif
    .Configuration.Binder.Tests
{
    public partial class ConfigurationBinderTests
    {
        public class DerivedOptions : ComplexOptions
        {
            public override string Virtual
            {
                get
                {
                    return base.Virtual;
                }
                set
                {
                    base.Virtual = "Derived:" + value;
                }
            }
        }

        public class GenericOptions<T>
        {
            public T Value { get; set; }
        }

        public record GenericOptionsRecord<T>(T Value);

        public class GenericOptionsWithParamCtor<T>
        {
            public GenericOptionsWithParamCtor(T value) => Value = value;

            public T Value { get; }
        }

        public class OptionsWithNesting
        {
            public NestedOptions Nested { get; set; }

            public class NestedOptions
            {
                public int Value { get; set; }
            }
        }

        public class ConfigurationInterfaceOptions
        {
            public IConfigurationSection Section { get; set; }
        }

        public class DerivedOptionsWithIConfigurationSection : DerivedOptions
        {
            public IConfigurationSection DerivedSection { get; set; }
        }

        public record struct RecordStructTypeOptions(string Color, int Length);

        public record RecordOptionsWithNesting(int Number, RecordOptionsWithNesting.RecordNestedOptions Nested1,
            RecordOptionsWithNesting.RecordNestedOptions Nested2 = null!)
        {
            public record RecordNestedOptions(string ValueA, int ValueB);
        }

        // Here, the constructor has three parameters, but not all of those match
        // match to a property or field
        public class ClassWhereParametersDoNotMatchProperties
        {
            public string Name { get; }
            public string Address { get; }

            public ClassWhereParametersDoNotMatchProperties(string name, string address, int age)
            {
                Name = name;
                Address = address;
            }
        }

        // Here, the constructor has three parameters, and two of them match properties
        // and one of them match a field.
        public class ClassWhereParametersMatchPropertiesAndFields
        {
            private int Age;

            public string Name { get; }
            public string Address { get; }

            public ClassWhereParametersMatchPropertiesAndFields(string name, string address, int age)
            {
                Name = name;
                Address = address;
                Age = age;
            }

            public int GetAge() => Age;
        }

        public record RecordWhereParametersHaveDefaultValue(string Name, string Address, int Age = 42);

        public class ClassWhereParametersHaveDefaultValue
        {
            public string? Name { get; }
            public string Address { get; }
            public int Age { get; }
            public float F { get; }
            public double D { get; }
            public decimal M { get; }
            public StringComparison SC { get; }
            public char C { get; }
            public int? NAge { get; }
            public float? NF { get; }
            public double? ND { get; }
            public decimal? NM { get; }
            public StringComparison? NSC { get; }
            public char? NC { get; }

            public ClassWhereParametersHaveDefaultValue(string? name = "John Doe", string address = "1 Microsoft Way",
                int age = 42, float f = 42.0f, double d = 3.14159, decimal m = 3.1415926535897932384626433M, StringComparison sc = StringComparison.Ordinal, char c = 'q',
                int? nage = 42, float? nf = 42.0f, double? nd = 3.14159, decimal? nm = 3.1415926535897932384626433M, StringComparison? nsc = StringComparison.Ordinal, char? nc = 'q')
            {
                Name = name;
                Address = address;
                Age = age;
                F = f;
                D = d;
                M = m;
                SC = sc;
                C = c;
                NAge = nage;
                NF = nf;
                ND = nd;
                NM = nm;
                NSC = nsc;
                NC = nc;
            }
        }

        public class ClassWithPrimaryCtor(string color, int length)
        {
            public string Color { get; } = color;
            public int Length { get; } = length;
        }

        public class ClassWithPrimaryCtorDefaultValues(string color = "blue", int length = 15, decimal height = 5.946238490567943927384M, EditorBrowsableState eb = EditorBrowsableState.Never)
        {
            public string Color { get; } = color;
            public int Length { get; } = length;
            public decimal Height { get; } = height;
            public EditorBrowsableState EB { get;} = eb;
        }
        public record RecordTypeOptions(string Color, int Length);

        public record Line(string Color, int Length, int Thickness);

        public class ClassWithMatchingParametersAndProperties
        {
            private readonly string _color;

            public ClassWithMatchingParametersAndProperties(string Color, int Length)
            {
                _color = Color;
                this.Length = Length;
            }

            public int Length { get; set; }

            public string Color
            {
                get => _color;
                init => _color = "the color is " + value;
            }
        }

        public readonly record struct ReadonlyRecordStructTypeOptions(string Color, int Length);

        public class ContainerWithNestedImmutableObject
        {
            public string ContainerName { get; set; }
            public ImmutableLengthAndColorClass LengthAndColor { get; set; }
        }

        public struct MutableStructWithConstructor
        {
            public MutableStructWithConstructor(string randomParameter)
            {
                Color = randomParameter;
                Length = randomParameter.Length;
            }

            public string Color { get; set; }
            public int Length { get; set; }
        }

        public class ImmutableLengthAndColorClass
        {
            public ImmutableLengthAndColorClass(string color, int length)
            {
                Color = color;
                Length = length;
            }

            public string Color { get; }
            public int Length { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructor
        {
            public ImmutableClassWithOneParameterizedConstructor(string string1, int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructorButWithInParameter
        {
            public ImmutableClassWithOneParameterizedConstructorButWithInParameter(in string string1, int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructorButWithRefParameter
        {
            public ImmutableClassWithOneParameterizedConstructorButWithRefParameter(string string1, ref int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructorButWithOutParameter
        {
            public ImmutableClassWithOneParameterizedConstructorButWithOutParameter(string string1, int int1,
                string string2, out decimal int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                int2 = 0;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithMultipleParameterizedConstructors
        {
            public ImmutableClassWithMultipleParameterizedConstructors(string string1, int int1)
            {
                String1 = string1;
                Int1 = int1;
            }

            public ImmutableClassWithMultipleParameterizedConstructors(string string1, int int1, string string2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
            }

            public ImmutableClassWithMultipleParameterizedConstructors(string string1, int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public ImmutableClassWithMultipleParameterizedConstructors(string string1)
            {
                String1 = string1;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class SemiImmutableClass
        {
            public SemiImmutableClass(string color, int length)
            {
                Color = color;
                Length = length;
            }

            public string Color { get; }
            public int Length { get; }
            public decimal Thickness { get; set; }
        }

        public class SemiImmutableClassWithInit
        {
            public SemiImmutableClassWithInit(string color, int length)
            {
                Color = color;
                Length = length;
            }

            public string Color { get; }
            public int Length { get; }
            public decimal Thickness { get; init; }
        }

        public struct ValueTypeOptions
        {
            public int MyInt32 { get; set; }
            public string MyString { get; set; }
        }

        public class ByteArrayOptions
        {
            public byte[] MyByteArray { get; set; }
        }

        public enum TestSettingsEnum
        {
            Option1,
            Option2,
        }

        public class CollectionsBindingWithErrorOnUnknownConfiguration
        {
            public class MyModelContainingArray
            {
                public TestSettingsEnum[] Enums { get; set; }
            }

            public class MyModelContainingADictionary
            {
                public Dictionary<string, TestSettingsEnum> Enums { get; set; }
            }

            [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
            public void WithFlagUnset_NoExceptionIsThrownWhenFailingToParseEnumsInAnArrayAndValidItemsArePreserved()
            {
                var dic = new Dictionary<string, string>
                            {
                                {"Section:Enums:0", "Option1"},
                                {"Section:Enums:1", "Option3"}, // invalid - ignored
                                {"Section:Enums:2", "Option4"}, // invalid - ignored
                                {"Section:Enums:3", "Option2"},
                            };

                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddInMemoryCollection(dic);
                var config = configurationBuilder.Build();
                var configSection = config.GetSection("Section");

                var model = configSection.Get<MyModelContainingArray>(o => o.ErrorOnUnknownConfiguration = false);

                Assert.Equal(2, model.Enums.Length);
                Assert.Equal(TestSettingsEnum.Option1, model.Enums[0]);
                Assert.Equal(TestSettingsEnum.Option2, model.Enums[1]);
            }

            [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
            public void WithFlagUnset_NoExceptionIsThrownWhenFailingToParseEnumsInADictionaryAndValidItemsArePreserved()
            {
                var dic = new Dictionary<string, string>
                            {
                                {"Section:Enums:First", "Option1"},
                                {"Section:Enums:Second", "Option3"}, // invalid - ignored
                                {"Section:Enums:Third", "Option4"}, // invalid - ignored
                                {"Section:Enums:Fourth", "Option2"},
                            };

                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddInMemoryCollection(dic);
                var config = configurationBuilder.Build();
                var configSection = config.GetSection("Section");

                var model = configSection.Get<MyModelContainingADictionary>(o =>
                    o.ErrorOnUnknownConfiguration = false);

                Assert.Equal(2, model.Enums.Count);
                Assert.Equal(TestSettingsEnum.Option1, model.Enums["First"]);
                Assert.Equal(TestSettingsEnum.Option2, model.Enums["Fourth"]);
            }

            [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
            public void WithFlagSet_AnExceptionIsThrownWhenFailingToParseEnumsInAnArray()
            {
                var dic = new Dictionary<string, string>
                            {
                                {"Section:Enums:0", "Option1"},
                                {"Section:Enums:1", "Option3"}, // invalid - exception thrown
                                {"Section:Enums:2", "Option1"},
                            };

                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddInMemoryCollection(dic);
                var config = configurationBuilder.Build();
                var configSection = config.GetSection("Section");

                var exception = Assert.Throws<InvalidOperationException>(
                    () => configSection.Get<MyModelContainingArray>(o => o.ErrorOnUnknownConfiguration = true));

                Assert.Equal(
                    SR.Format(SR.Error_GeneralErrorWhenBinding, nameof(BinderOptions.ErrorOnUnknownConfiguration)),
                    exception.Message);
            }

            [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
            public void WithFlagSet_AnExceptionIsThrownWhenFailingToParseEnumsInADictionary()
            {
                var dic = new Dictionary<string, string>
                            {
                                {"Section:Enums:First", "Option1"},
                                {"Section:Enums:Second", "Option3"}, // invalid - exception thrown
                                {"Section:Enums:Third", "Option1"},
                            };

                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddInMemoryCollection(dic);
                var config = configurationBuilder.Build();
                var configSection = config.GetSection("Section");

                var exception = Assert.Throws<InvalidOperationException>(
                    () => configSection.Get<MyModelContainingADictionary>(o =>
                        o.ErrorOnUnknownConfiguration = true));

                Assert.Equal(
                    SR.Format(SR.Error_GeneralErrorWhenBinding, nameof(BinderOptions.ErrorOnUnknownConfiguration)),
                    exception.Message);
            }
        }

        public record RootConfig(NestedConfig Nested);

        public record NestedConfig(string MyProp);

        public class OptionWithCollectionProperties
        {
            private int _otherCode;
            private int _otherCodeNullable;
            private string _otherCodeString = "default";
            private object _otherCodeNull;
            private Uri _otherCodeUri;
            private ICollection<string> blacklist = new HashSet<string>();

            public ICollection<string> Blacklist
            {
                get => this.blacklist;
                set
                {
                    this.blacklist = value ?? new HashSet<string>();
                    this.ParsedBlacklist = this.blacklist.Select(b => b).ToList();
                }
            }

            public int HttpStatusCode { get; set; } = 0;

            // ParsedBlacklist initialized using the setter of Blacklist.
            public ICollection<string> ParsedBlacklist { get; private set; } = new HashSet<string>();

            // This does not have a match in the configuration, however the setter should be called during the binding:
            public int OtherCode
            {
                get => _otherCode;
                set => _otherCode = value == 0 ? 2 : value;
            }

            // These do not have any match in the configuration, and the setters should not be called during the binding:
            public int? OtherCodeNullable
            {
                get => _otherCodeNullable;
                set => _otherCodeNullable = !value.HasValue ? 3 : value.Value;
            }

            public string OtherCodeString
            {
                get => _otherCodeString;
                set => _otherCodeString = value;
            }

            public object? OtherCodeNull
            {
                get => _otherCodeNull;
                set => _otherCodeNull = value is null ? 4 : value;
            }

            public Uri OtherCodeUri
            {
                get => _otherCodeUri;
                set => _otherCodeUri = value is null ? new Uri("hello") : value;
            }
        }

        public interface ISomeInterface
        {
        }

        public class ClassWithoutPublicConstructor
        {
            private ClassWithoutPublicConstructor()
            {
            }
        }

        public class ThrowsWhenActivated
        {
            public ThrowsWhenActivated()
            {
                throw new Exception();
            }
        }

        public class NestedOptions1
        {
            public NestedOptions2 NestedOptions2Property { get; set; }
        }

        public class NestedOptions2
        {
            public ISomeInterface ISomeInterfaceProperty { get; set; }
        }

        public class TestOptions
        {
            public ISomeInterface ISomeInterfaceProperty { get; set; }

            public ClassWithoutPublicConstructor ClassWithoutPublicConstructorProperty { get; set; }
            public ClassWhereParametersDoNotMatchProperties ClassWhereParametersDoNotMatchPropertiesProperty { get; set; }
            public Line LineProperty { get; set; }
            public ClassWhereParametersHaveDefaultValue ClassWhereParametersHaveDefaultValueProperty { get; set; }
            public ClassWhereParametersMatchPropertiesAndFields ClassWhereParametersMatchPropertiesAndFieldsProperty { get; set; }
            public RecordWhereParametersHaveDefaultValue RecordWhereParametersHaveDefaultValueProperty { get; set; }

            public int IntProperty { get; set; }

            public ThrowsWhenActivated ThrowsWhenActivatedProperty { get; set; }

            public NestedOptions1 NestedOptionsProperty { get; set; }
        }

        public class ClassWithReadOnlyPropertyThatThrows
        {
            public string StringThrows => throw new InvalidOperationException(nameof(StringThrows));

            public IEnumerable<int> EnumerableThrows => throw new InvalidOperationException(nameof(EnumerableThrows));

            public string Safe { get; set; }
        }

        public struct StructWithNestedStructs
        {
            public Nested ReadWriteNestedStruct { get; set; }

            public Nested ReadOnlyNestedStruct { get; }

            public Nested? NullableNestedStruct { get; set; }

            public struct Nested
            {
                public string String { get; set; }
                public DeeplyNested DeeplyNested { get; set; }
            }

            public struct DeeplyNested
            {
                public int Int32 { get; set; }
                public bool Boolean { get; set; }
            }
        }

        public struct StructWithNestedStructAndSetterLogic
        {
            private string _string;
            private int _int32;

            public string String
            {
                get => _string;
                // Setter should not be called for missing values.
                set { _string = string.IsNullOrEmpty(value) ? "Hello" : value; }
            }

            public int Int32
            {
                get => _int32;
                set { _int32 = value == 0 ? 42 : value; }
            }

            public Nested NestedStruct;
            public Nested[] NestedStructs;

            public struct Nested
            {
                private string _string;
                private int _int32;

                public string String
                {
                    get => _string;
                    // Setter should not be called for missing values.
                    set { _string = string.IsNullOrEmpty(value) ? "Hello2" : value; }
                }

                public int Int32
                {
                    get => _int32;
                    set { _int32 = value == 0 ? 43 : value; }
                }
            }
        }

        public class BaseClassWithVirtualProperty
        {
            private string? PrivateProperty { get; set; }

            public virtual string[] Test { get; set; } = System.Array.Empty<string>();

            public virtual string? TestGetSetOverridden { get; set; }
            public virtual string? TestGetOverridden { get; set; }
            public virtual string? TestSetOverridden { get; set; }

            private string? _testVirtualSet;
            public virtual string? TestVirtualSet
            {
                set => _testVirtualSet = value;
            }

            public virtual string? TestNoOverridden { get; set; }

            public string? ExposePrivatePropertyValue() => PrivateProperty;
        }

        public class ClassOverridingVirtualProperty : BaseClassWithVirtualProperty
        {
            public override string[] Test { get => base.Test; set => base.Test = value; }

            public override string? TestGetSetOverridden { get; set; }
            public override string? TestGetOverridden => base.TestGetOverridden;
            public override string? TestSetOverridden
            {
                set => base.TestSetOverridden = value;
            }

            private string? _testVirtualSet;
            public override string? TestVirtualSet
            {
                set => _testVirtualSet = value;
            }

            public string? ExposeTestVirtualSet() => _testVirtualSet;
        }

        public class ClassWithDirectSelfReference
        {
            public string MyString { get; set; }
            public ClassWithDirectSelfReference MyClass { get; set; }
        }

        public class ClassWithIndirectSelfReference
        {
            public string MyString { get; set; }
            public List<ClassWithIndirectSelfReference> MyList { get; set; }
        }

        public class DistributedQueueConfig
        {
            public List<QueueNamespaces> Namespaces { get; set; }
        }

        public class QueueNamespaces
        {
            public string Namespace { get; set; }

            public Dictionary<string, QueueProperties>? Queues { get; set; } = new();
        }

        public class QueueProperties
        {
            public DateTimeOffset? CreationDate { get; set; }

            public DateTimeOffset? DequeueOnlyMarkedDate { get; set; } = default(DateTimeOffset);
        }

        public record RecordWithPrimitives
        {
            public bool Prop0 { get; set; }
            public byte Prop1 { get; set; }
            public sbyte Prop2 { get; set; }
            public char Prop3 { get; set; }
            public double Prop4 { get; set; }
            public string Prop5 { get; set; }
            public int Prop6 { get; set; }
            public short Prop8 { get; set; }
            public long Prop9 { get; set; }
            public float Prop10 { get; set; }
            public ushort Prop13 { get; set; }
            public uint Prop14 { get; set; }
            public ulong Prop15 { get; set; }
            public object Prop16 { get; set; }
            public CultureInfo Prop17 { get; set; }
            public DateTime Prop19 { get; set; }
            public DateTimeOffset Prop20 { get; set; }
            public decimal Prop21 { get; set; }
            public TimeSpan Prop23 { get; set; }
            public Guid Prop24 { get; set; }
            public Uri Prop25 { get; set; }
            public Version Prop26 { get; set; }
            public DayOfWeek Prop27 { get; set; }
#if NETCOREAPP
            public Int128 Prop7 { get; set; }
            public Half Prop11 { get; set; }
            public UInt128 Prop12 { get; set; }
            public DateOnly Prop18 { get; set; }
            public TimeOnly Prop22 { get; set; }
#endif
        }

        public class ClassWithParameterlessAndParameterizedCtor
        {
            public ClassWithParameterlessAndParameterizedCtor() => MyInt = 1;

            public ClassWithParameterlessAndParameterizedCtor(int myInt) => MyInt = 10;

            public int MyInt { get; }
        }

        public struct StructWithParameterlessAndParameterizedCtor
        {
            public StructWithParameterlessAndParameterizedCtor() => MyInt = 1;

            public StructWithParameterlessAndParameterizedCtor(int myInt) => MyInt = 10;

            public int MyInt { get; }
        }

        [TypeConverter(typeof(GeolocationTypeConverter))]
        public struct Geolocation : IGeolocation
        {
            public static readonly Geolocation Zero = new(0, 0);

            public Geolocation(double latitude, double longitude)
            {
                Latitude = latitude;
                Longitude = longitude;
            }

            public double Latitude { get; set; }

            public double Longitude { get; set; }

            private sealed class GeolocationTypeConverter : TypeConverter
            {
                public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
                    throw new NotImplementedException();

                public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
                    throw new NotImplementedException();
            }
        }

        public sealed class GeolocationClass : IGeolocation
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        public class GeolocationWrapper
        {
            public Geolocation Location { get; set; }
        }

        public class GraphWithUnsupportedMember
        {
            public JsonWriterOptions WriterOptions { get; set; }
        }

        public record RemoteAuthenticationOptions<TRemoteAuthenticationProviderOptions> where TRemoteAuthenticationProviderOptions : new()
        {
            public TRemoteAuthenticationProviderOptions GenericProp { get; } = new();
            public OidcProviderOptions NonGenericProp { get; } = new();

            public TRemoteAuthenticationProviderOptions _genericField { get; } = new();
            public OidcProviderOptions _nonGenericField { get; } = new();

            public static TRemoteAuthenticationProviderOptions StaticGenericProp { get; } = new();
            public static OidcProviderOptions StaticNonGenericProp { get; } = new();

            public static TRemoteAuthenticationProviderOptions s_GenericField = new();
            public static OidcProviderOptions s_NonGenericField = new();

            public TRemoteAuthenticationProviderOptions? NullGenericProp { get; }
            public static OidcProviderOptions? s_NullNonGenericField;
        }

        public record OidcProviderOptions
        {
            public string? Authority { get; set; }
        }

        public class AClass
        {
            public EndPointCollection EndPoints { get; init; } = new EndPointCollection();

            public bool Property { get; set; } = false;
        }

        public sealed class EndPointCollection : Collection<EndPoint>, IEnumerable<EndPoint>
        {
            public EndPointCollection() { }

            public void Add(string hostAndPort)
            {
                EndPoint? endpoint;

                if (IPAddress.TryParse(hostAndPort, out IPAddress? address))
                {
                    endpoint = new IPEndPoint(address, 0);
                }
                else
                {
                    endpoint = new DnsEndPoint(hostAndPort, 0);
                }

                Add(endpoint);
            }
        }

        internal abstract class AbstractBase
        {
            public int Value { get; set; }
        }

        internal sealed class Derived : AbstractBase { }

        internal sealed class DerivedWithAnotherProp : AbstractBase
        {
            public int Value2 { get; set; }
        }
        
        internal class ClassWithAbstractProp
        {
            public AbstractBase AbstractProp { get; set; }
        }

        internal class ClassWithAbstractCtorParam
        {
            public AbstractBase AbstractProp { get; }

            public ClassWithAbstractCtorParam(AbstractBase abstractProp) => AbstractProp = abstractProp;
        }

        internal class ClassWithOptionalAbstractCtorParam
        {
            public AbstractBase AbstractProp { get; }

            public ClassWithOptionalAbstractCtorParam(AbstractBase? abstractProp = null) => AbstractProp = abstractProp;
        }

        internal class ClassWith_DirectlyAssignable_CtorParams
        {
            public IConfigurationSection MySection { get; }
            public object MyObject { get; }
            public string MyString { get; }

            public ClassWith_DirectlyAssignable_CtorParams(IConfigurationSection mySection, object myObject, string myString) =>
                (MySection, MyObject, MyString) = (mySection, myObject, myString);
        }

        public class SharedChildInstance_Class
        {
            public string? ConnectionString { get; set; }
        }

        public class ClassThatThrowsOnSetters
        {
            private int _myIntProperty;

            public ClassThatThrowsOnSetters()
            {
                _myIntProperty = 42;
            }

            public int MyIntProperty
            {
                get => _myIntProperty;
                set => throw new InvalidOperationException("Not expected");
            }
        }

        public class SimplePoco
        {
            public string A { get; set; }
            public string B { get; set; }
        }

    }
}
