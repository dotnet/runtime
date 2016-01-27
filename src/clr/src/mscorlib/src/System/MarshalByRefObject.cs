// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**              
**
** Purpose: Defines the root type for all marshal by reference aka
**          AppDomain bound types
**          
**
**
===========================================================*/
namespace System {
    
    using System;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Runtime.Remoting;
#if FEATURE_REMOTING    
    using System.Runtime.Remoting.Lifetime;
    using System.Runtime.Remoting.Services;
#endif
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using CultureInfo = System.Globalization.CultureInfo;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class MarshalByRefObject 
    {
#if FEATURE_REMOTING    
        private Object __identity;        

        private Object Identity { get { return __identity; } set { __identity = value; } }        
#if FEATURE_COMINTEROP
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal IntPtr GetComIUnknown(bool fIsBeingMarshalled)
        {
            IntPtr pUnk;
            if(RemotingServices.IsTransparentProxy(this))
            {
                pUnk = RemotingServices.GetRealProxy(this).GetCOMIUnknown(fIsBeingMarshalled);
            }
            else
            {
                pUnk = Marshal.GetIUnknownForObject(this);
            }
            return pUnk;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetComIUnknown(MarshalByRefObject o);
#endif // FEATURE_COMINTEROP

        // (1) for remote COM objects IsInstance of can't be executed on
        // the proxies, so we need this method to be executed on the 
        // actual object.
        // (2) for remote objects that do not have the complete type information
        // we intercept calls to check the type and execute it on the actual
        // object
        internal bool IsInstanceOfType(Type T)
        {
            return T.IsInstanceOfType(this);
        }

        // for remote COM Objects the late binding methods can't be
        // executed on proxies, so we need this method to execute on
        // the real object
        internal Object InvokeMember(String name,BindingFlags invokeAttr,Binder binder, 
                            Object[] args,ParameterModifier[] modifiers,CultureInfo culture,String[] namedParameters)
        {
            Type t = GetType();
            
            // Sanity check
            if(!t.IsCOMObject)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_InvokeMember"));

            // Call into the runtime to invoke on the COM object.
            return t.InvokeMember(name, invokeAttr, binder, this, args, modifiers, culture, namedParameters);
        }

        // Returns a new cloned MBR instance that is a memberwise copy of this 
        // with the identity nulled out, so there are no identity conflicts
        // when the cloned object is marshalled
        protected MarshalByRefObject MemberwiseClone(bool cloneIdentity)
        {
            MarshalByRefObject mbr = (MarshalByRefObject)base.MemberwiseClone();
            // set the identity on the cloned object to null
            if (!cloneIdentity)
                mbr.Identity = null;
            return mbr;
        }


        
        // A helper routine to extract the identity either from the marshalbyrefobject base
        // class if it is not a proxy, otherwise from the real proxy.
        // A flag is set to indicate whether the object passed in is a server or a proxy
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Identity GetIdentity(MarshalByRefObject obj, out bool fServer)
        {
            fServer = true;
            Identity id = null;

            if(null != obj)
            {
                if(!RemotingServices.IsTransparentProxy(obj))
                {
                    id = (Identity)obj.Identity;
                }
                else
                {                    
                    // Toggle flag to indicate that we have a proxy
                    fServer = false;
                    id = RemotingServices.GetRealProxy(obj).IdentityObject;
                }
            }

            return id;
        }

        // Another helper that delegates to the helper above
        internal static Identity GetIdentity(MarshalByRefObject obj)
        {
            Contract.Assert(!RemotingServices.IsTransparentProxy(obj), "Use this method for server objects only");

            bool fServer;            
            return GetIdentity(obj, out fServer);
        }

       
        internal ServerIdentity __RaceSetServerIdentity(ServerIdentity id)
        {
            if (__identity == null)
            {
                // For strictly MBR types, the TP field in the identity
                // holds the real server
                if (!id.IsContextBound)
                {
                    id.RaceSetTransparentProxy(this);
                }
                Interlocked.CompareExchange(ref __identity, id, null);
            }
            return (ServerIdentity)__identity;
        }


        internal void __ResetServerIdentity()
        {
            __identity = null;
        }
        
       // This method is used return a lifetime service object which
       // is used to control the lifetime policy to the object.
       // For the default Lifetime service this will be an object of typoe ILease.
       // 
        [System.Security.SecurityCritical]  // auto-generated_required
        public Object GetLifetimeService()
        {
            return LifetimeServices.GetLease(this); 
        }

       // This method is used return lifetime service object. This method
       // can be overridden to return a LifetimeService object with properties unique to
       // this object.
       // For the default Lifetime service this will be an object of type ILease.
       // 
        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual Object InitializeLifetimeService()
        {
            return LifetimeServices.GetLeaseInitial(this);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual ObjRef CreateObjRef(Type requestedType)
        {
            if(__identity == null)
            {
                throw new RemotingException(Environment.GetResourceString(
                    "Remoting_NoIdentityEntry"));
            }            
            return new ObjRef(this, requestedType);
        }

        // This is for casting interop ObjRefLite's.
        // ObjRefLite's have been deprecated. These methods are not exposed 
        // through any user APIs and would be removed in the future
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool CanCastToXmlType(String xmlTypeName, String xmlTypeNamespace)
        {
            Type castType = SoapServices.GetInteropTypeFromXmlType(xmlTypeName, xmlTypeNamespace);
            if (castType == null)
            {
                String typeNamespace;
                String assemblyName;
                if (!SoapServices.DecodeXmlNamespaceForClrTypeNamespace(xmlTypeNamespace, 
                        out typeNamespace, out assemblyName))
                    return false;

                String typeName;
                if ((typeNamespace != null) && (typeNamespace.Length > 0))
                    typeName = typeNamespace + "." + xmlTypeName;
                else
                    typeName = xmlTypeName;

                try
                {
                    Assembly asm = Assembly.Load(assemblyName);
                    castType = asm.GetType(typeName, false, false);
                }
                catch 
                {
                    return false;
                }
            }

            if (castType != null)
                return castType.IsAssignableFrom(this.GetType());

            return false;
        } // CanCastToXmlType

        // helper method for calling CanCastToXmlType
        // ObjRefLite's have been deprecated. These methods are not exposed 
        // through any user APIs and would be removed in the future
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool CanCastToXmlTypeHelper(RuntimeType castType, MarshalByRefObject o)
        {
            if (castType == null)
                throw new ArgumentNullException("castType");
        
            Contract.EndContractBlock();
            // MarshalByRefObject's can only be casted to MarshalByRefObject's or interfaces.
            if (!castType.IsInterface && !castType.IsMarshalByRef)
                return false;

            // figure out xml type name
            String xmlTypeName = null;
            String xmlTypeNamespace = null;
            if (!SoapServices.GetXmlTypeForInteropType(castType, out xmlTypeName, out xmlTypeNamespace))
            {
                // There's no registered interop type name, so just use the default.
                xmlTypeName = castType.Name;
                xmlTypeNamespace =
                    SoapServices.CodeXmlNamespaceForClrTypeNamespace(
                        castType.Namespace, castType.GetRuntimeAssembly().GetSimpleName());
            }

            return o.CanCastToXmlType(xmlTypeName, xmlTypeNamespace);
        } // CanCastToXmlType

#endif // FEATURE_REMOTING
    }            
} // namespace    
