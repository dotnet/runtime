// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Tests.Types
{
    public abstract class TypePropertyTestBase
    {
        public abstract Type CreateType();

        public virtual int? ArrayRank => null;

        public abstract TypeAttributes Attributes { get; }

        public virtual Type BaseType => typeof(object);

        public virtual bool ContainsGenericParameters => false;

        public virtual MethodBase DeclaringMethod => null;

        public virtual Type DeclaringType => null;

        public virtual Type ElementType => null;

        public virtual GenericParameterAttributes? GenericParameterAttributes => null;

        public virtual int? GenericParameterPosition => null;

        public virtual Type[] GenericTypeArguments => new Type[0];

        public virtual Type GenericTypeDefinition => null;

        public virtual bool HasElementType => false;

        public virtual bool IsArray => false;

        public virtual bool IsByRef => false;

        public virtual bool IsCOMObject => false;

        public virtual bool IsConstructedGenericType => false;

        public virtual bool IsContextful => false;

        public virtual bool IsGenericParameter => false;

        public virtual bool IsGenericMethodParameter => IsGenericParameter && DeclaringMethod != null;

        public virtual bool IsGenericTypeDefinition => false;

        public virtual bool IsGenericTypeParameter => IsGenericParameter && DeclaringMethod == null;

        public virtual bool IsGenericType => false;

        public virtual bool IsMarshalByRef => false;

        public virtual bool IsNested => DeclaringType != null;

        public virtual bool IsPointer => false;

        public virtual bool IsPrimitive => false;

        public virtual bool IsSecurityCritical => true;

        public virtual bool IsSecuritySafeCritical => false;

        public virtual bool IsSecurityTransparent => false;

        public virtual bool IsSignatureType => false;

        public virtual bool IsSZArray => false;

        public virtual bool IsTypeDefinition => true;

        public virtual bool IsValueType => BaseType == typeof(Enum) || (BaseType == typeof(ValueType) && CreateType() != typeof(Enum));

        public virtual bool IsVariableBoundArray => false;

        public virtual MemberTypes MemberType => IsNested ? MemberTypes.TypeInfo : MemberTypes.NestedType;

        public virtual Type ReflectedType => DeclaringType;

        public virtual Type UnderlyingSystemType => CreateType();

        [Fact]
        public void Attributes_Get_ReturnsExpected()
        {
            Type t = CreateType();
            Assert.Equal(Attributes, t.Attributes);

            Assert.Equal((Attributes & TypeAttributes.Abstract) != 0, t.IsAbstract);
            Assert.Equal((Attributes & TypeAttributes.Import) != 0, t.IsImport);
            Assert.Equal((Attributes & TypeAttributes.Sealed) != 0, t.IsSealed);
            Assert.Equal((Attributes & TypeAttributes.SpecialName) != 0, t.IsSpecialName);
            Assert.Equal((Attributes & TypeAttributes.Serializable) != 0 || t.IsEnum || t == typeof(Delegate), t.IsSerializable);

            Assert.Equal((Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class && !t.IsValueType, t.IsClass);
            Assert.Equal((Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface, t.IsInterface);

            Assert.Equal((Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass, t.IsAnsiClass);
            Assert.Equal((Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass, t.IsAutoClass);
            Assert.Equal((Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass, t.IsUnicodeClass);

            Assert.Equal((Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout, t.IsAutoLayout);
            Assert.Equal((Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout, t.IsExplicitLayout);
            Assert.Equal((Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout, t.IsLayoutSequential);

            Assert.Equal((Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly, t.IsNestedAssembly);
            Assert.Equal((Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem, t.IsNestedFamANDAssem);
            Assert.Equal((Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily, t.IsNestedFamily);
            Assert.Equal((Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem, t.IsNestedFamORAssem);
            Assert.Equal((Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate, t.IsNestedPrivate);
            Assert.Equal((Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic, t.IsNotPublic);
            Assert.Equal((Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public, t.IsPublic);
        }

        [Fact]
        public void BaseType_Get_ReturnsExpected()
        {
            Assert.Equal(BaseType, CreateType().BaseType);
        }

        [Fact]
        public void ContainsGenericParameters_Get_ReturnsExpected()
        {
            Assert.Equal(ContainsGenericParameters, CreateType().ContainsGenericParameters);
        }

        [Fact]
        public void DeclaringType_Get_ReturnsExpected()
        {
            Assert.Equal(DeclaringType, CreateType().DeclaringType);
        }

        [Fact]
        public void DeclaringMethod_Get_ReturnsExpected()
        {
            if (IsGenericParameter)
            {
                Assert.Equal(DeclaringMethod, CreateType().DeclaringMethod);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => CreateType().DeclaringMethod);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/864", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        [Fact]
        public void GenericTypeArguments_Get_ReturnsExpected()
        {
            Assert.Equal(GenericTypeArguments, CreateType().GenericTypeArguments);
        }

        [Fact]
        public void GetGenericTypeDefinition_Invoke_ReturnsExpected()
        {
            if (GenericTypeDefinition != null)
            {
                Assert.Equal(GenericTypeDefinition, CreateType().GetGenericTypeDefinition());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => CreateType().GetGenericTypeDefinition());
            }
        }

        [Fact]
        public void GenericParameterAttributes_Get_ReturnsExpected()
        {
            if (GenericParameterPosition != null)
            {
                Assert.Equal(GenericParameterAttributes, CreateType().GenericParameterAttributes);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => CreateType().GenericParameterAttributes);
            }
        }

        [Fact]
        public void GenericParameterPosition_Get_ReturnsExpected()
        {
            if (GenericParameterPosition != null)
            {
                Assert.Equal(GenericParameterPosition, CreateType().GenericParameterPosition);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => CreateType().GenericParameterPosition);
            }
        }

        [Fact]
        public void GetArrayRank_Invoke_ReturnsExpected()
        {
            if (ArrayRank != null)
            {
                Assert.Equal(ArrayRank, CreateType().GetArrayRank());
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>(null, () => CreateType().GetArrayRank());
            }
        }

        [Fact]
        public void GetElementType_Invoke_ReturnsExpected()
        {
            Assert.Equal(ElementType, CreateType().GetElementType());
        }

        [Fact]
        public void HasElementType_Get_ReturnsExpected()
        {
            Assert.Equal(HasElementType, CreateType().HasElementType);
        }

        [Fact]
        public void IsArray_Get_ReturnsExpected()
        {
            Assert.Equal(IsArray, CreateType().IsArray);
        }

        [Fact]
        public void IsByRef_Get_ReturnsExpected()
        {
            Type t = CreateType();
            Assert.Equal(IsByRef, t.IsByRef);
            if (!t.IsByRef && t != typeof(TypedReference))
            {
                Assert.True(t.MakeByRefType().IsByRef);
            }
        }

        [Fact]
        public void IsCOMObject_Get_ReturnsExpected()
        {
            Assert.Equal(IsCOMObject, CreateType().IsCOMObject);
        }

        [Fact]
        public void IsConstructedGenericType_Get_ReturnsExpected()
        {
            Assert.Equal(IsConstructedGenericType, CreateType().IsConstructedGenericType);
        }

        [Fact]
        public void IsContextful_Get_ReturnsExpected()
        {
            Assert.Equal(IsContextful, CreateType().IsContextful);
        }

        [Fact]
        public void IsEnum_Get_ReturnsExpected()
        {
            Assert.Equal(BaseType == typeof(Enum), CreateType().IsEnum);
        }

        [Fact]
        public void IsGenericParameter_Get_ReturnsExpected()
        {
            Assert.Equal(IsGenericParameter, CreateType().IsGenericParameter);
        }

        [Fact]
        public void IsGenericTypeDefinition_Get_ReturnsExpected()
        {
            Assert.Equal(IsGenericTypeDefinition, CreateType().IsGenericTypeDefinition);
        }

        [Fact]
        public void IsGenericType_Get_ReturnsExpected()
        {
            Assert.Equal(IsGenericType, CreateType().IsGenericType);
        }

        [Fact]
        public void IsMarshalByRef_Get_ReturnsExpected()
        {
            Assert.Equal(IsMarshalByRef, CreateType().IsMarshalByRef);
        }

        [Fact]
        public void IsNested_Get_ReturnsExpected()
        {
            Assert.Equal(IsNested, CreateType().IsNested);
        }

        [Fact]
        public void IsPointer_Get_ReturnsExpected()
        {
            Type t = CreateType();
            Assert.Equal(IsPointer, t.IsPointer);
            if (!t.IsByRef && t != typeof(TypedReference))
            {
                Assert.True(t.MakePointerType().IsPointer);
            }
        }

        [Fact]
        public void IsPrimitive_Get_ReturnsExpected()
        {
            Assert.Equal(IsPrimitive, CreateType().IsPrimitive);
        }

        [Fact]
        public void IsSecurityCritical_Get_ReturnsExpected()
        {
            Assert.Equal(IsSecurityCritical, CreateType().IsSecurityCritical);
        }

        [Fact]
        public void IsSecuritySafeCritical_Get_ReturnsExpected()
        {
            Assert.Equal(IsSecuritySafeCritical, CreateType().IsSecuritySafeCritical);
        }

        [Fact]
        public void IsSecurityTransparent_Get_ReturnsExpected()
        {
            Assert.Equal(IsSecurityTransparent, CreateType().IsSecurityTransparent);
        }

        [Fact]
        public void IsValueType_Get_ReturnsExpected()
        {
            Assert.Equal(IsValueType, CreateType().IsValueType);
        }

        [Fact]
        public void ReflectedType_Get_ReturnsExpected()
        {
            Assert.Equal(DeclaringType, CreateType().ReflectedType);
        }

        [Fact]
        public void UnderlyingSystemType_Get_ReturnsExpected()
        {
            Assert.Equal(UnderlyingSystemType, CreateType().UnderlyingSystemType);
        }

        [Fact]
        public void IsGenericMethodParameter_Get_ReturnsExpected()
        {
            Assert.Equal(IsGenericMethodParameter, CreateType().IsGenericMethodParameter);
        }

        [Fact]
        public void IsGenericTypeParameter_Get_ReturnsExpected()
        {
            Assert.Equal(IsGenericTypeParameter, CreateType().IsGenericTypeParameter);
        }

        [Fact]
        public void IsSignatureType_Get_ReturnsExpected()
        {
            Assert.Equal(IsSignatureType, CreateType().IsSignatureType);
        }

        [Fact]
        public void IsSZArray_Get_ReturnsExpected()
        {
            Assert.Equal(IsSZArray, CreateType().IsSZArray);
        }

        [Fact]
        public void IsTypeDefinition_Get_ReturnsExpected()
        {
            Assert.Equal(IsTypeDefinition, CreateType().IsTypeDefinition);
        }

        [Fact]
        public void IsVariableBoundArray_Get_ReturnsExpected()
        {
            Assert.Equal(IsVariableBoundArray, CreateType().IsVariableBoundArray);
        }
    }

    public abstract class ArrayTypeTestBase : TypePropertyTestBase
    {
        public override bool IsArray => true;

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Serializable;

        public override Type BaseType => typeof(Array);

        public override bool HasElementType => true;

        public override bool IsTypeDefinition => false;
    }

    public abstract class ClassTypeTestBase : TypePropertyTestBase
    {
        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.BeforeFieldInit;
    }

    public abstract class StructTypeTestBase : TypePropertyTestBase
    {
        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

        public override Type BaseType => typeof(ValueType);
    }

    public abstract class InterfaceTypeTestBase : TypePropertyTestBase
    {
        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit;

        public override Type BaseType => null;
    }

    public abstract class PrimitiveTypeTestBase : StructTypeTestBase
    {
        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.Serializable | TypeAttributes.BeforeFieldInit;

        public override bool IsPrimitive => true;
    }

    public abstract class EnumTypeTestBase : StructTypeTestBase
    {
        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;

        public override Type BaseType => typeof(Enum);
    }

    // Primitives
    public class StringTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(string);

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Serializable | TypeAttributes.BeforeFieldInit;
    }

    public class SByteTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(sbyte);
    }

    public class ByteTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(byte);
    }

    public class ShortTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(short);
    }

    public class UShortTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(ushort);
    }

    public class IntTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(int);
    }

    public class UIntTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(uint);
    }

    public class LongTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(long);
    }

    public class ULongTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(ulong);
    }

    public class CharTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(char);
    }

    public class BoolTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(bool);
    }

    public class FloatTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(float);
    }

    public class DoubleTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(double);
    }

    public class IntPtrTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(IntPtr);
    }

    public class UIntPtrTests : PrimitiveTypeTestBase
    {
        public override Type CreateType() => typeof(UIntPtr);
    }

    public class VoidTests : StructTypeTestBase
    {
        public override Type CreateType() => typeof(void);
    }

    public class ObjectTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(object);

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Serializable | TypeAttributes.BeforeFieldInit;

        public override Type BaseType => null;
    }

    public class ArrayTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(Array);

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Serializable | TypeAttributes.BeforeFieldInit;
    }

    public class ValueTypeTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(ValueType);

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Serializable | TypeAttributes.BeforeFieldInit;
    }

    public class DelegateTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(Delegate);

        public override TypeAttributes Attributes => PlatformDetection.IsMonoRuntime
            ? TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit
            : TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit;
    }

    public class EnumTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(Enum);

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Serializable | TypeAttributes.BeforeFieldInit;

        public override Type BaseType => typeof(ValueType);
    }

    // Primitives enums
    public class SByteEnumTests : EnumTypeTestBase
    {
        public override Type CreateType() => typeof(SByteEnum);
    }

    public class ByteEnumTests : EnumTypeTestBase
    {
        public override Type CreateType() => typeof(ByteEnum);
    }

    public class Int16EnumTests : EnumTypeTestBase
    {
        public override Type CreateType() => typeof(Int16Enum);
    }

    public class UInt16EnumTests : EnumTypeTestBase
    {
        public override Type CreateType() => typeof(UInt16Enum);
    }

    public class Int32EnumTests : EnumTypeTestBase
    {
        public override Type CreateType() => typeof(Int32Enum);
    }

    public class UInt32EnumTests : EnumTypeTestBase
    {
        public override Type CreateType() => typeof(UInt32Enum);
    }

    public class Int64EnumTests : EnumTypeTestBase
    {
        public override Type CreateType() => typeof(Int64Enum);
    }

    public class UInt64EnumTests : EnumTypeTestBase
    {
        public override Type CreateType() => typeof(UInt64Enum);
    }

    // Arrays, pointers.
    public class StringArrayTests : ArrayTypeTestBase
    {
        public override Type CreateType() => typeof(string[]);

        public override int? ArrayRank => 1;

        public override Type ElementType => typeof(string);

        public override bool IsSZArray => true;
    }

    public class IntArrayTests : ArrayTypeTestBase
    {
        public override Type CreateType() => typeof(int[]);

        public override int? ArrayRank => 1;

        public override Type ElementType => typeof(int);

        public override bool IsSZArray => true;
    }

    public class ArrayOfArrayTests : ArrayTypeTestBase
    {
        public override Type CreateType() => typeof(int[][]);

        public override int? ArrayRank => 1;

        public override Type ElementType => typeof(int[]);

        public override bool IsSZArray => true;
    }

    public class NestedArrayTests : ArrayTypeTestBase
    {
        public override Type CreateType() => typeof(Outside.Inside[]);

        public override int? ArrayRank => 1;

        public override Type ElementType => typeof(Outside.Inside);

        public override bool IsSZArray => true;
    }

    public class GenericNestedArrayTests : ArrayTypeTestBase
    {
        public override Type CreateType() => typeof(Outside<int>.Inside<double>[]);

        public override int? ArrayRank => 1;

        public override Type ElementType => typeof(Outside<int>.Inside<double>);

        public override bool IsSZArray => true;
    }

    public class NonSzIntArrayTests : ArrayTypeTestBase
    {
        public override Type CreateType() => typeof(int).MakeArrayType(1);

        public override int? ArrayRank => 1;

        public override Type ElementType => typeof(int);

        public override bool IsVariableBoundArray => true;
    }

    public class MultidimensionalIntArrayTests : ArrayTypeTestBase
    {
        public override Type CreateType() => typeof(int[,]);

        public override int? ArrayRank => 2;

        public override Type ElementType => typeof(int);

        public override bool IsVariableBoundArray => true;
    }

    public class IntPointerTests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(int).MakePointerType();

        public override TypeAttributes Attributes => TypeAttributes.Public;

        public override Type BaseType => null;

        public override bool IsPointer => true;

        public override bool IsTypeDefinition => false;

        public override bool HasElementType => true;

        public override Type ElementType => typeof(int);
    }

    public class IntRefTests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(int).MakeByRefType();

        public override TypeAttributes Attributes => TypeAttributes.Public;

        public override Type BaseType => null;

        public override bool IsByRef => true;

        public override bool IsTypeDefinition => false;

        public override bool HasElementType => true;

        public override Type ElementType => typeof(int);
    }

    // Classes, structs, interfaces, enums
    public class NonGenericClassTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(NonGenericClass);
    }

    public class NonGenericSubClassOfNonGenericTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(NonGenericSubClassOfNonGeneric);

        public override Type BaseType => typeof(NonGenericClass);
    }

    public class GenericClass1TypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(GenericClass<string>);

        public override bool IsGenericType => true;

        public override bool IsConstructedGenericType => true;

        public override bool IsTypeDefinition => false;

        public override Type[] GenericTypeArguments => new Type[] { typeof(string) };

        public override Type GenericTypeDefinition => typeof(GenericClass<>);
    }

    public class GenericClass2TypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(GenericClass<int, string>);

        public override bool IsGenericType => true;

        public override bool IsConstructedGenericType => true;

        public override bool IsTypeDefinition => false;

        public override Type[] GenericTypeArguments => new Type[] { typeof(int), typeof(string) };

        public override Type GenericTypeDefinition => typeof(GenericClass<,>);
    }

    public class OpenGenericClassTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(GenericClass<>);

        public override bool ContainsGenericParameters => true;

        public override bool IsGenericType => true;

        public override bool IsGenericTypeDefinition => true;

        public override Type GenericTypeDefinition => typeof(GenericClass<>);
    }

    public class OpenGenericNestedClassTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(GenericClass<>).MakeGenericType(typeof(GenericClass<>));

        public override bool ContainsGenericParameters => true;

        public override bool IsConstructedGenericType => true;

        public override bool IsGenericType => true;

        public override bool IsTypeDefinition => false;

        public override Type[] GenericTypeArguments => new Type[] { typeof(GenericClass<>) };

        public override Type GenericTypeDefinition => typeof(GenericClass<>);
    }

    public class NonGenericSubClassOfGenericTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(NonGenericSubClassOfGeneric);

        public override Type BaseType => typeof(GenericClass<string>);
    }

    public class NonGenericStructTests : StructTypeTestBase
    {
        public override Type CreateType() => typeof(NonGenericStruct);
    }

    public class RefStructTests : StructTypeTestBase
    {
        public override Type CreateType() => typeof(RefStruct);
    }

    public class GenericStruct1Tests : StructTypeTestBase
    {
        public override Type CreateType() => typeof(GenericStruct<string>);

        public override bool IsGenericType => true;

        public override bool IsConstructedGenericType => true;

        public override bool IsTypeDefinition => false;

        public override Type[] GenericTypeArguments => new Type[] { typeof(string) };

        public override Type GenericTypeDefinition => typeof(GenericStruct<>);
    }

    public class GenericStruct2Tests : StructTypeTestBase
    {
        public override Type CreateType() => typeof(GenericStruct<int, string>);

        public override bool IsGenericType => true;

        public override bool IsConstructedGenericType => true;

        public override bool IsTypeDefinition => false;

        public override Type[] GenericTypeArguments => new Type[] { typeof(int), typeof(string) };

        public override Type GenericTypeDefinition => typeof(GenericStruct<,>);
    }

    public class NonGenericInterfaceTests : InterfaceTypeTestBase
    {
        public override Type CreateType() => typeof(NonGenericInterface);
    }

    public class GenericInterface1Tests : InterfaceTypeTestBase
    {
        public override Type CreateType() => typeof(GenericInterface<string>);

        public override bool IsGenericType => true;

        public override bool IsConstructedGenericType => true;

        public override bool IsTypeDefinition => false;

        public override Type[] GenericTypeArguments => new Type[] { typeof(string) };

        public override Type GenericTypeDefinition => typeof(GenericInterface<>);
    }

    public class GenericInterface2Tests : InterfaceTypeTestBase
    {
        public override Type CreateType() => typeof(GenericInterface<int, string>);

        public override bool IsGenericType => true;

        public override bool IsConstructedGenericType => true;

        public override bool IsTypeDefinition => false;

        public override Type[] GenericTypeArguments => new Type[] { typeof(int), typeof(string) };

        public override Type GenericTypeDefinition => typeof(GenericInterface<,>);
    }

    public class AbstractClassTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(AbstractClass);

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit;
    }

    public class OpenGenericInterfaceTests : InterfaceTypeTestBase
    {
        public override Type CreateType() => typeof(GenericInterface<>);

        public override bool ContainsGenericParameters => true;

        public override bool IsGenericType => true;

        public override bool IsGenericTypeDefinition => true;

        public override Type GenericTypeDefinition => typeof(GenericInterface<>);
    }

    public class NonGenericNestedTests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(Outside.Inside);

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.NestedPublic | TypeAttributes.BeforeFieldInit;

        public override Type DeclaringType => typeof(Outside);
    }

    public class GenericNestedTests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(Outside<int>.Inside<double>);

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.NestedPublic | TypeAttributes.BeforeFieldInit;

        public override Type DeclaringType => typeof(Outside<>);

        public override bool IsGenericType => true;

        public override bool IsConstructedGenericType => true;

        public override bool IsTypeDefinition => false;

        public override Type[] GenericTypeArguments => new Type[] { typeof(int), typeof(double) };

        public override Type GenericTypeDefinition => typeof(Outside<>.Inside<>);
    }

    public class OpenGenericNestedTests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(Outside<>.Inside<>);

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.NestedPublic | TypeAttributes.BeforeFieldInit;

        public override bool ContainsGenericParameters => true;

        public override Type DeclaringType => typeof(Outside<>);

        public override bool IsGenericType => true;

        public override bool IsGenericTypeDefinition => true;

        public override Type GenericTypeDefinition => typeof(Outside<>.Inside<>);
    }

    public class GenericTypeParameter1Of1Tests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(Outside<>).GetTypeInfo().GenericTypeParameters[0];

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public;

        public override bool ContainsGenericParameters => true;

        public override Type DeclaringType => typeof(Outside<>);

        public override GenericParameterAttributes? GenericParameterAttributes => Reflection.GenericParameterAttributes.None;

        public override bool IsGenericParameter => true;

        public override bool IsTypeDefinition => false;

        public override int? GenericParameterPosition => 0;
    }

    public class GenericTypeParameter1Of2Tests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(GenericClass<,>).GetTypeInfo().GenericTypeParameters[0];

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public;

        public override bool ContainsGenericParameters => true;

        public override Type DeclaringType => typeof(GenericClass<,>);

        public override GenericParameterAttributes? GenericParameterAttributes => Reflection.GenericParameterAttributes.None;

        public override bool IsGenericParameter => true;

        public override bool IsTypeDefinition => false;

        public override int? GenericParameterPosition => 0;
    }

    public class GenericTypeParameter2Of2Tests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(GenericClass<,>).GetTypeInfo().GenericTypeParameters[1];

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public;

        public override bool ContainsGenericParameters => true;

        public override Type DeclaringType => typeof(GenericClass<,>);

        public override GenericParameterAttributes? GenericParameterAttributes => Reflection.GenericParameterAttributes.None;

        public override bool IsGenericParameter => true;

        public override bool IsTypeDefinition => false;

        public override int? GenericParameterPosition => 1;
    }

    public class NestedGenericTypeParameter1Tests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(Outside<>.Inside<>).GetTypeInfo().GenericTypeParameters[0];

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public;

        public override bool ContainsGenericParameters => true;

        public override GenericParameterAttributes? GenericParameterAttributes => Reflection.GenericParameterAttributes.None;

        public override Type DeclaringType => typeof(Outside<>.Inside<>);

        public override bool IsGenericParameter => true;

        public override bool IsTypeDefinition => false;

        public override int? GenericParameterPosition => 0;
    }

    public class NestedGenericTypeParameter2Tests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(Outside<>.Inside<>).GetTypeInfo().GenericTypeParameters[1];

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public;

        public override bool ContainsGenericParameters => true;

        public override Type DeclaringType => typeof(Outside<>.Inside<>);

        public override GenericParameterAttributes? GenericParameterAttributes => Reflection.GenericParameterAttributes.None;

        public override bool IsGenericParameter => true;

        public override bool IsTypeDefinition => false;

        public override int? GenericParameterPosition => 1;
    }

    public class GenericMethodParameter1Of2Tests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(Outside<>).GetMethod(nameof(Outside<string>.TwoGenericMethod)).GetGenericArguments()[0];

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public;

        public override bool ContainsGenericParameters => true;

        public override MethodBase DeclaringMethod => typeof(Outside<>).GetMethod(nameof(Outside<string>.TwoGenericMethod));

        public override Type DeclaringType => typeof(Outside<>);

        public override GenericParameterAttributes? GenericParameterAttributes => Reflection.GenericParameterAttributes.None;

        public override bool IsGenericParameter => true;

        public override bool IsTypeDefinition => false;

        public override int? GenericParameterPosition => 0;
    }

    public class GenericMethodParameter2Of2Tests : TypePropertyTestBase
    {
        public override Type CreateType() => typeof(Outside<>).GetMethod(nameof(Outside<string>.TwoGenericMethod)).GetGenericArguments()[1];

        public override TypeAttributes Attributes => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public;

        public override bool ContainsGenericParameters => true;

        public override MethodBase DeclaringMethod => typeof(Outside<>).GetMethod(nameof(Outside<string>.TwoGenericMethod));

        public override Type DeclaringType => typeof(Outside<>);

        public override GenericParameterAttributes? GenericParameterAttributes => Reflection.GenericParameterAttributes.None;

        public override bool IsGenericParameter => true;

        public override bool IsTypeDefinition => false;

        public override int? GenericParameterPosition => 1;
    }

    public class MarshalByRefObjectTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(MarshalByRefObject);

        public override TypeAttributes Attributes => base.Attributes | TypeAttributes.Abstract;

        // Remoting is not supported in .NET Core
        public override bool IsMarshalByRef => false;
    }

    public class ContextBoundObjectTypeTests : ClassTypeTestBase
    {
        public override Type CreateType() => typeof(ContextBoundObject);

        public override TypeAttributes Attributes => base.Attributes | TypeAttributes.Abstract;

        public override Type BaseType => typeof(MarshalByRefObject);

        // Remoting is not supported in .NET Core
        public override bool IsMarshalByRef => false;
        public override bool IsContextful => false;
    }

    // By-Ref types.
    public class TypedReferenceTypeTests : StructTypeTestBase
    {
        public override Type CreateType() => typeof(TypedReference);
    }

    public class ArgIteratorTypeTests : StructTypeTestBase
    {
        public override Type CreateType() => typeof(ArgIterator);

        public override TypeAttributes Attributes => PlatformDetection.IsMonoRuntime
            ? TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit
            : TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
    }

    public class RuntimeArgumentHandleTypeTests : StructTypeTestBase
    {
        public override Type CreateType() => typeof(RuntimeArgumentHandle);
    }

    public class SpanIntTypeTests : StructTypeTestBase
    {
        public override Type CreateType() => typeof(Span<int>);

        public override Type[] GenericTypeArguments => new Type[] { typeof(int) };

        public override bool IsConstructedGenericType => true;

        public override bool IsGenericType => true;

        public override bool IsTypeDefinition => false;

        public override Type GenericTypeDefinition => typeof(Span<>);
    }
}
