// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Tests;
using Xunit;

[module: Attr(77, name = "AttrSimple")]
[module: Int32Attr(77, name = "Int32AttrSimple")]
[module: Int64Attr(77, name = "Int64AttrSimple")]
[module: StringAttr("hello", name = "StringAttrSimple")]
[module: EnumAttr(PublicEnum.Case1, name = "EnumAttrSimple")]

namespace System.Reflection.Tests
{
    public class ModuleTest
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(List<>))]
        [InlineData(typeof(ModuleTest))]
        public void Assembly(TypeInfo typeInfo)
        {
            Module module = typeInfo.Module;
            Assert.Equal(typeInfo.Assembly, module.Assembly);
        }

        [Theory]
        [InlineData(typeof(Attr), 77, "AttrSimple")]
        [InlineData(typeof(Int32Attr), 77, "Int32AttrSimple")]
        [InlineData(typeof(Int64Attr), (long)77, "Int64AttrSimple")]
        [InlineData(typeof(StringAttr), "hello", "StringAttrSimple")]
        [InlineData(typeof(EnumAttr), PublicEnum.Case1, "EnumAttrSimple")]  
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Custom Attributes on Modules not supported on UapAot.")]
        public void CustomAttributes<CtorArg, NamedArg>(Type attrType, CtorArg expectedCtorValue, NamedArg expectedNamedValue)
        {
            Module module = typeof(ModuleTest).GetTypeInfo().Module;

            CustomAttributeData attribute = module.CustomAttributes.Single(a => a.AttributeType.Equals(attrType));
            Assert.Equal(1, attribute.ConstructorArguments.Count);
            Assert.Equal(1, attribute.NamedArguments.Count);
                        
            Assert.Equal(typeof(CtorArg), attribute.ConstructorArguments[0].ArgumentType);

            object actualCtorValue = attribute.ConstructorArguments[0].Value;
            if (typeof(CtorArg).GetTypeInfo().IsEnum)
            {
                actualCtorValue = Enum.ToObject(typeof(CtorArg), attribute.ConstructorArguments[0].Value);
            }

            Assert.Equal(expectedCtorValue, actualCtorValue);

            Assert.Equal("name", attribute.NamedArguments[0].MemberName);
            Assert.True(attribute.NamedArguments[0].IsField);

            Assert.Equal(typeof(NamedArg), attribute.NamedArguments[0].TypedValue.ArgumentType);
            Assert.Equal(expectedNamedValue, attribute.NamedArguments[0].TypedValue.Value);
        }

        [Theory]        
        [InlineData("System.Nullable`1[System.Int32]", typeof(int?))]
        [InlineData("System.Int32*", typeof(int*))]
        [InlineData("System.Int32**", typeof(int**))]
        [InlineData("Outside`1", typeof(Outside<>))]
        [InlineData("Outside`1+Inside`1", typeof(Outside<>.Inside<>))]
        [InlineData("Outside[]", typeof(Outside[]))]
        [InlineData("Outside[,,]", typeof(Outside[,,]))]
        [InlineData("Outside[][]", typeof(Outside[][]))]        
        public void GetType(string className, Type expectedType)
        {
            Module module = expectedType.GetTypeInfo().Module;

            Assert.Equal(expectedType, module.GetType(className, true, false));
            Assert.Equal(expectedType, module.GetType(className.ToLower(), false, true));
            
            Assert.Null(module.GetType(className.ToLower(), false, false));
            Assert.Throws<TypeLoadException>(() => module.GetType(className.ToLower(), true, false));
        }

        [Fact]
        public void FilterTypeName_DelegateFiltersExpectedTypes()
        {
            Assert.NotNull(Module.FilterTypeName);
            Assert.Same(Module.FilterTypeName, Module.FilterTypeName);
            Assert.NotSame(Module.FilterTypeName, Module.FilterTypeNameIgnoreCase);

            Assert.Throws<InvalidFilterCriteriaException>(() => Module.FilterTypeName(GetType(), null));
            Assert.Throws<InvalidFilterCriteriaException>(() => Module.FilterTypeName(GetType(), new object()));

            Assert.Empty(typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeName, "out*"));
            Assert.Equal(2, typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeName, "Out*").Length);
            Assert.Empty(typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeName, "outside"));
            Assert.Equal(1, typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeName, "Outside").Length);
            Assert.Equal(1, typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeName, "Inside").Length);

            Assert.True(Module.FilterTypeName(typeof(string), "String"));
            Assert.True(Module.FilterTypeName(typeof(string), "*"));
            Assert.True(Module.FilterTypeName(typeof(string), "S*"));
            Assert.True(Module.FilterTypeName(typeof(string), "Strin*"));
            Assert.True(Module.FilterTypeName(typeof(string), "String"));
            Assert.True(Module.FilterTypeName(typeof(string), "String*"));

            Assert.False(Module.FilterTypeName(typeof(string), ""));
            Assert.False(Module.FilterTypeName(typeof(string), "S"));
            Assert.False(Module.FilterTypeName(typeof(string), " String"));
            Assert.False(Module.FilterTypeName(typeof(string), "String "));
            Assert.False(Module.FilterTypeName(typeof(string), "Strings"));
        }

        [Fact]
        public void FilterTypeNameIgnoreCase_DelegateFiltersExpectedTypes()
        {
            Assert.NotNull(Module.FilterTypeNameIgnoreCase);
            Assert.Same(Module.FilterTypeNameIgnoreCase, Module.FilterTypeNameIgnoreCase);
            Assert.NotSame(Module.FilterTypeNameIgnoreCase, Module.FilterTypeName);

            Assert.Throws<InvalidFilterCriteriaException>(() => Module.FilterTypeName(GetType(), null));
            Assert.Throws<InvalidFilterCriteriaException>(() => Module.FilterTypeName(GetType(), new object()));

            Assert.Equal(2, typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeNameIgnoreCase, "out*").Length);
            Assert.Equal(2, typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeNameIgnoreCase, "Out*").Length);
            Assert.Equal(1, typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeNameIgnoreCase, "oUtside").Length);
            Assert.Equal(1, typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeNameIgnoreCase, "Outside").Length);
            Assert.Equal(1, typeof(ModuleTest).GetTypeInfo().Module.FindTypes(Module.FilterTypeNameIgnoreCase, "inSIDE").Length);

            Assert.True(Module.FilterTypeNameIgnoreCase(typeof(string), "string"));
            Assert.True(Module.FilterTypeNameIgnoreCase(typeof(string), "*"));
            Assert.True(Module.FilterTypeNameIgnoreCase(typeof(string), "s*"));
            Assert.True(Module.FilterTypeNameIgnoreCase(typeof(string), "stRIn*"));
            Assert.True(Module.FilterTypeNameIgnoreCase(typeof(string), "sTrInG"));
            Assert.True(Module.FilterTypeNameIgnoreCase(typeof(string), "STRING*"));

            Assert.False(Module.FilterTypeNameIgnoreCase(typeof(string), ""));
            Assert.False(Module.FilterTypeNameIgnoreCase(typeof(string), "s"));
            Assert.False(Module.FilterTypeNameIgnoreCase(typeof(string), " string"));
            Assert.False(Module.FilterTypeNameIgnoreCase(typeof(string), "string "));
            Assert.False(Module.FilterTypeNameIgnoreCase(typeof(string), "strings"));
        }
    }
}

public class Outside
{
    public class Inside { }
}

public class Outside<T>
{
    public class Inside<U> { }
}
