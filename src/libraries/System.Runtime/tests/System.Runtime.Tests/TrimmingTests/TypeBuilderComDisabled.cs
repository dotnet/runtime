// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;

AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("GeneratedAssembly"), AssemblyBuilderAccess.Run);
ModuleBuilder module = assembly.DefineDynamicModule("GeneratedModule");

string typeName = "GeneratedType";
TypeBuilder genericType = module.DefineType(typeName);
genericType.DefineField("_int", typeof(int), FieldAttributes.Private);
genericType.DefineProperty("Prop", PropertyAttributes.None, typeof(string), null);

Type generatedType = genericType.CreateType();
if (generatedType.Name != typeName)
{
    Console.WriteLine($"Unexpected name for generated type. Expected: {typeName}, Actual: {generatedType.Name}");
    return -1;
}

object obj = Activator.CreateInstance(generatedType);
string objAsString = obj.ToString();
if (objAsString != typeName)
{
    Console.WriteLine($"Unexpected result of ToString() for instance of generated type. Expected: {typeName}, Actual: {objAsString}");
    return -2;
}

return 100;
