// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace System.Reflection.Emit
{
    internal static class IgnoreAccessChecksToAttributeBuilder
    {
        /// <summary>
        /// Generate the declaration for the IgnoresAccessChecksToAttribute type.
        /// This attribute will be both defined and used in the dynamic assembly.
        /// Each usage identifies the name of the assembly containing non-public
        /// types the dynamic assembly needs to access.  Normally those types
        /// would be inaccessible, but this attribute allows them to be visible.
        /// It works like a reverse InternalsVisibleToAttribute.
        /// This method returns the ConstructorInfo of the generated attribute.
        /// </summary>
        public static ConstructorInfo AddToModule(ModuleBuilder mb)
        {
            TypeBuilder attributeTypeBuilder =
                mb.DefineType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
                               TypeAttributes.Public | TypeAttributes.Class,
                               typeof(Attribute));

            // Create backing field as:
            // private string assemblyName;
            FieldBuilder assemblyNameField =
                attributeTypeBuilder.DefineField("assemblyName", typeof(string), FieldAttributes.Private);

            // Create ctor as:
            // public IgnoresAccessChecksToAttribute(string)
            ConstructorBuilder constructorBuilder = attributeTypeBuilder.DefineConstructor(MethodAttributes.Public,
                                                         CallingConventions.HasThis,
                                                         new Type[] { assemblyNameField.FieldType });

            ILGenerator il = constructorBuilder.GetILGenerator();

            // Create ctor body as:
            // this.assemblyName = {ctor parameter 0}
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, 1);
            il.Emit(OpCodes.Stfld, assemblyNameField);

            // return
            il.Emit(OpCodes.Ret);

            // Define property as:
            // public string AssemblyName {get { return this.assemblyName; } }
            PropertyBuilder getterPropertyBuilder = attributeTypeBuilder.DefineProperty(
                                                   "AssemblyName",
                                                   PropertyAttributes.None,
                                                   CallingConventions.HasThis,
                                                   returnType: typeof(string),
                                                   parameterTypes: null);

            MethodBuilder getterMethodBuilder = attributeTypeBuilder.DefineMethod(
                                                   "get_AssemblyName",
                                                   MethodAttributes.Public,
                                                   CallingConventions.HasThis,
                                                   returnType: typeof(string),
                                                   parameterTypes: null);

            // Generate body:
            // return this.assemblyName;
            il = getterMethodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, assemblyNameField);
            il.Emit(OpCodes.Ret);

            // Generate the AttributeUsage attribute for this attribute type:
            // [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
            TypeInfo attributeUsageTypeInfo = typeof(AttributeUsageAttribute).GetTypeInfo();

            // Find the ctor that takes only AttributeTargets
            ConstructorInfo attributeUsageConstructorInfo =
                attributeUsageTypeInfo.DeclaredConstructors
                    .Single(c => c.GetParameters().Count() == 1 &&
                                 c.GetParameters()[0].ParameterType == typeof(AttributeTargets));

            // Find the property to set AllowMultiple
            PropertyInfo allowMultipleProperty =
                attributeUsageTypeInfo.DeclaredProperties
                    .Single(f => string.Equals(f.Name, "AllowMultiple"));

            // Create a builder to construct the instance via the ctor and property
            CustomAttributeBuilder customAttributeBuilder =
                new CustomAttributeBuilder(attributeUsageConstructorInfo,
                                            new object[] { AttributeTargets.Assembly },
                                            new PropertyInfo[] { allowMultipleProperty },
                                            new object[] { true });

            // Attach this attribute instance to the newly defined attribute type
            attributeTypeBuilder.SetCustomAttribute(customAttributeBuilder);

            // Make the TypeInfo real so the constructor can be used.
            return attributeTypeBuilder.CreateTypeInfo().DeclaredConstructors.Single();
        }
    }
}
