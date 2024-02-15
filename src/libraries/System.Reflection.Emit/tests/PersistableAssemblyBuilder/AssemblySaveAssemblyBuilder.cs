// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveAssemblyBuilder
    {
        public class Outer
        {
            public class Inner
            {
                public class InnerMostType
                {
                    void DoNothing () { }
                }
            }
        }

        [Fact]
        public void AssemblyWithDifferentTypes()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyName aName = new AssemblyName("h");
                aName.Version = new Version(1, 2, 3, 4);
                aName.CultureInfo = new CultureInfo("en");
                aName.Flags = AssemblyNameFlags.Retargetable;

                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(aName);

                ab.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyDelaySignAttribute).GetConstructor([typeof(bool)]), [true]));

                var cattrb = new CustomAttributeBuilder(typeof(AttributeUsageAttribute).GetConstructor([typeof(AttributeTargets)]), [AttributeTargets.Class],
                                                         [typeof(AttributeUsageAttribute).GetProperty("AllowMultiple")],
                                                         [true], [], []);
                ab.SetCustomAttribute(cattrb);

                var module = ab.DefineDynamicModule("h.dll");
                module.SetCustomAttribute(cattrb);

                TypeBuilder iface1 = module.DefineType("IFace1", TypeAttributes.Public | TypeAttributes.Interface, typeof(object));
                iface1.CreateType();

                // Interfaces, attributes, class size, packing size
                TypeBuilder tb1 = module.DefineType("Type1", TypeAttributes.Public | TypeAttributes.SequentialLayout, typeof(object), PackingSize.Size2, 16);
                tb1.AddInterfaceImplementation(iface1);
                tb1.AddInterfaceImplementation(typeof(IComparable));
                tb1.DefineMethod("CompareTo", MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis,
                    typeof(int), [typeof(object)]).GetILGenerator().Emit(OpCodes.Ret);
                tb1.SetCustomAttribute(cattrb);
                tb1.CreateType();

                // Nested type
                TypeBuilder tbNested = tb1.DefineNestedType("TypeNested", TypeAttributes.NestedPublic, typeof(object));
                tbNested.CreateType();

                // Generics
                TypeBuilder tbg = module.DefineType("GType1", TypeAttributes.Public, typeof(object));
                var gParams = tbg.DefineGenericParameters("K", "T");
                // Constraints
                gParams[0].SetBaseTypeConstraint(typeof(object));
                gParams[0].SetInterfaceConstraints([typeof(IComparable)]);
                gParams[0].SetCustomAttribute(cattrb);
                gParams[1].SetBaseTypeConstraint(tbg);
                // Type param
                tbg.DefineField("FieldGParam", tbg.GetGenericArguments()[0], FieldAttributes.Public | FieldAttributes.Static);
                // Open type
                tbg.DefineField("FieldListOfT", typeof(List<>).MakeGenericType([tbg.GetGenericArguments()[1]]), FieldAttributes.Public | FieldAttributes.Static);
                tbg.CreateType();

                TypeBuilder tbg2 = module.DefineType("GType2", TypeAttributes.Public, typeof(object));
                tbg2.DefineGenericParameters("K", "T");
                tbg2.CreateType();

                TypeBuilder tb3 = module.DefineType("Type3", TypeAttributes.Public, typeof(object));
                // Nested type
                tb3.DefineField("FieldNested", tbNested, FieldAttributes.Public | FieldAttributes.Static);
                // Nested type ref
                tb3.DefineField("FieldNestedRef", typeof(TimeZoneInfo.AdjustmentRule), FieldAttributes.Public | FieldAttributes.Static);
                // Double Nested type ref
                tb3.DefineField("FieldDoubleNestedRef", typeof(Outer.Inner.InnerMostType), FieldAttributes.Public | FieldAttributes.Static);
                // Primitive types
                tb3.DefineField("FieldInt", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
                // Typeref array
                tb3.DefineField("FieldArrayTyperef", typeof(object[]), FieldAttributes.Public | FieldAttributes.Static);
                // Type szarray
                tb3.DefineField("FieldSzArray", tb1.MakeArrayType(), FieldAttributes.Public | FieldAttributes.Static);
                // Multi-dim non szarray
                tb3.DefineField("FieldNonSzArray", Array.CreateInstance(typeof(int), [10], [1]).GetType(), FieldAttributes.Public | FieldAttributes.Static);
                // Multi-dim array
                tb3.DefineField("FieldMultiDimArray", Array.CreateInstance(typeof(int), [10, 10], [1, 1]).GetType(), FieldAttributes.Public | FieldAttributes.Static);
                // Type pointer
                tb3.DefineField("FieldPointer", tb1.MakePointerType(), FieldAttributes.Public | FieldAttributes.Static);
                // Generic instance
                tb3.DefineField("FieldGListOfInt", typeof(List<int>), FieldAttributes.Public | FieldAttributes.Static);
                // Generic instance of tbuilder
                tb3.DefineField("FieldGInstTBuilder", tbg2.MakeGenericType([typeof(int), typeof(string)]), FieldAttributes.Public | FieldAttributes.Static);
                tb3.CreateType();

                // Fields
                TypeBuilder tbFields = module.DefineType("Type4", TypeAttributes.Public, typeof(object));
                // Field with a constant
                tbFields.DefineField("FieldInt", typeof(int), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.HasDefault | FieldAttributes.Literal).SetConstant(42);
                // Field with an offset
                tbFields.DefineField("FieldOffset", typeof(int), FieldAttributes.Public | FieldAttributes.Static).SetOffset(64);
                // Modreq/modopt
                tbFields.DefineField("FieldModopt", typeof(int), [typeof(int)], [typeof(uint)], FieldAttributes.Public | FieldAttributes.Static);
                // Marshal
                var fb = tbFields.DefineField("FieldMarshal1", typeof(int), FieldAttributes.Public);
                fb.SetCustomAttribute(new CustomAttributeBuilder(typeof(MarshalAsAttribute).GetConstructor([typeof(UnmanagedType)]), [UnmanagedType.U4]));
                fb = tbFields.DefineField("FieldMarshalByvalArray", typeof(int), FieldAttributes.Public);
                fb.SetCustomAttribute(new CustomAttributeBuilder(typeof(MarshalAsAttribute).GetConstructor([typeof(UnmanagedType)]), [UnmanagedType.ByValArray],
                                                                   new FieldInfo[] { typeof(MarshalAsAttribute).GetField("SizeConst") }, [16]));
                fb = tbFields.DefineField("FieldMarshalByvalTStr", typeof(int), FieldAttributes.Public);
                fb.SetCustomAttribute(new CustomAttributeBuilder(typeof(MarshalAsAttribute).GetConstructor([typeof(UnmanagedType)]), [UnmanagedType.ByValTStr],
                                                                   new FieldInfo[] { typeof(MarshalAsAttribute).GetField("SizeConst") }, [16]));

		        fb = tbFields.DefineField ("FieldMarshalCustom", typeof (int), FieldAttributes.Public);
		        fb.SetCustomAttribute (new CustomAttributeBuilder (typeof (MarshalAsAttribute).GetConstructor ([typeof (UnmanagedType)]), [UnmanagedType.CustomMarshaler],
														           new FieldInfo[] { typeof (MarshalAsAttribute).GetField ("MarshalTypeRef"),
																			         typeof (MarshalAsAttribute).GetField ("MarshalCookie") },
														           [typeof (object), "Cookie"]));

                // Cattr
                fb = tbFields.DefineField("FieldCAttr", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
                fb.SetCustomAttribute(cattrb);
                tbFields.CreateType();

                // Data
                module.DefineUninitializedData("Data1", 16, FieldAttributes.Public);
                module.DefineInitializedData("Data2", new byte[] { 1, 2, 3, 4, 5, 6 }, FieldAttributes.Public);

                // Methods and signatures
                TypeBuilder tb5 = module.DefineType("TypeMethods", TypeAttributes.Public, typeof(object));
                // .ctor
                var cmodsReq1 = new Type[] { typeof(object) };
                var cmodsOpt1 = new Type[] { typeof(int) };
                var ctorb = tb5.DefineConstructor(MethodAttributes.Public | MethodAttributes.RTSpecialName, CallingConventions.HasThis, [typeof(int), typeof(object)], [cmodsReq1, null], [cmodsOpt1, null]);
                ctorb.SetImplementationFlags(MethodImplAttributes.NoInlining);
                ctorb.GetILGenerator().Emit(OpCodes.Ret);
                // Parameters
                var paramb = ctorb.DefineParameter(1, ParameterAttributes.None, "param1");
                paramb.SetConstant(16);
                paramb.SetCustomAttribute(cattrb);
                paramb = ctorb.DefineParameter(2, ParameterAttributes.Out, "param2");
                paramb.SetCustomAttribute (new CustomAttributeBuilder (typeof (MarshalAsAttribute).GetConstructor ([typeof (UnmanagedType)]), [UnmanagedType.U4]));
                // .cctor
                var ctorb2 = tb5.DefineConstructor(MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.RTSpecialName, CallingConventions.Standard, [typeof(int), typeof(object)]);
                ctorb2.GetILGenerator().Emit(OpCodes.Ret);
                // method
                var mb = tb5.DefineMethod("Method1", MethodAttributes.Public, CallingConventions.Standard, typeof(int), cmodsReq1, cmodsOpt1, [typeof(int), typeof(object)], [cmodsReq1, null], [cmodsOpt1, null]);
                mb.SetImplementationFlags(MethodImplAttributes.NoInlining);
                mb.GetILGenerator().Emit(OpCodes.Ldc_I4_0);
                mb.GetILGenerator().Emit(OpCodes.Ret);
                gParams = mb.DefineGenericParameters("K", "T");
                // Constraints
                gParams[0].SetBaseTypeConstraint(null);
                gParams[0].SetInterfaceConstraints([typeof(IComparable)]);
                paramb = mb.DefineParameter(1, ParameterAttributes.None, "param1");
                paramb.SetConstant(16);
                paramb = mb.DefineParameter(2, ParameterAttributes.Out, "param2");
                paramb.SetCustomAttribute (new CustomAttributeBuilder (typeof (MarshalAsAttribute).GetConstructor ([typeof (UnmanagedType)]), [UnmanagedType.U4]));
                // return value
                paramb = mb.DefineParameter(0, ParameterAttributes.None, "ret");
                paramb.SetCustomAttribute (new CustomAttributeBuilder (typeof (MarshalAsAttribute).GetConstructor ([typeof (UnmanagedType)]), [UnmanagedType.U4]));
                paramb.SetCustomAttribute(cattrb);
                // override method
                tb5.AddInterfaceImplementation(typeof(IComparable));
                mb = tb5.DefineMethod("MethodOverride", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard | CallingConventions.HasThis, typeof(int), [typeof(object)]);
                mb.GetILGenerator().Emit(OpCodes.Ldc_I4_1);
                mb.GetILGenerator().Emit(OpCodes.Ret);
                tb5.DefineMethodOverride(mb, typeof(IComparable).GetMethod("CompareTo"));
                tb5.CreateType();

                // Properties
                TypeBuilder tb_properties = module.DefineType("TypeProperties", TypeAttributes.Public, typeof(object));
                var mbGet = tb_properties.DefineMethod("GetMethod1", MethodAttributes.Public, CallingConventions.Standard, typeof(int), Type.EmptyTypes);
                mbGet.GetILGenerator().Emit(OpCodes.Ldc_I4_1);
                mbGet.GetILGenerator().Emit(OpCodes.Ret);
                var mbSet = tb_properties.DefineMethod("SetMethod1", MethodAttributes.Public, CallingConventions.Standard, typeof(void), [typeof(int)]);
                mbSet.GetILGenerator().Emit(OpCodes.Ret);
                var mbOther = tb_properties.DefineMethod("OtherMethod1", MethodAttributes.Public, CallingConventions.Standard, typeof(int), Type.EmptyTypes);
                mbOther.GetILGenerator().Emit(OpCodes.Ldc_I4_1);
                mbOther.GetILGenerator().Emit(OpCodes.Ret);
                var propertyb = tb_properties.DefineProperty("AProperty", PropertyAttributes.HasDefault, typeof(int), [typeof(object)]);
                propertyb.SetCustomAttribute(cattrb);
                propertyb.SetConstant(1);
                propertyb.SetGetMethod(mbGet);
                propertyb.SetSetMethod(mbSet);
                propertyb.AddOtherMethod(mbOther);
                tb_properties.CreateType();

                // Events
                TypeBuilder tbEvents = module.DefineType("TypeEvents", TypeAttributes.Public, typeof(object));
                var mbAdd = tbEvents.DefineMethod("AddMethod1", MethodAttributes.Public, CallingConventions.Standard, typeof(int), Type.EmptyTypes);
                mbAdd.GetILGenerator().Emit(OpCodes.Ret);
                var mbRaise = tbEvents.DefineMethod("RaiseMethod1", MethodAttributes.Public, CallingConventions.Standard, typeof(int), Type.EmptyTypes);
                mbRaise.GetILGenerator().Emit(OpCodes.Ret);
                var mbRemove = tbEvents.DefineMethod("RemoveMethod1", MethodAttributes.Public, CallingConventions.Standard, typeof(int), Type.EmptyTypes);
                mbRemove.GetILGenerator().Emit(OpCodes.Ret);
                var eventb = tbEvents.DefineEvent("Event1", EventAttributes.SpecialName, typeof(int));
                eventb.SetCustomAttribute(cattrb);
                eventb.SetAddOnMethod(mbAdd);
                eventb.SetRaiseMethod(mbRaise);
                eventb.SetRemoveOnMethod(mbRemove);
                tbEvents.CreateType();

                ab.Save(file.Path);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    CheckAssembly(mlc.LoadFromAssemblyPath(file.Path));
                }
            }
        }

        void CheckCattr(IList<CustomAttributeData> attributes)
        {
            CustomAttributeData cattr = attributes.First(a => a.AttributeType.Name == nameof(AttributeUsageAttribute));
            Assert.Equal(1, cattr.ConstructorArguments.Count);
            Assert.Equal(AttributeTargets.Class, (AttributeTargets)cattr.ConstructorArguments[0].Value);
            Assert.Equal(1, cattr.NamedArguments.Count);
            Assert.Equal("AllowMultiple", cattr.NamedArguments[0].MemberName);
            Assert.True((bool)cattr.NamedArguments[0].TypedValue.Value);
        }

        private static void CheckMarshallAttribute(IList<CustomAttributeData> attributes, UnmanagedType ctorValue, string? namedArgument = null, object? naValue = null)
        {
            CustomAttributeData cattr = attributes.First(a => a.AttributeType.Name == nameof(MarshalAsAttribute));
            Assert.Equal(1, cattr.ConstructorArguments.Count);
            Assert.Equal(ctorValue, (UnmanagedType)cattr.ConstructorArguments[0].Value);
            if (namedArgument != null)
            {
                CustomAttributeNamedArgument namedArg = cattr.NamedArguments.First(a => a.MemberName == namedArgument);
                Assert.Equal(namedArgument, namedArg.MemberName);
                Assert.Equal(naValue, namedArg.TypedValue.Value.ToString());
            }
        }

        void CheckAssembly(Assembly a)
        {
            // AssemblyName properties
            var aname = a.GetName(false);
            Assert.Equal(new Version(1, 2, 3, 4), aname.Version);
            Assert.Equal("en", aname.CultureInfo.Name);
            Assert.True((aname.Flags & AssemblyNameFlags.Retargetable) > 0);

            CheckCattr(a.GetCustomAttributesData());

            var iface1 = a.GetType("IFace1");
            var gtype2 = a.GetType("GType2");

            var type1 = a.GetType("Type1");
            Assert.NotNull(type1);

            // Type attributes
            Assert.Equal(TypeAttributes.Public | TypeAttributes.SequentialLayout, type1.Attributes);
            // Interfaces
            var ifaces = type1.GetInterfaces();
            Assert.Equal(2, ifaces.Length);
            Assert.True(iface1 == ifaces[0] || iface1 == ifaces[1]);
            Assert.True(typeof(IComparable).FullName == ifaces[0].FullName || typeof(IComparable).FullName == ifaces[1].FullName);
            CheckCattr(type1.GetCustomAttributesData());

            // Nested types
            var typeNested = a.GetType("Type1+TypeNested");
            Assert.NotNull(typeNested);

            // Generics
            var gtype1 = a.GetType("GType1");
            Assert.True(gtype1.IsGenericTypeDefinition);
            // Generic parameters
            var gparams = gtype1.GetGenericArguments();
            Assert.Equal(2, gparams.Length);
            Assert.Equal("K", gparams[0].Name);
            Assert.Equal("T", gparams[1].Name);
            var constraints = gparams[0].GetGenericParameterConstraints();
            Assert.Equal(2, constraints.Length);
            Assert.Equal(typeof(object).FullName, constraints[0].FullName);
            Assert.Equal(typeof(IComparable).FullName, constraints[1].FullName);
            CheckCattr(gparams[0].GetCustomAttributesData());
            constraints = gparams[1].GetGenericParameterConstraints();
            Assert.Equal(1, constraints.Length);
            Assert.Equal(gtype1, constraints[0]);
            // Type param encoding
            var field = gtype1.GetField("FieldGParam");
            Assert.Equal(gparams[0], field.FieldType);
            field = gtype1.GetField("FieldListOfT");
            Assert.Equal("List`1", field.FieldType.Name);

            // Type encoding
            var t = a.GetType("Type3");
            Assert.Equal(typeNested, t.GetField("FieldNested").FieldType);
            Assert.Equal(typeof(TimeZoneInfo.AdjustmentRule).FullName, t.GetField("FieldNestedRef").FieldType.FullName);
            Assert.Equal(typeof(Outer.Inner.InnerMostType).FullName, t.GetField("FieldDoubleNestedRef").FieldType.FullName);
            Assert.Equal(typeof(int).FullName, t.GetField("FieldInt").FieldType.FullName);
            Assert.Equal(typeof(object[]).FullName, t.GetField("FieldArrayTyperef").FieldType.FullName);
            Assert.Equal(type1.MakeArrayType(), t.GetField("FieldSzArray").FieldType);
            var arraytype1 = Array.CreateInstance(typeof(int), [10], [1]).GetType();

            Assert.Equal("System.Int32[]", t.GetField("FieldNonSzArray").FieldType.FullName);
            arraytype1 = Array.CreateInstance(typeof(int), [10, 10], [1, 1]).GetType();
            Assert.Equal(arraytype1.FullName, t.GetField("FieldMultiDimArray").FieldType.FullName);
            Assert.Equal(type1.MakePointerType(), t.GetField("FieldPointer").FieldType);
            Assert.Equal(typeof(List<int>).FullName, t.GetField("FieldGListOfInt").FieldType.FullName);
            Type gType = t.GetField("FieldGInstTBuilder").FieldType;
            Assert.True(gType.IsConstructedGenericType);
            Assert.Equal(2, gType.GenericTypeArguments.Length);
            Assert.Equal(typeof(int).FullName, gType.GenericTypeArguments[0].FullName);
            Assert.Equal(typeof(string).FullName, gType.GenericTypeArguments[1].FullName);

            // Field properties
            var type4 = a.GetType("Type4");
            field = type4.GetField("FieldInt");
            Assert.NotNull(field);
            Assert.Equal(42, field.GetRawConstantValue());
            field = type4.GetField("FieldOffset");
            Assert.NotNull(field);
            
            field = type4.GetField("FieldModopt");
            var cmods = field.GetRequiredCustomModifiers();
            Assert.Equal(1, cmods.Length);
            Assert.Equal(typeof(int).FullName, cmods[0].FullName);
            cmods = field.GetOptionalCustomModifiers();
            Assert.Equal(1, cmods.Length);
            Assert.Equal(typeof(uint).FullName, cmods[0].FullName);
            // Simple marshal
            field = type4.GetField("FieldMarshal1");
            CheckMarshallAttribute(field.GetCustomAttributesData(), UnmanagedType.U4);
            // ByValArray
            field = type4.GetField("FieldMarshalByvalArray");
            CheckMarshallAttribute(field.GetCustomAttributesData(), UnmanagedType.ByValArray, nameof(MarshalAsAttribute.SizeConst), "16");
            // ByValTStr
            field = type4.GetField("FieldMarshalByvalTStr");
            CheckMarshallAttribute(field.GetCustomAttributesData(), UnmanagedType.ByValTStr, nameof(MarshalAsAttribute.SizeConst), "16");
            // Custom marshaler
            field = type4.GetField("FieldMarshalCustom");
            CheckMarshallAttribute(field.GetCustomAttributesData(), UnmanagedType.CustomMarshaler, nameof(MarshalAsAttribute.MarshalCookie), "Cookie");
            CheckMarshallAttribute(field.GetCustomAttributesData(), UnmanagedType.CustomMarshaler, nameof(MarshalAsAttribute.MarshalTypeRef), typeof(object).ToString());

            field = type4.GetField("FieldCAttr");
            CheckCattr(field.GetCustomAttributesData());

            // Global fields
            field = a.ManifestModule.GetField("Data1");
            Assert.NotNull(field);
            field = a.ManifestModule.GetField("Data2");
            Assert.NotNull(field);

            // Methods and signatures
            var typeMethods = a.GetType("TypeMethods");
            var ctors = typeMethods.GetConstructors(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            Assert.Equal(2, ctors.Length);
            // .ctor
            var ctor = typeMethods.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
            Assert.NotNull(ctor);
            Assert.Equal(MethodImplAttributes.NoInlining | MethodImplAttributes.IL, ctor.GetMethodImplementationFlags());
            var parameters = ctor.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal("param1", parameters[0].Name);
            Assert.Equal(typeof(int).FullName, parameters[0].ParameterType.FullName);
            Assert.Equal(16, parameters[0].RawDefaultValue);
            CheckCattr(parameters[0].GetCustomAttributesData());
            cmods = parameters[0].GetRequiredCustomModifiers();
            Assert.Equal(1, cmods.Length);
            Assert.Equal(typeof(object).FullName, cmods[0].FullName);
            cmods = parameters[0].GetOptionalCustomModifiers();
            Assert.Equal(1, cmods.Length);
            Assert.Equal(typeof(int).FullName, cmods[0].FullName);
            Assert.Equal("param2", parameters[1].Name);
            Assert.Equal(ParameterAttributes.Out | ParameterAttributes.HasFieldMarshal, parameters[1].Attributes);
            Assert.Equal(typeof(object).FullName, parameters[1].ParameterType.FullName);
            CheckMarshallAttribute(parameters[1].GetCustomAttributesData(), UnmanagedType.U4);
            Assert.True(ctor.CallingConvention.HasFlag(CallingConventions.HasThis));
            // .cctor
            ctors = typeMethods.GetConstructors(BindingFlags.Public | BindingFlags.Static);
            Assert.Equal(1, ctors.Length);
            ctor = ctors[0];
            Assert.NotNull(ctor);

            // methods
            var method = typeMethods.GetMethod("Method1");
            Assert.NotNull(method);
            Assert.Equal(typeof(int).FullName, method.ReturnType.FullName);
            Assert.Equal(MethodImplAttributes.NoInlining | MethodImplAttributes.IL, method.GetMethodImplementationFlags());
            gparams = gtype1.GetGenericArguments();
            Assert.Equal(2, gparams.Length);
            Assert.Equal("K", gparams[0].Name);
            Assert.Equal("T", gparams[1].Name);
            constraints = gparams[0].GetGenericParameterConstraints();
            Assert.Equal(2, constraints.Length);
            Assert.Equal(typeof(object).FullName, constraints[0].FullName);
            Assert.Equal(typeof(IComparable).FullName, constraints[1].FullName);
            parameters = method.GetParameters();
            // method parameters
            Assert.Equal(2, parameters.Length);
            Assert.Equal("param1", parameters[0].Name);
            Assert.Equal(typeof(int).FullName, parameters[0].ParameterType.FullName);
            Assert.Equal(16, parameters[0].RawDefaultValue);
            Assert.Equal("param2", parameters[1].Name);

            Assert.Equal(ParameterAttributes.Out | ParameterAttributes.HasFieldMarshal, parameters[1].Attributes);
            Assert.Equal(typeof(object).FullName, parameters[1].ParameterType.FullName);
            CheckMarshallAttribute(parameters[1].GetCustomAttributesData(), UnmanagedType.U4);

            // return type
            var rparam = method.ReturnParameter;
            cmods = rparam.GetRequiredCustomModifiers();
            Assert.Equal(1, cmods.Length);
            Assert.Equal(typeof(object).FullName, cmods[0].FullName);
            cmods = rparam.GetOptionalCustomModifiers();
            Assert.Equal(1, cmods.Length);
            Assert.Equal(typeof(int).FullName, cmods[0].FullName);
            CheckMarshallAttribute(rparam.GetCustomAttributesData(), UnmanagedType.U4);
            CheckCattr(rparam.GetCustomAttributesData());

            // Properties
            var type_props = a.GetType("TypeProperties");
            var prop = type_props.GetProperty("AProperty");
            Assert.NotNull(prop);
            Assert.Equal(PropertyAttributes.HasDefault, prop.Attributes);
            var getter = prop.GetGetMethod();
            Assert.NotNull(getter);
            Assert.Equal("GetMethod1", getter.Name);
            var setter = prop.GetSetMethod();
            Assert.NotNull(setter);
            Assert.Equal("SetMethod1", setter.Name);
            CheckCattr(prop.GetCustomAttributesData());

            // Events
            var typeEvents = a.GetType("TypeEvents");
            var ev = typeEvents.GetEvent("Event1");
            Assert.NotNull(ev);
            var m = ev.AddMethod;
            Assert.NotNull(m);
            Assert.Equal("AddMethod1", m.Name);
            m = ev.RemoveMethod;
            Assert.NotNull(m);
            Assert.Equal("RemoveMethod1", m.Name);
            m = ev.RaiseMethod;
            Assert.NotNull(m);
            Assert.Equal("RaiseMethod1", m.Name);
            Assert.Equal(EventAttributes.SpecialName, ev.Attributes);
            CheckCattr(ev.GetCustomAttributesData());
        }
    }
}
