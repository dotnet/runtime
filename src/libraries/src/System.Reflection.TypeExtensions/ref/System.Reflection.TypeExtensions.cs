// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Reflection
{
    public static partial class AssemblyExtensions
    {
        public static System.Type[] GetExportedTypes(this System.Reflection.Assembly assembly) { return default(System.Type[]); }
        public static System.Reflection.Module[] GetModules(this System.Reflection.Assembly assembly) { return default(System.Reflection.Module[]); }
        public static System.Type[] GetTypes(this System.Reflection.Assembly assembly) { return default(System.Type[]); }
    }
    [System.FlagsAttribute]
    public enum BindingFlags
    {
        DeclaredOnly = 2,
        FlattenHierarchy = 64,
        IgnoreCase = 1,
        Instance = 4,
        NonPublic = 32,
        Public = 16,
        Static = 8,
    }
    public static partial class EventInfoExtensions
    {
        public static System.Reflection.MethodInfo GetAddMethod(this System.Reflection.EventInfo eventInfo) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetAddMethod(this System.Reflection.EventInfo eventInfo, bool nonPublic) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetRaiseMethod(this System.Reflection.EventInfo eventInfo) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetRaiseMethod(this System.Reflection.EventInfo eventInfo, bool nonPublic) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetRemoveMethod(this System.Reflection.EventInfo eventInfo) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetRemoveMethod(this System.Reflection.EventInfo eventInfo, bool nonPublic) { return default(System.Reflection.MethodInfo); }
    }
    public static partial class MemberInfoExtensions
    {
        public static bool HasMetadataToken(this MemberInfo member) { return default(bool); }
        public static int GetMetadataToken(this MemberInfo member) { return default(int); }
    }
    public static partial class MethodInfoExtensions
    {
        public static System.Reflection.MethodInfo GetBaseDefinition(this System.Reflection.MethodInfo method) { return default(System.Reflection.MethodInfo); }
    }
    public static partial class ModuleExtensions
    {
        public static bool HasModuleVersionId(this Module module) { return default(bool); }
        public static Guid GetModuleVersionId(this Module module) { return default(Guid); }
    }
    public static partial class PropertyInfoExtensions
    {
        public static System.Reflection.MethodInfo[] GetAccessors(this System.Reflection.PropertyInfo property) { return default(System.Reflection.MethodInfo[]); }
        public static System.Reflection.MethodInfo[] GetAccessors(this System.Reflection.PropertyInfo property, bool nonPublic) { return default(System.Reflection.MethodInfo[]); }
        public static System.Reflection.MethodInfo GetGetMethod(this System.Reflection.PropertyInfo property) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetGetMethod(this System.Reflection.PropertyInfo property, bool nonPublic) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetSetMethod(this System.Reflection.PropertyInfo property) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetSetMethod(this System.Reflection.PropertyInfo property, bool nonPublic) { return default(System.Reflection.MethodInfo); }
    }
    public static partial class TypeExtensions
    {
        public static System.Reflection.ConstructorInfo GetConstructor(this System.Type type, System.Type[] types) { return default(System.Reflection.ConstructorInfo); }
        public static System.Reflection.ConstructorInfo[] GetConstructors(this System.Type type) { return default(System.Reflection.ConstructorInfo[]); }
        public static System.Reflection.ConstructorInfo[] GetConstructors(this System.Type type, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.ConstructorInfo[]); }
        public static System.Reflection.MemberInfo[] GetDefaultMembers(this System.Type type) { return default(System.Reflection.MemberInfo[]); }
        public static System.Reflection.EventInfo GetEvent(this System.Type type, string name) { return default(System.Reflection.EventInfo); }
        public static System.Reflection.EventInfo GetEvent(this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.EventInfo); }
        public static System.Reflection.EventInfo[] GetEvents(this System.Type type) { return default(System.Reflection.EventInfo[]); }
        public static System.Reflection.EventInfo[] GetEvents(this System.Type type, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.EventInfo[]); }
        public static System.Reflection.FieldInfo GetField(this System.Type type, string name) { return default(System.Reflection.FieldInfo); }
        public static System.Reflection.FieldInfo GetField(this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.FieldInfo); }
        public static System.Reflection.FieldInfo[] GetFields(this System.Type type) { return default(System.Reflection.FieldInfo[]); }
        public static System.Reflection.FieldInfo[] GetFields(this System.Type type, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.FieldInfo[]); }
        public static System.Type[] GetGenericArguments(this System.Type type) { return default(System.Type[]); }
        public static System.Type[] GetInterfaces(this System.Type type) { return default(System.Type[]); }
        public static System.Reflection.MemberInfo[] GetMember(this System.Type type, string name) { return default(System.Reflection.MemberInfo[]); }
        public static System.Reflection.MemberInfo[] GetMember(this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.MemberInfo[]); }
        public static System.Reflection.MemberInfo[] GetMembers(this System.Type type) { return default(System.Reflection.MemberInfo[]); }
        public static System.Reflection.MemberInfo[] GetMembers(this System.Type type, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.MemberInfo[]); }
        public static System.Reflection.MethodInfo GetMethod(this System.Type type, string name) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetMethod(this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetMethod(this System.Type type, string name, System.Type[] types) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo[] GetMethods(this System.Type type) { return default(System.Reflection.MethodInfo[]); }
        public static System.Reflection.MethodInfo[] GetMethods(this System.Type type, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.MethodInfo[]); }
        public static System.Type GetNestedType(this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { return default(System.Type); }
        public static System.Type[] GetNestedTypes(this System.Type type, System.Reflection.BindingFlags bindingAttr) { return default(System.Type[]); }
        public static System.Reflection.PropertyInfo[] GetProperties(this System.Type type) { return default(System.Reflection.PropertyInfo[]); }
        public static System.Reflection.PropertyInfo[] GetProperties(this System.Type type, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.PropertyInfo[]); }
        public static System.Reflection.PropertyInfo GetProperty(this System.Type type, string name) { return default(System.Reflection.PropertyInfo); }
        public static System.Reflection.PropertyInfo GetProperty(this System.Type type, string name, System.Reflection.BindingFlags bindingAttr) { return default(System.Reflection.PropertyInfo); }
        public static System.Reflection.PropertyInfo GetProperty(this System.Type type, string name, System.Type returnType) { return default(System.Reflection.PropertyInfo); }
        public static System.Reflection.PropertyInfo GetProperty(this System.Type type, string name, System.Type returnType, System.Type[] types) { return default(System.Reflection.PropertyInfo); }
        public static bool IsAssignableFrom(this System.Type type, System.Type c) { return default(bool); }
        public static bool IsInstanceOfType(this System.Type type, object o) { return default(bool); }
    }
}
