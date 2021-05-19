// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection
{
    public static partial class AssemblyExtensions
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public static System.Type[] GetExportedTypes(this System.Reflection.Assembly assembly) { throw null; }
        public static System.Reflection.Module[] GetModules(this System.Reflection.Assembly assembly) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public static System.Type[] GetTypes(this System.Reflection.Assembly assembly) { throw null; }
    }
    public static partial class EventInfoExtensions
    {
        public static System.Reflection.MethodInfo? GetAddMethod(this System.Reflection.EventInfo eventInfo) { throw null; }
        public static System.Reflection.MethodInfo? GetAddMethod(this System.Reflection.EventInfo eventInfo, bool nonPublic) { throw null; }
        public static System.Reflection.MethodInfo? GetRaiseMethod(this System.Reflection.EventInfo eventInfo) { throw null; }
        public static System.Reflection.MethodInfo? GetRaiseMethod(this System.Reflection.EventInfo eventInfo, bool nonPublic) { throw null; }
        public static System.Reflection.MethodInfo? GetRemoveMethod(this System.Reflection.EventInfo eventInfo) { throw null; }
        public static System.Reflection.MethodInfo? GetRemoveMethod(this System.Reflection.EventInfo eventInfo, bool nonPublic) { throw null; }
    }
    public static partial class MemberInfoExtensions
    {
        public static int GetMetadataToken(this System.Reflection.MemberInfo member) { throw null; }
        public static bool HasMetadataToken(this System.Reflection.MemberInfo member) { throw null; }
    }
    public static partial class MethodInfoExtensions
    {
        public static System.Reflection.MethodInfo GetBaseDefinition(this System.Reflection.MethodInfo method) { throw null; }
    }
    public static partial class ModuleExtensions
    {
        public static System.Guid GetModuleVersionId(this System.Reflection.Module module) { throw null; }
        public static bool HasModuleVersionId(this System.Reflection.Module module) { throw null; }
    }
    public static partial class PropertyInfoExtensions
    {
        public static System.Reflection.MethodInfo[] GetAccessors(this System.Reflection.PropertyInfo property) { throw null; }
        public static System.Reflection.MethodInfo[] GetAccessors(this System.Reflection.PropertyInfo property, bool nonPublic) { throw null; }
        public static System.Reflection.MethodInfo? GetGetMethod(this System.Reflection.PropertyInfo property) { throw null; }
        public static System.Reflection.MethodInfo? GetGetMethod(this System.Reflection.PropertyInfo property, bool nonPublic) { throw null; }
        public static System.Reflection.MethodInfo? GetSetMethod(this System.Reflection.PropertyInfo property) { throw null; }
        public static System.Reflection.MethodInfo? GetSetMethod(this System.Reflection.PropertyInfo property, bool nonPublic) { throw null; }
    }
    public static partial class TypeExtensions
    {
        public static System.Reflection.ConstructorInfo? GetConstructor([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] this System.Type type, System.Type[] types) { throw null; }
        public static System.Reflection.ConstructorInfo[] GetConstructors([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] this System.Type type) { throw null; }
        public static System.Reflection.ConstructorInfo[] GetConstructors([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] this System.Type type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.MemberInfo[] GetDefaultMembers([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] this System.Type type) { throw null; }
        public static System.Reflection.EventInfo? GetEvent([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)] this System.Type type, string name) { throw null; }
        public static System.Reflection.EventInfo? GetEvent([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)] this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.EventInfo[] GetEvents([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)] this System.Type type) { throw null; }
        public static System.Reflection.EventInfo[] GetEvents([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)] this System.Type type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.FieldInfo? GetField([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] this System.Type type, string name) { throw null; }
        public static System.Reflection.FieldInfo? GetField([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.FieldInfo[] GetFields([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] this System.Type type) { throw null; }
        public static System.Reflection.FieldInfo[] GetFields([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] this System.Type type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Type[] GetGenericArguments(this System.Type type) { throw null; }
        public static System.Type[] GetInterfaces([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)] this System.Type type) { throw null; }
        public static System.Reflection.MemberInfo[] GetMember([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] this System.Type type, string name) { throw null; }
        public static System.Reflection.MemberInfo[] GetMember([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.MemberInfo[] GetMembers([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] this System.Type type) { throw null; }
        public static System.Reflection.MemberInfo[] GetMembers([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] this System.Type type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.MethodInfo? GetMethod([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] this System.Type type, string name) { throw null; }
        public static System.Reflection.MethodInfo? GetMethod([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.MethodInfo? GetMethod([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] this System.Type type, string name, System.Type[] types) { throw null; }
        public static System.Reflection.MethodInfo[] GetMethods([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] this System.Type type) { throw null; }
        public static System.Reflection.MethodInfo[] GetMethods([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] this System.Type type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Type? GetNestedType([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)] this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Type[] GetNestedTypes([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)] this System.Type type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.PropertyInfo[] GetProperties([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] this System.Type type) { throw null; }
        public static System.Reflection.PropertyInfo[] GetProperties([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] this System.Type type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.PropertyInfo? GetProperty([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] this System.Type type, string name) { throw null; }
        public static System.Reflection.PropertyInfo? GetProperty([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.PropertyInfo? GetProperty([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] this System.Type type, string name, System.Type? returnType) { throw null; }
        public static System.Reflection.PropertyInfo? GetProperty([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] this System.Type type, string name, System.Type? returnType, System.Type[] types) { throw null; }
        public static bool IsAssignableFrom(this System.Type type, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Type? c) { throw null; }
        public static bool IsInstanceOfType(this System.Type type, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? o) { throw null; }
    }
}
