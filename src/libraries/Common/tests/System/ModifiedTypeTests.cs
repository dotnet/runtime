// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Reflection.Tests;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests.Types
{
    // The "_Unmodified" tests use GetXxxType() and are essentially a baseline since they don't return modifiers.
    // The "_Modified" tests are based on the same types but use GetModifiedXxxType() in order to return the modifiers.
    public partial class ModifiedTypeTests
    {
        private const BindingFlags Bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void TypeMembers()
        {
            FieldInfo fi = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileInt), Bindings);

            Type unmodifiedType = fi.FieldType;
            CommonMembers(unmodifiedType);
            UnmodifiedTypeMembers(unmodifiedType);

            Type modifiedType = fi.GetModifiedFieldType();
            CommonMembers(modifiedType);
            ModifiedTypeMembers(modifiedType);

            void CommonMembers(Type t)
            {
                Assert.Equal("System.Int32", t.ToString());
                Assert.NotNull(t.AssemblyQualifiedName);
                Assert.Equal("Int32", t.Name);
                Assert.Equal("System", t.Namespace);
                Assert.False(t.IsFunctionPointer);
                Assert.False(t.IsPointer);
                Assert.False(t.IsUnmanagedFunctionPointer);
                Assert.NotNull(t.Assembly);
                Assert.True(t.Attributes != default);
                Assert.False(t.ContainsGenericParameters);
                Assert.False(t.ContainsGenericParameters);
                var _ = t.GUID; // Just ensure it doesn't throw.
                Assert.Throws<InvalidOperationException>(() => t.GenericParameterAttributes);
                Assert.Throws<InvalidOperationException>(() => t.GenericParameterPosition);
                Assert.Throws<InvalidOperationException>(() => t.GetFunctionPointerCallingConventions());
                Assert.Throws<InvalidOperationException>(() => t.GetFunctionPointerParameterTypes());
                Assert.Throws<InvalidOperationException>(() => t.GetFunctionPointerReturnType());
                Assert.False(t.HasElementType);
                Assert.False(t.IsAbstract);
                Assert.True(t.IsAnsiClass);
                Assert.False(t.IsArray);
                Assert.False(t.IsAutoClass);
                Assert.False(t.IsAutoLayout);
                Assert.False(t.IsByRef);
                Assert.False(t.IsByRefLike);
                Assert.False(t.IsCOMObject);
                Assert.False(t.IsClass);
                Assert.False(t.IsAbstract);
                Assert.False(t.IsConstructedGenericType);
                Assert.False(t.IsContextful);
                Assert.False(t.IsEnum);
                Assert.False(t.IsExplicitLayout);
                Assert.False(t.IsGenericMethodParameter);
                Assert.False(t.IsGenericParameter);
                Assert.False(t.IsGenericType);
                Assert.False(t.IsGenericTypeDefinition);
                Assert.False(t.IsGenericTypeParameter);
                Assert.False(t.IsImport);
                Assert.False(t.IsInterface);
                Assert.True(t.IsLayoutSequential);
                Assert.False(t.IsMarshalByRef);
                Assert.False(t.IsNotPublic);
                Assert.True(t.IsPublic);
                Assert.False(t.IsSZArray);
                Assert.True(t.IsSealed);

                if (FunctionPointerTestsExtensions.IsMetadataLoadContext)
                {
                    Assert.Throws<InvalidOperationException>(() => t.IsSecurityCritical);
                    Assert.Throws<InvalidOperationException>(() => t.IsSecuritySafeCritical);
                    Assert.Throws<InvalidOperationException>(() => t.IsSecurityTransparent);
                }
                else
                {
                    Assert.True(t.IsSecurityCritical);
                    Assert.False(t.IsSecuritySafeCritical);
                    Assert.False(t.IsSecurityTransparent);
                }

                Assert.True(t.IsSerializable);
                Assert.False(t.IsSignatureType);
                Assert.False(t.IsSpecialName);
                Assert.True(t.IsTypeDefinition);
                Assert.False(t.IsUnicodeClass);
                Assert.True(t.IsValueType);
                Assert.False(t.IsVariableBoundArray);
                Assert.Equal(MemberTypes.TypeInfo, t.MemberType);
                Assert.NotNull(t.Module);

                Assert.False(t.IsNestedAssembly);
                Assert.False(t.IsNestedFamANDAssem);
                Assert.False(t.IsNestedFamORAssem);
                Assert.False(t.IsNestedFamily);
                Assert.False(t.IsNestedPrivate);
                Assert.False(t.IsNestedPublic);
                Assert.Throws<ArgumentException>(() => t.GetArrayRank());
                Assert.Null(t.GetElementType());
                Assert.False(t.GenericTypeArguments.Any());
            }

            void UnmodifiedTypeMembers(Type t)
            {
                Assert.False(t.GetOptionalCustomModifiers().Any());
                Assert.False(t.GetRequiredCustomModifiers().Any());
                Assert.True(t.IsPrimitive);
                Assert.True(((object)t).Equals(t));
                Assert.True(t.Equals(t));
                Assert.Equal(t.GetHashCode(), t.GetHashCode());
                Assert.NotNull(t.BaseType);
                Assert.Null(t.DeclaringType);
                Assert.Null(t.ReflectedType);
                Assert.Null(t.TypeInitializer);

                if (PlatformDetection.IsMetadataTokenSupported)
                    Assert.True(t.MetadataToken != 0);

                Assert.False(t.IsNested);
                Assert.True(t.IsVisible);
            }

            void ModifiedTypeMembers(Type t)
            {
                Assert.False(t.GetOptionalCustomModifiers().Any());
                Assert.True(t.GetRequiredCustomModifiers().Any()); // The volatile modifier.
                Assert.True(t.IsPrimitive);
                Assert.Throws<NotSupportedException>(() => ((object)t).Equals(t));
                Assert.Throws<NotSupportedException>(() => t.Equals(t));
                Assert.Throws<NotSupportedException>(() => t.GetHashCode());
                Assert.Throws<NotSupportedException>(() => t.BaseType);
                Assert.Throws<NotSupportedException>(() => t.DeclaringType);
                Assert.Throws<NotSupportedException>(() => t.ReflectedType);
                Assert.Throws<NotSupportedException>(() => t.TypeInitializer);
                Assert.Throws<NotSupportedException>(() => t.MetadataToken);

                // These can call DeclaringType which is not supported.
                Assert.Throws<NotSupportedException>(() => t.IsNested);
                Assert.Throws<NotSupportedException>(() => t.IsVisible);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Fields_Modified()
        {
            Type volatileInt = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileInt), Bindings).GetModifiedFieldType();
            Verify(volatileInt);

            Type volatileIntArray = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileIntArray), Bindings).GetModifiedFieldType();
            Verify(volatileIntArray);

            Type volatileIntPointer = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileIntPointer), Bindings).GetModifiedFieldType();
            Verify(volatileIntPointer);
            Type volatileIntPointerElementType = volatileIntPointer.GetElementType();
            Assert.True(IsModifiedType(volatileIntPointerElementType));
            Assert.True(ReferenceEquals(volatileIntPointerElementType.UnderlyingSystemType, typeof(int).Project()));
            Assert.Equal(0, volatileIntPointerElementType.UnderlyingSystemType.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, volatileIntPointerElementType.UnderlyingSystemType.GetOptionalCustomModifiers().Length);

            Type volatileFcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileFcnPtr), Bindings).GetModifiedFieldType();
            Verify(volatileFcnPtr);
            Assert.True(IsModifiedType(volatileFcnPtr.GetFunctionPointerReturnType()));
            Assert.Equal(1, volatileFcnPtr.GetFunctionPointerParameterTypes().Length);
            Assert.True(IsModifiedType(volatileFcnPtr.GetFunctionPointerParameterTypes()[0]));

            void Verify(Type type)
            {
                Assert.True(IsModifiedType(type));

                Assert.Equal(1, type.GetRequiredCustomModifiers().Length);
                Assert.Equal(typeof(IsVolatile).Project(), type.GetRequiredCustomModifiers()[0]);

                Assert.Equal(0, type.GetOptionalCustomModifiers().Length);

                Assert.Equal(0, type.UnderlyingSystemType.GetRequiredCustomModifiers().Length);
                Assert.Equal(0, type.UnderlyingSystemType.GetOptionalCustomModifiers().Length);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Fields_Generic_Unmodified()
        {
            Type arrayGenericFcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._arrayGenericFcnPtr), Bindings).FieldType;
            Assert.True(arrayGenericFcnPtr.IsGenericType);
            Assert.False(arrayGenericFcnPtr.IsGenericTypeDefinition);
            Assert.False(IsModifiedType(arrayGenericFcnPtr));

            Type genericArg = arrayGenericFcnPtr.GetGenericArguments()[0];
            Assert.False(IsModifiedType(genericArg));

            Type fcnPtr = genericArg.GetElementType();
            Assert.True(fcnPtr.IsFunctionPointer);
            Assert.False(IsModifiedType(fcnPtr));

            Assert.Equal(1, fcnPtr.GetFunctionPointerParameterTypes().Length);
            Type paramType = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Assert.False(IsModifiedType(paramType));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Fields_Generic_Modified()
        {
            Type arrayGenericFcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._arrayGenericFcnPtr), Bindings).GetModifiedFieldType();
            Assert.True(IsModifiedType(arrayGenericFcnPtr));
            Assert.True(arrayGenericFcnPtr.IsGenericType);
            Assert.False(arrayGenericFcnPtr.IsGenericTypeDefinition);

            Type genericArg = arrayGenericFcnPtr.GetGenericArguments()[0];
            Assert.True(IsModifiedType(genericArg));

            Type fcnPtr = genericArg.GetElementType();
            Assert.True(fcnPtr.IsFunctionPointer);
            Assert.True(IsModifiedType(fcnPtr));

            Assert.Equal(1, fcnPtr.GetFunctionPointerParameterTypes().Length);
            Type paramType = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Assert.True(IsModifiedType(paramType));
            Assert.Equal(1, paramType.GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(OutAttribute).Project(), paramType.GetRequiredCustomModifiers()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Methods_OpenGeneric_Unmodified()
        {
            MethodInfo mi = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_ArrayOpenGenericFcnPtr), Bindings);
            Assert.Equal(1, mi.GetGenericArguments().Length);
            Type p0 = mi.GetGenericArguments()[0];
            Assert.True(p0.IsGenericMethodParameter);
            Assert.False(IsModifiedType(p0));

            Type arr = mi.GetParameters()[1].ParameterType;
            Assert.False(IsModifiedType(arr));

            Type p1 = arr.GetElementType();
            Assert.True(p1.IsFunctionPointer);
            Assert.False(p1.IsGenericTypeParameter);
            Assert.False(IsModifiedType(p1));
            Assert.Equal(1, p1.GetFunctionPointerParameterTypes().Length);
            Type paramType = p1.GetFunctionPointerParameterTypes()[0];
            Assert.Equal(0, paramType.GetRequiredCustomModifiers().Length);
            Assert.False(IsModifiedType(paramType));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Methods_OpenGeneric_Modified()
        {
            MethodInfo mi = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_ArrayOpenGenericFcnPtr), Bindings);
            Assert.Equal(1, mi.GetGenericArguments().Length);
            Type p0 = mi.GetGenericArguments()[0];
            Assert.True(p0.IsGenericMethodParameter);
            Assert.False(IsModifiedType(p0));

            Type arr = mi.GetParameters()[1].GetModifiedParameterType();
            Assert.True(IsModifiedType(arr));

            Type p1 = arr.GetElementType();
            Assert.True(p1.IsFunctionPointer);
            Assert.False(p1.IsGenericTypeParameter);
            Assert.True(IsModifiedType(p1));

            Assert.Equal(1, p1.GetFunctionPointerParameterTypes().Length);
            Type paramType = p1.GetFunctionPointerParameterTypes()[0];
            Assert.True(IsModifiedType(paramType));
            Assert.Equal(1, paramType.GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(OutAttribute).Project(), paramType.GetRequiredCustomModifiers()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Fields_Unmodified()
        {
            Type volatileInt = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileInt), Bindings).FieldType;
            Verify(volatileInt);

            Type volatileIntArray = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileIntArray), Bindings).FieldType;
            Verify(volatileIntArray);

            Type volatileIntPointer = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileIntPointer), Bindings).FieldType;
            Verify(volatileIntPointer);
            Type volatileIntPointerElementType = volatileIntPointer.GetElementType();
            Assert.False(IsModifiedType(volatileIntPointerElementType));
            Assert.True(ReferenceEquals(volatileIntPointerElementType, typeof(int).Project()));
            Assert.Equal(0, volatileIntPointerElementType.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, volatileIntPointerElementType.GetOptionalCustomModifiers().Length);

            Type volatileFcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileFcnPtr), Bindings).FieldType;
            Verify(volatileFcnPtr);
            Assert.False(IsModifiedType(volatileFcnPtr.GetFunctionPointerReturnType()));
            Assert.Equal(1, volatileFcnPtr.GetFunctionPointerParameterTypes().Length);
            Assert.False(IsModifiedType(volatileFcnPtr.GetFunctionPointerParameterTypes()[0]));

            void Verify(Type type)
            {
                Assert.False(IsModifiedType(type));
                Assert.Equal(0, type.GetRequiredCustomModifiers().Length);
                Assert.Equal(0, type.GetOptionalCustomModifiers().Length);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Fields_Parameterized_Basic()
        {
            Type ptr_ptr_int = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._ptr_ptr_int), Bindings).GetModifiedFieldType();
            Verify(ptr_ptr_int);
            Assert.True(ptr_ptr_int.UnderlyingSystemType.IsPointer);

            Type array_ptr_int = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._array_ptr_int), Bindings).GetModifiedFieldType();
            Verify(array_ptr_int);
            Assert.True(array_ptr_int.UnderlyingSystemType.IsArray);

            void Verify(Type type)
            {
                Assert.True(IsModifiedType(type));
                Assert.False(IsModifiedType(type.UnderlyingSystemType));
                Assert.True(IsModifiedType(type.GetElementType()));
                Assert.Equal(typeof(int).Project(), ptr_ptr_int.GetElementType().UnderlyingSystemType.GetElementType());
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Fields_Parameterized_FcnPtr()
        {
            Type ptr_fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._ptr_fcnPtr), Bindings).GetModifiedFieldType();
            Assert.True(ptr_fcnPtr.IsPointer);
            Assert.True(IsModifiedType(ptr_fcnPtr));
            Verify(ptr_fcnPtr.GetElementType());

            Type array_ptr_fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._array_ptr_fcnPtr), Bindings).GetModifiedFieldType();
            Assert.True(array_ptr_fcnPtr.IsArray);
            Assert.True(IsModifiedType(array_ptr_fcnPtr));
            Assert.True(array_ptr_fcnPtr.GetElementType().IsPointer);
            Assert.True(IsModifiedType(array_ptr_fcnPtr.GetElementType()));
            Verify(array_ptr_fcnPtr.GetElementType().GetElementType());

            Type fcnPtr_fcnPtrReturn = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrReturn), Bindings).GetModifiedFieldType();
            Assert.True(fcnPtr_fcnPtrReturn.GetFunctionPointerReturnType().IsFunctionPointer);
            Assert.True(IsModifiedType(fcnPtr_fcnPtrReturn.GetFunctionPointerReturnType()));
            Verify(fcnPtr_fcnPtrReturn.GetFunctionPointerReturnType());

            void Verify(Type type)
            {
                Assert.True(IsModifiedType(type));
                Assert.True(type.IsFunctionPointer);
                Assert.Equal(0, type.GetFunctionPointerParameterTypes().Length);
                Assert.NotSame(typeof(void).Project(), type.GetFunctionPointerReturnType());
                Assert.Same(typeof(void).Project(), type.GetFunctionPointerReturnType().UnderlyingSystemType);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Fields_VerifyIdempotency()
        {
            // Call these again to ensure any backing caching strategy works.
            Fields_Modified();
            Fields_Unmodified();
            Fields_Parameterized_Basic();
            Fields_Parameterized_FcnPtr();
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void MethodParameters()
        {
            ParameterInfo[] parameters = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_P0IntOut), Bindings).GetParameters();
            Assert.True(IsModifiedType(parameters[0].GetModifiedParameterType()));

            parameters = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_P0FcnPtrOut), Bindings).GetParameters();
            Type[] fnParameters = parameters[0].GetModifiedParameterType().GetFunctionPointerParameterTypes();
            Assert.Equal(1, fnParameters.Length);
            Assert.Equal(typeof(OutAttribute).Project(), fnParameters[0].GetRequiredCustomModifiers()[0]);

            ParameterInfo returnParameter = typeof(ModifiedTypeHolder).Project().GetProperty(nameof(ModifiedTypeHolder.InitProperty_Int), Bindings).GetSetMethod().ReturnParameter;
            Assert.Equal(1, returnParameter.GetRequiredCustomModifiers().Length);
            Assert.Equal(1, returnParameter.GetModifiedParameterType().GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(IsExternalInit).Project(), returnParameter.GetModifiedParameterType().GetRequiredCustomModifiers()[0]);

            returnParameter = typeof(ModifiedTypeHolder).Project().GetProperty(nameof(ModifiedTypeHolder.Property_FcnPtr), Bindings).GetGetMethod().ReturnParameter;
            Assert.True(returnParameter.ParameterType.IsFunctionPointer);
            Assert.Equal(0, returnParameter.ParameterType.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers().Length);
            Assert.Equal(1, returnParameter.GetModifiedParameterType().GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(OutAttribute).Project(), returnParameter.GetModifiedParameterType().GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void ConstructorParameters_Unmodified()
        {
            ParameterInfo[] parameters = typeof(ModifiedTypeHolder).Project().GetConstructors()[0].GetParameters();

            Type param0 = parameters[0].ParameterType;
            Assert.True(param0.IsFunctionPointer);
            Assert.False(IsModifiedType(param0));
            Type[] mods = param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers();
            Assert.Equal(0, mods.Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void ConstructorParameters_Modified()
        {
            ParameterInfo[] parameters = typeof(ModifiedTypeHolder).Project().GetConstructors()[0].GetParameters();

            Type param0 = parameters[0].GetModifiedParameterType();
            Assert.True(param0.IsFunctionPointer);
            Assert.True(IsModifiedType(param0));
            Type[] mods = param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers();
            Assert.Equal(1, mods.Length);
            Assert.Equal(typeof(OutAttribute).Project(), mods[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtrP0Out_Unmodified()
        {
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtrP0Out), Bindings).GetModifiedFieldType();
            Assert.True(fcnPtr.IsFunctionPointer);
            Assert.Equal(1, fcnPtr.GetFunctionPointerParameterTypes().Length);
            Assert.True(IsModifiedType(fcnPtr));
            Assert.Equal(typeof(int).Project().MakeByRefType(), fcnPtr.GetFunctionPointerParameterTypes()[0].UnderlyingSystemType);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtrP0Out_Modified()
        {
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtrP0Out), Bindings).FieldType;
            Assert.True(fcnPtr.IsFunctionPointer);
            Assert.Equal(1, fcnPtr.GetFunctionPointerParameterTypes().Length);
            Assert.False(IsModifiedType(fcnPtr));
            Assert.Equal(typeof(int).Project().MakeByRefType(), fcnPtr.GetFunctionPointerParameterTypes()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtr_fcnPtrP0Out_Unmodified()
        {
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrP0Out), Bindings).FieldType;
            Type param0 = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Assert.True(param0.IsFunctionPointer);
            Assert.Equal(1, param0.GetFunctionPointerParameterTypes().Length);
            Assert.False(IsModifiedType(param0));
            Assert.Equal(typeof(int).Project().MakeByRefType(), param0.GetFunctionPointerParameterTypes()[0]);
            Assert.Equal(0, param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtr_fcnPtrP0Out_Modified()
        {
            // Modified
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrP0Out), Bindings).GetModifiedFieldType();
            Type param0 = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Assert.True(param0.IsFunctionPointer);
            Assert.Equal(1, param0.GetFunctionPointerParameterTypes().Length);
            Assert.True(IsModifiedType(param0));
            Assert.Equal(typeof(int).Project().MakeByRefType(), param0.GetFunctionPointerParameterTypes()[0].UnderlyingSystemType);
            Assert.Equal(1, param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(OutAttribute).Project(), param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtr_fcnPtrP0Ref_Unmodified()
        {
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrP0Ref), Bindings).FieldType;
            Type param0 = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Assert.True(param0.IsFunctionPointer);
            Assert.Equal(1, param0.GetFunctionPointerParameterTypes().Length);
            Assert.False(IsModifiedType(param0));
            Assert.Equal(typeof(int).Project().MakeByRefType(), param0.GetFunctionPointerParameterTypes()[0]);
            Assert.Equal(0, param0.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, param0.GetOptionalCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtr_fcnPtrP0Ref_Modified()
        {
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrP0Ref), Bindings).GetModifiedFieldType();
            Type param0 = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Assert.True(param0.IsFunctionPointer);
            Assert.Equal(1, param0.GetFunctionPointerParameterTypes().Length);
            Assert.True(IsModifiedType(param0));
            Assert.Equal(typeof(int).Project().MakeByRefType(), param0.GetFunctionPointerParameterTypes()[0].UnderlyingSystemType);
            Assert.Equal(0, param0.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, param0.GetOptionalCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtr_fcnPtrRetP0Ref_Unmodified()
        {
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrRetP0Out), Bindings).FieldType;
            Type param0 = fcnPtr.GetFunctionPointerReturnType().GetFunctionPointerParameterTypes()[0];
            Assert.False(IsModifiedType(param0));
            Assert.Equal(0, param0.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, param0.GetOptionalCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtr_fcnPtrRetP0Ref_Modified()
        {
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrRetP0Out), Bindings).GetModifiedFieldType();
            Type param0 = fcnPtr.GetFunctionPointerReturnType().GetFunctionPointerParameterTypes()[0];
            Assert.True(IsModifiedType(param0));
            Assert.Equal(typeof(int).Project().MakeByRefType(), param0.UnderlyingSystemType);
            Assert.Equal(1, param0.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, param0.GetOptionalCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtr_complex_Unmodified()
        {
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_complex), Bindings).FieldType;
            Assert.Equal("System.SByte()(System.Byte(), System.Void(System.Int32)()[][], System.Void(System.Int32, System.String, System.Boolean&)()())", fcnPtr.ToString());

            Type f1 = fcnPtr.GetFunctionPointerParameterTypes()[2];
            Assert.Equal("System.Void(System.Int32, System.String, System.Boolean&)()()", f1.ToString());

            Type f2 = f1.GetFunctionPointerReturnType();
            Assert.Equal("System.Void(System.Int32, System.String, System.Boolean&)()", f2.ToString());

            Type f3 = f2.GetFunctionPointerReturnType();
            Assert.Equal("System.Void(System.Int32, System.String, System.Boolean&)", f3.ToString());

            Type target = f3.GetFunctionPointerParameterTypes()[2];
            Assert.Equal("System.Boolean&", target.ToString());

            Assert.False(IsModifiedType(target));
            Assert.Equal(0, target.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, target.GetOptionalCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void FunctionPointerParameters_fcnPtr_complex_Modified()
        {
            Type fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_complex), Bindings).GetModifiedFieldType();
            Assert.Equal("System.SByte()(System.Byte(), System.Void(System.Int32)()[][], System.Void(System.Int32, System.String, System.Boolean&)()())", fcnPtr.ToString());

            Type f1 = fcnPtr.GetFunctionPointerParameterTypes()[2];
            Assert.Equal("System.Void(System.Int32, System.String, System.Boolean&)()()", f1.ToString());

            Type f2 = f1.GetFunctionPointerReturnType();
            Assert.Equal("System.Void(System.Int32, System.String, System.Boolean&)()", f2.ToString());

            Type f3 = f2.GetFunctionPointerReturnType();
            Assert.Equal("System.Void(System.Int32, System.String, System.Boolean&)", f3.ToString());

            Type target = f3.GetFunctionPointerParameterTypes()[2];
            Assert.Equal("System.Boolean&", target.ToString());

            Assert.True(IsModifiedType(target));
            Assert.Equal(1, target.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, target.GetOptionalCustomModifiers().Length);
            Assert.Equal(typeof(OutAttribute).Project(), target.GetRequiredCustomModifiers()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Property_FcnPtr_Complex_Unmodified()
        {
            Type mt = typeof(ModifiedTypeHolder).Project().GetProperty(nameof(ModifiedTypeHolder.Property_FcnPtr_Complex), Bindings).PropertyType;
            Type f1 = mt.GetElementType();
            Assert.Equal("System.Boolean(System.Int32(), System.Void(System.Byte(), System.Int32(), System.Int64()))", f1.ToString());

            Type f2 = f1.GetFunctionPointerParameterTypes()[1];
            Assert.Equal("System.Void(System.Byte(), System.Int32(), System.Int64())", f2.ToString());

            Type target = f2.GetFunctionPointerParameterTypes()[2];
            Assert.Equal("System.Int64()", target.ToString());

            Assert.Equal(0, target.GetFunctionPointerCallingConventions().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Property_FcnPtr_Complex_Modified()
        {
            Type mt = typeof(ModifiedTypeHolder).Project().GetProperty(nameof(ModifiedTypeHolder.Property_FcnPtr_Complex), Bindings).GetModifiedPropertyType();

            Type f1 = mt.GetElementType();
            Assert.Equal("System.Boolean(System.Int32(), System.Void(System.Byte(), System.Int32(), System.Int64()))", f1.ToString());

            Type f2 = f1.GetFunctionPointerParameterTypes()[1];
            Assert.Equal("System.Void(System.Byte(), System.Int32(), System.Int64())", f2.ToString());

            Type target = f2.GetFunctionPointerParameterTypes()[2];
            Assert.Equal("System.Int64()", target.ToString());
            Assert.Equal(1, target.GetFunctionPointerCallingConventions().Length);
            Assert.Equal(typeof(CallConvCdecl).Project(), target.GetFunctionPointerCallingConventions()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void MethodWithGenericParameter_Unmodified()
        {
            MethodInfo mi = typeof(GenericWithModifiers).Project().GetMethod(nameof(GenericWithModifiers.MethodWithGenericParameter), Bindings);
            Assert.False(mi.ContainsGenericParameters);

            Type a1 = mi.GetParameters()[0].ParameterType;
            Assert.False(IsModifiedType(a1));
            Assert.Equal(typeof(Tuple<int, bool>).Project(), a1.Project());

            Type ga1 = a1.GetGenericArguments()[0];
            Assert.False(IsModifiedType(ga1));
            Assert.Equal(typeof(int).Project(), ga1);

            Type ga2 = a1.GetGenericArguments()[1];
            Assert.False(IsModifiedType(ga2));
            Assert.Equal(typeof(bool).Project(), ga2);
            Assert.Equal(0, ga2.GetOptionalCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void MethodWithGenericParameter_Modified()
        {
            MethodInfo mi = typeof(GenericWithModifiers).Project().GetMethod(nameof(GenericWithModifiers.MethodWithGenericParameter), Bindings);
            Assert.False(mi.ContainsGenericParameters);

            Type a1 = mi.GetParameters()[0].GetModifiedParameterType();
            Assert.True(IsModifiedType(a1));
            Assert.Equal(typeof(Tuple<int, bool>).Project(), a1.UnderlyingSystemType.Project());

            Type ga1 = a1.GetGenericArguments()[0];
            Assert.True(IsModifiedType(ga1));
            Assert.Equal(typeof(int).Project(), ga1.UnderlyingSystemType);
            Assert.Equal(0, ga1.GetOptionalCustomModifiers().Length);

            Type ga2 = a1.GetGenericArguments()[1];
            Assert.True(IsModifiedType(ga2));
            Assert.Equal(typeof(bool).Project(), ga2.UnderlyingSystemType);
            Assert.Equal(1, ga2.GetOptionalCustomModifiers().Length);
            Assert.Equal(typeof(IsConst).Project(), ga2.GetOptionalCustomModifiers()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void GenericMethod_Unmodified()
        {
            MethodInfo mi = typeof(GenericWithModifiers).Project().GetMethod(nameof(GenericWithModifiers.GenericMethod), Bindings);
            Assert.True(mi.ContainsGenericParameters);

            Type a1 = mi.GetParameters()[0].ParameterType;
            Assert.False(IsModifiedType(a1));
            Assert.True(a1.ContainsGenericParameters);
            Assert.Equal(0, a1.GetOptionalCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void GenericMethod_Modified()
        {
            MethodInfo mi = typeof(GenericWithModifiers).Project().GetMethod(nameof(GenericWithModifiers.GenericMethod), Bindings);
            Assert.True(mi.ContainsGenericParameters);

            Type a1 = mi.GetParameters()[0].GetModifiedParameterType();
            Assert.True(IsModifiedType(a1));
            Assert.True(a1.ContainsGenericParameters);
            Assert.Equal(1, a1.GetOptionalCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static void ParameterConstraints1()
        {
            MethodInfo mi = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_GenericWithParameterConstraint1), Bindings);
            Assert.True(mi.ContainsGenericParameters);
            Assert.True(mi.IsGenericMethod);
            Assert.True(mi.IsGenericMethodDefinition);

            Type p = mi.GetParameters()[0].ParameterType;
            Assert.False(IsModifiedType(p));
            Assert.True(p.ContainsGenericParameters);
            Assert.True(p.IsByRef);

            Type e = p.GetElementType();
            Assert.False(IsModifiedType(e));
            Assert.True(e.IsValueType);
            Assert.Equal(GenericParameterAttributes.DefaultConstructorConstraint | GenericParameterAttributes.NotNullableValueTypeConstraint, e.GenericParameterAttributes);
            Assert.True(e.GetGenericParameterConstraints().Length == 1);
            // The 'unmanaged' constraint is a modreq of type 'System.Runtime.InteropServices.' applied to 'ValueType'.
            Assert.Equal(typeof(ValueType).Project(), e.GetGenericParameterConstraints()[0]);
            // The 'UnmanagedType' modreq is not available for unmodified types.
            Assert.Equal(0, e.GetGenericParameterConstraints()[0].GetRequiredCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static void ParameterConstraints2()
        {
            MethodInfo mi = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_GenericWithParameterConstraint2), Bindings);
            Assert.True(mi.ContainsGenericParameters);
            Assert.True(mi.IsGenericMethod);
            Assert.True(mi.IsGenericMethodDefinition);

            Type p = mi.GetParameters()[0].ParameterType;
            Assert.False(IsModifiedType(p));
            Assert.True(p.ContainsGenericParameters);
            Assert.False(p.IsValueType);
            Assert.Equal(GenericParameterAttributes.DefaultConstructorConstraint, p.GenericParameterAttributes);
            Assert.True(p.GetGenericParameterConstraints().Length == 1);
            Assert.Equal(typeof(ModifiedTypeHolder.MyConstraint).Project(), p.GetGenericParameterConstraints()[0]);
        }

        private static bool IsModifiedType(Type type)
        {
            return !ReferenceEquals(type, type.UnderlyingSystemType);
        }

        public unsafe class ModifiedTypeHolder
        {
            public ModifiedTypeHolder(delegate*<out int, void> d) { }

            public static volatile int _volatileInt;
            public static volatile int[] _volatileIntArray;
            public static volatile int* _volatileIntPointer;
            public static volatile delegate* unmanaged[Cdecl]<bool, void> _volatileFcnPtr;

            // Although function pointer types can't be used as generic parameters, they can be used indirectly
            // as an array element type.
            public static volatile Tuple<delegate*<out bool, void>[]> _arrayGenericFcnPtr;

            public static int** _ptr_ptr_int;
            public static int*[] _array_ptr_int;
            public static delegate*<void>* _ptr_fcnPtr;
            public static delegate*<void>*[] _array_ptr_fcnPtr;
            public static delegate*<delegate*<void>> _fcnPtr_fcnPtrReturn;

            public static void M_P0IntOut(out int i) { i = 42; }
            public static void M_P0FcnPtrOut(delegate*<out int, void> fp) { }
            public static void M_ArrayOpenGenericFcnPtr<T>(T t, delegate*<out bool, void>[] fp) { }
            public static void M_GenericWithParameterConstraint1<T>(out T value) where T : unmanaged { value = default; }
            public static void M_GenericWithParameterConstraint2<T>(T value) where T : MyConstraint, new() { value = default; }

            public int InitProperty_Int { get; init; }

            public class MyConstraint { }

            public static delegate*<out int, void> Property_FcnPtr { get; set; }

            public static delegate*<out int, void> _fcnPtrP0Out;
            public static delegate*<delegate*<out int, void>, void> _fcnPtr_fcnPtrP0Out;
            public static delegate*<delegate*<ref int, void>, void> _fcnPtr_fcnPtrP0Ref;
            public static delegate*<delegate*<out int, void>> _fcnPtr_fcnPtrRetP0Out;

            public delegate*
            <
                delegate*<int>, // p0
                delegate*       // p1
                <
                    delegate*<byte>, // p0
                    delegate*<int>,  // p1
                    delegate* unmanaged[Cdecl]<long>, // p2
                    void // ret
                >,
                bool // ret
            >[] Property_FcnPtr_Complex { get; }

            public static delegate*
            <
                delegate*<byte>, // p0
                delegate*        // p1
                <
                    delegate*<int, void> // ret
                >[][],
                delegate* // p2
                <
                    delegate* // ret
                    <
                        delegate* // ret
                        <
                            int, string, out bool, void // p0-3
                        >
                    >
                >,
                delegate*<sbyte> // ret
            > _fcnPtr_complex;
        }
    }
}
