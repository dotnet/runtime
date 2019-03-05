// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    public sealed partial class LicenseManager
    {
        // A private implementation of a LicenseContext used for instantiating
        // managed objects exposed to COM. It has memory for the license key
        // of a single Type.
        private class CLRLicenseContext : LicenseContext
        {
            private readonly Type _type;
            private string _key;

            private CLRLicenseContext(Type type, LicenseUsageMode mode)
            {
                UsageMode = mode;
                _type = type;
            }

            public static CLRLicenseContext CreateDesignContext(Type type)
            {
                return new CLRLicenseContext(type, LicenseUsageMode.Designtime);
            }

            public static CLRLicenseContext CreateRuntimeContext(Type type, string key)
            {
                var cxt = new CLRLicenseContext(type, LicenseUsageMode.Runtime);
                if (key != null)
                {
                    cxt.SetSavedLicenseKey(type, key);
                }

                return cxt;
            }

            public override LicenseUsageMode UsageMode { get; }

            public override string GetSavedLicenseKey(Type type, Assembly resourceAssembly)
            {
                if (type == _type)
                {
                    return _key;
                }

                return null;
            }

            public override void SetSavedLicenseKey(Type type, string key)
            {
                if (type == _type)
                {
                    _key = key;
                }
            }
        }

        // Used from IClassFactory2 when retrieving LicInfo
        private class LicInfoHelperLicenseContext : LicenseContext
        {
            private Hashtable _savedLicenseKeys = new Hashtable();

            public bool Contains(string assemblyName) => _savedLicenseKeys.Contains(assemblyName);

            public override LicenseUsageMode UsageMode => LicenseUsageMode.Designtime;

            public override string GetSavedLicenseKey(Type type, Assembly resourceAssembly) => null;

            public override void SetSavedLicenseKey(Type type, string key)
            {
                _savedLicenseKeys[type.AssemblyQualifiedName] = key;
            }
        }

        // This is a helper class that supports the CLR's IClassFactory2 marshaling
        // support.
        private class LicenseInteropHelper
        {
            // Used to validate a type and retrieve license details
            // when activating a managed COM server from an IClassFactory2 instance.
            public static bool ValidateAndRetrieveLicenseDetails(
                LicenseContext context,
                Type type,
                out License license,
                out string licenseKey)
            {
                if (context == null)
                {
                    context = LicenseManager.CurrentContext;
                }

                return LicenseManager.ValidateInternalRecursive(
                    context,
                    type,
                    instance: null,
                    allowExceptions: false,
                    out license,
                    out licenseKey);
            }

            // The CLR invokes this when instantiating an unmanaged COM
            // object. The purpose is to decide which IClassFactory method to
            // use.
            public static LicenseContext GetCurrentContextInfo(Type type, out bool isDesignTime, out string key)
            {
                LicenseContext licContext = LicenseManager.CurrentContext;
                isDesignTime = licContext.UsageMode == LicenseUsageMode.Designtime;
                key = null;
                if (!isDesignTime)
                {
                    key = licContext.GetSavedLicenseKey(type, resourceAssembly: null);
                }

                return licContext;
            }
        }
    }
}
