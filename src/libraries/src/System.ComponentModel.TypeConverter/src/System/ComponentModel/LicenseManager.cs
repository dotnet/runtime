// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel.Design;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides properties and methods to add a license
    /// to a component and to manage a <see cref='System.ComponentModel.LicenseProvider'/>. This class cannot be inherited.
    /// </summary>
    public sealed partial class LicenseManager
    {
        private static readonly object s_selfLock = new object();

        private static volatile LicenseContext s_context;
        private static object s_contextLockHolder;
        private static volatile Hashtable s_providers;
        private static volatile Hashtable s_providerInstances;
        private static readonly object s_internalSyncObject = new object();

        // not creatable...
        private LicenseManager()
        {
        }

        /// <summary>
        /// Gets or sets the current <see cref='System.ComponentModel.LicenseContext'/> which specifies when the licensed object can be
        /// used.
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
                            s_context = new RuntimeLicenseContext();
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
        /// Gets the <see cref='System.ComponentModel.LicenseUsageMode'/> that
        /// specifies when the licensed object can be used, for the <see cref='System.ComponentModel.LicenseManager.CurrentContext'/>.
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
        /// Caches the provider, both in the instance cache, and the type
        /// cache.
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
        /// Creates an instance of the specified type, using 
        /// creationContext
        /// as the context in which the licensed instance can be used.
        /// </summary>
        public static object CreateWithContext(Type type, LicenseContext creationContext)
        {
            return CreateWithContext(type, creationContext, Array.Empty<object>());
        }

        /// <summary>
        /// Creates an instance of the specified type with the 
        /// specified arguments, using creationContext as the context in which the licensed
        /// instance can be used.
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
                        created = Activator.CreateInstance(type, args);
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
        /// Determines if type was actually cached to have _no_ provider,
        /// as opposed to not being cached.
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
        /// Retrieves a cached instance of the provider associated with the
        /// specified type.
        /// </summary>
        private static LicenseProvider GetCachedProvider(Type type)
        {
            return (LicenseProvider)s_providers?[type];
        }


        /// <summary>
        /// Retrieves a cached instance of the provider of the specified
        /// type.
        /// </summary>
        private static LicenseProvider GetCachedProviderInstance(Type providerType)
        {
            Debug.Assert(providerType != null, "Type cannot ever be null");
            return (LicenseProvider) s_providerInstances?[providerType];
        }

        /// <summary>
        /// Determines if the given type has a valid license or not.
        /// </summary>
        public static bool IsLicensed(Type type)
        {
            Debug.Assert(type != null, "IsValid Type cannot ever be null");
            bool value = ValidateInternal(type, null, false, out License license);
            if (license != null)
            {
                license.Dispose();
                license = null;
            }
            return value;
        }

        /// <summary>
        /// Determines if a valid license can be granted for the specified type.
        /// </summary>
        public static bool IsValid(Type type)
        {
            Debug.Assert(type != null, "IsValid Type cannot ever be null");
            bool value = ValidateInternal(type, null, false, out License license);
            if (license != null)
            {
                license.Dispose();
                license = null;
            }
            return value;
        }

        /// <summary>
        /// Determines if a valid license can be granted for the 
        /// specified instance of the type. This method creates a valid <see cref='System.ComponentModel.License'/>. 
        /// </summary>
        public static bool IsValid(Type type, object instance, out License license)
        {
            return ValidateInternal(type, instance, false, out license);
        }

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
        /// Internal validation helper.
        /// </summary>
        private static bool ValidateInternal(Type type, object instance, bool allowExceptions, out License license)
        {
            return ValidateInternalRecursive(CurrentContext,
                                             type,
                                             instance,
                                             allowExceptions,
                                             out license,
                                             out string licenseKey);
        }


        /// <summary>
        /// Since we want to walk up the entire inheritance change, when not 
        /// give an instance, we need another helper method to walk up
        /// the chain...
        /// </summary>
        private static bool ValidateInternalRecursive(LicenseContext context, Type type, object instance, bool allowExceptions, out License license, out string licenseKey)
        {
            LicenseProvider provider = GetCachedProvider(type);
            if (provider == null && !GetCachedNoLicenseProvider(type))
            {
                // NOTE : Must look directly at the class, we want no inheritance.
                LicenseProviderAttribute attr = (LicenseProviderAttribute)Attribute.GetCustomAttribute(type, typeof(LicenseProviderAttribute), false);

                if (attr != null)
                {
                    Type providerType = attr.LicenseProvider;
                    provider = GetCachedProviderInstance(providerType) ?? (LicenseProvider)Activator.CreateInstance(providerType);
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
                    licenseKey = license.LicenseKey;
                }
            }

            // When looking only at a type, we need to recurse up the inheritence
            // chain, however, we can't give out the license, since this may be
            // from more than one provider.
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
        /// Determines if a license can be granted for the specified type.
        /// </summary>
        public static void Validate(Type type)
        {
            if (!ValidateInternal(type, null, true, out License lic))
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
        /// Determines if a license can be granted for the instance of the specified type.
        /// </summary>
        public static License Validate(Type type, object instance)
        {
            if (!ValidateInternal(type, instance, true, out License lic))
            {
                throw new LicenseException(type, instance);
            }

            return lic;
        }
    }
}
