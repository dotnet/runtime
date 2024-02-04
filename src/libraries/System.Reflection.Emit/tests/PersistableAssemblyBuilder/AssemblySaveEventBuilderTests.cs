// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveEventBuilderTests
    {
        [Fact]
        public void DefineEventAndItsAccessors()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                EventBuilder eventType = type.DefineEvent("TestEvent", EventAttributes.SpecialName, typeof(int));
                MethodBuilder addMethod = type.DefineMethod("AddMethod", MethodAttributes.Public | MethodAttributes.SpecialName);
                MethodBuilder addMethod2 = type.DefineMethod("AddMethod2", MethodAttributes.Public | MethodAttributes.HideBySig, typeof(int), Type.EmptyTypes);
                MethodBuilder raiseMethod = type.DefineMethod("RaiseMethod", MethodAttributes.Assembly | MethodAttributes.SpecialName, typeof(int), Type.EmptyTypes);
                MethodBuilder removeMethod = type.DefineMethod("RemoveMethod", MethodAttributes.Public, typeof(void), Type.EmptyTypes);
                MethodBuilder otherMethod = type.DefineMethod("OtherMethod", MethodAttributes.Family, typeof(int), [typeof(int)]);
                CustomAttributeBuilder customAttrBuilder = new CustomAttributeBuilder(typeof(IntPropertyAttribute).GetConstructor([typeof(int)]), [9]);
                eventType.SetCustomAttribute(customAttrBuilder);
                addMethod.GetILGenerator().Emit(OpCodes.Ret);
                ILGenerator adderIL = addMethod2.GetILGenerator();
                adderIL.Emit(OpCodes.Ldc_I4_1);
                adderIL.Emit(OpCodes.Ret);
                eventType.SetAddOnMethod(addMethod);
                eventType.SetAddOnMethod(addMethod2); // last set wins
                ILGenerator raiseIL = raiseMethod.GetILGenerator();
                raiseIL.Emit(OpCodes.Ldc_I4_2);
                raiseIL.Emit(OpCodes.Ret);
                eventType.SetRaiseMethod(raiseMethod);
                removeMethod.GetILGenerator().Emit(OpCodes.Ret);
                eventType.SetRemoveOnMethod(removeMethod);
                ILGenerator otherILGenerator = otherMethod.GetILGenerator();
                otherILGenerator.Emit(OpCodes.Ldarg_1);
                otherILGenerator.Emit(OpCodes.Ret);
                eventType.AddOtherMethod(otherMethod);
                type.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    EventInfo eventFromDisk = typeFromDisk.GetEvent("TestEvent");
                    Assert.Equal(addMethod2.Name, eventFromDisk.AddMethod.Name);
                    Assert.Equal(raiseMethod.Name, eventFromDisk.RaiseMethod.Name);
                    Assert.Equal(removeMethod.Name, eventFromDisk.RemoveMethod.Name);
                    Assert.Equal(typeof(int).FullName, eventFromDisk.EventHandlerType.FullName);
                    Assert.NotNull(typeFromDisk.GetMethod("OtherMethod", BindingFlags.NonPublic | BindingFlags.Instance));
                    Assert.Equal(EventAttributes.SpecialName, eventFromDisk.Attributes);
                    IList<CustomAttributeData> caData = eventFromDisk.GetCustomAttributesData();
                    Assert.Equal(1, caData.Count);
                    Assert.Equal(typeof(IntPropertyAttribute).FullName, caData[0].AttributeType.FullName);
                    Assert.Equal(1, caData[0].ConstructorArguments.Count);
                    Assert.Equal(9, caData[0].ConstructorArguments[0].Value);
                }
            }
        }

        [Fact]
        public void Set_NullValue_ThrowsArgumentNullException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(string));

            AssertExtensions.Throws<ArgumentNullException>("eventtype", () => type.DefineEvent("EventTypeNull", EventAttributes.None, null));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => eventBuilder.SetRaiseMethod(null));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => eventBuilder.SetRemoveOnMethod(null));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => eventBuilder.SetAddOnMethod(null));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => eventBuilder.AddOtherMethod(null));
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => eventBuilder.SetCustomAttribute(null));
        }

        [Fact]
        public void Set_WhenTypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(int));

            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.SpecialName, typeof(int), null);
            method.GetILGenerator().Emit(OpCodes.Ret);
            CustomAttributeBuilder customAttrBuilder = new CustomAttributeBuilder(typeof(IntPropertyAttribute).GetConstructor([typeof(int)]), [10]);
            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => eventBuilder.SetAddOnMethod(method));
            Assert.Throws<InvalidOperationException>(() => eventBuilder.SetRaiseMethod(method));
            Assert.Throws<InvalidOperationException>(() => eventBuilder.SetRemoveOnMethod(method));
            Assert.Throws<InvalidOperationException>(() => eventBuilder.AddOtherMethod(method));
            Assert.Throws<InvalidOperationException>(() => eventBuilder.SetCustomAttribute(customAttrBuilder));
        }
    }
}
