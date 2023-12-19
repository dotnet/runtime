using System;
using System.Resources;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Xunit;

#nullable disable

namespace DynamicObjects {
    public class M {
        public const string ObjectRequiredMessage = "some string";
        [Fact]
        public static void TestEntryPoint()
        {
            var instance = createObject();
            var attrs = instance.GetType().GetProperty("prop1").GetCustomAttributes();

            Assert.True(attrs.Count() == 2);
            Assert.Equal("System.ComponentModel.DataAnnotations.DisplayAttribute", attrs.ElementAt(0).ToString());
            Assert.Equal("System.ComponentModel.DataAnnotations.RequiredAttribute", attrs.ElementAt(1).ToString());
            Assert.Equal(typeof(RequiredAttribute), attrs.ElementAt(1).GetType());
            Assert.Equal(ObjectRequiredMessage, ((RequiredAttribute)attrs.ElementAt(1)).FormatErrorMessage("abc"));

            Console.WriteLine("Success");
        }

        public static object createObject () {
            var an = new AssemblyName { Name = "TempAssembly" ,Version = new Version(1, 0, 0, 0) };
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("TempWorkflowAssembly.dll");
            var tb = moduleBuilder.DefineType("namespace.myclass"
                                            , TypeAttributes.Public |
                                            TypeAttributes.Class |
                                            TypeAttributes.AnsiClass |
                                            TypeAttributes.BeforeFieldInit
                                            , typeof(object));

            FieldBuilder fb = tb.DefineField("_prop1",
                                                        typeof(string),
                                                        FieldAttributes.Private);

            var pb = tb.DefineProperty("prop1", PropertyAttributes.HasDefault, typeof(string), null);
            MethodAttributes getSetAttr =
                MethodAttributes.Public | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig;

            // Define the "get" accessor method for prop1.
            MethodBuilder custNameGetPropMthdBldr =
                tb.DefineMethod("get_prop1",
                                        getSetAttr,
                                        typeof(string),
                                        Type.EmptyTypes);

            ILGenerator custNameGetIL = custNameGetPropMthdBldr.GetILGenerator();

            custNameGetIL.Emit(OpCodes.Ldarg_0);
            custNameGetIL.Emit(OpCodes.Ldfld, fb);
            custNameGetIL.Emit(OpCodes.Ret);

            // Define the "set" accessor method for prop1.
            MethodBuilder custNameSetPropMthdBldr =
                tb.DefineMethod("set_prop1",
                                        getSetAttr,
                                        null,
                                        new Type[] { typeof(string) });

            ILGenerator custNameSetIL = custNameSetPropMthdBldr.GetILGenerator();

            custNameSetIL.Emit(OpCodes.Ldarg_0);
            custNameSetIL.Emit(OpCodes.Ldarg_1);
            custNameSetIL.Emit(OpCodes.Stfld, fb);
            custNameSetIL.Emit(OpCodes.Ret);

            // Last, we must map the two methods created above to our PropertyBuilder to
            // their corresponding behaviors, "get" and "set" respectively.
            pb.SetGetMethod(custNameGetPropMthdBldr);
            pb.SetSetMethod(custNameSetPropMthdBldr);


            ///create display attribute
            var dat = typeof(DisplayAttribute);
            CustomAttributeBuilder CAB = new CustomAttributeBuilder(dat.GetConstructor(new Type[0]), 
                new object[0],
                new PropertyInfo[1] { dat.GetProperty(nameof(DisplayAttribute.Name))}, 
                new object[] { "property 1"});
            pb.SetCustomAttribute(CAB);

            // //create required attribute
            var rat = typeof(RequiredAttribute);
            CustomAttributeBuilder CABR = new CustomAttributeBuilder(rat.GetConstructor(new Type[0]),
                                new object[0],
                                new PropertyInfo[2] { rat.GetProperty(nameof(RequiredAttribute.ErrorMessageResourceType)),rat.GetProperty(nameof(RequiredAttribute.ErrorMessageResourceName))},
                                new object[] {typeof(ValidationErrors), "ObjectRequired" });
            pb.SetCustomAttribute(CABR);

            var objectType = tb.CreateType();
            return Activator.CreateInstance(objectType);
        }
    }

    public class ValidationErrors {
        public static string ObjectRequired => M.ObjectRequiredMessage; 
    }

}
