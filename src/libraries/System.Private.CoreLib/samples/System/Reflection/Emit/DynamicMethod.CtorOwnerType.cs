// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#:property PublishAot=false

// Example: Create a DynamicMethod with an owner type to access private members.
// Shows CreateDelegate with a bound instance (instance-style invocation).
// Run: dotnet run DynamicMethod.CtorOwnerType.cs

using System.Reflection;
using System.Reflection.Emit;

#region OwnerTypeAccess
// A DynamicMethod associated with a type can access its private members.
DynamicMethod changeID = new(
    "", typeof(int), [typeof(Example), typeof(int)], typeof(Example));

// Get a FieldInfo for the private field 'id'.
FieldInfo fid = typeof(Example).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance)!;

ILGenerator ilg = changeID.GetILGenerator();
// Push current value of 'id' onto the stack.
ilg.Emit(OpCodes.Ldarg_0);
ilg.Emit(OpCodes.Ldfld, fid);
// Store the new value.
ilg.Emit(OpCodes.Ldarg_0);
ilg.Emit(OpCodes.Ldarg_1);
ilg.Emit(OpCodes.Stfld, fid);
// Return the old value.
ilg.Emit(OpCodes.Ret);

// Static-style delegate: takes (Example, int), returns old id.
var setId = changeID.CreateDelegate<Func<Example, int, int>>();

Example ex = new(42);
int oldId = setId(ex, 1492);
Console.WriteLine($"Previous id: {oldId}, new id: {ex.ID}");

// Instance-style delegate: bind to a specific Example instance.
var setBound = (Func<int, int>)changeID.CreateDelegate(typeof(Func<int, int>), ex);
oldId = setBound(2700);
Console.WriteLine($"Previous id: {oldId}, new id: {ex.ID}");
#endregion OwnerTypeAccess

// Verify
if (ex.ID != 2700)
{
    Console.Error.WriteLine($"FAIL: expected 2700, got {ex.ID}");
    return 1;
}

Console.WriteLine("Passed.");
return 0;

// Helper types
public class Example(int id)
{
    private int id = id;
    public int ID => id;
}
