// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#:property PublishAot=false

// Example: Create a DynamicMethod, emit IL, and execute via delegate and Invoke.
// Referenced by DynamicMethod class overview and GetILGenerator/constructor docs.
// Run: dotnet run DynamicMethod.Overview.cs

using System.Reflection;
using System.Reflection.Emit;

#region Snippet1
// Create a dynamic method with return type int and two parameters (string, int).
DynamicMethod hello = new("Hello", typeof(int), [typeof(string), typeof(int)], typeof(string).Module);

// Emit a body: print the string argument, then return the int argument.
MethodInfo writeString = typeof(Console).GetMethod("WriteLine", [typeof(string)])!;
ILGenerator il = hello.GetILGenerator();
il.Emit(OpCodes.Ldarg_0);
il.EmitCall(OpCodes.Call, writeString, null);
il.Emit(OpCodes.Ldarg_1);
il.Emit(OpCodes.Ret);

// Create a delegate that represents the dynamic method.
Func<string, int, int> hi = hello.CreateDelegate<Func<string, int, int>>();

// Execute via delegate.
int retval = hi("Hello, World!", 42);
Console.WriteLine($"Delegate returned: {retval}");

// Execute via Invoke (slower — requires boxing and array allocation).
object? objRet = hello.Invoke(null, ["Hello via Invoke!", 99]);
Console.WriteLine($"Invoke returned: {objRet}");
#endregion

// Verify results
if (retval != 42 || objRet is not 99)
{
    Console.Error.WriteLine("FAIL: unexpected return values");
    return 1;
}

Console.WriteLine("Passed.");
return 0;
