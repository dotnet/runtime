// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Security.Policy;

namespace System.Runtime.InteropServices
{
    [GuidAttribute("BCA8B44D-AAD6-3A86-8AB7-03349F4F2DA2")]
    [CLSCompliant(false)]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeLibImportClassAttribute(typeof(System.Type))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _Type
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region Type Members
        Guid GUID { get; }           
        Module Module { get; }            
        Assembly Assembly { get; }            
        RuntimeTypeHandle TypeHandle { get; }            
        String FullName { get; }            
        String Namespace { get; }            
        String AssemblyQualifiedName { get; }            
        int GetArrayRank();        
        Type BaseType { get; }
            
        ConstructorInfo[] GetConstructors(BindingFlags bindingAttr);
        Type GetInterface(String name, bool ignoreCase);
        Type[] GetInterfaces();        
        Type[] FindInterfaces(TypeFilter filter,Object filterCriteria);        
        EventInfo GetEvent(String name,BindingFlags bindingAttr);        
        EventInfo[] GetEvents();
        EventInfo[] GetEvents(BindingFlags bindingAttr);
        Type[] GetNestedTypes(BindingFlags bindingAttr);
        Type GetNestedType(String name, BindingFlags bindingAttr);
        MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr);        
        MemberInfo[] GetDefaultMembers();               
        MemberInfo[] FindMembers(MemberTypes memberType,BindingFlags bindingAttr,MemberFilter filter,Object filterCriteria);    
        Type GetElementType();
        bool IsSubclassOf(Type c);        
        bool IsInstanceOfType(Object o);
        bool IsAssignableFrom(Type c);
        InterfaceMapping GetInterfaceMap(Type interfaceType);
        MethodInfo GetMethod(String name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers);
        MethodInfo GetMethod(String name, BindingFlags bindingAttr);            
        MethodInfo[] GetMethods(BindingFlags bindingAttr);    
        FieldInfo GetField(String name, BindingFlags bindingAttr);    
        FieldInfo[] GetFields(BindingFlags bindingAttr);        
        PropertyInfo GetProperty(String name, BindingFlags bindingAttr);                
        PropertyInfo GetProperty(String name,BindingFlags bindingAttr,Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers);                
        PropertyInfo[] GetProperties(BindingFlags bindingAttr);
        MemberInfo[] GetMember(String name, BindingFlags bindingAttr);
        MemberInfo[] GetMembers(BindingFlags bindingAttr);        
        Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters);
        Type UnderlyingSystemType
        {
            get;
        }       
    
        Object InvokeMember(String name,BindingFlags invokeAttr,Binder binder, Object target, Object[] args, CultureInfo culture);   
        Object InvokeMember(String name,BindingFlags invokeAttr,Binder binder, Object target, Object[] args);   
        ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,  Type[] types, ParameterModifier[] modifiers);   
        ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers);            
        ConstructorInfo GetConstructor(Type[] types);
        ConstructorInfo[] GetConstructors();
        ConstructorInfo TypeInitializer
        {
            get;
        }
            
        MethodInfo GetMethod(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);        
        MethodInfo GetMethod(String name, Type[] types, ParameterModifier[] modifiers);
        MethodInfo GetMethod(String name, Type[] types);
        MethodInfo GetMethod(String name);                
        MethodInfo[] GetMethods();
        FieldInfo GetField(String name);   
        FieldInfo[] GetFields();
        Type GetInterface(String name);
        EventInfo GetEvent(String name);           
        PropertyInfo GetProperty(String name, Type returnType, Type[] types,ParameterModifier[] modifiers);                
        PropertyInfo GetProperty(String name, Type returnType, Type[] types);                
        PropertyInfo GetProperty(String name, Type[] types);               
        PropertyInfo GetProperty(String name, Type returnType);
        PropertyInfo GetProperty(String name);            
        PropertyInfo[] GetProperties();
        Type[] GetNestedTypes();
        Type GetNestedType(String name);
        MemberInfo[] GetMember(String name);
        MemberInfo[] GetMembers();
        TypeAttributes Attributes { get; }            
        bool IsNotPublic { get; }            
        bool IsPublic { get; }            
        bool IsNestedPublic { get; }            
        bool IsNestedPrivate { get; }            
        bool IsNestedFamily { get; }            
        bool IsNestedAssembly { get; }            
        bool IsNestedFamANDAssem { get; }            
        bool IsNestedFamORAssem { get; }            
        bool IsAutoLayout { get; }            
        bool IsLayoutSequential { get; }            
        bool IsExplicitLayout { get; }            
        bool IsClass { get; }            
        bool IsInterface { get; }            
        bool IsValueType { get; }            
        bool IsAbstract { get; }            
        bool IsSealed { get; }            
        bool IsEnum { get; }            
        bool IsSpecialName { get; }            
        bool IsImport { get; }            
        bool IsSerializable { get; }            
        bool IsAnsiClass { get; }            
        bool IsUnicodeClass { get; }            
        bool IsAutoClass { get; }            
        bool IsArray { get; }            
        bool IsByRef { get; }            
        bool IsPointer { get; }            
        bool IsPrimitive { get; }            
        bool IsCOMObject { get; }            
        bool HasElementType { get; }            
        bool IsContextful { get; }          
        bool IsMarshalByRef { get; }            
        bool Equals(Type o);
        #endregion
#endif
    }

    [GuidAttribute("17156360-2f1a-384a-bc52-fde93c215c5b")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Assembly))]
    [CLSCompliant(false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _Assembly
    {
#if !FEATURE_CORECLR
        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region Assembly Members
        String CodeBase { 
#if FEATURE_CORECLR
[System.Security.SecurityCritical] // auto-generated
#endif
get; }
        String EscapedCodeBase { get; }
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        AssemblyName GetName();
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        AssemblyName GetName(bool copiedName);
        String FullName { get; }
        MethodInfo EntryPoint { get; }
        Type GetType(String name);
        Type GetType(String name, bool throwOnError);
        Type[] GetExportedTypes();
        Type[] GetTypes();
        Stream GetManifestResourceStream(Type type, String name);
        Stream GetManifestResourceStream(String name);
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        FileStream GetFile(String name);
        FileStream[] GetFiles();
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        FileStream[] GetFiles(bool getResourceModules);
        String[] GetManifestResourceNames();
        ManifestResourceInfo GetManifestResourceInfo(String resourceName);
        String Location { 
#if FEATURE_CORECLR
[System.Security.SecurityCritical] // auto-generated
#endif
get; }
#if FEATURE_CAS_POLICY
        Evidence Evidence { get; }
#endif // FEATURE_CAS_POLICY
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
#if FEATURE_SERIALIZATION
        [System.Security.SecurityCritical]  // auto-generated_required
        void GetObjectData(SerializationInfo info, StreamingContext context);
#endif
        [method: System.Security.SecurityCritical]
        event ModuleResolveEventHandler ModuleResolve;
        Type GetType(String name, bool throwOnError, bool ignoreCase);     
        Assembly GetSatelliteAssembly(CultureInfo culture);
        Assembly GetSatelliteAssembly(CultureInfo culture, Version version);
#if FEATURE_MULTIMODULE_ASSEMBLIES        
        Module LoadModule(String moduleName, byte[] rawModule);
        Module LoadModule(String moduleName, byte[] rawModule, byte[] rawSymbolStore);
#endif        
        Object CreateInstance(String typeName);
        Object CreateInstance(String typeName, bool ignoreCase);
        Object CreateInstance(String typeName, bool ignoreCase, BindingFlags bindingAttr,  Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes);
        Module[] GetLoadedModules();
        Module[] GetLoadedModules(bool getResourceModules);
        Module[] GetModules();
        Module[] GetModules(bool getResourceModules);
        Module GetModule(String name);
        AssemblyName[] GetReferencedAssemblies();
        bool GlobalAssemblyCache { get; }
        #endregion
#endif
    }


    [GuidAttribute("f7102fa9-cabb-3a74-a6da-b4567ef1b079")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.MemberInfo))]
    [CLSCompliant(false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _MemberInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion
#endif
    }


    [GuidAttribute("6240837A-707F-3181-8E98-A36AE086766B")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.MethodBase))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _MethodBase
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region MethodBase Members
        ParameterInfo[] GetParameters();
        MethodImplAttributes GetMethodImplementationFlags();
        RuntimeMethodHandle MethodHandle { get; }
        MethodAttributes Attributes { get; }
        CallingConventions CallingConvention { get; }
        Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsFamily { get; }
        bool IsAssembly { get; }
        bool IsFamilyAndAssembly { get; }
        bool IsFamilyOrAssembly { get; }
        bool IsStatic { get; }
        bool IsFinal { get; }
        bool IsVirtual { get; }
        bool IsHideBySig { get; }
        bool IsAbstract { get; }
        bool IsSpecialName { get; }      
        bool IsConstructor { get; }      
        Object Invoke(Object obj, Object[] parameters);        
        #endregion
#endif
    }


    [GuidAttribute("FFCC1B5D-ECB8-38DD-9B01-3DC8ABC2AA5F")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.MethodInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _MethodInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region MethodBase Members
        ParameterInfo[] GetParameters();
        MethodImplAttributes GetMethodImplementationFlags();
        RuntimeMethodHandle MethodHandle { get; }
        MethodAttributes Attributes { get; }
        CallingConventions CallingConvention { get; }
        Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsFamily { get; }
        bool IsAssembly { get; }
        bool IsFamilyAndAssembly { get; }
        bool IsFamilyOrAssembly { get; }
        bool IsStatic { get; }
        bool IsFinal { get; }
        bool IsVirtual { get; }
        bool IsHideBySig { get; }
        bool IsAbstract { get; }
        bool IsSpecialName { get; }      
        bool IsConstructor { get; }      
        Object Invoke(Object obj, Object[] parameters);        
        #endregion

        #region MethodInfo Members
        Type ReturnType { get; }
        ICustomAttributeProvider ReturnTypeCustomAttributes { get; }
        MethodInfo GetBaseDefinition();
        #endregion
#endif
    }
        

    [GuidAttribute("E9A19478-9646-3679-9B10-8411AE1FD57D")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.ConstructorInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _ConstructorInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region MethodBase Members
        ParameterInfo[] GetParameters();
        MethodImplAttributes GetMethodImplementationFlags();
        RuntimeMethodHandle MethodHandle { get; }
        MethodAttributes Attributes { get; }
        CallingConventions CallingConvention { get; }
        Object Invoke_2(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsFamily { get; }
        bool IsAssembly { get; }
        bool IsFamilyAndAssembly { get; }
        bool IsFamilyOrAssembly { get; }
        bool IsStatic { get; }
        bool IsFinal { get; }
        bool IsVirtual { get; }
        bool IsHideBySig { get; }
        bool IsAbstract { get; }
        bool IsSpecialName { get; }      
        bool IsConstructor { get; }      
        Object Invoke_3(Object obj, Object[] parameters);        
        #endregion

        #region ConstructorInfo
        Object Invoke_4(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        Object Invoke_5(Object[] parameters);
        #endregion
#endif
    }


    [GuidAttribute("8A7C1442-A9FB-366B-80D8-4939FFA6DBE0")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.FieldInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _FieldInfo
    {        
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region FieldInfo Members
        Type FieldType { get; }
        Object GetValue(Object obj);        
        Object GetValueDirect(TypedReference obj);                
        void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture);        
        void SetValueDirect(TypedReference obj,Object value);        
        RuntimeFieldHandle FieldHandle { get; }        
        FieldAttributes Attributes { get; }           
        void SetValue(Object obj, Object value);
        bool IsPublic { get; }
        bool IsPrivate { get; }
        bool IsFamily { get; }
        bool IsAssembly { get; }
        bool IsFamilyAndAssembly { get; }
        bool IsFamilyOrAssembly { get; }
        bool IsStatic { get; }
        bool IsInitOnly { get; }
        bool IsLiteral { get; }
        bool IsNotSerialized { get; }
        bool IsSpecialName { get; }
        bool IsPinvokeImpl { get; }
        #endregion
#endif
    }

    
    [GuidAttribute("F59ED4E4-E68F-3218-BD77-061AA82824BF")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.PropertyInfo))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _PropertyInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region Property Members
        Type PropertyType { get; }
        Object GetValue(Object obj,Object[] index);
        Object GetValue(Object obj,BindingFlags invokeAttr,Binder binder, Object[] index, CultureInfo culture);
        void SetValue(Object obj, Object value, Object[] index);
        void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture);
        MethodInfo[] GetAccessors(bool nonPublic);
        MethodInfo GetGetMethod(bool nonPublic);
        MethodInfo GetSetMethod(bool nonPublic);
        ParameterInfo[] GetIndexParameters();
        PropertyAttributes Attributes { get; }
        bool CanRead { get; }
        bool CanWrite { get; }
        MethodInfo[] GetAccessors();
        MethodInfo GetGetMethod();
        MethodInfo GetSetMethod();
        bool IsSpecialName { get; }
        #endregion
#endif
    }


    [GuidAttribute("9DE59C64-D889-35A1-B897-587D74469E5B")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.EventInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _EventInfo
    {
#if !FEATURE_CORECLR
        #region IDispatch Members
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        #endregion

        #region Object Members
        String ToString();
        bool Equals(Object other);
        int GetHashCode();
        Type GetType();
        #endregion

        #region MemberInfo Members
        MemberTypes MemberType { get; }
        String Name { get; }
        Type DeclaringType { get; }
        Type ReflectedType { get; }
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
        #endregion

        #region EventInfo Members
        MethodInfo GetAddMethod(bool nonPublic);
        MethodInfo GetRemoveMethod(bool nonPublic);
        MethodInfo GetRaiseMethod(bool nonPublic);
        EventAttributes Attributes { get; }        
        MethodInfo GetAddMethod();
        MethodInfo GetRemoveMethod();
        MethodInfo GetRaiseMethod();
        void AddEventHandler(Object target, Delegate handler);        
        void RemoveEventHandler(Object target, Delegate handler);        
        Type EventHandlerType { get; }        
        bool IsSpecialName { get; }        
        bool IsMulticast { get; }
        #endregion
#endif
    }

    [GuidAttribute("993634C4-E47A-32CC-BE08-85F567DC27D6")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.ParameterInfo))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _ParameterInfo
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("D002E9BA-D9E3-3749-B1D3-D565A08B13E7")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.Module))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _Module
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("B42B6AAC-317E-34D5-9FA9-093BB4160C50")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Reflection.AssemblyName))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _AssemblyName
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);
        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);
        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }
}

