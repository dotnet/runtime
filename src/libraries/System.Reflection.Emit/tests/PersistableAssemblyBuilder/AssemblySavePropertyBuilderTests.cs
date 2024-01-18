// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySavePropertyBuilderTests
    {
        [Fact]
        public void SetPropertyAccessorsAndOtherValues()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
                PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.SpecialName | PropertyAttributes.HasDefault, typeof(int), null);
                MethodBuilder getMethod = type.DefineMethod("GetMethod", MethodAttributes.Public | MethodAttributes.HideBySig, typeof(int), null);
                MethodBuilder setMethod = type.DefineMethod("SetMethod", MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), [typeof(int)]);
                MethodBuilder otherMethod = type.DefineMethod("OtherMethod", MethodAttributes.Family, typeof(int), [typeof(int)]);
                CustomAttributeBuilder customAttrBuilder = new CustomAttributeBuilder(typeof(IntPropertyAttribute).GetConstructor([typeof(int)]), [9]);
                property.SetCustomAttribute(customAttrBuilder);
                property.SetConstant(5);
                ILGenerator getterLGenerator = getMethod.GetILGenerator();
                getterLGenerator.Emit(OpCodes.Ldarg_0);
                getterLGenerator.Emit(OpCodes.Ldfld, field);
                getterLGenerator.Emit(OpCodes.Ret);
                property.SetGetMethod(getMethod);
                ILGenerator setterILGenerator = setMethod.GetILGenerator();
                setterILGenerator.Emit(OpCodes.Ldarg_0);
                setterILGenerator.Emit(OpCodes.Ldarg_1);
                setterILGenerator.Emit(OpCodes.Stfld, field);
                setterILGenerator.Emit(OpCodes.Ret);
                property.SetSetMethod(setMethod);
                ILGenerator otherILGenerator = otherMethod.GetILGenerator();
                otherILGenerator.Emit(OpCodes.Ldarg_0);
                otherILGenerator.Emit(OpCodes.Ldarg_1);
                otherILGenerator.Emit(OpCodes.Ret);
                property.AddOtherMethod(otherMethod);
                type.CreateType();
                saveMethod.Invoke(ab, new [] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                PropertyInfo propertyFromDisk = typeFromDisk.GetProperty("TestProperty", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo getMethodFromFile = propertyFromDisk.GetGetMethod(true);
                MethodInfo setMethodFromFile = propertyFromDisk.GetSetMethod(true);
                Assert.Equal(getMethod.Name, getMethodFromFile.Name);
                Assert.Equal(setMethod.Name, setMethodFromFile.Name);
                // Not sure how other methods should have loaded/tested
                // 'propertyFromDisk.GetAccessors(true)' did not return other method
                Assert.NotNull(typeFromDisk.GetMethod("OtherMethod", BindingFlags.NonPublic | BindingFlags.Instance));
                Assert.True(property.CanRead);
                Assert.True(property.CanWrite);
                Assert.Equal(property.CanRead, propertyFromDisk.CanRead);
                Assert.Equal(property.CanWrite, propertyFromDisk.CanWrite);
                Assert.Equal(property.Attributes, propertyFromDisk.Attributes);
                Assert.Equal(property.DeclaringType.FullName, propertyFromDisk.DeclaringType.FullName);
                IList<CustomAttributeData> caData = propertyFromDisk.GetCustomAttributesData();
                Assert.Equal(1, caData.Count);
                Assert.Equal(typeof(IntPropertyAttribute).FullName, caData[0].AttributeType.FullName);
                Assert.Equal(1, caData[0].ConstructorArguments.Count);
                Assert.Equal(9, caData[0].ConstructorArguments[0].Value);
            }
        }

        [Fact]
        public void SetVariousCustomAttributes_ForProperty()
        {
            using (TempFile file = TempFile.Create())
            {
                int expectedValue = 9;
                ConstructorInfo con = typeof(IntPropertyAttribute).GetConstructor([typeof(int)]);
                CustomAttributeBuilder customAttrBuilder = new CustomAttributeBuilder(con, [expectedValue]);
                PropertyInfo prop = typeof(CustomAttributeBuilder).GetProperty("Data", BindingFlags.NonPublic | BindingFlags.Instance);
                byte[] binaryData = (byte[])prop.GetValue(customAttrBuilder, null);

                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null);
                property.SetCustomAttribute(con, binaryData);
                property.SetCustomAttribute(new CustomAttributeBuilder(typeof(SpecialNameAttribute).GetConstructor(Type.EmptyTypes), []));
                property.SetCustomAttribute(new CustomAttributeBuilder(typeof(MaybeNullAttribute).GetConstructor(Type.EmptyTypes), []));
                property.SetConstant(99);
                MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(int), null);
                ILGenerator methodILGenerator = method.GetILGenerator();
                methodILGenerator.Emit(OpCodes.Ldarg_0);
                methodILGenerator.Emit(OpCodes.Ret);
                property.SetGetMethod(method);
                type.CreateType();
                saveMethod.Invoke(ab, [file.Path]);

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                PropertyInfo propertyFromDisk = typeFromDisk.GetProperty("TestProperty", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.True(propertyFromDisk.Attributes.HasFlag(PropertyAttributes.SpecialName));
                IList<CustomAttributeData> attributes = propertyFromDisk.GetCustomAttributesData();
                Assert.Equal(2, attributes.Count);
                if (typeof(MaybeNullAttribute).FullName == attributes[0].AttributeType.FullName)
                {
                    Assert.Equal(0, attributes[0].ConstructorArguments.Count);
                    Assert.Equal(1, attributes[1].ConstructorArguments.Count);
                    Assert.Equal(typeof(IntPropertyAttribute).FullName, attributes[1].AttributeType.FullName);
                    Assert.Equal(expectedValue, attributes[1].ConstructorArguments[0].Value);
                }
                else
                {
                    Assert.Equal(0, attributes[1].ConstructorArguments.Count);
                    Assert.Equal(1, attributes[0].ConstructorArguments.Count);
                    Assert.Equal(typeof(IntPropertyAttribute).FullName, attributes[0].AttributeType.FullName);
                    Assert.Equal(expectedValue, attributes[0].ConstructorArguments[0].Value);
                }
                Assert.Empty(attributes[0].NamedArguments);
                Assert.Empty(attributes[1].NamedArguments);
            }
        }

        public static IEnumerable<object[]> SetConstant_TestData()
        {
            yield return new object[] { typeof(int), 10 };
            yield return new object[] { typeof(bool), true };
            yield return new object[] { typeof(sbyte), (sbyte)10 };
            yield return new object[] { typeof(short), (short)10 };
            yield return new object[] { typeof(long), 10L };

            yield return new object[] { typeof(byte), (byte)10 };
            yield return new object[] { typeof(ushort), (ushort)10 };
            yield return new object[] { typeof(uint), 10u };
            yield return new object[] { typeof(ulong), 10UL };

            yield return new object[] { typeof(float), 10f };
            yield return new object[] { typeof(double), 10d };

            yield return new object[] { typeof(DateTime), DateTime.Now };
            yield return new object[] { typeof(char), 'a' };
            yield return new object[] { typeof(string), "a" };

            yield return new object[] { typeof(PropertyBuilderTest11.Colors), PropertyBuilderTest11.Colors.Blue };
            yield return new object[] { typeof(object), null };
            yield return new object[] { typeof(object), "a" };
        }

        [Theory]
        [MemberData(nameof(SetConstant_TestData))]
        public void SetConstantVariousValues(Type returnType, object defaultValue)
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);

            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, returnType, null);
            property.SetConstant(defaultValue);

            Assert.Equal(defaultValue, property.GetConstantValue());
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullConstructorInfo_ThrowsArgumentNullException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null);

            AssertExtensions.Throws<ArgumentNullException>("con", () => property.SetCustomAttribute(null, new byte[6]));
        }

        [Fact]
        public void Set_NullValue_ThrowsArgumentNullException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), null);

            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => property.SetGetMethod(null));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => property.SetSetMethod(null));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => property.AddOtherMethod(null));
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => property.SetCustomAttribute(null));
        }

        [Fact]
        public void Set_WhenTypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null);

            MethodAttributes getMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            MethodBuilder method = type.DefineMethod("TestMethod", getMethodAttributes, typeof(int), null);
            method.GetILGenerator().Emit(OpCodes.Ret);
            AssertExtensions.Throws<ArgumentException>(() => property.SetConstant((decimal)10));
            CustomAttributeBuilder customAttrBuilder = new CustomAttributeBuilder(typeof(IntPropertyAttribute).GetConstructor([typeof(int)]), [10]);
            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => property.SetGetMethod(method));
            Assert.Throws<InvalidOperationException>(() => property.SetSetMethod(method));
            Assert.Throws<InvalidOperationException>(() => property.AddOtherMethod(method));
            Assert.Throws<InvalidOperationException>(() => property.SetConstant(1));
            Assert.Throws<InvalidOperationException>(() => property.SetCustomAttribute(customAttrBuilder));
        }

        [Fact]
        public void SetConstant_ValidationThrows()
        {
            AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo _);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Private);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(int), null);

            AssertExtensions.Throws<ArgumentException>(() => property.SetConstant((decimal)10));
            AssertExtensions.Throws<ArgumentException>(() => property.SetConstant(null));
            type.CreateType();
            Assert.Throws<InvalidOperationException>(() => property.SetConstant(1));
        }
    }
}
