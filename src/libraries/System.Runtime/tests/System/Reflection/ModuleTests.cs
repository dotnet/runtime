// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;

namespace System.Reflection.Tests
{
    public class ModuleTests
    {
        [Fact]
        public void Assembly_Get_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.Assembly);
        }

        [Fact]
        public void CustomAttributes_Get_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.CustomAttributes);
        }

        public static IEnumerable<object[]> CustomAttributes_CustomGetCustomAttributesData_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new CustomAttributeData[0] };
            yield return new object[] { new CustomAttributeData[] { null } };
            yield return new object[] { new CustomAttributeData[] { new SubCustomAttributeData() } };
        }

        [Theory]
        [MemberData(nameof(CustomAttributes_CustomGetCustomAttributesData_TestData))]
        public void CustomAttributes_GetCustomGetCustomAttributesData_Success(IList<CustomAttributeData> result)
        {
            var module = new CustomModule
            {
                GetCustomAttributesDataAction = () => result
            };
            Assert.Same(result, module.CustomAttributes);
        }

        [Fact]
        public void FilterTypeName_Get_ReturnsExpected()
        {
            Assert.NotNull(Module.FilterTypeName);
            Assert.Same(Module.FilterTypeName, Module.FilterTypeName);
            Assert.NotSame(Module.FilterTypeName, Module.FilterTypeNameIgnoreCase);
        }

        public static IEnumerable<object[]> FilterTypeName_TestData()
        {
            yield return new object[] { typeof(int), "", false };
            yield return new object[] { typeof(int), typeof(int).Name, true };
            yield return new object[] { typeof(int), typeof(int).Name.ToLower(), false };
            yield return new object[] { typeof(int), "*", true };
            yield return new object[] { typeof(int), "*abc", false };
            yield return new object[] { typeof(int), "*Int32", false };
            yield return new object[] { typeof(int), "I*", true };
            yield return new object[] { typeof(int), "i*", false };
            yield return new object[] { typeof(int), typeof(int).Name + "*", true };
            
            yield return new object[] { typeof(string), "String", true };
            yield return new object[] { typeof(string), "*", true };
            yield return new object[] { typeof(string), "S*", true };
            yield return new object[] { typeof(string), "Strin*", true };
            yield return new object[] { typeof(string), "String*", true };
            yield return new object[] { typeof(string), "", false };
            yield return new object[] { typeof(string), "S", false };
            yield return new object[] { typeof(string), " String", false };
            yield return new object[] { typeof(string), "String ", false };
            yield return new object[] { typeof(string), "Strings", false };
        }

        [Theory]
        [MemberData(nameof(FilterTypeName_TestData))]
        public void FilterTypeName_Invoke_ReturnsExpected(Type m, object filterCriteria, bool expected)
        {
            Assert.Equal(expected, Module.FilterTypeName.Invoke(m, filterCriteria));
        }

        [Theory]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("abc*")]
        [InlineData("abc")]
        public void FilterTypeName_NullM_ThrowsNullReferenceException(object filterCriteria)
        {
            Assert.Throws<NullReferenceException>(() => Module.FilterTypeName(null, filterCriteria));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void FilterTypeName_InvalidFilterCriteria_ThrowsInvalidFilterCriteriaException(object filterCriteria)
        {
            Assert.Throws<InvalidFilterCriteriaException>(() => Module.FilterTypeName(typeof(int), filterCriteria));
        }

        [Fact]
        public void FilterTypeNameIgnoreCase_Get_ReturnsExpected()
        {
            Assert.NotNull(Module.FilterTypeNameIgnoreCase);
            Assert.Same(Module.FilterTypeNameIgnoreCase, Module.FilterTypeNameIgnoreCase);
            Assert.NotSame(Module.FilterTypeNameIgnoreCase, Module.FilterTypeName);
        }

        public static IEnumerable<object[]> FilterTypeNameIgnoreCase_TestData()
        {
            yield return new object[] { typeof(int), "", false };
            yield return new object[] { typeof(int), typeof(int).Name, true };
            yield return new object[] { typeof(int), typeof(int).Name.ToLower(), true };
            yield return new object[] { typeof(int), "*", true };
            yield return new object[] { typeof(int), "*abc", false };
            yield return new object[] { typeof(int), "*Int32", false };
            yield return new object[] { typeof(int), "I*", true };
            yield return new object[] { typeof(int), "i*", true };
            yield return new object[] { typeof(int), typeof(int).Name + "*", true };
            
            yield return new object[] { typeof(string), "string", true };
            yield return new object[] { typeof(string), "*", true };
            yield return new object[] { typeof(string), "s*", true };
            yield return new object[] { typeof(string), "stRIn*", true };
            yield return new object[] { typeof(string), "sTrInG", true };
            yield return new object[] { typeof(string), "STRING*", true };
            yield return new object[] { typeof(string), "", false };
            yield return new object[] { typeof(string), "s", false };
            yield return new object[] { typeof(string), " string", false };
            yield return new object[] { typeof(string), "string ", false };
            yield return new object[] { typeof(string), "strings", false };

        }

        [Theory]
        [MemberData(nameof(FilterTypeNameIgnoreCase_TestData))]
        public void FilterTypeNameIgnoreCase_Invoke_ReturnsExpected(Type m, object filterCriteria, bool expected)
        {
            Assert.Equal(expected, Module.FilterTypeNameIgnoreCase.Invoke(m, filterCriteria));
        }

        [Theory]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("abc*")]
        [InlineData("abc")]
        public void FilterTypeNameIgnoreCase_NullM_ThrowsNullReferenceException(object filterCriteria)
        {
            Assert.Throws<NullReferenceException>(() => Module.FilterTypeNameIgnoreCase(null, filterCriteria));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void FilterTypeNameIgnoreCase_InvalidFilterCriteria_ThrowsInvalidFilterCriteriaException(object filterCriteria)
        {
            Assert.Throws<InvalidFilterCriteriaException>(() => Module.FilterTypeNameIgnoreCase(typeof(int), filterCriteria));
        }

        [Fact]
        public void FulllyQualifiedName_Get_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.FullyQualifiedName);
        }

        [Fact]
        public void MDStreamVersion_Get_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.MDStreamVersion);
        }

        [Fact]
        public void MetadataToken_Get_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.MetadataToken);
        }

        [Fact]
        public void ModuleHandle_Get_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Equal(ModuleHandle.EmptyHandle, module.ModuleHandle);
        }

        [Fact]
        public void ModuleVersionId_Get_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ModuleVersionId);
        }

        [Fact]
        public void Name_Get_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.Name);
        }

        [Fact]
        public void ScopeName_Get_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ScopeName);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var module = new SubModule();
            yield return new object[] { module, module, true };
            yield return new object[] { module, new SubModule(), false };
            yield return new object[] { module, new object(), false };
            yield return new object[] { module, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(Module module, object other, bool expected)
        {
            Assert.Equal(expected, module.Equals(other));
        }

        public static IEnumerable<object[]> OperatorEquals_TestData()
        {
            var module = new SubModule();
            yield return new object[] { null, null, true };
            yield return new object[] { null, module, false };
            yield return new object[] { module, module, true };
            yield return new object[] { module, new SubModule(), false };
            yield return new object[] { module, null, false };

            yield return new object[] { new AlwaysEqualsModule(), null, false };
            yield return new object[] { null, new AlwaysEqualsModule(), false };
            yield return new object[] { new AlwaysEqualsModule(), new SubModule(), true };
            yield return new object[] { new SubModule(), new AlwaysEqualsModule(), false };
            yield return new object[] { new AlwaysEqualsModule(), new AlwaysEqualsModule(), true };
        }

        [Theory]
        [MemberData(nameof(OperatorEquals_TestData))]
        public void OperatorEquals_Invoke_ReturnsExpected(Module module1, Module module2, bool expected)
        {
            Assert.Equal(expected, module1 == module2);
            Assert.Equal(!expected, module1 != module2);
        }

        public static IEnumerable<object[]> FindTypes_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { null, new object() };
            yield return new object[] { (TypeFilter)((m, filterCriteria) => true), null };
            yield return new object[] { (TypeFilter)((m, filterCriteria) => true), new object() };
        }

        [Theory]
        [MemberData(nameof(FindTypes_TestData))]
        public void FindTypes_Invoke_ThrowsNotImplementedException(TypeFilter filter, object filterCriteria)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.FindTypes(filter, filterCriteria));
        }

        public static IEnumerable<object[]> FindTypesCustom_TestData()
        {
            yield return new object[] { new Type[0], null, null, new Type[0] };
            yield return new object[] { new Type[0], null, new object(), new Type[0] };
            yield return new object[] { new Type[0], (TypeFilter)((m, filterCriteria) => true), null, new Type[0] };
            yield return new object[] { new Type[0], (TypeFilter)((m, filterCriteria) => true), new object(), new Type[0] };

            yield return new object[] { new Type[] { null }, null, null, new Type[] { null } };
            yield return new object[] { new Type[] { null }, null, new object(), new Type[] { null } };
            yield return new object[] { new Type[] { null }, (TypeFilter)((m, filterCriteria) => true), null, new Type[] { null } };
            yield return new object[] { new Type[] { null }, (TypeFilter)((m, filterCriteria) => true), new object(), new Type[] { null } };

            yield return new object[] { new Type[] { typeof(int) }, null, null, new Type[] { typeof(int) } };
            yield return new object[] { new Type[] { typeof(int) }, null, new object(), new Type[] { typeof(int) } };
            yield return new object[] { new Type[] { typeof(int) }, (TypeFilter)((m, filterCriteria) => true), null, new Type[] { typeof(int) } };
            yield return new object[] { new Type[] { typeof(int) }, (TypeFilter)((m, filterCriteria) => true), new object(), new Type[] { typeof(int) } };            yield return new object[] { new Type[] { typeof(int) }, (TypeFilter)((m, filterCriteria) => false), null, new Type[0] };
            yield return new object[] { new Type[] { typeof(int) }, (TypeFilter)((m, filterCriteria) => false), new object(), new Type[0] };

            yield return new object[] { new Type[] { typeof(int), typeof(string) }, (TypeFilter)((m, filterCriteria) => m == typeof(int)), null, new Type[] { typeof(int) } };
            yield return new object[] { new Type[] { typeof(int), typeof(string) }, (TypeFilter)((m, filterCriteria) => m == typeof(int)), new object(), new Type[] { typeof(int) } };
        }

        [Theory]
        [MemberData(nameof(FindTypesCustom_TestData))]
        public void FindTypes_InvokeCustom_ReturnsExpected(Type[] types, TypeFilter filter, object filterCriteria, Type[] expected)
        {
            var module = new CustomModule
            {
                GetTypesAction = () => types
            };
            Assert.Equal(expected, module.FindTypes(filter, filterCriteria));
        }

        [Theory]
        [MemberData(nameof(FindTypes_TestData))]
        public void FindTypes_NullGetTypes_ThrowsNullReferenceException(TypeFilter filter, object filterCriteria)
        {
            var module = new CustomModule
            {
                GetTypesAction = () => null
            };
            Assert.Throws<NullReferenceException>(() => module.FindTypes(filter, filterCriteria));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCustomAttributes_InvokeBool_ThrowsNotImplementedException(bool inherit)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetCustomAttributes(inherit));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCustomAttributes_InvokeBoolCustom_ThrowsNotImplementedException(bool inherit)
        {
            var module = new CustomModule
            {
                GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
            };
            Assert.Throws<NotImplementedException>(() => module.GetCustomAttributes(inherit));
        }

        [Theory]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(int), false)]
        [InlineData(null, true)]
        [InlineData(null, false)]
        public void GetCustomAttributes_InvokeTypeBool_ThrowsNotImplementedException(Type attributeType, bool inherit)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetCustomAttributes(attributeType, inherit));
        }

        [Fact]
        public void GetCustomAttributesData_Invoke_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetCustomAttributesData());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void GetField_InvokeString_ThrowsNotImplementedException(string name)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetField(name));
        }

        public static IEnumerable<object[]> GetFieldCustom_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, typeof(ConsoleColor).GetFields()[0] };
            yield return new object[] { "Field", typeof(ConsoleColor).GetFields()[0] };
        }

        [Theory]
        [MemberData(nameof(GetFieldCustom_TestData))]
        public void GetField_InvokeStringCustom_ReturnsExpected(string name, FieldInfo result)
        {
            var module = new CustomModule
            {
                GetFieldAction = (nameParam, bindingAttrParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, module.GetField(name));
        }

        [Theory]
        [InlineData(null, BindingFlags.Public)]
        [InlineData("", BindingFlags.Public)]
        [InlineData("name", BindingFlags.Public)]
        public void GetField_InvokeStringBindingAttr_ThrowsNotImplementedException(string name, BindingFlags bindingAttr)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetField(name, bindingAttr));
        }

        [Fact]
        public void GetFields_Invoke_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetFields());
        }

        public static IEnumerable<object[]> GetFieldsCustom_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new FieldInfo[0] };
            yield return new object[] { new FieldInfo[] { typeof(ConsoleColor).GetFields()[0] } };
            yield return new object[] { new FieldInfo[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetFieldsCustom_TestData))]
        public void GetFields_InvokeCustom_ReturnsExpected(FieldInfo[] result)
        {
            var module = new CustomModule
            {
                GetFieldsAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, module.GetFields());
        }

        [Theory]
        [InlineData(BindingFlags.Public)]
        public void GetFields_InvokeBindingFlags_ThrowsNotImplementedException(BindingFlags bindingFlags)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetFields(bindingFlags));
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var module = new SubModule();
            Assert.NotEqual(0, module.GetHashCode());
            Assert.Equal(module.GetHashCode(), module.GetHashCode());
        }

        public static IEnumerable<object[]> GetMethod_String_TestData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "Name" };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_TestData))]
        public void GetMethod_InvokeString_ThrowNotImplementedException(string name)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetMethod(name));
        }

        public static IEnumerable<object[]> GetMethod_StringCustom_TestData()
        {
            yield return new object[] { string.Empty, null };
            yield return new object[] { "Name", typeof(ModuleTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_StringCustom_TestData))]
        public void GetMethod_InvokeStringCustom_ReturnsExpected(string name, MethodInfo result)
        {
            var module = new CustomModule
            {
                GetMethodImplAction = (nameParam, bindingAttrParam, binderParam, callConventionParam, modulesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Null(modulesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, module.GetMethod(name));
        }

        public static IEnumerable<object[]> GetMethod_String_TypeArray_TestData()
        {
            yield return new object[] { string.Empty, new Type[0] };
            yield return new object[] { "Name", new Type[] { typeof(int) } };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_TypeArray_TestData))]
        public void GetMethod_InvokeStringTypeArray_ThrowsNotImplementedException(string name, Type[] modules)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetMethod(name, modules));
        }

        public static IEnumerable<object[]> GetMethod_String_TypeArrayCustom_TestData()
        {
            yield return new object[] { string.Empty, new Type[0], null };
            yield return new object[] { "Name", new Type[] { typeof(int) }, typeof(ModuleTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_TypeArrayCustom_TestData))]
        public void GetMethod_InvokeStringTypeArrayCustom_ReturnsExpected(string name, Type[] modules, MethodInfo result)
        {
            var module = new CustomModule
            {
                GetMethodImplAction = (nameParam, bindingAttrParam, binderParam, callConventionParam, modulesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Same(modules, modulesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, module.GetMethod(name, modules));
        }

        public static IEnumerable<object[]> GetMethod_String_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { string.Empty, BindingFlags.Public, null, CallingConventions.Any, new Type[0], null };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { typeof(int) }, new ParameterModifier[0] };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Standard - 1, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() } };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArray_TestData))]
        public void GetMethod_InvokeStringBindingFlagsBinderCallingConventionsTypeArrayParameterModifierArrayCustom_ThrowsNotImplementedException(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] modules, ParameterModifier[] modifiers)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetMethod(name, bindingAttr, binder, callConvention, modules, modifiers));
        }

        public static IEnumerable<object[]> GetMethod_String_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArrayCustom_TestData()
        {
            yield return new object[] { string.Empty, BindingFlags.Public, null, CallingConventions.Any, new Type[0], null, null };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { typeof(int) }, new ParameterModifier[0], typeof(ModuleTests).GetMethods()[0] };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Standard - 1, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() }, typeof(ModuleTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArrayCustom_TestData))]
        public void GetMethod_InvokeStringBindingFlagsBinderCallingConventionsTypeArrayParameterModifierArrayCustom_ReturnsExpected(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] modules, ParameterModifier[] modifiers, MethodInfo result)
        {
            var module = new CustomModule
            {
                GetMethodImplAction = (nameParam, bindingAttrParam, binderParam, callConventionParam, modulesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Equal(callConvention, callConventionParam);
                    Assert.Same(modules, modulesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, module.GetMethod(name, bindingAttr, binder, callConvention, modules, modifiers));
        }

        [Fact]
        public void GetMethod_NullName_ThrowsArgumentNullException()
        {
            var module = new SubModule();
            Assert.Throws<ArgumentNullException>("name", () => module.GetMethod(null));
            Assert.Throws<ArgumentNullException>("name", () => module.GetMethod(null, new Type[0]));
            Assert.Throws<ArgumentNullException>("name", () => module.GetMethod(null, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[0], new ParameterModifier[0]));
        }

        [Fact]
        public void GetMethod_NullTypes_ThrowsArgumentNullException()
        {
            var module = new SubModule();
            Assert.Throws<ArgumentNullException>("types", () => module.GetMethod("name", null));
            Assert.Throws<ArgumentNullException>("types", () => module.GetMethod("name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, null, new ParameterModifier[0]));
        }

        [Fact]
        public void GetMethod_NullTypeInTypes_ThrowsArgumentNullException()
        {
            var module = new SubModule();
            Assert.Throws<ArgumentNullException>("types", () => module.GetMethod("name", new Type[] { null }));
            Assert.Throws<ArgumentNullException>("types", () => module.GetMethod("name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { null }, new ParameterModifier[0]));
        }

        public static IEnumerable<object[]> GetMethodImpl_String_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { null, BindingFlags.Public, null, CallingConventions.Any, null, null };
            yield return new object[] { string.Empty, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[0], new ParameterModifier[0] };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Standard - 1, new Type[] { null }, new ParameterModifier[] { new ParameterModifier() } };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Standard - 1, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() } };
        }

        [Theory]
        [MemberData(nameof(GetMethodImpl_String_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArray_TestData))]
        public void GetMethodImpl_InvokeStringBindingFlagsBinderCallingConventionsTypeArrayParameterModifierArrayCustom_ThrowsNotImplementedException(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] modules, ParameterModifier[] modifiers)
        {
            var module = new ProtectedModule();
            Assert.Throws<NotImplementedException>(() => module.GetMethodImpl(name, bindingAttr, binder, callConvention, modules, modifiers));
        }

        [Fact]
        public void GetMethods_Invoke_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetMethods());
        }

        public static IEnumerable<object[]> GetMethodsCustom_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new MethodInfo[0] };
            yield return new object[] { new MethodInfo[] { typeof(List<int>).GetMethods()[0] } };
            yield return new object[] { new MethodInfo[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetMethodsCustom_TestData))]
        public void GetMethods_InvokeCustom_ReturnsExpected(MethodInfo[] result)
        {
            var module = new CustomModule
            {
                GetMethodsAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, module.GetMethods());
        }

        [Theory]
        [InlineData(BindingFlags.Public)]
        public void GetMethods_InvokeBindingFlags_ThrowsNotImplementedException(BindingFlags bindingFlags)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetMethods(bindingFlags));
        }

        public static IEnumerable<object[]> GetObjectData_TestData()
        {
            yield return new object[] { null, default(StreamingContext) };
            yield return new object[] { new SerializationInfo(typeof(int), new FormatterConverter()), default(StreamingContext) };
        }

        [Theory]
        [MemberData(nameof(GetObjectData_TestData))]
        public void GetObjectData_Invoke_ThrowsNotImplementedException(SerializationInfo info, StreamingContext context)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetObjectData(info, context));
        }

        [Fact]
        public void GetPEKind_Invoke_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void GetType_InvokeString_ThrowsNotImplementedException(string name)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetType(name));
        }

        public static IEnumerable<object[]> GetType_StringCustom_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, typeof(List<int>) };
            yield return new object[] { "Type", typeof(List<int>) };
        }

        [Theory]
        [MemberData(nameof(GetType_StringCustom_TestData))]
        public void GetType_InvokeStringCustom_ReturnsExpected(string name, Type result)
        {
            var module = new CustomModule
            {
                GetTypeAction = (nameParam, throwOnErrorParam, ignoreCaseParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.False(throwOnErrorParam);
                    Assert.False(ignoreCaseParam);
                    return result;
                }
            };
            Assert.Same(result, module.GetType(name));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("", false)]
        [InlineData("name", true)]
        [InlineData("name", false)]
        public void GetType_InvokeStringBool_ThrowsNotImplementedException(string name, bool ignoreCase)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetType(name, ignoreCase));
        }

        public static IEnumerable<object[]> GetType_String_BoolCustom_TestData()
        {
            yield return new object[] { null, true, null };
            yield return new object[] { null, false, null };
            yield return new object[] { string.Empty, true, typeof(List<int>) };
            yield return new object[] { string.Empty, false, typeof(List<int>) };
            yield return new object[] { "Type", true, typeof(List<int>) };
            yield return new object[] { "Type", false, typeof(List<int>) };
        }

        [Theory]
        [MemberData(nameof(GetType_String_BoolCustom_TestData))]
        public void GetType_InvokeStringBoolCustom_ReturnsExpected(string name, bool ignoreCase, Type result)
        {
            var module = new CustomModule
            {
                GetTypeAction = (nameParam, throwOnErrorParam, ignoreCaseParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.False(throwOnErrorParam);
                    Assert.Equal(ignoreCase, ignoreCaseParam);
                    return result;
                }
            };
            Assert.Same(result, module.GetType(name, ignoreCase));
        }

        [Theory]
        [InlineData(null, true, true)]
        [InlineData(null, true, false)]
        [InlineData(null, false, true)]
        [InlineData(null, false, false)]
        [InlineData("", true, true)]
        [InlineData("", true, false)]
        [InlineData("", false, true)]
        [InlineData("", false, false)]
        [InlineData("name", true, true)]
        [InlineData("name", true, false)]
        [InlineData("name", false, true)]
        [InlineData("name", false, false)]
        public void GetType_InvokeStringBoolBool_ThrowsNotImplementedException(string name, bool throwOnError, bool ignoreCase)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetType(name, throwOnError, ignoreCase));
        }

        public static IEnumerable<object[]> GetType_String_Bool_BoolCustom_TestData()
        {
            yield return new object[] { null, true, true, null };
            yield return new object[] { null, true, false, null };
            yield return new object[] { null, false, true, null };
            yield return new object[] { null, false, false, null };
            yield return new object[] { string.Empty, true, true, typeof(List<int>) };
            yield return new object[] { string.Empty, true, false, typeof(List<int>) };
            yield return new object[] { string.Empty, false, true, typeof(List<int>) };
            yield return new object[] { string.Empty, false, false, typeof(List<int>) };
            yield return new object[] { "Type", true, true, typeof(List<int>) };
            yield return new object[] { "Type", true, false, typeof(List<int>) };
            yield return new object[] { "Type", false, true, typeof(List<int>) };
            yield return new object[] { "Type", false, false, typeof(List<int>) };
        }

        [Theory]
        [MemberData(nameof(GetType_String_Bool_BoolCustom_TestData))]
        public void GetType_InvokeStringBoolBoolCustom_ReturnsExpected(string name, bool throwOnError, bool ignoreCase, Type result)
        {
            var module = new CustomModule
            {
                GetTypeAction = (nameParam, throwOnErrorParam, ignoreCaseParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(throwOnError, throwOnErrorParam);
                    Assert.Equal(ignoreCase, ignoreCaseParam);
                    return result;
                }
            };
            Assert.Same(result, module.GetType(name, throwOnError, ignoreCase));
        }

        [Fact]
        public void GetTypes_Invoke_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.GetTypes());
        }

        [Theory]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(int), false)]
        [InlineData(null, true)]
        [InlineData(null, false)]
        public void IsDefined_Invoke_ThrowsNotImplementedException(Type attributeType, bool inherit)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.IsDefined(attributeType, inherit));
        }

        [Fact]
        public void IsResource_Invoke_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.IsResource());
        }

        [Fact]
        public void ToString_Invoke_ThrowsNotImplementedException()
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Name")]
        public void ToString_InvokeCustom_ReturnsExpected(string scopeName)
        {
            var module = new CustomModule
            {
                ScopeNameAction = () => scopeName
            };
            Assert.Equal(scopeName, module.ToString());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void ResolveField_Invoke_ThrowsNotImplementedException(int metadataToken)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveField(metadataToken));
        }

        public static IEnumerable<object[]> ResolveFieldCustom_TestData()
        {
            yield return new object[] { -1, null };
            yield return new object[] { 0, typeof(ConsoleColor).GetFields()[0] };
            yield return new object[] { 1, typeof(ConsoleColor).GetFields()[0] };
        }

        [Theory]
        [MemberData(nameof(ResolveFieldCustom_TestData))]
        public void ResolveField_InvokeCustom_ReturnsExpected(int metadataToken, FieldInfo result)
        {
            var module = new CustomModule
            {
                ResolveFieldAction = (metadataTokenParam, genericTypeArgumentsParam, genericMethodArgumentsParam) =>
                {
                    Assert.Equal(metadataToken, metadataTokenParam);
                    Assert.Null(genericTypeArgumentsParam);
                    Assert.Null(genericMethodArgumentsParam);
                    return result;
                }
            };
            Assert.Same(result, module.ResolveField(metadataToken));
        }

        public static IEnumerable<object[]> ResolveField_Int_TypeArray_TypeArray_TestData()
        {
            yield return new object[] { -1, null, null };
            yield return new object[] { 0, new Type[0], new Type[0] };
            yield return new object[] { 1, new Type[] { null }, new Type[] { null } };
            yield return new object[] { 1, new Type[] { typeof(int) }, new Type[] { typeof(string) } };
        }

        [Theory]
        [MemberData(nameof(ResolveField_Int_TypeArray_TypeArray_TestData))]
        public void ResolveField_InvokeIntTypeArrayTypeArray_ThrowsNotImplementedException(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void ResolveMember_InvokeInt_ThrowsNotImplementedException(int metadataToken)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveMember(metadataToken));
        }

        public static IEnumerable<object[]> ResolveMemberIntCustom_TestData()
        {
            yield return new object[] { -1, null };
            yield return new object[] { 0, typeof(ConsoleColor).GetMembers()[0] };
            yield return new object[] { 1, typeof(ConsoleColor).GetMembers()[0] };
        }

        [Theory]
        [MemberData(nameof(ResolveMemberIntCustom_TestData))]
        public void ResolveMember_InvokeIntCstom_ReturnsExpected(int metadataToken, MemberInfo result)
        {
            var module = new CustomModule
            {
                ResolveMemberAction = (metadataTokenParam, genericTypeArgumentsParam, genericMethodArgumentsParam) =>
                {
                    Assert.Equal(metadataToken, metadataTokenParam);
                    Assert.Null(genericTypeArgumentsParam);
                    Assert.Null(genericMethodArgumentsParam);
                    return result;
                }
            };
            Assert.Same(result, module.ResolveMember(metadataToken));
        }

        public static IEnumerable<object[]> ResolveMember_Int_TypeArray_TypeArray_TestData()
        {
            yield return new object[] { -1, null, null };
            yield return new object[] { 0, new Type[0], new Type[0] };
            yield return new object[] { 1, new Type[] { null }, new Type[] { null } };
            yield return new object[] { 1, new Type[] { typeof(int) }, new Type[] { typeof(string) } };
        }

        [Theory]
        [MemberData(nameof(ResolveMember_Int_TypeArray_TypeArray_TestData))]
        public void ResolveMember_InvokeIntTypeArrayTypeArray_ThrowsNotImplementedException(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void ResolveMethod_InvokeInt_ThrowsNotImplementedException(int metadataToken)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveMethod(metadataToken));
        }

        public static IEnumerable<object[]> ResolveMethod_IntCustom_TestData()
        {
            yield return new object[] { -1, null };
            yield return new object[] { 0, typeof(ModuleTests).GetMethods()[0] };
            yield return new object[] { 1, typeof(ModuleTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(ResolveMethod_IntCustom_TestData))]
        public void ResolveMethod_InvokeIntCustom_ReturnsExpected(int metadataToken, MethodBase result)
        {
            var module = new CustomModule
            {
                ResolveMethodAction = (metadataTokenParam, genericTypeArgumentsParam, genericMethodArgumentsParam) =>
                {
                    Assert.Equal(metadataToken, metadataTokenParam);
                    Assert.Null(genericTypeArgumentsParam);
                    Assert.Null(genericMethodArgumentsParam);
                    return result;
                }
            };
            Assert.Same(result, module.ResolveMethod(metadataToken));
        }

        public static IEnumerable<object[]> ResolveMethod_Int_TypeArray_TypeArray_TestData()
        {
            yield return new object[] { -1, null, null };
            yield return new object[] { 0, new Type[0], new Type[0] };
            yield return new object[] { 1, new Type[] { null }, new Type[] { null } };
            yield return new object[] { 1, new Type[] { typeof(int) }, new Type[] { typeof(string) } };
        }

        [Theory]
        [MemberData(nameof(ResolveMethod_Int_TypeArray_TypeArray_TestData))]
        public void ResolveMethod_InvokeIntTypeArrayTypeArray_ThrowsNotImplementedException(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void ResolveSignature_InvokeInt_ThrowsNotImplementedException(int metadataToken)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveSignature(metadataToken));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void ResolveString_InvokeInt_ThrowsNotImplementedException(int metadataToken)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveString(metadataToken));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void ResolveType_InvokeInt_ThrowsNotImplementedException(int metadataToken)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveType(metadataToken));
        }

        public static IEnumerable<object[]> ResolveType_IntCustom_TestData()
        {
            yield return new object[] { -1, null };
            yield return new object[] { 0, typeof(int) };
            yield return new object[] { 1, typeof(int) };
        }

        [Theory]
        [MemberData(nameof(ResolveType_IntCustom_TestData))]
        public void ResolveType_InvokeIntCustom_ReturnsExpected(int metadataToken, Type result)
        {
            var module = new CustomModule
            {
                ResolveTypeAction = (metadataTokenParam, genericTypeArgumentsParam, genericMethodArgumentsParam) =>
                {
                    Assert.Equal(metadataToken, metadataTokenParam);
                    Assert.Null(genericTypeArgumentsParam);
                    Assert.Null(genericMethodArgumentsParam);
                    return result;
                }
            };
            Assert.Same(result, module.ResolveType(metadataToken));
        }

        public static IEnumerable<object[]> ResolveType_Int_TypeArray_TypeArray_TestData()
        {
            yield return new object[] { -1, null, null };
            yield return new object[] { 0, new Type[0], new Type[0] };
            yield return new object[] { 1, new Type[] { null }, new Type[] { null } };
            yield return new object[] { 1, new Type[] { typeof(int) }, new Type[] { typeof(string) } };
        }

        [Theory]
        [MemberData(nameof(ResolveType_Int_TypeArray_TypeArray_TestData))]
        public void ResolveType_InvokeIntTypeArrayTypeArray_ThrowsNotImplementedException(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArgumentsParam)
        {
            var module = new SubModule();
            Assert.Throws<NotImplementedException>(() => module.ResolveType(metadataToken, genericTypeArguments, genericMethodArgumentsParam));
        }

        private class AlwaysEqualsModule : SubModule
        {
            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }

        private class ProtectedModule : Module
        {
            public new MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
                => base.GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        private class SubCustomAttributeData : CustomAttributeData
        {
        }

        private class CustomModule : SubModule
        {
            public Func<string> ScopeNameAction { get; set; }

            public override string ScopeName
            {
                get
                {
                    if (ScopeNameAction == null)
                    {
                        return base.ScopeName;
                    }

                    return ScopeNameAction();
                }
            }

            public Func<Type, bool, object[]> GetCustomAttributesAction { get; set; }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                if (GetCustomAttributesAction == null)
                {
                    return base.GetCustomAttributes(attributeType, inherit);
                }

                return GetCustomAttributesAction(attributeType, inherit);
            }

            public Func<IList<CustomAttributeData>> GetCustomAttributesDataAction { get; set; }

            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                if (GetCustomAttributesDataAction == null)
                {
                    return base.GetCustomAttributesData();
                }

                return GetCustomAttributesDataAction();
            }

            public Func<string, BindingFlags, FieldInfo> GetFieldAction { get; set; }

            public override FieldInfo GetField(string name, BindingFlags bindingAttr)
            {
                if (GetFieldAction == null)
                {
                    return base.GetField(name, bindingAttr);
                }

                return GetFieldAction(name, bindingAttr);
            }

            public Func<BindingFlags, FieldInfo[]> GetFieldsAction { get; set; }

            public override FieldInfo[] GetFields(BindingFlags bindingAttr)
            {
                if (GetFieldsAction == null)
                {
                    return base.GetFields(bindingAttr);
                }

                return GetFieldsAction(bindingAttr);
            }

            public Func<string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[], MethodInfo> GetMethodImplAction { get; set; }

            protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
            {
                if (GetMethodImplAction == null)
                {
                    return base.GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
                }

                return GetMethodImplAction(name, bindingAttr, binder, callConvention, types, modifiers);
            }

            public Func<BindingFlags, MethodInfo[]> GetMethodsAction { get; set; }

            public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
            {
                if (GetMethodsAction == null)
                {
                    return base.GetMethods(bindingAttr);
                }

                return GetMethodsAction(bindingAttr);
            }

            public Func<string, bool, bool, Type> GetTypeAction { get; set; }

            public override Type GetType(string name, bool throwOnError, bool ignoreCase)
            {
                if (GetTypeAction == null)
                {
                    return base.GetType(name, throwOnError, ignoreCase);
                }

                return GetTypeAction(name, throwOnError, ignoreCase);
            }

            public Func<Type[]> GetTypesAction { get; set; }

            public override Type[] GetTypes()
            {
                if (GetTypesAction == null)
                {
                    return base.GetTypes();
                }

                return GetTypesAction();
            }

            public Func<int, Type[], Type[], FieldInfo> ResolveFieldAction { get; set; }

            public override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                if (ResolveFieldAction == null)
                {
                    return base.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
                }

                return ResolveFieldAction(metadataToken, genericTypeArguments, genericMethodArguments);
            }

            public Func<int, Type[], Type[], MemberInfo> ResolveMemberAction { get; set; }

            public override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                if (ResolveMemberAction == null)
                {
                    return base.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
                }

                return ResolveMemberAction(metadataToken, genericTypeArguments, genericMethodArguments);
            }

            public Func<int, Type[], Type[], MethodBase> ResolveMethodAction { get; set; }

            public override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                if (ResolveMethodAction == null)
                {
                    return base.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
                }

                return ResolveMethodAction(metadataToken, genericTypeArguments, genericMethodArguments);
            }

            public Func<int, Type[], Type[], Type> ResolveTypeAction { get; set; }

            public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                if (ResolveTypeAction == null)
                {
                    return base.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
                }

                return ResolveTypeAction(metadataToken, genericTypeArguments, genericMethodArguments);
            }
        }

        private class SubModule : Module
        {
        }
    }
}
