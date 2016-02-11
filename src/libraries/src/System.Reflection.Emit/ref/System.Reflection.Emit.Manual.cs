// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


// This is only needed for COMAwareEventInfo to inherit from EventInfo. Next version when
// Reflection is extensible then we should remove this InternalsVisibleTo.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Runtime.InteropServices, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")]

// This is required so that AssemblyBuilder can derive from Assembly.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Reflection.Emit, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")]

// This is required so that DynamicMethod can derive from MethodInfo.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Reflection.Emit.Lightweight, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")]

namespace System.Reflection.Emit
{
    public partial class TypeBuilder : System.Reflection.TypeInfo
    {
        // These members override base definitions on TypeInfo.
        public override System.Reflection.TypeAttributes Attributes { get { return default(System.Reflection.TypeAttributes); } }
        public override Assembly Assembly { get { return null; } }
        public override Type BaseType { get { return null; } }
        public override MethodBase DeclaringMethod { get { return null; } }
        public override GenericParameterAttributes GenericParameterAttributes { get { return default(GenericParameterAttributes); } }
        public override Module Module { get { return null; } }

        public override int GetArrayRank() { return default(int); }
        public override Type GetElementType() { return default(Type); }
        public override System.Type[] GetGenericParameterConstraints() { return default(System.Type[]); }
        public override System.Type[] GenericTypeArguments { get { return default(System.Type[]); } }
        public override bool ContainsGenericParameters { get { return default(bool); } }
        public override int GenericParameterPosition { get { return default(int); } }
        public override Guid GUID { get { return default(Guid); } }
        public override bool IsEnum { get { return default(bool); } }
        public override bool IsGenericType { get { return default(bool); } }
        public override bool IsGenericTypeDefinition { get { return default(bool); } }
        public override bool IsSerializable { get { return default(bool); } }
        public override string Namespace { get { return default(string); } }

        public override Type DeclaringType { get { return default(Type); } }
    }

    public partial class EnumBuilder : System.Reflection.TypeInfo
    {
        // These members override base definitions on TypeInfo.
        public override System.Reflection.TypeAttributes Attributes { get { return default(System.Reflection.TypeAttributes); } }
        public override Assembly Assembly { get { return null; } }
        public override Type BaseType { get { return null; } }
        public override Module Module { get { return null; } }

        public override int GetArrayRank() { return default(int); }
        public override Type GetElementType() { return default(Type); }
        public override System.Type[] GetGenericParameterConstraints() { return default(System.Type[]); }
        public override System.Type[] GenericTypeArguments { get { return default(System.Type[]); } }
        public override Type GetGenericTypeDefinition() { return default(Type); }
        public override bool ContainsGenericParameters { get { return default(bool); } }
        public override MethodBase DeclaringMethod { get { return default(MethodBase); } }
        public override GenericParameterAttributes GenericParameterAttributes { get { return default(GenericParameterAttributes); } }
        public override int GenericParameterPosition { get { return default(int); } }
        public override Guid GUID { get { return default(Guid); } }
        public override bool IsEnum { get { return default(bool); } }
        public override bool IsGenericParameter { get { return default(bool); } }
        public override bool IsGenericType { get { return default(bool); } }
        public override bool IsGenericTypeDefinition { get { return default(bool); } }
        public override bool IsSerializable { get { return default(bool); } }
        public override string Namespace { get { return default(string); } }
        public override Type MakeGenericType(params System.Type[] typeArguments) { return default(Type); }

        public override Type DeclaringType { get { return default(Type); } }
    }

    public partial class GenericTypeParameterBuilder : System.Reflection.TypeInfo
    {
        // These members override base definitions on TypeInfo.
        public override bool IsSubclassOf(Type c) { return default(bool); }

        public override System.Reflection.TypeAttributes Attributes { get { return default(System.Reflection.TypeAttributes); } }
        public override Assembly Assembly { get { return null; } }
        public override Type BaseType { get { return null; } }
        public override MethodBase DeclaringMethod { get { return null; } }
        public override GenericParameterAttributes GenericParameterAttributes { get { return default(GenericParameterAttributes); } }
        public override Module Module { get { return null; } }

        public override int GetArrayRank() { return default(int); }
        public override Type GetElementType() { return default(Type); }
        public override System.Type[] GetGenericParameterConstraints() { return default(System.Type[]); }
        public override System.Type[] GenericTypeArguments { get { return default(System.Type[]); } }
        public override bool ContainsGenericParameters { get { return default(bool); } }
        public override int GenericParameterPosition { get { return default(int); } }
        public override Guid GUID { get { return default(Guid); } }
        public override bool IsEnum { get { return default(bool); } }
        public override bool IsGenericType { get { return default(bool); } }
        public override bool IsGenericTypeDefinition { get { return default(bool); } }
        public override bool IsSerializable { get { return default(bool); } }
        public override string Namespace { get { return default(string); } }

        public override Type DeclaringType { get { return default(Type); } }
    }

    public partial class AssemblyBuilder : System.Reflection.Assembly
    {
        public override System.Collections.Generic.IEnumerable<TypeInfo> DefinedTypes { get { return null; } }

        public override System.Collections.Generic.IEnumerable<Module> Modules { get { return null; } }
    }

    public partial class MethodBuilder : System.Reflection.MethodInfo
    {
        public override MethodImplAttributes MethodImplementationFlags { get { return default(MethodImplAttributes); } }
    }

    public partial class ConstructorBuilder : System.Reflection.ConstructorInfo
    {
        public override MethodImplAttributes MethodImplementationFlags { get { return default(MethodImplAttributes); } }
    }
}
