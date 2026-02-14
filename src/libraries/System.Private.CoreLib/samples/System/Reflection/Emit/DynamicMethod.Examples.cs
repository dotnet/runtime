// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Runnable examples for DynamicMethod XML doc comments.
// Each local function is a self-contained sample referenced by <code> tags in DynamicMethod.cs.
// Run: dotnet run DynamicMethod.Examples.cs

using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

MetadataAndProperties();
DefineParameterAndGetParameters();
GetILGeneratorAndInvoke();

Console.WriteLine("All examples passed.");
return 0;

void MetadataAndProperties()
{
    // Create a dynamic method associated with a module.
    DynamicMethod hello = new("Hello", typeof(int), [typeof(string), typeof(int)], typeof(string).Module);

    // Name: the name specified at creation.
    Console.WriteLine($"Name: {hello.Name}");

    // DeclaringType is always null for dynamic methods.
    Console.WriteLine($"DeclaringType: {hello.DeclaringType?.ToString() ?? "(null)"}");

    // ReflectedType is always null for dynamic methods.
    Console.WriteLine($"ReflectedType: {hello.ReflectedType?.ToString() ?? "(null)"}");

    // Module: the module the dynamic method is associated with.
    Console.WriteLine($"Module: {hello.Module}");

    // Attributes are always Public | Static.
    Console.WriteLine($"Attributes: {hello.Attributes}");

    // CallingConvention is always Standard.
    Console.WriteLine($"CallingConvention: {hello.CallingConvention}");

    // ReturnType: the return type specified at creation.
    Console.WriteLine($"ReturnType: {hello.ReturnType}");

    // ReturnTypeCustomAttributes: no way to set custom attributes on the return type.
    ICustomAttributeProvider caProvider = hello.ReturnTypeCustomAttributes;
    object[] returnAttributes = caProvider.GetCustomAttributes(true);
    Console.WriteLine($"Return type custom attributes: {returnAttributes.Length}");

    // InitLocals defaults to true — local variables are zero-initialized.
    Console.WriteLine($"InitLocals: {hello.InitLocals}");

    // ToString returns the method signature (return type, name, parameter types).
    Console.WriteLine($"ToString: {hello.ToString()}");
}

void DefineParameterAndGetParameters()
{
    DynamicMethod hello = new("Hello", typeof(int), [typeof(string), typeof(int)], typeof(string).Module);

    // DefineParameter adds metadata such as name and attributes.
    // Parameter positions are 1-based; position 0 refers to the return value.
    hello.DefineParameter(1, ParameterAttributes.In, "message");
    hello.DefineParameter(2, ParameterAttributes.In, "valueToReturn");

    // GetParameters retrieves the parameter info.
    ParameterInfo[] parameters = hello.GetParameters();
    foreach (ParameterInfo p in parameters)
        Console.WriteLine($"  Param: {p.Name}, {p.ParameterType}, {p.Attributes}");
}

void GetILGeneratorAndInvoke()
{
    DynamicMethod hello = new("Hello", typeof(int), [typeof(string), typeof(int)], typeof(string).Module);
    MethodInfo writeString = typeof(Console).GetMethod("WriteLine", [typeof(string)])!;

    // GetILGenerator returns an ILGenerator for emitting the method body.
    ILGenerator il = hello.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0);            // push first arg (string)
    il.EmitCall(OpCodes.Call, writeString, null); // Console.WriteLine(string)
    il.Emit(OpCodes.Ldarg_1);            // push second arg (int)
    il.Emit(OpCodes.Ret);                // return it

    // CreateDelegate produces a strongly-typed delegate for the dynamic method.
    Func<string, int, int> hi = hello.CreateDelegate<Func<string, int, int>>();
    int retval = hi("Hello from delegate!", 42);
    Console.WriteLine($"Delegate returned: {retval}");

    // Invoke calls the dynamic method via reflection (slower than a delegate).
    object? objRet = hello.Invoke(null, BindingFlags.ExactBinding, null,
        ["Hello from Invoke!", 99], CultureInfo.InvariantCulture);
    Console.WriteLine($"Invoke returned: {objRet}");
}
