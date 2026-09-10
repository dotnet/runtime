// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using Xunit;

namespace System.Reflection.Tests
{
    public class CustomAttributeTests
    {
        public static IEnumerable<object[]> ConstructorArguments_TestData()
        {
            yield return new object[] { typeof(NoConstructorArguments), Array.Empty<object>() };
            yield return new object[] { typeof(OneConstructorArgument), new object[] { int.MinValue } };
            yield return new object[] { typeof(TwoConstructorArguments), new object[] { "value", typeof(string) } };
            yield return new object[] { typeof(ThreeConstructorArguments), new object?[] { 42, ConstructorEnum.Negative, null } };
            yield return new object[]
            {
                typeof(ArrayConstructorArguments),
                new object[]
                {
                    new string?[] { null, "value" },
                    new Type?[] { typeof(int), null },
                    new[] { int.MinValue, int.MaxValue },
                    new[] { ConstructorEnum.Negative }
                }
            };
            yield return new object[] { typeof(NullArrayConstructorArguments), new object?[4] };
            yield return new object[]
            {
                typeof(ManyConstructorArguments),
                new object[]
                {
                    true, byte.MaxValue, sbyte.MinValue, '\u03bb', short.MinValue, ushort.MaxValue,
                    int.MinValue, uint.MaxValue, long.MinValue, ulong.MaxValue, 12.5f, -25.5,
                    ConstructorEnum.Negative
                }
            };
            // Exceeds MaxStackAttributeArguments (16): exercises CreateCustomAttributeInstance's heap-array
            // fallback (primitives/references/byrefs) together with the existing forced compacting GC in
            // Capture(), so a moving GC during construction must not corrupt the heap-backed byref storage.
            yield return new object[]
            {
                typeof(ManyConstructorArgumentsExceedingStackCap),
                new object[]
                {
                    true, byte.MaxValue, sbyte.MinValue, '\u03bb', short.MinValue, ushort.MaxValue,
                    int.MinValue, uint.MaxValue, long.MinValue, ulong.MaxValue, 12.5f, -25.5,
                    ConstructorEnum.Negative, "text", typeof(string), 42, new int[] { 1, 2, 3 }
                }
            };
        }

        [Theory]
        [MemberData(nameof(ConstructorArguments_TestData))]
        public void ConstructorsPreserveArgumentsAcrossTiers(Type target, object?[] expected)
        {
            for (int i = 0; i < 150; i++)
            {
                ConstructorArgumentsAttribute attribute = target.GetCustomAttribute<ConstructorArgumentsAttribute>();
                Assert.Equal(expected, attribute.Arguments);
            }
        }

        [Fact]
        public void ConstructorExceptionsAreNotWrapped()
        {
            for (int i = 0; i < 150; i++)
            {
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => typeof(ThrowingConstructorTarget).GetCustomAttribute<ThrowingConstructorAttribute>());
                Assert.Equal("Attribute constructor failure", exception.Message);
            }
        }

        public enum ConstructorEnum : long
        {
            Negative = long.MinValue
        }

        public sealed class ConstructorArgumentsAttribute : Attribute
        {
            public object?[] Arguments { get; }

            public ConstructorArgumentsAttribute() => Arguments = Capture([]);
            public ConstructorArgumentsAttribute(int value) => Arguments = Capture([value]);
            public ConstructorArgumentsAttribute(string value, Type type) => Arguments = Capture([value, type]);
            public ConstructorArgumentsAttribute(object? first, object? second, object? third) => Arguments = Capture([first, second, third]);
            public ConstructorArgumentsAttribute(string?[]? strings, Type?[]? types, int[]? integers, ConstructorEnum[]? enums)
                => Arguments = Capture([strings, types, integers, enums]);
            public ConstructorArgumentsAttribute(
                bool boolean, byte unsignedByte, sbyte signedByte, char character, short shortInteger, ushort unsignedShort,
                int integer, uint unsignedInteger, long longInteger, ulong unsignedLong, float single, double dbl, ConstructorEnum enumeration)
                => Arguments = Capture([boolean, unsignedByte, signedByte, character, shortInteger, unsignedShort,
                    integer, unsignedInteger, longInteger, unsignedLong, single, dbl, enumeration]);
            // 17 fixed arguments: one past MaxStackAttributeArguments (16), forcing CreateCustomAttributeInstance's
            // heap-array fallback for its primitive/reference/byref storage instead of stackalloc/InlineArray.
            public ConstructorArgumentsAttribute(
                bool boolean, byte unsignedByte, sbyte signedByte, char character, short shortInteger, ushort unsignedShort,
                int integer, uint unsignedInteger, long longInteger, ulong unsignedLong, float single, double dbl, ConstructorEnum enumeration,
                string text, Type type, object boxed, int[] array)
                => Arguments = Capture([boolean, unsignedByte, signedByte, character, shortInteger, unsignedShort,
                    integer, unsignedInteger, longInteger, unsignedLong, single, dbl, enumeration, text, type, boxed, array]);

            private static object?[] Capture(object?[] arguments)
            {
                GC.Collect(0, GCCollectionMode.Forced, blocking: true, compacting: true);
                return arguments;
            }
        }

        [ConstructorArguments]
        private sealed class NoConstructorArguments { }

        [ConstructorArguments(int.MinValue)]
        private sealed class OneConstructorArgument { }

        [ConstructorArguments("value", typeof(string))]
        private sealed class TwoConstructorArguments { }

        [ConstructorArguments(42, ConstructorEnum.Negative, null)]
        private sealed class ThreeConstructorArguments { }

        [ConstructorArguments(new string[] { null, "value" }, new Type[] { typeof(int), null },
            new[] { int.MinValue, int.MaxValue }, new[] { ConstructorEnum.Negative })]
        private sealed class ArrayConstructorArguments { }

        [ConstructorArguments(null, null, null, null)]
        private sealed class NullArrayConstructorArguments { }

        [ConstructorArguments(true, byte.MaxValue, sbyte.MinValue, '\u03bb', short.MinValue, ushort.MaxValue,
            int.MinValue, uint.MaxValue, long.MinValue, ulong.MaxValue, 12.5f, -25.5, ConstructorEnum.Negative)]
        private sealed class ManyConstructorArguments { }

        [ConstructorArguments(true, byte.MaxValue, sbyte.MinValue, '\u03bb', short.MinValue, ushort.MaxValue,
            int.MinValue, uint.MaxValue, long.MinValue, ulong.MaxValue, 12.5f, -25.5, ConstructorEnum.Negative,
            "text", typeof(string), 42, new int[] { 1, 2, 3 })]
        private sealed class ManyConstructorArgumentsExceedingStackCap { }

        public sealed class ThrowingConstructorAttribute : Attribute
        {
            public ThrowingConstructorAttribute(string message) => throw new InvalidOperationException(message);
        }

        [ThrowingConstructor("Attribute constructor failure")]
        private sealed class ThrowingConstructorTarget { }

        private class SameTypesAttribute : Attribute
        {
            public object[] ObjectArray1 { get; set; }
            public object[] ObjectArray2 { get; set; }
        }

        [SameTypes(ObjectArray1 = null, ObjectArray2 = new object[] { "" })]
        private class SameTypesClass1 { }

        [SameTypes(ObjectArray1 = new object[] { "" }, ObjectArray2 = null)]
        private class SameTypesClass2 { }

        [Fact]
        public void AttributeWithSamePropertyTypes()
        {
            SameTypesAttribute attr;

            attr = typeof(SameTypesClass1)
                .GetCustomAttributes(typeof(SameTypesAttribute), true)
                .Cast<SameTypesAttribute>()
                .Single();

            Assert.Null(attr.ObjectArray1);
            Assert.Equal(1, attr.ObjectArray2.Length);

            attr = typeof(SameTypesClass2)
                .GetCustomAttributes(typeof(SameTypesAttribute), true)
                .Cast<SameTypesAttribute>()
                .Single();

            Assert.Equal(1, attr.ObjectArray1.Length);
            Assert.Null(attr.ObjectArray2);
        }

        private class DifferentTypesAttribute : Attribute
        {
            public object[] ObjectArray { get; set; }
            public string[] StringArray { get; set; }
        }

        [DifferentTypes(ObjectArray = null, StringArray = new[] { "" })]
        private class DifferentTypesClass1 { }

        [DifferentTypes(ObjectArray = new object[] { "" }, StringArray = null)]
        private class DifferentTypesClass2 { }

        [Fact]
        public void AttributeWithDifferentPropertyTypes()
        {
            DifferentTypesAttribute attr;

            attr = typeof(DifferentTypesClass1)
                .GetCustomAttributes(typeof(DifferentTypesAttribute), true)
                .Cast<DifferentTypesAttribute>()
                .Single();

            Assert.Null(attr.ObjectArray);
            Assert.Equal(1, attr.StringArray.Length);

            attr = typeof(DifferentTypesClass2)
                .GetCustomAttributes(typeof(DifferentTypesAttribute), true)
                .Cast<DifferentTypesAttribute>()
                .Single();

            Assert.Equal(1, attr.ObjectArray.Length);
            Assert.Null(attr.StringArray);
        }

        public class StringValuedAttribute : Attribute
        {
            public StringValuedAttribute (string s)
            {
                NamedField = s;
            }
            public StringValuedAttribute () {}
            public string NamedProperty
            {
                get => NamedField;
                set { NamedField = value; }
            }
            public string NamedField;
        }

        internal class ClassWithAttrs
        {
            [StringValuedAttribute("")]
            public void M1() {}

            [StringValuedAttribute(NamedProperty = "")]
            public void M2() {}

            [StringValuedAttribute(NamedField = "")]
            public void M3() {}
        }

        [Fact]
        public void StringAttributeValueRefEqualsStringEmpty () {
            StringValuedAttribute attr;
            attr = typeof (ClassWithAttrs).GetMethod("M1")
                .GetCustomAttributes(typeof(StringValuedAttribute), true)
                .Cast<StringValuedAttribute>()
                .Single();

            Assert.Same(string.Empty, attr.NamedField);

            attr = typeof (ClassWithAttrs).GetMethod("M2")
                .GetCustomAttributes(typeof(StringValuedAttribute), true)
                .Cast<StringValuedAttribute>()
                .Single();
            
            Assert.Same(string.Empty, attr.NamedField);


            attr = typeof (ClassWithAttrs).GetMethod("M3")
                .GetCustomAttributes(typeof(StringValuedAttribute), true)
                .Cast<StringValuedAttribute>()
                .Single();
            
            Assert.Same(string.Empty, attr.NamedField);
        }

        [AttributeUsage(AttributeTargets.Parameter)]
        internal class MyParameterAttribute : Attribute {}

        [AttributeUsage(AttributeTargets.Property)]
        internal class MyPropertyAttribute : Attribute {}

        internal sealed class PropertyAsParameterInfo : ParameterInfo
        {
            private readonly PropertyInfo _underlyingProperty;
            private readonly ParameterInfo? _constructionParameterInfo;

            public PropertyAsParameterInfo(PropertyInfo property, ParameterInfo parameterInfo)
            {
                _underlyingProperty = property;
                _constructionParameterInfo = parameterInfo;
                MemberImpl = _underlyingProperty;
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                var constructorAttributes = _constructionParameterInfo?.GetCustomAttributes(attributeType, inherit);

                if (constructorAttributes == null || constructorAttributes is { Length: 0 })
                {
                    return _underlyingProperty.GetCustomAttributes(attributeType, inherit);
                }

                var propertyAttributes = _underlyingProperty.GetCustomAttributes(attributeType, inherit);

                var mergedAttributes = new Attribute[constructorAttributes.Length + propertyAttributes.Length];
                Array.Copy(constructorAttributes, mergedAttributes, constructorAttributes.Length);
                Array.Copy(propertyAttributes, 0, mergedAttributes, constructorAttributes.Length, propertyAttributes.Length);

                return mergedAttributes;
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                var constructorAttributes = _constructionParameterInfo?.GetCustomAttributes(inherit);

                if (constructorAttributes == null || constructorAttributes is { Length: 0 })
                {
                    return _underlyingProperty.GetCustomAttributes(inherit);
                }

                var propertyAttributes = _underlyingProperty.GetCustomAttributes(inherit);

                var mergedAttributes = new object[constructorAttributes.Length + propertyAttributes.Length];
                Array.Copy(constructorAttributes, mergedAttributes, constructorAttributes.Length);
                Array.Copy(propertyAttributes, 0, mergedAttributes, constructorAttributes.Length, propertyAttributes.Length);

                return mergedAttributes;
            }

            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                var attributes = new List<CustomAttributeData>(
                    _constructionParameterInfo?.GetCustomAttributesData() ?? Array.Empty<CustomAttributeData>());
                attributes.AddRange(_underlyingProperty.GetCustomAttributesData());

                return attributes.AsReadOnly();
            }
        }

        internal class CustomAttributeProviderTestClass
        {
            public CustomAttributeProviderTestClass([MyParameter] int integerProperty)
            {
                IntegerProperty = integerProperty;
            }

            [MyProperty]
            public int IntegerProperty { get; set; }
        }

        [Fact]
        public void CustomAttributeProvider ()
        {
            var type = typeof(CustomAttributeProviderTestClass);
            var propertyInfo = type.GetProperty(nameof(CustomAttributeProviderTestClass.IntegerProperty));
            var ctorInfo = type.GetConstructor(new Type[] { typeof(int) });
            var ctorParamInfo = ctorInfo.GetParameters()[0];
            var propertyAndParamInfo = new PropertyAsParameterInfo(propertyInfo, ctorParamInfo);

            // check GetCustomAttribute API
            var cattrObjects = propertyAndParamInfo.GetCustomAttributes(true);
            Assert.Equal(2, cattrObjects.Length);
            Assert.Equal(typeof(MyParameterAttribute), cattrObjects[0].GetType());
            Assert.Equal(typeof(MyPropertyAttribute), cattrObjects[1].GetType());

            cattrObjects = propertyAndParamInfo.GetCustomAttributes(typeof(Attribute), true);
            Assert.Equal(2, cattrObjects.Length);
            Assert.Equal(typeof(MyParameterAttribute), cattrObjects[0].GetType());
            Assert.Equal(typeof(MyPropertyAttribute), cattrObjects[1].GetType());

            var cattrsEnumerable = propertyAndParamInfo.GetCustomAttributes();
            Attribute[] cattrs = cattrsEnumerable.Cast<Attribute>().ToArray();
            Assert.Equal(2, cattrs.Length);
            Assert.Equal(typeof(MyParameterAttribute), cattrs[0].GetType());
            Assert.Equal(typeof(MyPropertyAttribute), cattrs[1].GetType());

            // check GetCustomAttributeData API
            var customAttributesData = propertyAndParamInfo.GetCustomAttributesData();
            Assert.Equal(2, customAttributesData.Count);
            Assert.Equal(typeof(MyParameterAttribute), customAttributesData[0].AttributeType);
            Assert.Equal(typeof(MyPropertyAttribute), customAttributesData[1].AttributeType);
        }

        internal enum EnumForArrays { Zero, One, Two }

        [CustomAttributeWithObjects(EnumForArrays.One, new object[] { EnumForArrays.One }, new EnumForArrays[] { EnumForArrays.Two },
                B1 = EnumForArrays.One, B2 = new object[] { EnumForArrays.One }, B3 = new EnumForArrays[] { EnumForArrays.Two })]
        internal class ClassWithObjectEnums { }

        internal class CustomAttributeWithObjectsAttribute(object a1, object a2, object a3) : Attribute
        {
            public object A1 => a1;
            public object A2 => a2;
            public object A3 => a3;

            public object B1 { get; set; }
            public object B2 { get; set; }
            public object B3 { get; set; }
        }

        [Fact]
        public void BoxedEnumAttributes()
        {
            CustomAttributeWithObjectsAttribute att = typeof(ClassWithObjectEnums)
                .GetCustomAttribute<CustomAttributeWithObjectsAttribute>()!;

            Assert.Equal(typeof(EnumForArrays), att.A1.GetType());
            Assert.Equal(typeof(object[]), att.A2.GetType());
            Assert.Equal(typeof(EnumForArrays), ((object[])att.A2)[0].GetType());
            Assert.Equal(EnumForArrays.One, ((object[])att.A2)[0]);
            Assert.Equal(typeof(EnumForArrays[]), att.A3.GetType());
            Assert.Equal(EnumForArrays.Two, ((EnumForArrays[])att.A3)[0]);

            Assert.Equal(typeof(EnumForArrays), att.B1.GetType());
            Assert.Equal(typeof(object[]), att.B2.GetType());
            Assert.Equal(typeof(EnumForArrays), ((object[])att.B2)[0].GetType());
            Assert.Equal(EnumForArrays.One, ((object[])att.B2)[0]);
            Assert.Equal(typeof(EnumForArrays[]), att.B3.GetType());
            Assert.Equal(EnumForArrays.Two, ((EnumForArrays[])att.B3)[0]);
        }

        [Fact]
        public void SystemObject_GetCustomAttributes_InheritanceConsistency()
        {
            Type objectType = typeof(object);
            object[] attributesWithoutInherit = objectType.GetCustomAttributes(inherit: false);
            object[] attributesWithInherit = objectType.GetCustomAttributes(inherit: true);
            Assert.Equal(attributesWithoutInherit.Length, attributesWithInherit.Length);
        }

        // BoxedEnumAttributes above already covers the default (int) underlying width via the tagged/object
        // path. These cover the remaining widths, each as both a tagged scalar and a tagged array element,
        // to exercise RuntimeHelpers.Box's per-width byte offset and the array bulk/per-element copy paths.
        public enum ByteWidthEnum : byte { Value = byte.MaxValue }
        public enum SByteWidthEnum : sbyte { Value = sbyte.MinValue }
        public enum Int16WidthEnum : short { Value = short.MinValue }
        public enum UInt16WidthEnum : ushort { Value = ushort.MaxValue }
        public enum UInt32WidthEnum : uint { Value = uint.MaxValue }
        public enum Int64WidthEnum : long { Value = long.MinValue }
        public enum UInt64WidthEnum : ulong { Value = ulong.MaxValue }

        internal sealed class EnumWidthAttribute : Attribute
        {
            public object Scalar { get; }
            public object Array { get; }

            public EnumWidthAttribute(object scalar, object array)
            {
                Scalar = scalar;
                Array = array;
            }
        }

        [EnumWidth(ByteWidthEnum.Value, new ByteWidthEnum[] { ByteWidthEnum.Value, default })]
        private sealed class ByteWidthTarget { }

        [EnumWidth(SByteWidthEnum.Value, new SByteWidthEnum[] { SByteWidthEnum.Value, default })]
        private sealed class SByteWidthTarget { }

        [EnumWidth(Int16WidthEnum.Value, new Int16WidthEnum[] { Int16WidthEnum.Value, default })]
        private sealed class Int16WidthTarget { }

        [EnumWidth(UInt16WidthEnum.Value, new UInt16WidthEnum[] { UInt16WidthEnum.Value, default })]
        private sealed class UInt16WidthTarget { }

        [EnumWidth(UInt32WidthEnum.Value, new UInt32WidthEnum[] { UInt32WidthEnum.Value, default })]
        private sealed class UInt32WidthTarget { }

        [EnumWidth(Int64WidthEnum.Value, new Int64WidthEnum[] { Int64WidthEnum.Value, default })]
        private sealed class Int64WidthTarget { }

        [EnumWidth(UInt64WidthEnum.Value, new UInt64WidthEnum[] { UInt64WidthEnum.Value, default })]
        private sealed class UInt64WidthTarget { }

        public static IEnumerable<object[]> EnumWidths_TestData()
        {
            yield return new object[] { typeof(ByteWidthTarget), typeof(ByteWidthEnum), (object)ByteWidthEnum.Value };
            yield return new object[] { typeof(SByteWidthTarget), typeof(SByteWidthEnum), (object)SByteWidthEnum.Value };
            yield return new object[] { typeof(Int16WidthTarget), typeof(Int16WidthEnum), (object)Int16WidthEnum.Value };
            yield return new object[] { typeof(UInt16WidthTarget), typeof(UInt16WidthEnum), (object)UInt16WidthEnum.Value };
            yield return new object[] { typeof(UInt32WidthTarget), typeof(UInt32WidthEnum), (object)UInt32WidthEnum.Value };
            yield return new object[] { typeof(Int64WidthTarget), typeof(Int64WidthEnum), (object)Int64WidthEnum.Value };
            yield return new object[] { typeof(UInt64WidthTarget), typeof(UInt64WidthEnum), (object)UInt64WidthEnum.Value };
        }

        [Theory]
        [MemberData(nameof(EnumWidths_TestData))]
        public void BoxedEnumAttributes_AllUnderlyingWidths(Type target, Type enumType, object expectedValue)
        {
            EnumWidthAttribute attribute = target.GetCustomAttribute<EnumWidthAttribute>();

            Assert.Equal(enumType, attribute.Scalar.GetType());
            Assert.Equal(expectedValue, attribute.Scalar);

            Assert.Equal(enumType.MakeArrayType(), attribute.Array.GetType());
            Array array = (Array)attribute.Array;
            Assert.Equal(2, array.Length);
            Assert.Equal(enumType, array.GetValue(0).GetType());
            Assert.Equal(expectedValue, array.GetValue(0));
            Assert.Equal(Activator.CreateInstance(enumType), array.GetValue(1));
        }

        // A null enum-array named argument is a distinct branch from a null string/Type/object-array named
        // argument: GetPropertyOrFieldData deliberately leaves the declared-type output unspecified for it,
        // falling back to name-only member lookup. This regresses that it still resolves without throwing.
        private sealed class NullEnumArrayAttribute : Attribute
        {
            public EnumForArrays[] NamedProperty { get; set; }
            public EnumForArrays[] NamedField;
        }

        [NullEnumArray(NamedProperty = null, NamedField = null)]
        private sealed class NullEnumArrayClass { }

        [Fact]
        public void NamedArgument_NullEnumArray_ResolvesPropertyAndField()
        {
            NullEnumArrayAttribute attribute = typeof(NullEnumArrayClass).GetCustomAttribute<NullEnumArrayAttribute>();

            Assert.Null(attribute.NamedProperty);
            Assert.Null(attribute.NamedField);
        }

        private sealed class NestedTaggedArraysAttribute : Attribute
        {
            public object Value { get; }

            public NestedTaggedArraysAttribute(object value) => Value = value;
        }

        [NestedTaggedArrays(new object[] { new int[] { 1, 2, 3 }, "text", null, new object[] { 4, "five", null } })]
        private sealed class NestedTaggedArraysClass { }

        [Fact]
        public void NestedTaggedObjectArrays_Materialize()
        {
            NestedTaggedArraysAttribute attribute = typeof(NestedTaggedArraysClass).GetCustomAttribute<NestedTaggedArraysAttribute>();

            object[] outer = Assert.IsType<object[]>(attribute.Value);
            Assert.Equal(4, outer.Length);
            Assert.Equal(new[] { 1, 2, 3 }, Assert.IsType<int[]>(outer[0]));
            Assert.Equal("text", outer[1]);
            Assert.Null(outer[2]);

            object[] inner = Assert.IsType<object[]>(outer[3]);
            Assert.Equal(3, inner.Length);
            Assert.Equal(4, inner[0]);
            Assert.Equal("five", inner[1]);
            Assert.Null(inner[2]);
        }

        // Member declaration order controls the named-argument blob order in this fixture.
        private static readonly List<string> s_sideEffectLog = new();

        private sealed class SideEffectAttribute : Attribute
        {
            private int _first, _second, _third;

            public SideEffectAttribute() => s_sideEffectLog.Add("ctor");

            public int First { get => _first; set { _first = value; s_sideEffectLog.Add($"first:{value}"); } }
            public int Second { get => _second; set { _second = value; s_sideEffectLog.Add($"second:{value}"); throw new InvalidOperationException("second failed"); } }
            public int Third { get => _third; set { _third = value; s_sideEffectLog.Add($"third:{value}"); } }
        }

        [SideEffect(First = 1, Second = 2, Third = 3)]
        private sealed class SideEffectTarget { }

        [Fact]
        public void NamedArgumentAssignment_SequentialOrder_StopsAtFirstSetterFailure()
        {
            s_sideEffectLog.Clear();

            CustomAttributeFormatException exception = Assert.Throws<CustomAttributeFormatException>(
                () => typeof(SideEffectTarget).GetCustomAttribute<SideEffectAttribute>());

            // Property setter invocation wraps the original exception in a TargetInvocationException before
            // AddCustomAttributes wraps that again as a CustomAttributeFormatException.
            TargetInvocationException invocationException = Assert.IsType<TargetInvocationException>(exception.InnerException);
            Assert.IsType<InvalidOperationException>(invocationException.InnerException);
            Assert.Equal(new[] { "ctor", "first:1", "second:2" }, s_sideEffectLog);
        }

        public sealed class TypeArgumentAttribute : Attribute
        {
            public TypeArgumentAttribute(Type type) => Type = type;

            public Type Type { get; }
        }

        [TypeArgument(typeof(int))]
        private sealed class ClassWithTypeArgument { }

        // CA type resolution must ignore the ambient contextual reflection context.
        [Fact]
        public void TypeNamedArgument_TypeResolution_IgnoresActiveContextualReflectionContext()
        {
            var alc = new AssemblyLoadContext(nameof(TypeNamedArgument_TypeResolution_IgnoresActiveContextualReflectionContext), isCollectible: true);
            try
            {
                using (alc.EnterContextualReflection())
                {
                    TypeArgumentAttribute attribute = typeof(ClassWithTypeArgument).GetCustomAttribute<TypeArgumentAttribute>();
                    Assert.Equal(typeof(int), attribute.Type);
                }
            }
            finally
            {
                alc.Unload();
            }
        }
    }
}
