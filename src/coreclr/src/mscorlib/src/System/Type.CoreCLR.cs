// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;
using StackCrawlMark = System.Threading.StackCrawlMark;

namespace System
{
    public abstract partial class Type : MemberInfo, IReflect
    {
        public bool IsInterface
        {
            get
            {
                RuntimeType rt = this as RuntimeType;
                if (rt != null)
                    return RuntimeTypeHandle.IsInterface(rt);
                return ((GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface);
            }
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type GetType(String typeName, bool throwOnError, bool ignoreCase)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwOnError, ignoreCase, false, ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type GetType(String typeName, bool throwOnError)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwOnError, false, false, ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type GetType(String typeName)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, false, false, false, ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type GetType(
            string typeName,
            Func<AssemblyName, Assembly> assemblyResolver,
            Func<Assembly, string, bool, Type> typeResolver)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, false, false, ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Type GetType(
            string typeName,
            Func<AssemblyName, Assembly> assemblyResolver,
            Func<Assembly, string, bool, Type> typeResolver,
            bool throwOnError)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, false, ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
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

        ////////////////////////////////////////////////////////////////////////////////
        // This will return a class based upon the progID.  This is provided for 
        // COM classic support.  Program ID's are not used in COM+ because they 
        // have been superceded by namespace.  (This routine is called this instead 
        // of getClass() because of the name conflict with the first method above.)
        //
        //   param progID:     the progID of the class to retrieve
        //   returns:          the class object associated to the progID
        ////
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
        public static Type GetTypeFromCLSID(Guid clsid, String server, bool throwOnError)
        {
            return RuntimeType.GetTypeFromCLSIDImpl(clsid, server, throwOnError);
        }

        internal virtual RuntimeTypeHandle GetTypeHandleInternal()
        {
            return TypeHandle;
        }

        // Given a class handle, this will return the class for that handle.
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetTypeFromHandleUnsafe(IntPtr handle);

        [Pure]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern Type GetTypeFromHandle(RuntimeTypeHandle handle);




#if FEATURE_COMINTEROP
        internal bool IsWindowsRuntimeObject
        {
            [Pure]
            get { return IsWindowsRuntimeObjectImpl(); }
        }

        internal bool IsExportedToWindowsRuntime
        {
            [Pure]
            get { return IsExportedToWindowsRuntimeImpl(); }
        }


        // Protected routine to determine if this class represents a Windows Runtime object
        virtual internal bool IsWindowsRuntimeObjectImpl()
        {
            throw new NotImplementedException();
        }

        // Determines if this type is exported to WinRT (i.e. is an activatable class in a managed .winmd)
        virtual internal bool IsExportedToWindowsRuntimeImpl()
        {
            throw new NotImplementedException();
        }
#endif // FEATURE_COMINTEROP

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

        // This is only ever called on RuntimeType objects.
        internal string FormatTypeName()
        {
            return FormatTypeName(false);
        }

        internal virtual string FormatTypeName(bool serialization)
        {
            throw new NotImplementedException();
        }

        [Pure]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern bool operator ==(Type left, Type right);

        [Pure]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern bool operator !=(Type left, Type right);

        // Exists to faciliate code sharing between CoreCLR and CoreRT.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsRuntimeImplemented() => this is RuntimeType;
    }
}
