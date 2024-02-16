// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

        [Fact]
        public void ReferenceEventInIL()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
                TypeBuilder delegateType = ab.GetDynamicModule("MyModule").DefineType("OnMissingString", TypeAttributes.Public | TypeAttributes.Sealed, typeof(MulticastDelegate));
                delegateType.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    typeof(void), [typeof(string)]).SetImplementationFlags(MethodImplAttributes.Runtime);
                delegateType.DefineMethod("BeginInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    typeof(IAsyncResult), [typeof(string), typeof(AsyncCallback), typeof(object)]).SetImplementationFlags(MethodImplAttributes.Runtime);
                delegateType.DefineMethod("EndInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    typeof(void), [typeof(IAsyncResult)]).SetImplementationFlags(MethodImplAttributes.Runtime);
                delegateType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(object), typeof(IntPtr)]).
                    SetImplementationFlags(MethodImplAttributes.Runtime);
                MethodInfo combineMethod = typeof(Delegate).GetMethod("Combine", [typeof(Delegate), typeof(Delegate)]);
                MethodInfo interlockedGenericMethod = typeof(Interlocked).GetMethods(BindingFlags.Public | BindingFlags.Static).
                    Where(m => m.Name == "CompareExchange" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1).First().MakeGenericMethod(delegateType);
                EventBuilder eventBuilder = type.DefineEvent("MissingString", EventAttributes.SpecialName, delegateType);
                FieldBuilder field = type.DefineField("MissingString", delegateType, FieldAttributes.Private);
                MethodBuilder addMethod = type.DefineMethod("add_MissingString", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, [delegateType]);
                ILGenerator addIL = addMethod.GetILGenerator();
                addIL.DeclareLocal(delegateType);
                addIL.DeclareLocal(delegateType);
                addIL.DeclareLocal(delegateType);
                Label loop = addIL.DefineLabel();
                addIL.Emit(OpCodes.Ldarg_0);
                addIL.Emit(OpCodes.Ldfld, field);
                addIL.Emit(OpCodes.Stloc_0);
                addIL.MarkLabel(loop);
                addIL.Emit(OpCodes.Ldloc_0);
                addIL.Emit(OpCodes.Stloc_1);
                addIL.Emit(OpCodes.Ldloc_1);
                addIL.Emit(OpCodes.Ldarg_1);
                addIL.Emit(OpCodes.Call, combineMethod);
                addIL.Emit(OpCodes.Castclass, delegateType);
                addIL.Emit(OpCodes.Stloc_2);
                addIL.Emit(OpCodes.Ldarg_0);
                addIL.Emit(OpCodes.Ldflda, field);
                addIL.Emit(OpCodes.Ldloc_2);
                addIL.Emit(OpCodes.Ldloc_1);
                addIL.Emit(OpCodes.Call, interlockedGenericMethod);
                addIL.Emit(OpCodes.Stloc_0);
                addIL.Emit(OpCodes.Ldloc_0);
                addIL.Emit(OpCodes.Ldloc_1);
                addIL.Emit(OpCodes.Bne_Un_S, loop);
                addIL.Emit(OpCodes.Ret);
                eventBuilder.SetAddOnMethod(addMethod);

                delegateType.CreateType();
                type.CreateType();
                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                    EventInfo eventFromDisk = typeFromDisk.GetEvent("MissingString");
                    Assert.Equal(addMethod.Name, eventFromDisk.AddMethod.Name);
                    Assert.Equal(delegateType.FullName, eventFromDisk.EventHandlerType.FullName);                }
            }
        }
    }
}
