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

// This is required for ProjectN to extend reflection. Once we make extensibility via contracts work on desktop, this can be removed.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Private.Reflection.Extensibility, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")]

namespace System.Reflection
{
    public partial class TypeInfo
    {
        // These members are promoted from Type.
        public abstract System.Reflection.TypeAttributes Attributes { get; }
        public abstract int GetArrayRank();
        public abstract System.Type GetElementType();
        public abstract Type[] GetGenericParameterConstraints();
        public virtual bool IsSubclassOf(System.Type c) { return default(bool); }
        public virtual bool IsEquivalentTo(Type other) { return default(bool); }
        public abstract Type[] GenericTypeArguments { get; }
        public abstract Type GetGenericTypeDefinition();
        public abstract Assembly Assembly { get; }
        public abstract Type BaseType { get; }
        public abstract bool ContainsGenericParameters { get; }

        public abstract MethodBase DeclaringMethod { get; }
        public abstract string FullName { get; }
        public abstract GenericParameterAttributes GenericParameterAttributes { get; }
        public abstract int GenericParameterPosition { get; }
        public abstract Guid GUID { get; }
        public bool HasElementType { get { return default(bool); } }
        public bool IsAbstract { get { return default(bool); } }
        public bool IsAnsiClass { get { return default(bool); } }
        public bool IsArray { get { return default(bool); } }
        public bool IsAutoClass { get { return default(bool); } }
        public bool IsAutoLayout { get { return default(bool); } }
        public bool IsByRef { get { return default(bool); } }
        public bool IsClass { get { return default(bool); } }
        public virtual bool IsCOMObject { get { return default(bool); } }
        public abstract bool IsEnum { get; }
        public bool IsExplicitLayout { get { return default(bool); } }
        public abstract bool IsGenericParameter { get; }
        public abstract bool IsGenericType { get; }
        public abstract bool IsGenericTypeDefinition { get; }
        public bool IsImport { get { return default(bool); } }
        public bool IsInterface { get { return default(bool); } }
        public bool IsLayoutSequential { get { return default(bool); } }
        public bool IsMarshalByRef { get { return default(bool); } }
        public bool IsNested { get { return default(bool); } }
        public bool IsNestedAssembly { get { return default(bool); } }
        public bool IsNestedFamANDAssem { get { return default(bool); } }
        public bool IsNestedFamily { get { return default(bool); } }
        public bool IsNestedFamORAssem { get { return default(bool); } }
        public bool IsNestedPrivate { get { return default(bool); } }
        public bool IsNestedPublic { get { return default(bool); } }
        public bool IsNotPublic { get { return default(bool); } }
        public bool IsPointer { get { return default(bool); } }
        public virtual bool IsPrimitive { get { return default(bool); } }
        public bool IsPublic { get { return default(bool); } }
        public bool IsSealed { get { return default(bool); } }
        public bool IsVisible { get { return default(bool); } }
        public abstract bool IsSerializable { get; }
        public bool IsSpecialName { get { return default(bool); } }
        public bool IsUnicodeClass { get { return default(bool); } }
        public virtual bool IsValueType { get { return default(bool); } }
        public abstract string Namespace { get; }
        public abstract string AssemblyQualifiedName { get; }

        public abstract Type MakeArrayType();
        public abstract Type MakeArrayType(int rank);
        public abstract Type MakeByRefType();
        public abstract Type MakeGenericType(params System.Type[] typeArguments);
        public abstract Type MakePointerType();
    }

    // These should be made public when reflection extensibility via contracts is supported on all platforms.
    // In the meantime, these will be exposed via wrapper factory methods in System.Private.Reflection.Extensibility.
    public partial struct CustomAttributeNamedArgument
    {
        internal CustomAttributeNamedArgument(Type attributeType, string memberName, bool isField, CustomAttributeTypedArgument typedValue) { }
    }

    public partial struct CustomAttributeTypedArgument
    {
        internal CustomAttributeTypedArgument(Type argumentType, object value) { }
    }
}
