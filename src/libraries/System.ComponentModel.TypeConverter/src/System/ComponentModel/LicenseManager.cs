// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System;
using System.Text;
using System.Collections;
using System.ComponentModel.Design;
using Microsoft.Win32;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides properties and methods to add a license
    ///       to a component and to manage a <see cref='System.ComponentModel.LicenseProvider'/>. This class cannot be inherited.</para>
    /// </summary>
    public sealed class LicenseManager
    {
        private static readonly object s_selfLock = new object();

        private static volatile LicenseContext s_context = null;
        private static object s_contextLockHolder = null;
        private static volatile Hashtable s_providers;
        private static volatile Hashtable s_providerInstances;
        private static object s_internalSyncObject = new object();

        // not creatable...
        //
        private LicenseManager()
        {
        }


        /// <summary>
        ///    <para>
        ///       Gets or sets the current <see cref='System.ComponentModel.LicenseContext'/> which specifies when the licensed object can be
        ///       used.
        ///    </para>
        /// </summary>
        public static LicenseContext CurrentContext
        {
            get
            {
                if (s_context == null)
                {
                    lock (s_internalSyncObject)
                    {
                        if (s_context == null)
                        {
                            s_context = new System.ComponentModel.Design.RuntimeLicenseContext();
                        }
                    }
                }
                return s_context;
            }
            set
            {
                lock (s_internalSyncObject)
                {
                    if (s_contextLockHolder != null)
                    {
                        throw new InvalidOperationException(SR.LicMgrContextCannotBeChanged);
                    }
                    s_context = value;
                }
            }
        }


        /// <summary>
        /// <para>Gets the <see cref='System.ComponentModel.LicenseUsageMode'/> that
        ///    specifies when the licensed object can be used, for the <see cref='System.ComponentModel.LicenseManager.CurrentContext'/>.</para>
        /// </summary>
        public static LicenseUsageMode UsageMode
        {
            get
            {
                if (s_context != null)
                {
                    return s_context.UsageMode;
                }
                return LicenseUsageMode.Runtime;
            }
        }


        /// <summary>
        ///     Caches the provider, both in the instance cache, and the type
        ///     cache.
        /// </summary>
        private static void CacheProvider(Type type, LicenseProvider provider)
        {
            if (s_providers == null)
            {
                s_providers = new Hashtable();
            }
            s_providers[type] = provider;

            if (provider != null)
            {
                if (s_providerInstances == null)
                {
                    s_providerInstances = new Hashtable();
                }
                s_providerInstances[provider.GetType()] = provider;
            }
        }


        /// <summary>
        ///    <para>Creates an instance of the specified type, using 
        ///       creationContext
        ///       as the context in which the licensed instance can be used.</para>
        /// </summary>
        public static object CreateWithContext(Type type, LicenseContext creationContext)
        {
            return CreateWithContext(type, creationContext, new object[0]);
        }


        /// <summary>
        ///    <para>Creates an instance of the specified type with the 
        ///       specified arguments, using creationContext as the context in which the licensed
        ///       instance can be used.</para>
        /// </summary>
        public static object CreateWithContext(Type type, LicenseContext creationContext, object[] args)
        {
            object created = null;

            lock (s_internalSyncObject)
            {
                LicenseContext normal = CurrentContext;
                try
                {
                    CurrentContext = creationContext;
                    LockContext(s_selfLock);
                    try
                    {
                        created = SecurityUtils.SecureCreateInstance(type, args);
                    }
                    catch (TargetInvocationException e)
                    {
                        throw e.InnerException;
                    }
                }
                finally
                {
                    UnlockContext(s_selfLock);
                    CurrentContext = normal;
                }
            }

            return created;
        }


        /// <summary>
        ///     Determines if type was actually cached to have _no_ provider,
        ///     as opposed to not being cached.
        /// </summary>
        private static bool GetCachedNoLicenseProvider(Type type)
        {
            if (s_providers != null)
            {
                return s_providers.ContainsKey(type);
            }
            return false;
        }


        /// <summary>
        ///     Retrieves a cached instance of the provider associated with the
        ///     specified type.
        /// </summary>
        private static LicenseProvider GetCachedProvider(Type type)
        {
            if (s_providers != null)
            {
                return (LicenseProvider)s_providers[type];
            }
            return null;
        }


        /// <summary>
        ///     Retrieves a cached instance of the provider of the specified
        ///     type.
        /// </summary>
        private static LicenseProvider GetCachedProviderInstance(Type providerType)
        {
            Debug.Assert(providerType != null, "Type cannot ever be null");
            if (s_providerInstances != null)
            {
                return (LicenseProvider)s_providerInstances[providerType];
            }
            return null;
        }

        /// <summary>
        ///    <para>Determines if the given type has a valid license or not.</para>
        /// </summary>
        public static bool IsLicensed(Type type)
        {
            Debug.Assert(type != null, "IsValid Type cannot ever be null");
            License license;
            bool value = ValidateInternal(type, null, false, out license);
            if (license != null)
            {
                license.Dispose();
                license = null;
            }
            return value;
        }


        /// <summary>
        ///    <para>Determines if a valid license can be granted for the specified type.</para>
        /// </summary>
        public static bool IsValid(Type type)
        {
            Debug.Assert(type != null, "IsValid Type cannot ever be null");
            License license;
            bool value = ValidateInternal(type, null, false, out license);
            if (license != null)
            {
                license.Dispose();
                license = null;
            }
            return value;
        }



        /// <summary>
        ///    <para>Determines if a valid license can be granted for the 
        ///       specified instance of the type. This method creates a valid <see cref='System.ComponentModel.License'/>. </para>
        /// </summary>
        public static bool IsValid(Type type, object instance, out License license)
        {
            return ValidateInternal(type, instance, false, out license);
        }


        /// <summary>
        /// </summary>
        public static void LockContext(object contextUser)
        {
            lock (s_internalSyncObject)
            {
                if (s_contextLockHolder != null)
                {
                    throw new InvalidOperationException(SR.LicMgrAlreadyLocked);
                }
                s_contextLockHolder = contextUser;
            }
        }


        /// <summary>
        /// </summary>
        public static void UnlockContext(object contextUser)
        {
            lock (s_internalSyncObject)
            {
                if (s_contextLockHolder != contextUser)
                {
                    throw new ArgumentException(SR.LicMgrDifferentUser);
                }
                s_contextLockHolder = null;
            }
        }


        /// <summary>
        ///     Internal validation helper.
        /// </summary>
        private static bool ValidateInternal(Type type, object instance, bool allowExceptions, out License license)
        {
            string licenseKey;
            return ValidateInternalRecursive(CurrentContext,
                                             type,
                                             instance,
                                             allowExceptions,
                                             out license,
                                             out licenseKey);
        }


        /// <summary>
        ///     Since we want to walk up the entire inheritance change, when not 
        ///     give an instance, we need another helper method to walk up
        ///     the chain...
        /// </summary>
        private static bool ValidateInternalRecursive(LicenseContext context, Type type, object instance, bool allowExceptions, out License license, out string licenseKey)
        {
            LicenseProvider provider = GetCachedProvider(type);
            if (provider == null && !GetCachedNoLicenseProvider(type))
            {
                // NOTE : Must look directly at the class, we want no inheritance.
                //

                LicenseProviderAttribute attr = (LicenseProviderAttribute)Attribute.GetCustomAttribute(type, typeof(LicenseProviderAttribute), false);

                if (attr != null)
                {
                    Type providerType = attr.LicenseProvider;
                    provider = GetCachedProviderInstance(providerType);

                    if (provider == null)
                    {
                        provider = (LicenseProvider)SecurityUtils.SecureCreateInstance(providerType);
                    }
                }

                CacheProvider(type, provider);
            }

            license = null;
            bool isValid = true;

            licenseKey = null;
            if (provider != null)
            {
                license = provider.GetLicense(context, type, instance, allowExceptions);
                if (license == null)
                {
                    isValid = false;
                }
                else
                {
                    // For the case where a COM client is calling "RequestLicKey", 
                    // we try to squirrel away the first found license key
                    //
                    licenseKey = license.LicenseKey;
                }
            }

            // When looking only at a type, we need to recurse up the inheritence
            // chain, however, we can't give out the license, since this may be
            // from more than one provider.
            //
            if (isValid && instance == null)
            {
                Type baseType = type.BaseType;
                if (baseType != typeof(object) && baseType != null)
                {
                    if (license != null)
                    {
                        license.Dispose();
                        license = null;
                    }
                    string temp;
                    isValid = ValidateInternalRecursive(context, baseType, null, allowExceptions, out license, out temp);
                    if (license != null)
                    {
                        license.Dispose();
                        license = null;
                    }
                }
            }

            return isValid;
        }


        /// <summary>
        ///    <para>Determines if a license can be granted for the specified type.</para>
        /// </summary>
        public static void Validate(Type type)
        {
            License lic;

            if (!ValidateInternal(type, null, true, out lic))
            {
                throw new LicenseException(type);
            }

            if (lic != null)
            {
                lic.Dispose();
                lic = null;
            }
        }


        /// <summary>
        ///    <para>Determines if a license can be granted for the instance of the specified type.</para>
        /// </summary>
        public static License Validate(Type type, object instance)
        {
            License lic;

            if (!ValidateInternal(type, instance, true, out lic))
            {
                throw new LicenseException(type, instance);
            }

            return lic;
        }

        // FxCop complaint about uninstantiated internal classes
        // Re-activate if this class proves to be useful.

        // This is a helper class that supports the CLR's IClassFactory2 marshaling
        // support.
        //
        // When a managed object is exposed to COM, the CLR invokes
        // AllocateAndValidateLicense() to set up the appropriate
        // license context and instantiate the object.
        //
        // When the CLR consumes an unmanaged COM object, the CLR invokes
        // GetCurrentContextInfo() to figure out the licensing context
        // and decide whether to call ICF::CreateInstance() (designtime) or
        // ICF::CreateInstanceLic() (runtime). In the former case, it also
        // requests the class factory for a runtime license key and invokes
        // SaveKeyInCurrentContext() to stash a copy in the current licensing
        // context
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class LicenseInteropHelper
        {
            // Define some common HRESULTs.
            private const int S_OK = 0;
            private const int E_NOTIMPL = unchecked((int)0x80004001);
            private const int CLASS_E_NOTLICENSED = unchecked((int)0x80040112);
            private const int E_FAIL = unchecked((int)0x80000008);

            private DesigntimeLicenseContext _helperContext;
            private LicenseContext _savedLicenseContext;
            private Type _savedType;

            // The CLR invokes this whenever a COM client invokes
            // IClassFactory::CreateInstance() or IClassFactory2::CreateInstanceLic()
            // on a managed managed that has a LicenseProvider custom attribute.
            //
            // If we are being entered because of a call to ICF::CreateInstance(),
            // fDesignTime will be "true".
            //
            // If we are being entered because of a call to ICF::CreateInstanceLic(),
            // fDesignTime will be "false" and bstrKey will point a non-null
            // license key.
            private static object AllocateAndValidateLicense(RuntimeTypeHandle rth, IntPtr bstrKey, int fDesignTime)
            {
                Type type = Type.GetTypeFromHandle(rth);
                CLRLicenseContext licensecontext = new CLRLicenseContext(fDesignTime != 0 ? LicenseUsageMode.Designtime : LicenseUsageMode.Runtime, type);
                if (fDesignTime == 0 && bstrKey != (IntPtr)0)
                {
                    licensecontext.SetSavedLicenseKey(type, Marshal.PtrToStringBSTR(bstrKey));
                }


                try
                {
                    return LicenseManager.CreateWithContext(type, licensecontext);
                }
                catch (LicenseException lexp)
                {
                    throw new COMException(lexp.Message, CLASS_E_NOTLICENSED);
                }
            }

            // The CLR invokes this whenever a COM client invokes
            // IClassFactory2::RequestLicKey on a managed class.
            //
            // This method should return the appropriate HRESULT and set pbstrKey
            // to the licensing key.
            private static int RequestLicKey(RuntimeTypeHandle rth, ref IntPtr pbstrKey)
            {
                Type type = Type.GetTypeFromHandle(rth);
                License license;
                string licenseKey;

                // license will be null, since we passed no instance,
                // however we can still retrieve the "first" license
                // key from the file. This really will only
                // work for simple COM-compatible license providers
                // like LicFileLicenseProvider that don't require the
                // instance to grant a key.
                //
                if (!LicenseManager.ValidateInternalRecursive(LicenseManager.CurrentContext,
                                                              type,
                                                              null,
                                                              false,
                                                              out license,
                                                              out licenseKey))
                {
                    return E_FAIL;
                }

                if (licenseKey == null)
                {
                    return E_FAIL;
                }

                pbstrKey = Marshal.StringToBSTR(licenseKey);

                if (license != null)
                {
                    license.Dispose();
                    license = null;
                }

                return S_OK;
            }


            // The CLR invokes this whenever a COM client invokes
            // IClassFactory2::GetLicInfo on a managed class.
            //
            // COM normally doesn't expect this function to fail so this method
            // should only throw in the case of a catastrophic error (stack, memory, etc.)
            private void GetLicInfo(RuntimeTypeHandle rth, ref int pRuntimeKeyAvail, ref int pLicVerified)
            {
                pRuntimeKeyAvail = 0;
                pLicVerified = 0;

                Type type = Type.GetTypeFromHandle(rth);
                License license;
                string licenseKey;

                if (_helperContext == null)
                {
                    _helperContext = new DesigntimeLicenseContext();
                }
                else
                {
                    _helperContext.savedLicenseKeys.Clear();
                }

                if (LicenseManager.ValidateInternalRecursive(_helperContext, type, null, false, out license, out licenseKey))
                {
                    if (_helperContext.savedLicenseKeys.Contains(type.AssemblyQualifiedName))
                    {
                        pRuntimeKeyAvail = 1;
                    }

                    if (license != null)
                    {
                        license.Dispose();
                        license = null;

                        pLicVerified = 1;
                    }
                }
            }

            // The CLR invokes this when instantiating an unmanaged COM
            // object. The purpose is to decide which classfactory method to
            // use.
            //
            // If the current context is design time, the CLR will
            // use ICF::CreateInstance().
            //
            // If the current context is runtime and the current context
            // exposes a non-null license key and the COM object supports
            // IClassFactory2, the CLR will use ICF2::CreateInstanceLic().
            // Otherwise, the CLR will use ICF::CreateInstance.
            //
            // Arguments:
            //    ref int fDesignTime:   on exit, this will be set to indicate
            //                           the nature of the current license context.
            //    ref int bstrKey:       on exit, this will point to the
            //                           licensekey saved inside the license context.
            //                           (only if the license context is runtime)
            //    RuntimeTypeHandle rth: the managed type of the wrapper
            private void GetCurrentContextInfo(ref int fDesignTime, ref IntPtr bstrKey, RuntimeTypeHandle rth)
            {
                _savedLicenseContext = LicenseManager.CurrentContext;
                _savedType = Type.GetTypeFromHandle(rth);
                if (_savedLicenseContext.UsageMode == LicenseUsageMode.Designtime)
                {
                    fDesignTime = 1;
                    bstrKey = (IntPtr)0;
                }
                else
                {
                    fDesignTime = 0;
                    String key = _savedLicenseContext.GetSavedLicenseKey(_savedType, null);
                    bstrKey = Marshal.StringToBSTR(key);
                }
            }

            // The CLR invokes this when instantiating a licensed COM
            // object inside a designtime license context.
            // It's purpose is to save away the license key that the CLR
            // retrieved using RequestLicKey(). This license key can be NULL.
            private void SaveKeyInCurrentContext(IntPtr bstrKey)
            {
                if (bstrKey != (IntPtr)0)
                {
                    _savedLicenseContext.SetSavedLicenseKey(_savedType, Marshal.PtrToStringBSTR(bstrKey));
                }
            }

            // A private implementation of a LicenseContext used for instantiating
            // managed objects exposed to COM. It has memory for the license key
            // of a single Type.
            internal class CLRLicenseContext : LicenseContext
            {
                private LicenseUsageMode _usageMode;
                private Type _type;
                private string _key;

                public CLRLicenseContext(LicenseUsageMode usageMode, Type type)
                {
                    _usageMode = usageMode;
                    _type = type;
                }

                public override LicenseUsageMode UsageMode
                {
                    get
                    {
                        return _usageMode;
                    }
                }


                public override string GetSavedLicenseKey(Type type, Assembly resourceAssembly)
                {
                    return type == _type ? _key : null;
                }
                public override void SetSavedLicenseKey(Type type, string key)
                {
                    if (type == _type)
                    {
                        _key = key;
                    }
                }
            }
        }
    }
}
