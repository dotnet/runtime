using System;
using System.Reflection;
using System.Reflection.Emit;

// This is a temporary workaround for TypeBuilder and EnumBuilder not extending TypeInfo. See https://github.com/dotnet/corefx/issues/14334. 
public static class TypeBuilderExtensions
{
    public static Type AsType(this TypeBuilder tb) => tb;
    public static Type AsType(this EnumBuilder eb) => eb;
}