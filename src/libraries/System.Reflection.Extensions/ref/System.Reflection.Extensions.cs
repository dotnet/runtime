// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Reflection
{
    public static partial class CustomAttributeExtensions
    {
        public static System.Attribute GetCustomAttribute(this System.Reflection.Assembly element, System.Type attributeType) { return default(System.Attribute); }
        public static System.Attribute GetCustomAttribute(this System.Reflection.MemberInfo element, System.Type attributeType) { return default(System.Attribute); }
        public static System.Attribute GetCustomAttribute(this System.Reflection.MemberInfo element, System.Type attributeType, bool inherit) { return default(System.Attribute); }
        public static System.Attribute GetCustomAttribute(this System.Reflection.Module element, System.Type attributeType) { return default(System.Attribute); }
        public static System.Attribute GetCustomAttribute(this System.Reflection.ParameterInfo element, System.Type attributeType) { return default(System.Attribute); }
        public static System.Attribute GetCustomAttribute(this System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { return default(System.Attribute); }
        public static T GetCustomAttribute<T>(this System.Reflection.Assembly element) where T : System.Attribute { return default(T); }
        public static T GetCustomAttribute<T>(this System.Reflection.MemberInfo element) where T : System.Attribute { return default(T); }
        public static T GetCustomAttribute<T>(this System.Reflection.MemberInfo element, bool inherit) where T : System.Attribute { return default(T); }
        public static T GetCustomAttribute<T>(this System.Reflection.Module element) where T : System.Attribute { return default(T); }
        public static T GetCustomAttribute<T>(this System.Reflection.ParameterInfo element) where T : System.Attribute { return default(T); }
        public static T GetCustomAttribute<T>(this System.Reflection.ParameterInfo element, bool inherit) where T : System.Attribute { return default(T); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.Assembly element) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.Assembly element, System.Type attributeType) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.MemberInfo element) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.MemberInfo element, bool inherit) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.MemberInfo element, System.Type attributeType) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.MemberInfo element, System.Type attributeType, bool inherit) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.Module element) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.Module element, System.Type attributeType) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.ParameterInfo element) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.ParameterInfo element, bool inherit) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.ParameterInfo element, System.Type attributeType) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { return default(System.Collections.Generic.IEnumerable<System.Attribute>); }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.Assembly element) where T : System.Attribute { return default(System.Collections.Generic.IEnumerable<T>); }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.MemberInfo element) where T : System.Attribute { return default(System.Collections.Generic.IEnumerable<T>); }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.MemberInfo element, bool inherit) where T : System.Attribute { return default(System.Collections.Generic.IEnumerable<T>); }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.Module element) where T : System.Attribute { return default(System.Collections.Generic.IEnumerable<T>); }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.ParameterInfo element) where T : System.Attribute { return default(System.Collections.Generic.IEnumerable<T>); }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.ParameterInfo element, bool inherit) where T : System.Attribute { return default(System.Collections.Generic.IEnumerable<T>); }
        public static bool IsDefined(this System.Reflection.Assembly element, System.Type attributeType) { return default(bool); }
        public static bool IsDefined(this System.Reflection.MemberInfo element, System.Type attributeType) { return default(bool); }
        public static bool IsDefined(this System.Reflection.MemberInfo element, System.Type attributeType, bool inherit) { return default(bool); }
        public static bool IsDefined(this System.Reflection.Module element, System.Type attributeType) { return default(bool); }
        public static bool IsDefined(this System.Reflection.ParameterInfo element, System.Type attributeType) { return default(bool); }
        public static bool IsDefined(this System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct InterfaceMapping
    {
        public System.Reflection.MethodInfo[] InterfaceMethods;
        public System.Type InterfaceType;
        public System.Reflection.MethodInfo[] TargetMethods;
        public System.Type TargetType;
    }
    public static partial class RuntimeReflectionExtensions
    {
        public static System.Reflection.MethodInfo GetMethodInfo(this System.Delegate del) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.MethodInfo GetRuntimeBaseDefinition(this System.Reflection.MethodInfo method) { return default(System.Reflection.MethodInfo); }
        public static System.Reflection.EventInfo GetRuntimeEvent(this System.Type type, string name) { return default(System.Reflection.EventInfo); }
        public static System.Collections.Generic.IEnumerable<System.Reflection.EventInfo> GetRuntimeEvents(this System.Type type) { return default(System.Collections.Generic.IEnumerable<System.Reflection.EventInfo>); }
        public static System.Reflection.FieldInfo GetRuntimeField(this System.Type type, string name) { return default(System.Reflection.FieldInfo); }
        public static System.Collections.Generic.IEnumerable<System.Reflection.FieldInfo> GetRuntimeFields(this System.Type type) { return default(System.Collections.Generic.IEnumerable<System.Reflection.FieldInfo>); }
        public static System.Reflection.InterfaceMapping GetRuntimeInterfaceMap(this System.Reflection.TypeInfo typeInfo, System.Type interfaceType) { return default(System.Reflection.InterfaceMapping); }
        public static System.Reflection.MethodInfo GetRuntimeMethod(this System.Type type, string name, System.Type[] parameters) { return default(System.Reflection.MethodInfo); }
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> GetRuntimeMethods(this System.Type type) { return default(System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo>); }
        public static System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetRuntimeProperties(this System.Type type) { return default(System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo>); }
        public static System.Reflection.PropertyInfo GetRuntimeProperty(this System.Type type, string name) { return default(System.Reflection.PropertyInfo); }
    }
}
