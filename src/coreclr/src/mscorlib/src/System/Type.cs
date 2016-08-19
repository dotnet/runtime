// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Implements System.Type
//
// ======================================================================================

namespace System {

    using System;
    using System.Reflection;
    using System.Threading;
    using System.Runtime;
    using System.Runtime.Remoting;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using CultureInfo = System.Globalization.CultureInfo;
    using StackCrawlMark = System.Threading.StackCrawlMark;
    using DebuggerStepThroughAttribute = System.Diagnostics.DebuggerStepThroughAttribute;

    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_Type))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class Type : MemberInfo, _Type, IReflect
    {
        //
        // System.Type is appdomain agile type. Appdomain agile types cannot have precise static constructors. Make
        // sure to never introduce one here!
        //
        public static readonly MemberFilter FilterAttribute = new MemberFilter(__Filters.Instance.FilterAttribute);
        public static readonly MemberFilter FilterName = new MemberFilter(__Filters.Instance.FilterName);
        public static readonly MemberFilter FilterNameIgnoreCase = new MemberFilter(__Filters.Instance.FilterIgnoreCase);

        public static readonly Object Missing = System.Reflection.Missing.Value;

        public static readonly char Delimiter = '.'; 

        // EmptyTypes is used to indicate that we are looking for someting without any parameters.
        public readonly static Type[] EmptyTypes = EmptyArray<Type>.Value;

        // The Default binder.  We create a single one and expose that.
        private static Binder defaultBinder;


        protected Type() {}        


        // MemberInfo Methods....
        // The Member type Field.
        public override MemberTypes MemberType {
            get {return System.Reflection.MemberTypes.TypeInfo;}
        }

        // Return the class that declared this type.
        public override Type DeclaringType {
            get {return null;}
        }

        public virtual MethodBase DeclaringMethod { get { return null; } }

        // Return the class that was used to obtain this type.
        public override Type ReflectedType
        {
            get {return null;}
        }

        ////////////////////////////////////////////////////////////////////////////////
        // This is a static method that returns a Class based upon the name of the class
        // (this name needs to be fully qualified with the package name and is
        // case-sensitive by default).
        ////  

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Type GetType(String typeName, bool throwOnError, bool ignoreCase) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwOnError, ignoreCase, false, ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Type GetType(String typeName, bool throwOnError) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwOnError, false, false, ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Type GetType(String typeName) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, false, false, false, ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Type GetType(
            string typeName,
            Func<AssemblyName, Assembly> assemblyResolver,
            Func<Assembly, string, bool, Type> typeResolver)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, false, false, ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Type GetType(
            string typeName,
            Func<AssemblyName, Assembly> assemblyResolver,
            Func<Assembly, string, bool, Type> typeResolver,
            bool throwOnError)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, false, ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Type GetType(
            string typeName,
            Func<AssemblyName, Assembly> assemblyResolver,
            Func<Assembly, string, bool, Type> typeResolver,
            bool throwOnError,
            bool ignoreCase)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Type ReflectionOnlyGetType(String typeName, bool throwIfNotFound, bool ignoreCase) 
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwIfNotFound, ignoreCase, true /*reflectionOnly*/, ref stackMark);
        }

        public virtual Type MakePointerType() { throw new NotSupportedException(); }
        public virtual StructLayoutAttribute StructLayoutAttribute { get { throw new NotSupportedException(); } }
        public virtual Type MakeByRefType() { throw new NotSupportedException(); }
        public virtual Type MakeArrayType() { throw new NotSupportedException(); }
        public virtual Type MakeArrayType(int rank) { throw new NotSupportedException(); }

        ////////////////////////////////////////////////////////////////////////////////
        // This will return a class based upon the progID.  This is provided for 
        // COM classic support.  Program ID's are not used in COM+ because they 
        // have been superceded by namespace.  (This routine is called this instead 
        // of getClass() because of the name conflict with the first method above.)
        //
        //   param progID:     the progID of the class to retrieve
        //   returns:          the class object associated to the progID
        ////
        [System.Security.SecurityCritical]  // auto-generated_required
        public static Type GetTypeFromProgID(String progID)
        {
                return RuntimeType.GetTypeFromProgIDImpl(progID, null, false);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // This will return a class based upon the progID.  This is provided for 
        // COM classic support.  Program ID's are not used in COM+ because they 
        // have been superceded by namespace.  (This routine is called this instead 
        // of getClass() because of the name conflict with the first method above.)
        //
        //   param progID:     the progID of the class to retrieve
        //   returns:          the class object associated to the progID
        ////
        [System.Security.SecurityCritical]  // auto-generated_required
        public static Type GetTypeFromProgID(String progID, bool throwOnError)
        {
                return RuntimeType.GetTypeFromProgIDImpl(progID, null, throwOnError);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public static Type GetTypeFromProgID(String progID, String server)
        {
                return RuntimeType.GetTypeFromProgIDImpl(progID, server, false);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public static Type GetTypeFromProgID(String progID, String server, bool throwOnError)
        {
                return RuntimeType.GetTypeFromProgIDImpl(progID, server, throwOnError);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // This will return a class based upon the CLSID.  This is provided for 
        // COM classic support.  
        //
        //   param CLSID:      the CLSID of the class to retrieve
        //   returns:          the class object associated to the CLSID
        ////
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static Type GetTypeFromCLSID(Guid clsid)
        {
                return RuntimeType.GetTypeFromCLSIDImpl(clsid, null, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static Type GetTypeFromCLSID(Guid clsid, bool throwOnError)
        {
                return RuntimeType.GetTypeFromCLSIDImpl(clsid, null, throwOnError);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static Type GetTypeFromCLSID(Guid clsid, String server)
        {
                return RuntimeType.GetTypeFromCLSIDImpl(clsid, server, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static Type GetTypeFromCLSID(Guid clsid, String server, bool throwOnError)
        {
                return RuntimeType.GetTypeFromCLSIDImpl(clsid, server, throwOnError);
        }

        // GetTypeCode
        // This method will return a TypeCode for the passed
        //  type.
        public static TypeCode GetTypeCode(Type type)
        {
            if (type == null)
                return TypeCode.Empty;
            return type.GetTypeCodeImpl();
        }

        protected virtual TypeCode GetTypeCodeImpl()
        {
            // System.RuntimeType overrides GetTypeCodeInternal
            // so we can assume that this is not a runtime type

            // this is true for EnumBuilder but not the other System.Type subclasses in BCL
            if (this != UnderlyingSystemType && UnderlyingSystemType != null)
                return Type.GetTypeCode(UnderlyingSystemType);
            
            return TypeCode.Object;
        }

        // Property representing the GUID associated with a class.
        public abstract Guid GUID {
            get;
        }

        // Return the Default binder used by the system.
        static public Binder DefaultBinder {
            get {
                // Allocate the default binder if it hasn't been allocated yet.
                if (defaultBinder == null)
                    CreateBinder();
                return defaultBinder;
            }
        }

        static private void CreateBinder() 
        {
            if (defaultBinder == null)
            {
                DefaultBinder binder = new DefaultBinder();
                Interlocked.CompareExchange<Binder>(ref defaultBinder, binder, null);
            }
        }

       // Description of the Binding Process.
       // We must invoke a method that is accessable and for which the provided
       // parameters have the most specific match.  A method may be called if
       // 1. The number of parameters in the method declaration equals the number of 
       //      arguments provided to the invocation
       // 2. The type of each argument can be converted by the binder to the
       //      type of the type of the parameter.
       //      
       // The binder will find all of the matching methods.  These method are found based
       // upon the type of binding requested (MethodInvoke, Get/Set Properties).  The set
       // of methods is filtered by the name, number of arguments and a set of search modifiers
       // defined in the Binder.
       // 
       // After the method is selected, it will be invoked.  Accessability is checked
       // at that point.  The search may be control which set of methods are searched based
       // upon the accessibility attribute associated with the method.
       // 
       // The BindToMethod method is responsible for selecting the method to be invoked.
       // For the default binder, the most specific method will be selected.
       // 
       // This will invoke a specific member...

        abstract public Object InvokeMember(String name,BindingFlags invokeAttr,Binder binder,Object target,
                                    Object[] args, ParameterModifier[] modifiers,CultureInfo culture,String[] namedParameters);

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public Object InvokeMember(String name,BindingFlags invokeAttr,Binder binder, Object target, Object[] args, CultureInfo culture)
        {
            return InvokeMember(name,invokeAttr,binder,target,args,null,culture,null);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public Object InvokeMember(String name,BindingFlags invokeAttr,Binder binder, Object target, Object[] args)
        {
            return InvokeMember(name,invokeAttr,binder,target,args,null,null,null);
        }


        // Module Property associated with a class.
        // _Type.Module
        public new abstract Module Module { get; }

        // Assembly Property associated with a class.
        public abstract Assembly Assembly {
            [Pure]
            get;
        }

        // Assembly Property associated with a class.
        // A class handle is a unique integer value associated with
        // each class.  The handle is unique during the process life time.
        public virtual RuntimeTypeHandle TypeHandle
        {
            [Pure]
            get
            {
                throw new NotSupportedException();
            }
        }

        internal virtual RuntimeTypeHandle GetTypeHandleInternal() {
            return TypeHandle;
        }

        public static RuntimeTypeHandle GetTypeHandle(Object o)
        {
            if (o == null)
                throw new ArgumentNullException(null, Environment.GetResourceString("Arg_InvalidHandle"));
            return new RuntimeTypeHandle((RuntimeType)o.GetType());
        }

        // Given a class handle, this will return the class for that handle.
        [System.Security.SecurityCritical]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetTypeFromHandleUnsafe(IntPtr handle);

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern Type GetTypeFromHandle(RuntimeTypeHandle handle);


        // Return the fully qualified name.  The name does contain the namespace.
        public abstract String FullName {
            [Pure]
            get;
        }

        // Return the name space of the class.  
        public abstract String Namespace {
            [Pure]
            get;
        }


        public abstract String AssemblyQualifiedName {
            [Pure]
            get;
        }


        [Pure]
        public virtual int GetArrayRank() {
            Contract.Ensures(Contract.Result<int>() >= 0);
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        // Returns the base class for a class.  If this is an interface or has
        // no base class null is returned.  Object is the only Type that does not 
        // have a base class.  
        public abstract Type BaseType {
            [Pure]
            get;
        }


        // GetConstructor
        // This method will search for the specified constructor.  For constructors,
        //  unlike everything else, the default is to not look for static methods.  The
        //  reason is that we don't typically expose the class initializer.
        [System.Runtime.InteropServices.ComVisible(true)]
        public ConstructorInfo GetConstructor(BindingFlags bindingAttr,
                                              Binder binder,
                                              CallingConventions callConvention, 
                                              Type[] types,
                                              ParameterModifier[] modifiers)
        {               
           // Must provide some types (Type[0] for nothing)
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            for (int i=0;i<types.Length;i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetConstructorImpl(bindingAttr, binder, callConvention, types, modifiers);
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
        {
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            for (int i=0;i<types.Length;i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetConstructorImpl(bindingAttr, binder, CallingConventions.Any, types, modifiers);
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public ConstructorInfo GetConstructor(Type[] types)
        {
            // The arguments are checked in the called version of GetConstructor.
            return GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, types, null);
        }

        abstract protected ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr,
                                                              Binder binder,
                                                              CallingConventions callConvention, 
                                                              Type[] types,
                                                              ParameterModifier[] modifiers);

        // GetConstructors()
        // This routine will return an array of all constructors supported by the class.
        //  Unlike everything else, the default is to not look for static methods.  The
        //  reason is that we don't typically expose the class initializer.
        [System.Runtime.InteropServices.ComVisible(true)]
        public ConstructorInfo[] GetConstructors() {
            return GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        }
 
        [System.Runtime.InteropServices.ComVisible(true)]
        abstract public ConstructorInfo[] GetConstructors(BindingFlags bindingAttr);

        [System.Runtime.InteropServices.ComVisible(true)]
        public ConstructorInfo TypeInitializer {
            get {
                return GetConstructorImpl(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                          null,
                                          CallingConventions.Any,
                                          Type.EmptyTypes,
                                          null);
            }
        }


        // Return a method based upon the passed criteria.  The name of the method
        // must be provided, and exception is thrown if it is not.  The bindingAttr
        // parameter indicates if non-public methods should be searched.  The types
        // array indicates the types of the parameters being looked for.
        public MethodInfo GetMethod(String name,
                                    BindingFlags bindingAttr,
                                    Binder binder,
                                    CallingConventions callConvention,
                                    Type[] types,
                                    ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            for (int i = 0; i < types.Length; i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public MethodInfo GetMethod(String name,
                                    BindingFlags bindingAttr,
                                    Binder binder,
                                    Type[] types,
                                    ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            for (int i = 0; i < types.Length; i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetMethodImpl(name, bindingAttr, binder, CallingConventions.Any, types, modifiers);
        }

        public MethodInfo GetMethod(String name, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            for (int i=0;i<types.Length;i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetMethodImpl(name, Type.DefaultLookup, null, CallingConventions.Any, types, modifiers);
        }

        public MethodInfo GetMethod(String name,Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            for (int i=0;i<types.Length;i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetMethodImpl(name, Type.DefaultLookup, null, CallingConventions.Any, types, null);
        }

        public MethodInfo GetMethod(String name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            return GetMethodImpl(name, bindingAttr, null, CallingConventions.Any, null, null);
        }

        public MethodInfo GetMethod(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            return GetMethodImpl(name, Type.DefaultLookup, null, CallingConventions.Any, null, null);
        }

        abstract protected MethodInfo GetMethodImpl(String name,
                                                    BindingFlags bindingAttr,
                                                    Binder binder,
                                                    CallingConventions callConvention, 
                                                    Type[] types,
                                                    ParameterModifier[] modifiers);


        // GetMethods
        // This routine will return all the methods implemented by the class
        public MethodInfo[] GetMethods() {
            return GetMethods(Type.DefaultLookup);
        }

        abstract public MethodInfo[] GetMethods(BindingFlags bindingAttr);

        // GetField
        // Get Field will return a specific field based upon name
        abstract public FieldInfo GetField(String name, BindingFlags bindingAttr);


        public FieldInfo GetField(String name) {
            return GetField(name, Type.DefaultLookup);
        }


        // GetFields
        // Get fields will return a full array of fields implemented by a class
        public FieldInfo[] GetFields() {
            return GetFields(Type.DefaultLookup);
        }
        abstract public FieldInfo[] GetFields(BindingFlags bindingAttr);

        // GetInterface
        // This method will return an interface (as a class) based upon
        //  the passed in name.
        public Type GetInterface(String name) {
            return GetInterface(name,false);
        }
        abstract public Type GetInterface(String name, bool ignoreCase);


        // GetInterfaces
        // This method will return all of the interfaces implemented by a class
        abstract public Type[] GetInterfaces();

        // FindInterfaces
        // This method will filter the interfaces supported the class
        public virtual Type[] FindInterfaces(TypeFilter filter,Object filterCriteria)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");
            Contract.EndContractBlock();
            Type[] c = GetInterfaces();
            int cnt = 0;
            for (int i = 0;i<c.Length;i++) {
                if (!filter(c[i],filterCriteria))
                    c[i] = null;
                else
                    cnt++;
            }
            if (cnt == c.Length)
                return c;
            
            Type[] ret = new Type[cnt];
            cnt=0;
            for (int i=0;i<c.Length;i++) {
                if (c[i] != null)
                    ret[cnt++] = c[i];
            }
            return ret;
        }

        // GetEvent
        // This method will return a event by name if it is found.
        //  null is returned if the event is not found


        public EventInfo GetEvent(String name) {
            return GetEvent(name,Type.DefaultLookup);
        }
        abstract public EventInfo GetEvent(String name,BindingFlags bindingAttr);

        // GetEvents
        // This method will return an array of EventInfo.  If there are not Events
        //  an empty array will be returned.         
        virtual public EventInfo[] GetEvents() {
            return GetEvents(Type.DefaultLookup);
        }
        abstract public EventInfo[] GetEvents(BindingFlags bindingAttr);


        // Return a property based upon the passed criteria.  The nameof the
        // parameter must be provided.  
        public PropertyInfo GetProperty(String name,BindingFlags bindingAttr,Binder binder, 
                        Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            return GetPropertyImpl(name,bindingAttr,binder,returnType,types,modifiers);
        }

        public PropertyInfo GetProperty(String name, Type returnType, Type[] types,ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            return GetPropertyImpl(name,Type.DefaultLookup,null,returnType,types,modifiers);
        }

        public PropertyInfo GetProperty(String name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            return GetPropertyImpl(name,bindingAttr,null,null,null,null);
        }

        public PropertyInfo GetProperty(String name, Type returnType, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            return GetPropertyImpl(name,Type.DefaultLookup,null,returnType,types,null);
        }

        public PropertyInfo GetProperty(String name, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            return GetPropertyImpl(name,Type.DefaultLookup,null,null,types,null);
        }

        public PropertyInfo GetProperty(String name, Type returnType)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (returnType == null)
                throw new ArgumentNullException("returnType");
            Contract.EndContractBlock();
            return GetPropertyImpl(name,Type.DefaultLookup,null,returnType,null,null);
        }

        internal PropertyInfo GetProperty(String name, BindingFlags bindingAttr, Type returnType)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (returnType == null)
                throw new ArgumentNullException("returnType");
            Contract.EndContractBlock();
            return GetPropertyImpl(name, bindingAttr, null, returnType, null, null);
        }

        public PropertyInfo GetProperty(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            return GetPropertyImpl(name,Type.DefaultLookup,null,null,null,null);
        }

        protected abstract PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr,Binder binder,
                        Type returnType, Type[] types, ParameterModifier[] modifiers);


        // GetProperties
        // This method will return an array of all of the properties defined
        //  for a Type.
        abstract public PropertyInfo[] GetProperties(BindingFlags bindingAttr);
        public PropertyInfo[] GetProperties()
        {
            return GetProperties(Type.DefaultLookup);
        }
#if	!FEATURE_CORECLR
#endif	
        // GetNestedTypes()
        // This set of method will return any nested types that are found inside
        //  of the type.
        public Type[] GetNestedTypes()
        {
            return GetNestedTypes(Type.DefaultLookup);
        }

        abstract public Type[] GetNestedTypes(BindingFlags bindingAttr);

#if	!FEATURE_CORECLR
        // GetNestedType()
#endif
        public Type GetNestedType(String name)
        {
            return GetNestedType(name,Type.DefaultLookup);
        }

        abstract public Type GetNestedType(String name, BindingFlags bindingAttr);

        // GetMember
        // This method will return all of the members which match the specified string
        // passed into the method
        public MemberInfo[] GetMember(String name) {
            return GetMember(name,Type.DefaultLookup);
        }

        virtual public MemberInfo[] GetMember(String name, BindingFlags bindingAttr)
        {
            return GetMember(name,MemberTypes.All,bindingAttr);
        }
         
        virtual public MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }


        // GetMembers
        // This will return a Member array of all of the members of a class
        public MemberInfo[] GetMembers() {
            return GetMembers(Type.DefaultLookup);
        }
        abstract public MemberInfo[] GetMembers(BindingFlags bindingAttr);

        // GetDefaultMembers
        // This will return a MemberInfo that has been marked with the
        //      DefaultMemberAttribute
        public virtual MemberInfo[] GetDefaultMembers()
        {
            throw new NotImplementedException();
        }

        // FindMembers
        // This will return a filtered version of the member information
        public virtual MemberInfo[] FindMembers(MemberTypes memberType,BindingFlags bindingAttr,MemberFilter filter,Object filterCriteria)
        {
            // Define the work arrays
            MethodInfo[] m = null;
            ConstructorInfo[] c = null;
            FieldInfo[] f = null;
            PropertyInfo[] p = null;
            EventInfo[] e = null;
            Type[] t = null;
            
            int i = 0;
            int cnt = 0;            // Total Matchs
            
            // Check the methods
            if ((memberType & System.Reflection.MemberTypes.Method) != 0) {
                m = GetMethods(bindingAttr);
                if (filter != null) {
                    for (i=0;i<m.Length;i++)
                        if (!filter(m[i],filterCriteria))
                            m[i] = null;
                        else
                            cnt++;
                } else {
                    cnt+=m.Length;
                }
            }
            
            // Check the constructors
            if ((memberType & System.Reflection.MemberTypes.Constructor) != 0) {
                c = GetConstructors(bindingAttr);
                if (filter != null) {
                    for (i=0;i<c.Length;i++)
                        if (!filter(c[i],filterCriteria))
                            c[i] = null;
                        else
                            cnt++;
                } else {
                    cnt+=c.Length;
                }
            }
            
            // Check the fields
            if ((memberType & System.Reflection.MemberTypes.Field) != 0) {
                f = GetFields(bindingAttr);
                if (filter != null) {
                    for (i=0;i<f.Length;i++)
                        if (!filter(f[i],filterCriteria))
                            f[i] = null;
                        else
                            cnt++;
                } else {
                    cnt+=f.Length;
                }
            }
            
            // Check the Properties
            if ((memberType & System.Reflection.MemberTypes.Property) != 0) {
                p = GetProperties(bindingAttr);
                if (filter != null) {
                    for (i=0;i<p.Length;i++)
                        if (!filter(p[i],filterCriteria))
                            p[i] = null;
                        else
                            cnt++;
                } else {
                    cnt+=p.Length;
                }
            }
            
            // Check the Events
            if ((memberType & System.Reflection.MemberTypes.Event) != 0) {
                e = GetEvents(bindingAttr);
                if (filter != null) {
                    for (i=0;i<e.Length;i++)
                        if (!filter(e[i],filterCriteria))
                            e[i] = null;
                        else
                            cnt++;
                } else {
                    cnt+=e.Length;
                }
            }
            
            // Check the Types
            if ((memberType & System.Reflection.MemberTypes.NestedType) != 0) {
                t = GetNestedTypes(bindingAttr);
                if (filter != null) {
                    for (i=0;i<t.Length;i++)
                        if (!filter(t[i],filterCriteria))
                            t[i] = null;
                        else
                            cnt++;
                } else {
                    cnt+=t.Length;
                }
            }
            
            // Allocate the Member Info
            MemberInfo[] ret = new MemberInfo[cnt];
            
            // Copy the Methods
            cnt = 0;
            if (m != null) {
                for (i=0;i<m.Length;i++)
                    if (m[i] != null)
                        ret[cnt++] = m[i];
            }
            
            // Copy the Constructors
            if (c != null) {
                for (i=0;i<c.Length;i++)
                    if (c[i] != null)
                        ret[cnt++] = c[i];
            }
            
            // Copy the Fields
            if (f != null) {
                for (i=0;i<f.Length;i++)
                    if (f[i] != null)
                        ret[cnt++] = f[i];
            }
            
            // Copy the Properties
            if (p != null) {
                for (i=0;i<p.Length;i++)
                    if (p[i] != null)
                        ret[cnt++] = p[i];
            }
            
            // Copy the Events
            if (e != null) {
                for (i=0;i<e.Length;i++)
                    if (e[i] != null)
                        ret[cnt++] = e[i];
            }
            
            // Copy the Types
            if (t != null) {
                for (i=0;i<t.Length;i++)
                    if (t[i] != null)
                        ret[cnt++] = t[i];
            }
            
            return ret;
        }

    ////////////////////////////////////////////////////////////////////////////////
    //
    // Attributes
    //
    //   The attributes are all treated as read-only properties on a class.  Most of
    //  these boolean properties have flag values defined in this class and act like
    //  a bit mask of attributes.  There are also a set of boolean properties that
    //  relate to the classes relationship to other classes and to the state of the
    //  class inside the runtime.
    //
    ////////////////////////////////////////////////////////////////////////////////

        public bool IsNested 
        {
            [Pure]
            get 
            {
                return DeclaringType != null; 
            }
        }

        // The attribute property on the Type.
        public TypeAttributes Attributes     {
            [Pure]
            get {return GetAttributeFlagsImpl();}
        }

        public virtual GenericParameterAttributes GenericParameterAttributes
        {
            get { throw new NotSupportedException(); }
        }

        public bool IsVisible
        {
            [Pure]
            get 
            {
                RuntimeType rt = this as RuntimeType;
                if (rt != null)
                    return RuntimeTypeHandle.IsVisible(rt);

                if (IsGenericParameter)
                    return true;

                if (HasElementType)
                    return GetElementType().IsVisible;

                Type type = this;
                while (type.IsNested)
                {
                    if (!type.IsNestedPublic)
                        return false;

                    // this should be null for non-nested types.
                    type = type.DeclaringType;
                }

                // Now "type" should be a top level type
                if (!type.IsPublic)
                    return false;

                if (IsGenericType && !IsGenericTypeDefinition)
                {
                    foreach (Type t in GetGenericArguments())
                    {
                        if (!t.IsVisible)
                            return false;
                    }
                }

                return true;
            }
        }

        public bool IsNotPublic
        {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic);}
        }

        public bool IsPublic {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.Public);}
        }

        public bool IsNestedPublic {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic);}
        }

        public bool IsNestedPrivate {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate);}
        }
        public bool IsNestedFamily {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily);}
        }
        public bool IsNestedAssembly {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly);}
        }
        public bool IsNestedFamANDAssem {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem);}
        }
        public bool IsNestedFamORAssem{
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem);}
        }

        public bool IsAutoLayout {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout);}
        }
        public bool IsLayoutSequential {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout);}
        }
        public bool IsExplicitLayout {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout);}
        }

        public bool IsClass {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class && !IsValueType);}
        }

        public bool IsInterface {
            [Pure]
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                RuntimeType rt = this as RuntimeType;
                if (rt != null)
                    return RuntimeTypeHandle.IsInterface(rt);

                return ((GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface);
            }
        }

        public bool IsValueType {
            [Pure]
            get {return IsValueTypeImpl();}
        }

        public bool IsAbstract {
            [Pure]
             get { return ((GetAttributeFlagsImpl() & TypeAttributes.Abstract) != 0); }
         }
         
        public bool IsSealed {
            [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.Sealed) != 0);}
        }       
        
#if FEATURE_CORECLR
         public bool IsEnum {
#else
         public virtual bool IsEnum {
#endif
             [Pure]
             get
             {
                // This will return false for a non-runtime Type object unless it overrides IsSubclassOf.
                return IsSubclassOf(RuntimeType.EnumType);
             }
         }
        
         public bool IsSpecialName {
             [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.SpecialName) != 0);}
       }

         public bool IsImport {
             [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.Import) != 0);}
        }

        public virtual bool IsSerializable
        {
            [Pure]
            get
            {
                if ((GetAttributeFlagsImpl() & TypeAttributes.Serializable) != 0)
                    return true;

                RuntimeType rt = this.UnderlyingSystemType as RuntimeType;

                if (rt != null)
                    return rt.IsSpecialSerializableType();

                return false;
            }
        }

         public bool IsAnsiClass {
             [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass);}
        }

         public bool IsUnicodeClass {
             [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass);}
        }

         public bool IsAutoClass {
             [Pure]
            get {return ((GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass);}
        }
                
         // These are not backed up by attributes.  Instead they are implemented
         //      based internally.
         public bool IsArray {
             [Pure]
             get {return IsArrayImpl();}
         }

         internal virtual bool IsSzArray {
             [Pure]
            get {return false;}
        }

         public virtual bool IsGenericType {
             [Pure]
            get { return false; }
        }

         public virtual bool IsGenericTypeDefinition {
             [Pure]
            get { return false; }
        }

        public virtual bool IsConstructedGenericType
        {
            [Pure]
            get { throw new NotImplementedException(); }
        }

        public virtual bool IsGenericParameter
        {
            [Pure]
            get { return false; }
        }

         public virtual int GenericParameterPosition {
             [Pure]
            get {throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter")); }
        }

        public virtual bool ContainsGenericParameters 
        {
            [Pure]
            get 
            {
                if (HasElementType)
                    return GetRootElementType().ContainsGenericParameters;

                if (IsGenericParameter)
                    return true;
            
                if (!IsGenericType)
                    return false;

                Type[] genericArguments = GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (genericArguments[i].ContainsGenericParameters)
                        return true;
                }

                return false;
            }
        }

        [Pure]
        public virtual Type[] GetGenericParameterConstraints()
        {
            if (!IsGenericParameter)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
            Contract.EndContractBlock();

            throw new InvalidOperationException();
        }

         public bool IsByRef {
             [Pure]
            get {return IsByRefImpl();}
        }
         public bool IsPointer {
             [Pure]
            get {return IsPointerImpl();}
        }
         public bool IsPrimitive {
             [Pure]
            get {return IsPrimitiveImpl();}
        }
         public bool IsCOMObject {
             [Pure]
            get {return IsCOMObjectImpl();}
        }

#if FEATURE_COMINTEROP
        internal bool IsWindowsRuntimeObject {
            [Pure]
            get { return IsWindowsRuntimeObjectImpl(); }
        }

        internal bool IsExportedToWindowsRuntime {
            [Pure]
            get { return IsExportedToWindowsRuntimeImpl(); }
        }
#endif // FEATURE_COMINTEROP

         public bool HasElementType {
             [Pure]
             get {return HasElementTypeImpl();}
         }

         public bool IsContextful {
             [Pure]
            get {return IsContextfulImpl();}
        }

         public bool IsMarshalByRef {
             [Pure]
             get {return IsMarshalByRefImpl();}
         }

         internal bool HasProxyAttribute {
             [Pure]
            get {return HasProxyAttributeImpl();}
        }
                       
        // Protected routine to determine if this class represents a value class
        // The default implementation of IsValueTypeImpl never returns true for non-runtime types.
        protected virtual bool IsValueTypeImpl()
        {
            // Note that typeof(Enum) and typeof(ValueType) are not themselves value types.
            // But there is no point excluding them here because customer derived System.Type 
            // (non-runtime type) objects can never be equal to a runtime type, which typeof(XXX) is.
            // Ideally we should throw a NotImplementedException here or just return false because
            // customer implementations of IsSubclassOf should never return true between a non-runtime
            // type and a runtime type. There is no benefits in making that breaking change though.

            return IsSubclassOf(RuntimeType.ValueType);
        }

        // Protected routine to get the attributes.
        abstract protected TypeAttributes GetAttributeFlagsImpl();
            
        // Protected routine to determine if this class represents an Array
        abstract protected bool IsArrayImpl();

        // Protected routine to determine if this class is a ByRef
        abstract protected bool IsByRefImpl();

        // Protected routine to determine if this class is a Pointer
        abstract protected bool IsPointerImpl();
            
        // Protected routine to determine if this class represents a primitive type
        abstract protected bool IsPrimitiveImpl();
            
        // Protected routine to determine if this class represents a COM object
        abstract protected bool IsCOMObjectImpl();

#if FEATURE_COMINTEROP
        // Protected routine to determine if this class represents a Windows Runtime object
        virtual internal bool IsWindowsRuntimeObjectImpl() {
            throw new NotImplementedException();
        }

        // Determines if this type is exported to WinRT (i.e. is an activatable class in a managed .winmd)
        virtual internal bool IsExportedToWindowsRuntimeImpl() {
            throw new NotImplementedException();
        }
#endif // FEATURE_COMINTEROP

        public virtual Type MakeGenericType(params Type[] typeArguments) {
            Contract.Ensures(Contract.Result<Type>() != null);
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

    
        // Protected routine to determine if this class is contextful
        protected virtual bool IsContextfulImpl(){
            return typeof(ContextBoundObject).IsAssignableFrom(this);
        }
    

        // Protected routine to determine if this class is marshaled by ref
        protected virtual bool IsMarshalByRefImpl(){
            return typeof(MarshalByRefObject).IsAssignableFrom(this);
        }

        internal virtual bool HasProxyAttributeImpl()
        {
            // We will override this in RuntimeType
            return false;
        }

        [Pure]
        abstract public Type GetElementType();

        [Pure]
        public virtual Type[] GetGenericArguments() 
        { 
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        public virtual Type[] GenericTypeArguments{
            get{
                if(IsGenericType && !IsGenericTypeDefinition){
                    return GetGenericArguments();
                }
                else{
                    return Type.EmptyTypes;
                }

            }
        }

        [Pure]
        public virtual Type GetGenericTypeDefinition() 
        {
            Contract.Ensures(Contract.Result<Type>() != null);
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        [Pure]
        abstract protected bool HasElementTypeImpl();

        internal Type GetRootElementType()
        {
            Type rootElementType = this;

            while (rootElementType.HasElementType)
                rootElementType = rootElementType.GetElementType();

            return rootElementType;
        }

        #region Enum methods

        // Default implementations of GetEnumNames, GetEnumValues, and GetEnumUnderlyingType
        // Subclass of types can override these methods.

        public virtual string[] GetEnumNames()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.Ensures(Contract.Result<String[]>() != null);

            string[] names;
            Array values;
            GetEnumData(out names, out values);
            return names;
        }

        // We don't support GetEnumValues in the default implementation because we cannot create an array of
        // a non-runtime type. If there is strong need we can consider returning an object or int64 array.
        public virtual Array GetEnumValues()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.Ensures(Contract.Result<Array>() != null);

            throw new NotImplementedException();
        }

        // Returns the enum values as an object array.
        private Array GetEnumRawConstantValues()
        {
            string[] names;
            Array values;
            GetEnumData(out names, out values);
            return values;
        }

        // This will return enumValues and enumNames sorted by the values.
        private void GetEnumData(out string[] enumNames, out Array enumValues)
        {
            Contract.Ensures(Contract.ValueAtReturn<String[]>(out enumNames) != null);
            Contract.Ensures(Contract.ValueAtReturn<Array>(out enumValues) != null);

            FieldInfo[] flds = GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            object[] values = new object[flds.Length];
            string[] names = new string[flds.Length];

            for (int i = 0; i < flds.Length; i++)
            {
                names[i] = flds[i].Name;
                values[i] = flds[i].GetRawConstantValue();
            }

            // Insertion Sort these values in ascending order.
            // We use this O(n^2) algorithm, but it turns out that most of the time the elements are already in sorted order and
            // the common case performance will be faster than quick sorting this.
            IComparer comparer = Comparer.Default;
            for (int i = 1; i < values.Length; i++)
            {
                int j = i;
                string tempStr = names[i];
                object val = values[i];
                bool exchanged = false;

                // Since the elements are sorted we only need to do one comparision, we keep the check for j inside the loop.
                while (comparer.Compare(values[j - 1], val) > 0)
                {
                    names[j] = names[j - 1];
                    values[j] = values[j - 1];
                    j--;
                    exchanged = true;
                    if (j == 0)
                        break;
                }

                if (exchanged)
                {
                    names[j] = tempStr;
                    values[j] = val;
                }
            }

            enumNames = names;
            enumValues = values;
        }

        public virtual Type GetEnumUnderlyingType()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.Ensures(Contract.Result<Type>() != null);

            FieldInfo[] fields = GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fields == null || fields.Length != 1)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEnum"), "enumType");

            return fields[0].FieldType;
        }

        public virtual bool IsEnumDefined(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();

            // Check if both of them are of the same type
            Type valueType = value.GetType();

            // If the value is an Enum then we need to extract the underlying value from it
            if (valueType.IsEnum)
            {
                if (!valueType.IsEquivalentTo(this))
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumAndObjectMustBeSameType", valueType.ToString(), this.ToString()));

                valueType = valueType.GetEnumUnderlyingType();
            }

            // If a string is passed in
            if (valueType == typeof(string))
            {
                string[] names = GetEnumNames();
                if (Array.IndexOf(names, value) >= 0)
                    return true;
                else
                    return false;
            }

            // If an enum or integer value is passed in
            if (Type.IsIntegerType(valueType))
            {
                Type underlyingType = GetEnumUnderlyingType();
                // We cannot compare the types directly because valueType is always a runtime type but underlyingType might not be.
                if (underlyingType.GetTypeCodeImpl() != valueType.GetTypeCodeImpl())
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumUnderlyingTypeAndObjectMustBeSameType", valueType.ToString(), underlyingType.ToString()));

                Array values = GetEnumRawConstantValues();
                return (BinarySearch(values, value) >= 0);
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
            }
        }

        public virtual string GetEnumName(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();

            Type valueType = value.GetType();

            if (!(valueType.IsEnum || Type.IsIntegerType(valueType)))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnumBaseTypeOrEnum"), "value");

            Array values = GetEnumRawConstantValues();
            int index = BinarySearch(values, value);

            if (index >= 0)
            {
                string[] names = GetEnumNames();
                return names[index];
            }

            return null;
        }

        // Convert everything to ulong then perform a binary search.
        private static int BinarySearch(Array array, object value)
        {
            ulong[] ulArray = new ulong[array.Length];
            for (int i = 0; i < array.Length; ++i)
                ulArray[i] = Enum.ToUInt64(array.GetValue(i));

            ulong ulValue = Enum.ToUInt64(value);

            return Array.BinarySearch(ulArray, ulValue);
        }

        internal static bool IsIntegerType(Type t)
        {
            return (t == typeof(int) ||
                    t == typeof(short) ||
                    t == typeof(ushort) ||
                    t == typeof(byte) ||
                    t == typeof(sbyte) ||
                    t == typeof(uint) ||
                    t == typeof(long) ||
                    t == typeof(ulong) ||
                    t == typeof(char) ||
                    t == typeof(bool));
        }
        #endregion

        public virtual bool IsSecurityCritical { [Pure] get { throw new NotImplementedException(); } }

        public virtual bool IsSecuritySafeCritical { [Pure] get { throw new NotImplementedException(); } }

        public virtual bool IsSecurityTransparent { [Pure] get { throw new NotImplementedException(); } }

        internal bool NeedsReflectionSecurityCheck
        {
            get
            {
                if (!IsVisible)
                {
                    // Types which are not externally visible require security checks
                    return true;
                }
                else if (IsSecurityCritical && !IsSecuritySafeCritical)
                {
                    // Critical types require security checks
                    return true;
                }
                else if (IsGenericType)
                {
                    // If any of the generic arguments to this type require a security check, then this type
                    // also requires one.
                    foreach (Type genericArgument in GetGenericArguments())
                    {
                        if (genericArgument.NeedsReflectionSecurityCheck)
                        {
                            return true;
                        }
                    }
                }
                else if (IsArray || IsPointer)
                {
                    return GetElementType().NeedsReflectionSecurityCheck;
                }

                return false;
            }
        }

        // The behavior of UnderlyingSystemType varies from type to type.
        // For IReflect objects: Return the underlying Type that represents the IReflect Object.
        // For expando object: this is the (Object) IReflectInstance.GetType().  For Type object it is this.
        // It could also return the baked type or the underlying enum type in RefEmit. See the comment in
        // code:TypeBuilder.SetConstantValue.
        public abstract Type UnderlyingSystemType {
            get;
        }       

        // Returns true of this class is a true subclass of c.  Everything 
        // else returns false.  If this class and c are the same class false is
        // returned.
        // 
        [System.Runtime.InteropServices.ComVisible(true)]
        [Pure]
        public virtual bool IsSubclassOf(Type c)
        {
            Type p = this;
            if (p == c)
                return false;
            while (p != null) {
                if (p == c)
                    return true;
                p = p.BaseType;
            }
            return false;
        }
        
        // Returns true if the object passed is assignable to an instance of this class.
        // Everything else returns false. 
        // 
        [Pure]
        public virtual bool IsInstanceOfType(Object o) 
        {
            if (o == null)
                return false;

            // No need for transparent proxy casting check here
            // because it never returns true for a non-rutnime type.
            
            return IsAssignableFrom(o.GetType());
        }
        
        // Returns true if an instance of Type c may be assigned
        // to an instance of this class.  Return false otherwise.
        // 
        [Pure]
        public virtual bool IsAssignableFrom(Type c)
        {
            if (c == null)
                return false;

            if (this == c)
                return true;

            // For backward-compatibility, we need to special case for the types
            // whose UnderlyingSystemType are RuntimeType objects. 
            RuntimeType toType = this.UnderlyingSystemType as RuntimeType;
            if (toType != null)
                return toType.IsAssignableFrom(c);

            // If c is a subclass of this class, then c can be cast to this type.
            if (c.IsSubclassOf(this))
                return true;

            if (this.IsInterface)
            {
                return c.ImplementInterface(this);
            }
            else if (IsGenericParameter)
            {
                Type[] constraints = GetGenericParameterConstraints();
                for (int i = 0; i < constraints.Length; i++)
                    if (!constraints[i].IsAssignableFrom(c))
                        return false;

                return true;
            }

            return false;
        }

        // Base implementation that does only ==.
        [Pure]
        public virtual bool IsEquivalentTo(Type other)
        {
            return (this == other);
        }

        internal bool ImplementInterface(Type ifaceType)
        {
            Contract.Requires(ifaceType != null);
            Contract.Requires(ifaceType.IsInterface, "ifaceType must be an interface type");

            Type t = this;
            while (t != null)
            {
                Type[] interfaces = t.GetInterfaces();
                if (interfaces != null)
                {
                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        // Interfaces don't derive from other interfaces, they implement them.
                        // So instead of IsSubclassOf, we should use ImplementInterface instead.
                        if (interfaces[i] == ifaceType || 
                            (interfaces[i] != null && interfaces[i].ImplementInterface(ifaceType)))
                            return true;
                    }
                }

                t = t.BaseType;
            }

            return false;
        }

        // This is only ever called on RuntimeType objects.
        internal string FormatTypeName()
        {
            return FormatTypeName(false);
        }

        internal virtual string FormatTypeName(bool serialization)
        {
            throw new NotImplementedException();
        }

        // ToString
        // Print the String Representation of the Type
        public override String ToString()
        {
            // Why do we add the "Type: " prefix? RuntimeType.ToString() doesn't include it.
            return "Type: " + Name;
        }

        // This method will return an array of classes based upon the array of 
        // types.
        public static Type[] GetTypeArray(Object[] args) {
            if (args == null)
                throw new ArgumentNullException("args");
            Contract.EndContractBlock();
            Type[] cls = new Type[args.Length];
            for (int i = 0;i < cls.Length;i++)
            {
                if (args[i] == null)
                    throw new ArgumentNullException();
                cls[i] = args[i].GetType();
            }
            return cls;
        }

        [Pure]
        public override bool Equals(Object o)
        {
            if (o == null)
                return false;

            return Equals(o as Type);
        }

        // _Type.Equals(Type)
        [Pure]
#if !FEATURE_CORECLR
        public virtual bool Equals(Type o)
#else
        public bool Equals(Type o)
#endif
        {
            if ((object)o == null)
                return false;

            return (Object.ReferenceEquals(this.UnderlyingSystemType, o.UnderlyingSystemType));
        }

        [System.Security.SecuritySafeCritical]
        [Pure]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern bool operator ==(Type left, Type right);

        [System.Security.SecuritySafeCritical]
        [Pure]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern bool operator !=(Type left, Type right);

        public override int GetHashCode()
        {
            Type SystemType = UnderlyingSystemType;
            if (!Object.ReferenceEquals(SystemType, this))
                return SystemType.GetHashCode();
            return base.GetHashCode();
        }


        // GetInterfaceMap
        // This method will return an interface mapping for the interface
        //  requested.  It will throw an argument exception if the Type doesn't
        //  implemenet the interface.
        [System.Runtime.InteropServices.ComVisible(true)]
        public virtual InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        // this method is required so Object.GetType is not made virtual by the compiler 
        // _Type.GetType()
        public new Type GetType()
        {
            return base.GetType();
        }

#if !FEATURE_CORECLR
        void _Type.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _Type.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _Type.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        // If you implement this method, make sure to include _Type.Invoke in VM\DangerousAPIs.h and 
        // include _Type in SystemDomain::IsReflectionInvocationMethod in AppDomain.cpp.
        void _Type.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif

        // private convenience data
        private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        internal const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
}
}
