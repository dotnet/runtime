// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    public sealed partial class LicenseManager
    {
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
            // on a managed that has a LicenseProvider custom attribute.
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
                    _helperContext._savedLicenseKeys.Clear();
                }
                if (LicenseManager.ValidateInternalRecursive(_helperContext, type, null, false, out license, out licenseKey))
                {
                    if (_helperContext._savedLicenseKeys.Contains(type.AssemblyQualifiedName))
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
                private Type _type;
                private string _key;
                public CLRLicenseContext(LicenseUsageMode usageMode, Type type)
                {
                    UsageMode = usageMode;
                    _type = type;
                }
                public override LicenseUsageMode UsageMode { get; }
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
