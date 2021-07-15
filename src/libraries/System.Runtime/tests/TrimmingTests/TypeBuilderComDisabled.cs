using System;
using System.Reflection;
using System.Reflection.Emit;

AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("GeneratedAssembly"), AssemblyBuilderAccess.Run);
ModuleBuilder module = assembly.DefineDynamicModule("GeneratedModule");
TypeBuilder genericType = module.DefineType("GeneratedType");
genericType.DefineField("_int", typeof(int), FieldAttributes.Private);
genericType.DefineProperty("Prop", PropertyAttributes.None, typeof(string), null);

Type generatedType = genericType.CreateType();
return 100;
