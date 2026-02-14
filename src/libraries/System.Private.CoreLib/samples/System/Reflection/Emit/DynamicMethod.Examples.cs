// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#:property PublishAot=false

// Runnable examples for DynamicMethod XML doc comments.
// Each #region is referenced by <code> tags in DynamicMethod.cs.
// Run: dotnet run DynamicMethod.Examples.cs

using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

// Create a DynamicMethod shared by the property/method examples below.
DynamicMethod hello = new("Hello", typeof(int), [typeof(string), typeof(int)], typeof(string).Module);
int failures = 0;

void Fail(string name, string message)
{
    Console.Error.WriteLine($"FAIL: {name} — {message}");
    failures++;
}

#region Name
// The name specified when the dynamic method was created.
Console.WriteLine($"Name: {hello.Name}");
#endregion
if (hello.Name != "Hello") Fail("Name", $"Expected 'Hello', got '{hello.Name}'");

#region DeclaringType
// DeclaringType is always null for dynamic methods.
Console.WriteLine($"DeclaringType: {hello.DeclaringType?.ToString() ?? "(null)"}");
#endregion
if (hello.DeclaringType is not null) Fail("DeclaringType", "Expected null");

#region ReflectedType
// ReflectedType is always null for dynamic methods.
Console.WriteLine($"ReflectedType: {hello.ReflectedType?.ToString() ?? "(null)"}");
#endregion
if (hello.ReflectedType is not null) Fail("ReflectedType", "Expected null");

#region Module
// The module the dynamic method is associated with.
Console.WriteLine($"Module: {hello.Module}");
#endregion
if (hello.Module != typeof(string).Module) Fail("Module", "Expected System.Private.CoreLib module");

#region Attributes
// Dynamic methods always have Public | Static attributes.
Console.WriteLine($"Attributes: {hello.Attributes}");
#endregion
if (hello.Attributes != (MethodAttributes.Public | MethodAttributes.Static))
    Fail("Attributes", $"Expected Public | Static, got {hello.Attributes}");

#region CallingConvention
// Dynamic methods always use Standard calling convention.
Console.WriteLine($"CallingConvention: {hello.CallingConvention}");
#endregion
if (hello.CallingConvention != CallingConventions.Standard)
    Fail("CallingConvention", $"Expected Standard, got {hello.CallingConvention}");

#region InitLocals
// InitLocals defaults to true — local variables are zero-initialized.
Console.WriteLine($"InitLocals: {hello.InitLocals}");
#endregion
if (!hello.InitLocals) Fail("InitLocals", "Expected true");

#region ReturnType
// The return type specified when the dynamic method was created.
Console.WriteLine($"ReturnType: {hello.ReturnType}");
#endregion
if (hello.ReturnType != typeof(int)) Fail("ReturnType", $"Expected Int32, got {hello.ReturnType}");

#region ReturnTypeCustomAttributes
// At present there is no way to set custom attributes on the return type,
// so the list is always empty.
ICustomAttributeProvider caProvider = hello.ReturnTypeCustomAttributes;
object[] returnAttributes = caProvider.GetCustomAttributes(true);
Console.WriteLine($"Return type custom attributes: {returnAttributes.Length}");
#endregion
if (returnAttributes.Length != 0) Fail("ReturnTypeCustomAttributes", "Expected 0 attributes");

#region DefineParameter
// Optionally add parameter metadata (useful for debugging).
hello.DefineParameter(1, ParameterAttributes.In, "message");
hello.DefineParameter(2, ParameterAttributes.In, "valueToReturn");
#endregion

#region GetParameters
// Retrieve parameter info after calling DefineParameter.
ParameterInfo[] parameters = hello.GetParameters();
foreach (ParameterInfo p in parameters)
    Console.WriteLine($"  Param: {p.Name}, {p.ParameterType}, {p.Attributes}");
#endregion
if (parameters.Length != 2) Fail("GetParameters", $"Expected 2 parameters, got {parameters.Length}");

#region ToString
// ToString returns the signature: return type, name, and parameter types.
string sig = hello.ToString();
Console.WriteLine($"ToString: {sig}");
#endregion
if (!sig.Contains("Hello")) Fail("ToString", $"Expected signature to contain 'Hello'");

// Emit a method body so we can test CreateDelegate and Invoke.
MethodInfo writeString = typeof(Console).GetMethod("WriteLine", [typeof(string)])!;
ILGenerator il = hello.GetILGenerator();
il.Emit(OpCodes.Ldarg_0);
il.EmitCall(OpCodes.Call, writeString, null);
il.Emit(OpCodes.Ldarg_1);
il.Emit(OpCodes.Ret);

// Complete the method and execute it via delegate and reflection.
Func<string, int, int> hi = hello.CreateDelegate<Func<string, int, int>>();
int retval = hi("Hello from delegate!", 42);
if (retval != 42) Fail("CreateDelegate", $"Expected 42, got {retval}");

object? objRet = hello.Invoke(null, BindingFlags.ExactBinding, null,
    ["Hello from Invoke!", 99], CultureInfo.InvariantCulture);
if (objRet is not 99) Fail("Invoke", $"Expected 99, got {objRet}");

if (failures > 0)
{
    Console.Error.WriteLine($"\n{failures} example(s) failed.");
    return 1;
}

Console.WriteLine("\nAll examples passed.");
return 0;
