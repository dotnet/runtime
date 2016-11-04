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
        public override System.Reflection.TypeAttributes Attributes { get { throw null; } }
        public override Assembly Assembly { get { throw null; } }
        public override Type BaseType { get { throw null; } }
        public override MethodBase DeclaringMethod { get { throw null; } }
        public override GenericParameterAttributes GenericParameterAttributes { get { throw null; } }
        public override Module Module { get { throw null; } }

        public override int GetArrayRank() { throw null; }
        public override Type GetElementType() { throw null; }
        public override System.Type[] GetGenericParameterConstraints() { throw null; }
        public override System.Type[] GenericTypeArguments { get { throw null; } }
        public override bool ContainsGenericParameters { get { throw null; } }
        public override int GenericParameterPosition { get { throw null; } }
        public override Guid GUID { get { throw null; } }
        public override bool IsEnum { get { throw null; } }
        public override bool IsGenericType { get { throw null; } }
        public override bool IsGenericTypeDefinition { get { throw null; } }
        public override bool IsSerializable { get { throw null; } }
        public override string Namespace { get { throw null; } }

        public override Type DeclaringType { get { throw null; } }
    }

    public partial class EnumBuilder : System.Reflection.TypeInfo
    {
        // These members override base definitions on TypeInfo.
        public override System.Reflection.TypeAttributes Attributes { get { throw null; } }
        public override Assembly Assembly { get { throw null; } }
        public override Type BaseType { get { throw null; } }
        public override Module Module { get { throw null; } }

        public override int GetArrayRank() { throw null; }
        public override Type GetElementType() { throw null; }
        public override System.Type[] GetGenericParameterConstraints() { throw null; }
        public override System.Type[] GenericTypeArguments { get { throw null; } }
        public override Type GetGenericTypeDefinition() { throw null; }
        public override bool ContainsGenericParameters { get { throw null; } }
        public override MethodBase DeclaringMethod { get { throw null; } }
        public override GenericParameterAttributes GenericParameterAttributes { get { throw null; } }
        public override int GenericParameterPosition { get { throw null; } }
        public override Guid GUID { get { throw null; } }
        public override bool IsEnum { get { throw null; } }
        public override bool IsGenericParameter { get { throw null; } }
        public override bool IsGenericType { get { throw null; } }
        public override bool IsGenericTypeDefinition { get { throw null; } }
        public override bool IsSerializable { get { throw null; } }
        public override string Namespace { get { throw null; } }
        public override Type MakeGenericType(params System.Type[] typeArguments) { throw null; }

        public override Type DeclaringType { get { throw null; } }
    }

    public partial class GenericTypeParameterBuilder : System.Reflection.TypeInfo
    {
        // These members override base definitions on TypeInfo.
        public override bool IsSubclassOf(Type c) { throw null; }

        public override System.Reflection.TypeAttributes Attributes { get { throw null; } }
        public override Assembly Assembly { get { throw null; } }
        public override Type BaseType { get { throw null; } }
        public override MethodBase DeclaringMethod { get { throw null; } }
        public override GenericParameterAttributes GenericParameterAttributes { get { throw null; } }
        public override Module Module { get { throw null; } }

        public override int GetArrayRank() { throw null; }
        public override Type GetElementType() { throw null; }
        public override System.Type[] GetGenericParameterConstraints() { throw null; }
        public override System.Type[] GenericTypeArguments { get { throw null; } }
        public override bool ContainsGenericParameters { get { throw null; } }
        public override int GenericParameterPosition { get { throw null; } }
        public override Guid GUID { get { throw null; } }
        public override bool IsEnum { get { throw null; } }
        public override bool IsGenericType { get { throw null; } }
        public override bool IsGenericTypeDefinition { get { throw null; } }
        public override bool IsSerializable { get { throw null; } }
        public override string Namespace { get { throw null; } }

        public override Type DeclaringType { get { throw null; } }
    }

    public partial class AssemblyBuilder : System.Reflection.Assembly
    {
        public override System.Collections.Generic.IEnumerable<TypeInfo> DefinedTypes { get { throw null; } }

        public override System.Collections.Generic.IEnumerable<Module> Modules { get { throw null; } }
    }

    public partial class MethodBuilder : System.Reflection.MethodInfo
    {
        public override MethodImplAttributes MethodImplementationFlags { get { throw null; } }
    }

    public partial class ConstructorBuilder : System.Reflection.ConstructorInfo
    {
        public override MethodImplAttributes MethodImplementationFlags { get { throw null; } }
    }
}
