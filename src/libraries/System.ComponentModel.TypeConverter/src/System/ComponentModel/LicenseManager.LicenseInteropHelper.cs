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
        //
        // When the CLR consumes an unmanaged COM object, the CLR invokes
        // GetCurrentContextInfo() to figure out the licensing context
        // and decide whether to call ICF::CreateInstance() (designtime) or
        // ICF::CreateInstanceLic() (runtime). In the former case, it also
        // requests the class factory for a runtime license key and invokes
        // SaveKeyInCurrentContext() to stash a copy in the current licensing
        // context
        //
        private class LicenseInteropHelper
        {
            private LicenseContext _savedLicenseContext;
            private Type _savedType;

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
        }
    }
}
