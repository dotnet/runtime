// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;

// We need to target netstandard2.0, so keep using ref for MemoryMarshal.Write
// CS9191: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
#pragma warning disable CS9191

namespace WbemClient_v1 { }
namespace WbemUtilities_v1 { }

namespace System.Management
{
    #region FreeThreadedInterfaces
    internal sealed class IWbemClassObjectFreeThreaded : IDisposable
    {
        //
        // This is to force load wminet_utils.dll as a COM component. Since wminet_utils.dll
        // lives in the version framework directory, a direct DllImport will try to load the
        // wminet_utils.dll from the currently set environment path. In certain cases, such as
        // redist install this path will not be set and we get a DllNotFound exception thrown.
        // By forcing the DLL to be loaded as a COM DLL, mscoree will go to the registry and get
        // the correct path and succeed in loading the DLL. Once the DllImport occurs, the DLL will
        // already be in the cache.
        //
        private static readonly string name = typeof(IWbemClassObjectFreeThreaded).FullName;
        public static Guid IID_IWbemClassObject = new Guid("DC12A681-737F-11CF-884D-00AA004B2E24");

        private IntPtr pWbemClassObject = IntPtr.Zero;

        public IWbemClassObjectFreeThreaded(IntPtr pWbemClassObject)
        {
            // This instance will now own a single ref count on pWbemClassObject
            this.pWbemClassObject = pWbemClassObject;
        }

        public static implicit operator IntPtr(IWbemClassObjectFreeThreaded wbemClassObject)
        {
            if (null == wbemClassObject)
                return IntPtr.Zero;
            return wbemClassObject.pWbemClassObject;
        }

        public void Dispose()
        {
            Dispose_(false);
        }

        private void Dispose_(bool finalization)
        {
            if (pWbemClassObject != IntPtr.Zero)
            {
                Marshal.Release(pWbemClassObject);
                pWbemClassObject = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        ~IWbemClassObjectFreeThreaded()
        {
            Dispose_(true);
        }

        // Interface methods
        public int GetQualifierSet_(out IWbemQualifierSetFreeThreaded ppQualSet)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            IntPtr pQualSet;
            int hResult = WmiNetUtilsHelper.GetQualifierSet_f(3, pWbemClassObject, out pQualSet);
            if (hResult < 0)
                ppQualSet = null;
            else
                ppQualSet = new IWbemQualifierSetFreeThreaded(pQualSet);
            GC.KeepAlive(this);
            return hResult;
        }
        public int Get_(string wszName, int lFlags, ref object pVal, ref int pType, ref int plFlavor)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int hr = WmiNetUtilsHelper.Get_f(4, pWbemClassObject, wszName, lFlags, ref pVal, ref pType, ref plFlavor);
            // In certain cases some instances (events and out params from method invocations)
            // do not have a __PATH property.  Unfortunately, GetNames says the object DOES have a __PATH
            // property.  Going under the assumption that __PATH should always exist, we make a slight fixup
            // if we detect a missing __PATH
            if (hr == (int)ManagementStatus.InvalidObject)
            {
                if (string.Equals(wszName, "__path", StringComparison.OrdinalIgnoreCase))
                {
                    hr = 0;
                    pType = (int)tag_CIMTYPE_ENUMERATION.CIM_STRING;
                    plFlavor = (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_ORIGIN_SYSTEM;
                    pVal = DBNull.Value;
                }
            }
            GC.KeepAlive(this);
            return hr;
        }
        public int Put_(string wszName, int lFlags, ref object pVal, int Type)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.Put_f(5, pWbemClassObject, wszName, lFlags, ref pVal, Type);
            GC.KeepAlive(this);
            return res;
        }
        public int Delete_(string wszName)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.Delete_f(6, pWbemClassObject, wszName);
            GC.KeepAlive(this);
            return res;
        }
        public int GetNames_(string wszQualifierName, int lFlags, ref object pQualifierVal, out string[] pNames)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.GetNames_f(7, pWbemClassObject, wszQualifierName, lFlags, ref pQualifierVal, out pNames);
            GC.KeepAlive(this);
            return res;

        }
        public int BeginEnumeration_(int lEnumFlags)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.BeginEnumeration_f(8, pWbemClassObject, lEnumFlags);
            GC.KeepAlive(this);
            return res;
        }
        public int Next_(int lFlags, ref string strName, ref object pVal, ref int pType, ref int plFlavor)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            pVal = null;
            strName = null;
            int res = WmiNetUtilsHelper.Next_f(9, pWbemClassObject, lFlags, ref strName, ref pVal, ref pType, ref plFlavor);
            GC.KeepAlive(this);
            return res;
        }
        public int EndEnumeration_()
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.EndEnumeration_f(10, pWbemClassObject);
            GC.KeepAlive(this);
            return res;
        }
        public int GetPropertyQualifierSet_(string wszProperty, out IWbemQualifierSetFreeThreaded ppQualSet)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            IntPtr pQualSet;
            int hResult = WmiNetUtilsHelper.GetPropertyQualifierSet_f(11, pWbemClassObject, wszProperty, out pQualSet);
            if (hResult < 0)
                ppQualSet = null;
            else
                ppQualSet = new IWbemQualifierSetFreeThreaded(pQualSet);

            GC.KeepAlive(this);
            return hResult;
        }
        public int Clone_(out IWbemClassObjectFreeThreaded ppCopy)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            IntPtr pWbemClassObjectNew;
            int hResult = WmiNetUtilsHelper.Clone_f(12, pWbemClassObject, out pWbemClassObjectNew);
            if (hResult < 0)
                ppCopy = null;
            else
                ppCopy = new IWbemClassObjectFreeThreaded(pWbemClassObjectNew);

            GC.KeepAlive(this);
            return hResult;
        }
        public int GetObjectText_(int lFlags, out string pstrObjectText)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.GetObjectText_f(13, pWbemClassObject, lFlags, out pstrObjectText);
            GC.KeepAlive(this);
            return res;
        }
        public int SpawnDerivedClass_(int lFlags, out IWbemClassObjectFreeThreaded ppNewClass)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            IntPtr pWbemClassObjectNew;
            int hResult = WmiNetUtilsHelper.SpawnDerivedClass_f(14, pWbemClassObject, lFlags, out pWbemClassObjectNew);
            if (hResult < 0)
                ppNewClass = null;
            else
                ppNewClass = new IWbemClassObjectFreeThreaded(pWbemClassObjectNew);

            GC.KeepAlive(this);
            return hResult;
        }
        public int SpawnInstance_(int lFlags, out IWbemClassObjectFreeThreaded ppNewInstance)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            IntPtr pWbemClassObjectNew;
            int hResult = WmiNetUtilsHelper.SpawnInstance_f(15, pWbemClassObject, lFlags, out pWbemClassObjectNew);
            if (hResult < 0)
                ppNewInstance = null;
            else
                ppNewInstance = new IWbemClassObjectFreeThreaded(pWbemClassObjectNew);

            GC.KeepAlive(this);
            return hResult;
        }
        public int CompareTo_(int lFlags, IWbemClassObjectFreeThreaded pCompareTo)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.CompareTo_f(16, pWbemClassObject, lFlags, pCompareTo.pWbemClassObject);
            GC.KeepAlive(this);
            return res;
        }
        public int GetPropertyOrigin_(string wszName, out string pstrClassName)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.GetPropertyOrigin_f(17, pWbemClassObject, wszName, out pstrClassName);
            GC.KeepAlive(this);
            return res;
        }
        public int InheritsFrom_(string strAncestor)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.InheritsFrom_f(18, pWbemClassObject, strAncestor);
            GC.KeepAlive(this);
            return res;
        }
        public int GetMethod_(string wszName, int lFlags, out IWbemClassObjectFreeThreaded ppInSignature, out IWbemClassObjectFreeThreaded ppOutSignature)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            IntPtr pInSignature;
            IntPtr pOutSignature;
            int hResult = WmiNetUtilsHelper.GetMethod_f(19, pWbemClassObject, wszName, lFlags, out pInSignature, out pOutSignature);
            ppInSignature = null;
            ppOutSignature = null;
            if (hResult >= 0)
            {
                // This can be NULL
                if (pInSignature != IntPtr.Zero)
                    ppInSignature = new IWbemClassObjectFreeThreaded(pInSignature);
                if (pOutSignature != IntPtr.Zero)
                    ppOutSignature = new IWbemClassObjectFreeThreaded(pOutSignature);
            }

            GC.KeepAlive(this);
            return hResult;
        }
        public int PutMethod_(string wszName, int lFlags, IWbemClassObjectFreeThreaded pInSignature, IWbemClassObjectFreeThreaded pOutSignature)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.PutMethod_f(20, pWbemClassObject, wszName, lFlags, pInSignature, pOutSignature);
            GC.KeepAlive(this);
            return res;
        }
        public int DeleteMethod_(string wszName)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.DeleteMethod_f(21, pWbemClassObject, wszName);
            GC.KeepAlive(this);
            return res;
        }
        public int BeginMethodEnumeration_(int lEnumFlags)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.BeginMethodEnumeration_f(22, pWbemClassObject, lEnumFlags);
            GC.KeepAlive(this);
            return res;
        }
        public int NextMethod_(int lFlags, out string pstrName, out IWbemClassObjectFreeThreaded ppInSignature, out IWbemClassObjectFreeThreaded ppOutSignature)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);

            IntPtr pInSignature;
            IntPtr pOutSignature;
            int hResult = WmiNetUtilsHelper.NextMethod_f(23, pWbemClassObject, lFlags, out pstrName, out pInSignature, out pOutSignature);
            ppInSignature = null;
            ppOutSignature = null;
            if (hResult >= 0)
            {
                // This can be NULL
                if (pInSignature != IntPtr.Zero)
                    ppInSignature = new IWbemClassObjectFreeThreaded(pInSignature);
                if (pOutSignature != IntPtr.Zero)
                    ppOutSignature = new IWbemClassObjectFreeThreaded(pOutSignature);
            }
            GC.KeepAlive(this);
            return hResult;
        }
        public int EndMethodEnumeration_()
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.EndMethodEnumeration_f(24, pWbemClassObject);
            GC.KeepAlive(this);
            return res;
        }
        public int GetMethodQualifierSet_(string wszMethod, out IWbemQualifierSetFreeThreaded ppQualSet)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            IntPtr pQualSet;
            int hResult = WmiNetUtilsHelper.GetMethodQualifierSet_f(25, pWbemClassObject, wszMethod, out pQualSet);
            if (hResult < 0)
                ppQualSet = null;
            else
                ppQualSet = new IWbemQualifierSetFreeThreaded(pQualSet);
            GC.KeepAlive(this);
            return hResult;
        }
        public int GetMethodOrigin_(string wszMethodName, out string pstrClassName)
        {
            if (pWbemClassObject == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.GetMethodOrigin_f(26, pWbemClassObject, wszMethodName, out pstrClassName);
            GC.KeepAlive(this);
            return res;
        }

        private enum STATFLAG
        {
            STATFLAG_DEFAULT = 0,
            STATFLAG_NONAME  = 1
        }

        private enum MSHCTX
        {
            MSHCTX_LOCAL            = 0,
            MSHCTX_NOSHAREDMEM      = 1,
            MSHCTX_DIFFERENTMACHINE = 2,
            MSHCTX_INPROC           = 3
        }

        private enum MSHLFLAGS
        {
            MSHLFLAGS_NORMAL      = 0,
            MSHLFLAGS_TABLESTRONG = 1,
            MSHLFLAGS_TABLEWEAK   = 2,
            MSHLFLAGS_NOPING      = 3
        }
    }

    internal sealed class IWbemQualifierSetFreeThreaded : IDisposable
    {
        private static readonly string name = typeof(IWbemQualifierSetFreeThreaded).FullName;
        public static Guid IID_IWbemClassObject = new Guid("DC12A681-737F-11CF-884D-00AA004B2E24");

        private IntPtr pWbemQualifierSet = IntPtr.Zero;
        public IWbemQualifierSetFreeThreaded(IntPtr pWbemQualifierSet)
        {
            // This instance will now own a single ref count on pWbemClassObject
            this.pWbemQualifierSet = pWbemQualifierSet;
        }

        public void Dispose()
        {
            Dispose_(false);
        }

        private void Dispose_(bool finalization)
        {
            if (pWbemQualifierSet != IntPtr.Zero)
            {
                Marshal.Release(pWbemQualifierSet);
                pWbemQualifierSet = IntPtr.Zero;
            }
            if (finalization == false)
            {
                GC.KeepAlive(this);
            }
            GC.SuppressFinalize(this);
        }


        ~IWbemQualifierSetFreeThreaded()
        {
            Dispose_(true);
        }

        public int Get_(string wszName, int lFlags, ref object pVal, ref int plFlavor)
        {
            if (pWbemQualifierSet == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.QualifierGet_f(3, pWbemQualifierSet, wszName, lFlags, ref pVal, ref plFlavor);
            GC.KeepAlive(this);
            return res;

        }
        public int Put_(string wszName, ref object pVal, int lFlavor)
        {
            if (pWbemQualifierSet == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.QualifierPut_f(4, pWbemQualifierSet, wszName, ref pVal, lFlavor);
            GC.KeepAlive(this);
            return res;
        }
        public int Delete_(string wszName)
        {
            if (pWbemQualifierSet == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.QualifierDelete_f(5, pWbemQualifierSet, wszName);
            GC.KeepAlive(this);
            return res;
        }
        public int GetNames_(int lFlags, out string[] pNames)
        {
            if (pWbemQualifierSet == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.QualifierGetNames_f(6, pWbemQualifierSet, lFlags, out pNames);
            GC.KeepAlive(this);
            return res;
        }
        public int BeginEnumeration_(int lFlags)
        {
            if (pWbemQualifierSet == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.QualifierBeginEnumeration_f(7, pWbemQualifierSet, lFlags);
            GC.KeepAlive(this);
            return res;
        }
        public int Next_(int lFlags, out string pstrName, out object pVal, out int plFlavor)
        {
            if (pWbemQualifierSet == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.QualifierNext_f(8, pWbemQualifierSet, lFlags, out pstrName, out pVal, out plFlavor);
            GC.KeepAlive(this);
            return res;
        }
        public int EndEnumeration_()
        {
            if (pWbemQualifierSet == IntPtr.Zero)
                throw new ObjectDisposedException(name);
            int res = WmiNetUtilsHelper.QualifierEndEnumeration_f(9, pWbemQualifierSet);
            GC.KeepAlive(this);
            return res;
        }
    }

    internal sealed class MarshalWbemObject : ICustomMarshaler
    {
        public static ICustomMarshaler GetInstance(string cookie)
        {
            return new MarshalWbemObject(cookie);
        }

        private readonly string cookie;
        private MarshalWbemObject(string cookie)
        {
            this.cookie = cookie;
        }

        public void CleanUpManagedData(object obj)
        {
        }

        public void CleanUpNativeData(IntPtr pObj)
        {
        }

        public int GetNativeDataSize()
        {
            return -1; // not a value type, so use -1
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            return (IntPtr)obj;
        }

        public object MarshalNativeToManaged(IntPtr pObj)
        {
            return new IWbemClassObjectFreeThreaded(pObj);
        }
    }
    #endregion

    #region Interfaces
    [InterfaceTypeAttribute(0x0001)]
    //[TypeLibTypeAttribute(0x0200)]
    [GuidAttribute("DC12A681-737F-11CF-884D-00AA004B2E24")]
    [ComImport]
    internal interface IWbemClassObject_DoNotMarshal
    {
        [PreserveSig] int GetQualifierSet_([Out][MarshalAs(UnmanagedType.Interface)]  out IWbemQualifierSet_DoNotMarshal ppQualSet);
        [PreserveSig] int Get_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [In][Out] ref object pVal, [In][Out] ref int pType, [In][Out] ref int plFlavor);
        [PreserveSig] int Put_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [In] ref object pVal, [In] int Type);
        [PreserveSig] int Delete_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName);
        [PreserveSig] int GetNames_([In][MarshalAs(UnmanagedType.LPWStr)] string wszQualifierName, [In] int lFlags, [In] ref object pQualifierVal, [Out][MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]  out string[] pNames);
        [PreserveSig] int BeginEnumeration_([In] int lEnumFlags);
        [PreserveSig] int Next_([In] int lFlags, [In][Out][MarshalAs(UnmanagedType.BStr)] ref string strName, [In][Out] ref object pVal, [In][Out] ref int pType, [In][Out] ref int plFlavor);
        [PreserveSig] int EndEnumeration_();
        [PreserveSig] int GetPropertyQualifierSet_([In][MarshalAs(UnmanagedType.LPWStr)] string wszProperty, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemQualifierSet_DoNotMarshal ppQualSet);
        [PreserveSig] int Clone_([Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppCopy);
        [PreserveSig] int GetObjectText_([In] int lFlags, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrObjectText);
        [PreserveSig] int SpawnDerivedClass_([In] int lFlags, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppNewClass);
        [PreserveSig] int SpawnInstance_([In] int lFlags, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppNewInstance);
        [PreserveSig] int CompareTo_([In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemClassObject_DoNotMarshal pCompareTo);
        [PreserveSig] int GetPropertyOrigin_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrClassName);
        [PreserveSig] int InheritsFrom_([In][MarshalAs(UnmanagedType.LPWStr)] string strAncestor);
        [PreserveSig] int GetMethod_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppInSignature, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppOutSignature);
        [PreserveSig] int PutMethod_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemClassObject_DoNotMarshal pInSignature, [In][MarshalAs(UnmanagedType.Interface)]  IWbemClassObject_DoNotMarshal pOutSignature);
        [PreserveSig] int DeleteMethod_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName);
        [PreserveSig] int BeginMethodEnumeration_([In] int lEnumFlags);
        [PreserveSig] int NextMethod_([In] int lFlags, [In][Out][MarshalAs(UnmanagedType.BStr)]  ref string pstrName, [In][Out][MarshalAs(UnmanagedType.Interface)]  ref IWbemClassObject_DoNotMarshal ppInSignature, [In][Out][MarshalAs(UnmanagedType.Interface)]  ref IWbemClassObject_DoNotMarshal ppOutSignature);
        [PreserveSig] int EndMethodEnumeration_();
        [PreserveSig] int GetMethodQualifierSet_([In][MarshalAs(UnmanagedType.LPWStr)] string wszMethod, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemQualifierSet_DoNotMarshal ppQualSet);
        [PreserveSig] int GetMethodOrigin_([In][MarshalAs(UnmanagedType.LPWStr)] string wszMethodName, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrClassName);
    }

    [InterfaceTypeAttribute(0x0001)]
    [GuidAttribute("DC12A680-737F-11CF-884D-00AA004B2E24")]
    //[TypeLibTypeAttribute(0x0200)]
    [ComImport]
    internal interface IWbemQualifierSet_DoNotMarshal
    {
        [PreserveSig] int Get_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [In][Out] ref object pVal, [In][Out] ref int plFlavor);
        [PreserveSig] int Put_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] ref object pVal, [In] int lFlavor);
        [PreserveSig] int Delete_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName);
        [PreserveSig] int GetNames_([In] int lFlags, [Out][MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]  out string[] pNames);
        [PreserveSig] int BeginEnumeration_([In] int lFlags);
        [PreserveSig] int Next_([In] int lFlags, [In][Out][MarshalAs(UnmanagedType.BStr)]  ref string pstrName, [In][Out] ref object pVal, [In][Out] ref int plFlavor);
        [PreserveSig] int EndEnumeration_();
    }

    [GuidAttribute("44ACA674-E8FC-11D0-A07C-00C04FB68820")]
    //[TypeLibTypeAttribute(0x0200)]
    [InterfaceTypeAttribute(0x0001)]
    [ComImport]
    internal interface IWbemContext
    {
        [PreserveSig] int Clone_([Out][MarshalAs(UnmanagedType.Interface)]  out IWbemContext ppNewCopy);
        [PreserveSig] int GetNames_([In] int lFlags, [Out][MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]  out string[] pNames);
        [PreserveSig] int BeginEnumeration_([In] int lFlags);
        [PreserveSig] int Next_([In] int lFlags, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrName, [Out] out object pValue);
        [PreserveSig] int EndEnumeration_();
        [PreserveSig] int SetValue_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [In] ref object pValue);
        [PreserveSig] int GetValue_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [Out] out object pValue);
        [PreserveSig] int DeleteValue_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags);
        [PreserveSig] int DeleteAll_();
    }

    [InterfaceTypeAttribute(0x0001)]
    //[TypeLibTypeAttribute(0x0200)]
    [GuidAttribute("9556DC99-828C-11CF-A37E-00AA003240C7")]
    [ComImport]
    internal interface IWbemServices
    {
        [PreserveSig] int OpenNamespace_([In][MarshalAs(UnmanagedType.BStr)] string strNamespace, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][Out][MarshalAs(UnmanagedType.Interface)]  ref IWbemServices ppWorkingNamespace, [In] IntPtr ppCallResult);
        [PreserveSig] int CancelAsyncCall_([In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pSink);
        [PreserveSig] int QueryObjectSink_([In] int lFlags, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemObjectSink ppResponseHandler);
        [PreserveSig] int GetObject_([In][MarshalAs(UnmanagedType.BStr)] string strObjectPath, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [Out][MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(MarshalWbemObject))] out IWbemClassObjectFreeThreaded ppObject, [In] IntPtr ppCallResult);
        [PreserveSig] int GetObjectAsync_([In][MarshalAs(UnmanagedType.BStr)] string strObjectPath, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
        [PreserveSig] int PutClass_([In] IntPtr pObject, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In] IntPtr ppCallResult);
        [PreserveSig] int PutClassAsync_([In] IntPtr pObject, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
        [PreserveSig] int DeleteClass_([In][MarshalAs(UnmanagedType.BStr)] string strClass, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In] IntPtr ppCallResult);
        [PreserveSig] int DeleteClassAsync_([In][MarshalAs(UnmanagedType.BStr)] string strClass, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
        [PreserveSig] int CreateClassEnum_([In][MarshalAs(UnmanagedType.BStr)] string strSuperclass, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum);
        [PreserveSig] int CreateClassEnumAsync_([In][MarshalAs(UnmanagedType.BStr)] string strSuperclass, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
        [PreserveSig] int PutInstance_([In] IntPtr pInst, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In] IntPtr ppCallResult);
        [PreserveSig] int PutInstanceAsync_([In] IntPtr pInst, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
        [PreserveSig] int DeleteInstance_([In][MarshalAs(UnmanagedType.BStr)] string strObjectPath, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In] IntPtr ppCallResult);
        [PreserveSig] int DeleteInstanceAsync_([In][MarshalAs(UnmanagedType.BStr)] string strObjectPath, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
        [PreserveSig] int CreateInstanceEnum_([In][MarshalAs(UnmanagedType.BStr)] string strFilter, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum);
        [PreserveSig] int CreateInstanceEnumAsync_([In][MarshalAs(UnmanagedType.BStr)] string strFilter, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
        [PreserveSig] int ExecQuery_([In][MarshalAs(UnmanagedType.BStr)] string strQueryLanguage, [In][MarshalAs(UnmanagedType.BStr)] string strQuery, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum);
        [PreserveSig] int ExecQueryAsync_([In][MarshalAs(UnmanagedType.BStr)] string strQueryLanguage, [In][MarshalAs(UnmanagedType.BStr)] string strQuery, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
        [PreserveSig] int ExecNotificationQuery_([In][MarshalAs(UnmanagedType.BStr)] string strQueryLanguage, [In][MarshalAs(UnmanagedType.BStr)] string strQuery, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum);
        [PreserveSig] int ExecNotificationQueryAsync_([In][MarshalAs(UnmanagedType.BStr)] string strQueryLanguage, [In][MarshalAs(UnmanagedType.BStr)] string strQuery, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
        [PreserveSig] int ExecMethod_([In][MarshalAs(UnmanagedType.BStr)] string strObjectPath, [In][MarshalAs(UnmanagedType.BStr)] string strMethodName, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In] IntPtr pInParams, [Out][MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(MarshalWbemObject))] out IWbemClassObjectFreeThreaded ppOutParams, [In] IntPtr ppCallResult);
        [PreserveSig] int ExecMethodAsync_([In][MarshalAs(UnmanagedType.BStr)] string strObjectPath, [In][MarshalAs(UnmanagedType.BStr)] string strMethodName, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [In] IntPtr pInParams, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pResponseHandler);
    }

    [GuidAttribute("44ACA675-E8FC-11D0-A07C-00C04FB68820")]
    //[TypeLibTypeAttribute(0x0200)]
    [InterfaceTypeAttribute(0x0001)]
    [ComImport]
    internal interface IWbemCallResult
    {
        [PreserveSig] int GetResultObject_([In] int lTimeout, [Out][MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(MarshalWbemObject))] out IWbemClassObjectFreeThreaded ppResultObject);
        [PreserveSig] int GetResultString_([In] int lTimeout, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrResultString);
        [PreserveSig] int GetResultServices_([In] int lTimeout, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemServices ppServices);
        [PreserveSig] int GetCallStatus_([In] int lTimeout, [Out] out int plStatus);
    }

    //[TypeLibTypeAttribute(0x0200)]
    [GuidAttribute("7C857801-7381-11CF-884D-00AA004B2E24")]
    [InterfaceTypeAttribute(0x0001)]
    [ComImport]
    internal interface IWbemObjectSink
    {
        [PreserveSig] int Indicate_([In] int lObjectCount, [In][MarshalAs(UnmanagedType.LPArray)] IntPtr[] apObjArray);
        [PreserveSig] int SetStatus_([In] int lFlags, [In][MarshalAs(UnmanagedType.Error)]  int hResult, [In][MarshalAs(UnmanagedType.BStr)] string strParam, [In] IntPtr pObjParam);
    }

    [InterfaceTypeAttribute(0x0001)]
    //[TypeLibTypeAttribute(0x0200)]
    [GuidAttribute("027947E1-D731-11CE-A357-000000000001")]
    [ComImport]
    internal interface IEnumWbemClassObject
    {
        [PreserveSig] int Reset_();
        [PreserveSig] int Next_([In] int lTimeout, [In] uint uCount, [In][Out][MarshalAs(UnmanagedType.LPArray)] IWbemClassObject_DoNotMarshal[] apObjects, [Out] out uint puReturned);
        [PreserveSig] int NextAsync_([In] uint uCount, [In][MarshalAs(UnmanagedType.Interface)]  IWbemObjectSink pSink);
        [PreserveSig] int Clone_([Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum);
        [PreserveSig] int Skip_([In] int lTimeout, [In] uint nCount);
    }

    [InterfaceTypeAttribute(0x0001)]
    //[TypeLibTypeAttribute(0x0200)]
    [GuidAttribute("BFBF883A-CAD7-11D3-A11B-00105A1F515A")]
    [ComImport]
    internal interface IWbemObjectTextSrc
    {
        [PreserveSig] int GetText_([In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemClassObject_DoNotMarshal pObj, [In] uint uObjTextFormat, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [Out][MarshalAs(UnmanagedType.BStr)]  out string strText);
        [PreserveSig] int CreateFromText_([In] int lFlags, [In][MarshalAs(UnmanagedType.BStr)] string strText, [In] uint uObjTextFormat, [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal pNewObj);
    }

    [GuidAttribute("49353C9A-516B-11D1-AEA6-00C04FB68820")]
    //[TypeLibTypeAttribute(0x0200)]
    [InterfaceTypeAttribute(0x0001)]
    [ComImport]
    internal interface IWbemObjectAccess
    {
        [PreserveSig] int GetQualifierSet_([Out][MarshalAs(UnmanagedType.Interface)]  out IWbemQualifierSet_DoNotMarshal ppQualSet);
        [PreserveSig] int Get_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [In][Out] ref object pVal, [In][Out] ref int pType, [In][Out] ref int plFlavor);
        [PreserveSig] int Put_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [In] ref object pVal, [In] int Type);
        [PreserveSig] int Delete_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName);
        [PreserveSig] int GetNames_([In][MarshalAs(UnmanagedType.LPWStr)] string wszQualifierName, [In] int lFlags, [In] ref object pQualifierVal, [Out][MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]  out string[] pNames);
        [PreserveSig] int BeginEnumeration_([In] int lEnumFlags);
        [PreserveSig] int Next_([In] int lFlags, [In][Out][MarshalAs(UnmanagedType.BStr)]  ref string strName, [In][Out] ref object pVal, [In][Out] ref int pType, [In][Out] ref int plFlavor);
        [PreserveSig] int EndEnumeration_();
        [PreserveSig] int GetPropertyQualifierSet_([In][MarshalAs(UnmanagedType.LPWStr)] string wszProperty, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemQualifierSet_DoNotMarshal ppQualSet);
        [PreserveSig] int Clone_([Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppCopy);
        [PreserveSig] int GetObjectText_([In] int lFlags, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrObjectText);
        [PreserveSig] int SpawnDerivedClass_([In] int lFlags, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppNewClass);
        [PreserveSig] int SpawnInstance_([In] int lFlags, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppNewInstance);
        [PreserveSig] int CompareTo_([In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemClassObject_DoNotMarshal pCompareTo);
        [PreserveSig] int GetPropertyOrigin_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrClassName);
        [PreserveSig] int InheritsFrom_([In][MarshalAs(UnmanagedType.LPWStr)] string strAncestor);
        [PreserveSig] int GetMethod_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppInSignature, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemClassObject_DoNotMarshal ppOutSignature);
        [PreserveSig] int PutMethod_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] int lFlags, [In][MarshalAs(UnmanagedType.Interface)]  IWbemClassObject_DoNotMarshal pInSignature, [In][MarshalAs(UnmanagedType.Interface)]  IWbemClassObject_DoNotMarshal pOutSignature);
        [PreserveSig] int DeleteMethod_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName);
        [PreserveSig] int BeginMethodEnumeration_([In] int lEnumFlags);
        [PreserveSig] int NextMethod_([In] int lFlags, [In][Out][MarshalAs(UnmanagedType.BStr)]  ref string pstrName, [In][Out][MarshalAs(UnmanagedType.Interface)]  ref IWbemClassObject_DoNotMarshal ppInSignature, [In][Out][MarshalAs(UnmanagedType.Interface)]  ref IWbemClassObject_DoNotMarshal ppOutSignature);
        [PreserveSig] int EndMethodEnumeration_();
        [PreserveSig] int GetMethodQualifierSet_([In][MarshalAs(UnmanagedType.LPWStr)] string wszMethod, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemQualifierSet_DoNotMarshal ppQualSet);
        [PreserveSig] int GetMethodOrigin_([In][MarshalAs(UnmanagedType.LPWStr)] string wszMethodName, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrClassName);
        [PreserveSig] int GetPropertyHandle_([In][MarshalAs(UnmanagedType.LPWStr)] string wszPropertyName, [Out] out int pType, [Out] out int plHandle);
        [PreserveSig] int WritePropertyValue_([In] int lHandle, [In] int lNumBytes, [In] ref byte aData);
        [PreserveSig] int ReadPropertyValue_([In] int lHandle, [In] int lBufferSize, [Out] out int plNumBytes, [Out] out byte aData);
        [PreserveSig] int ReadDWORD_([In] int lHandle, [Out] out uint pdw);
        [PreserveSig] int WriteDWORD_([In] int lHandle, [In] uint dw);
        [PreserveSig] int ReadQWORD_([In] int lHandle, [Out] out ulong pqw);
        [PreserveSig] int WriteQWORD_([In] int lHandle, [In] ulong pw);
        [PreserveSig] int GetPropertyInfoByHandle_([In] int lHandle, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrName, [Out] out int pType);
        [PreserveSig] int Lock_([In] int lFlags);
        [PreserveSig] int Unlock_([In] int lFlags);
    }

    [GuidAttribute("EB87E1BC-3233-11D2-AEC9-00C04FB68820")]
    [InterfaceTypeAttribute(0x0001)]
    [ComImport]
    internal interface IWbemStatusCodeText
    {
        [PreserveSig] int GetErrorCodeText_([In][MarshalAs(UnmanagedType.Error)]  int hRes, [In] uint LocaleId, [In] int lFlags, [Out][MarshalAs(UnmanagedType.BStr)]  out string MessageText);
        [PreserveSig] int GetFacilityCodeText_([In][MarshalAs(UnmanagedType.Error)]  int hRes, [In] uint LocaleId, [In] int lFlags, [Out][MarshalAs(UnmanagedType.BStr)]  out string MessageText);
    }

    [InterfaceTypeAttribute(0x0001)]
    //[TypeLibTypeAttribute(0x0200)]
    [GuidAttribute("E246107B-B06E-11D0-AD61-00C04FD8FDFF")]
    [ComImport]
    internal interface IWbemUnboundObjectSink
    {
        [PreserveSig] int IndicateToConsumer_([In][MarshalAs(UnmanagedType.Interface)]  IWbemClassObject_DoNotMarshal pLogicalConsumer, [In] int lNumObjects, [In][MarshalAs(UnmanagedType.Interface)]  ref IWbemClassObject_DoNotMarshal apObjects);
    }

    [InterfaceTypeAttribute(0x0001)]
    //[TypeLibTypeAttribute(0x0200)]
    [GuidAttribute("3AE0080A-7E3A-4366-BF89-0FEEDC931659")]
    [ComImport]
    internal interface IWbemEventSink
    {
        [PreserveSig] int Indicate_([In] int lObjectCount, [In][MarshalAs(UnmanagedType.Interface)]  ref IWbemClassObject_DoNotMarshal apObjArray);
        [PreserveSig] int SetStatus_([In] int lFlags, [In][MarshalAs(UnmanagedType.Error)]  int hResult, [In][MarshalAs(UnmanagedType.BStr)] string strParam, [In][MarshalAs(UnmanagedType.Interface)]  IWbemClassObject_DoNotMarshal pObjParam);
        [PreserveSig] int IndicateWithSD_([In] int lNumObjects, [In][MarshalAs(UnmanagedType.IUnknown)]  ref object apObjects, [In] int lSDLength, [In] ref byte pSD);
        [PreserveSig] int SetSinkSecurity_([In] int lSDLength, [In] ref byte pSD);
        [PreserveSig] int IsActive_();
        [PreserveSig] int GetRestrictedSink_([In] int lNumQueries, [In][MarshalAs(UnmanagedType.LPWStr)]  ref string awszQueries, [In][MarshalAs(UnmanagedType.IUnknown)]  object pCallback, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemEventSink ppSink);
        [PreserveSig] int SetBatchingParameters_([In] int lFlags, [In] uint dwMaxBufferSize, [In] uint dwMaxSendLatency);
    }

    [GuidAttribute("9AE62877-7544-4BB0-AA26-A13824659ED6")]
    /*[ComConversionLossAttribute]*/
    [InterfaceTypeAttribute(0x0001)]
    [ComImport]
    internal interface IWbemPathKeyList
    {
        [PreserveSig] int GetCount_([Out] out uint puKeyCount);
        [PreserveSig] int SetKey_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] uint uFlags, [In] uint uCimType, [In] IntPtr pKeyVal);
        [PreserveSig] int SetKey2_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] uint uFlags, [In] uint uCimType, [In] ref object pKeyVal);
        [PreserveSig] int GetKey_([In] uint uKeyIx, [In] uint uFlags, [In][Out] ref uint puNameBufSize, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 2)]  char[] pszKeyName, [In][Out] ref uint puKeyValBufSize, [In][Out] IntPtr pKeyVal, [Out] out uint puApparentCimType);
        [PreserveSig] int GetKey2_([In] uint uKeyIx, [In] uint uFlags, [In][Out] ref uint puNameBufSize, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 2)]  char[] pszKeyName, [In][Out] ref object pKeyValue, [Out] out uint puApparentCimType);
        [PreserveSig] int RemoveKey_([In][MarshalAs(UnmanagedType.LPWStr)] string wszName, [In] uint uFlags);
        [PreserveSig] int RemoveAllKeys_([In] uint uFlags);
        [PreserveSig] int MakeSingleton_([In] sbyte bSet);
        [PreserveSig] int GetInfo_([In] uint uRequestedInfo, [Out] out ulong puResponse);
        [PreserveSig] int GetText_([In] int lFlags, [In][Out] ref uint puBuffLength, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 1)]  char[] pszText);
    }

    [GuidAttribute("3BC15AF2-736C-477E-9E51-238AF8667DCC")]
    [InterfaceTypeAttribute(0x0001)]
    [ComImport]
    internal interface IWbemPath
    {
        [PreserveSig] int SetText_([In] uint uMode, [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath);
        [PreserveSig] int GetText_([In] int lFlags, [In][Out] ref uint puBuffLength, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 1)]  char[] pszText);
        [PreserveSig] int GetInfo_([In] uint uRequestedInfo, [Out] out ulong puResponse);
        [PreserveSig] int SetServer_([In][MarshalAs(UnmanagedType.LPWStr)] string Name);
        [PreserveSig] int GetServer_([In][Out] ref uint puNameBufLength, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 0)]  char[] pName);
        [PreserveSig] int GetNamespaceCount_([Out] out uint puCount);
        [PreserveSig] int SetNamespaceAt_([In] uint uIndex, [In][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 0)]  char[] pszName);
        [PreserveSig] int GetNamespaceAt_([In] uint uIndex, [In][Out] ref uint puNameBufLength, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 1)]  char[] pName);
        [PreserveSig] int RemoveNamespaceAt_([In] uint uIndex);
        [PreserveSig] int RemoveAllNamespaces_();
        [PreserveSig] int GetScopeCount_([Out] out uint puCount);
        [PreserveSig] int SetScope_([In] uint uIndex, [In][MarshalAs(UnmanagedType.LPWStr)] string pszClass);
        [PreserveSig] int SetScopeFromText_([In] uint uIndex, [In][MarshalAs(UnmanagedType.LPWStr)] string pszText);
        [PreserveSig] int GetScope_([In] uint uIndex, [In][Out] ref uint puClassNameBufSize, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 1)]  char[] pszClass, [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemPathKeyList pKeyList);
        [PreserveSig] int GetScopeAsText_([In] uint uIndex, [In][Out] ref uint puTextBufSize, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 1)]  char[] pszText);
        [PreserveSig] int RemoveScope_([In] uint uIndex);
        [PreserveSig] int RemoveAllScopes_();
        [PreserveSig] int SetClassName_([In][MarshalAs(UnmanagedType.LPWStr)] string Name);
        [PreserveSig] int GetClassName_([In][Out] ref uint puBuffLength, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 0)]  char[] pszName);
        [PreserveSig] int GetKeyList_([Out][MarshalAs(UnmanagedType.Interface)]  out IWbemPathKeyList pOut);
        [PreserveSig] int CreateClassPart_([In] int lFlags, [In][MarshalAs(UnmanagedType.LPWStr)] string Name);
        [PreserveSig] int DeleteClassPart_([In] int lFlags);
        [PreserveSig] int IsRelative_([In][MarshalAs(UnmanagedType.LPWStr)] string wszMachine, [In][MarshalAs(UnmanagedType.LPWStr)] string wszNamespace);
        [PreserveSig] int IsRelativeOrChild_([In][MarshalAs(UnmanagedType.LPWStr)] string wszMachine, [In][MarshalAs(UnmanagedType.LPWStr)] string wszNamespace, [In] int lFlags);
        [PreserveSig] int IsLocal_([In][MarshalAs(UnmanagedType.LPWStr)] string wszMachine);
        [PreserveSig] int IsSameClassName_([In][MarshalAs(UnmanagedType.LPWStr)] string wszClass);
    }

    #endregion

    #region Enums
    internal enum tag_WBEM_GENUS_TYPE
    {
        WBEM_GENUS_CLASS = unchecked((int)0x00000001),
        WBEM_GENUS_INSTANCE = unchecked((int)0x00000002),
    }

    internal enum tag_WBEM_CHANGE_FLAG_TYPE
    {
        WBEM_FLAG_CREATE_OR_UPDATE = unchecked((int)0x00000000),
        WBEM_FLAG_UPDATE_ONLY = unchecked((int)0x00000001),
        WBEM_FLAG_CREATE_ONLY = unchecked((int)0x00000002),
        WBEM_FLAG_UPDATE_COMPATIBLE = unchecked((int)0x00000000),
        WBEM_FLAG_UPDATE_SAFE_MODE = unchecked((int)0x00000020),
        WBEM_FLAG_UPDATE_FORCE_MODE = unchecked((int)0x00000040),
        WBEM_MASK_UPDATE_MODE = unchecked((int)0x00000060),
        WBEM_FLAG_ADVISORY = unchecked((int)0x00010000),
    }

    internal enum tag_WBEM_GENERIC_FLAG_TYPE
    {
        WBEM_FLAG_RETURN_IMMEDIATELY = unchecked((int)0x00000010),
        WBEM_FLAG_RETURN_WBEM_COMPLETE = unchecked((int)0x00000000),
        WBEM_FLAG_BIDIRECTIONAL = unchecked((int)0x00000000),
        WBEM_FLAG_FORWARD_ONLY = unchecked((int)0x00000020),
        WBEM_FLAG_NO_ERROR_OBJECT = unchecked((int)0x00000040),
        WBEM_FLAG_RETURN_ERROR_OBJECT = unchecked((int)0x00000000),
        WBEM_FLAG_SEND_STATUS = unchecked((int)0x00000080),
        WBEM_FLAG_DONT_SEND_STATUS = unchecked((int)0x00000000),
        WBEM_FLAG_ENSURE_LOCATABLE = unchecked((int)0x00000100),
        WBEM_FLAG_DIRECT_READ = unchecked((int)0x00000200),
        WBEM_FLAG_SEND_ONLY_SELECTED = unchecked((int)0x00000000),
        WBEM_RETURN_WHEN_COMPLETE = unchecked((int)0x00000000),
        WBEM_RETURN_IMMEDIATELY = unchecked((int)0x00000010),
        WBEM_MASK_RESERVED_FLAGS = unchecked((int)0x0001F000),
        WBEM_FLAG_USE_AMENDED_QUALIFIERS = unchecked((int)0x00020000),
        WBEM_FLAG_STRONG_VALIDATION = unchecked((int)0x00100000),
    }

    internal enum tag_WBEM_STATUS_TYPE
    {
        WBEM_STATUS_COMPLETE = unchecked((int)0x00000000),
        WBEM_STATUS_REQUIREMENTS = unchecked((int)0x00000001),
        WBEM_STATUS_PROGRESS = unchecked((int)0x00000002),
    }

    internal enum tag_WBEM_TIMEOUT_TYPE
    {
        WBEM_NO_WAIT = unchecked((int)0x00000000),
        WBEM_INFINITE = unchecked((int)0xFFFFFFFF),
    }

    internal enum tag_WBEM_CONDITION_FLAG_TYPE
    {
        WBEM_FLAG_ALWAYS = unchecked((int)0x00000000),
        WBEM_FLAG_ONLY_IF_TRUE = unchecked((int)0x00000001),
        WBEM_FLAG_ONLY_IF_FALSE = unchecked((int)0x00000002),
        WBEM_FLAG_ONLY_IF_IDENTICAL = unchecked((int)0x00000003),
        WBEM_MASK_PRIMARY_CONDITION = unchecked((int)0x00000003),
        WBEM_FLAG_KEYS_ONLY = unchecked((int)0x00000004),
        WBEM_FLAG_REFS_ONLY = unchecked((int)0x00000008),
        WBEM_FLAG_LOCAL_ONLY = unchecked((int)0x00000010),
        WBEM_FLAG_PROPAGATED_ONLY = unchecked((int)0x00000020),
        WBEM_FLAG_SYSTEM_ONLY = unchecked((int)0x00000030),
        WBEM_FLAG_NONSYSTEM_ONLY = unchecked((int)0x00000040),
        WBEM_MASK_CONDITION_ORIGIN = unchecked((int)0x00000070),
        WBEM_FLAG_CLASS_OVERRIDES_ONLY = unchecked((int)0x00000100),
        WBEM_FLAG_CLASS_LOCAL_AND_OVERRIDES = unchecked((int)0x00000200),
        WBEM_MASK_CLASS_CONDITION = unchecked((int)0x00000300),
    }
    internal enum tag_WBEM_FLAVOR_TYPE
    {
        WBEM_FLAVOR_DONT_PROPAGATE = unchecked((int)0x00000000),
        WBEM_FLAVOR_FLAG_PROPAGATE_TO_INSTANCE = unchecked((int)0x00000001),
        WBEM_FLAVOR_FLAG_PROPAGATE_TO_DERIVED_CLASS = unchecked((int)0x00000002),
        WBEM_FLAVOR_MASK_PROPAGATION = unchecked((int)0x0000000F),
        WBEM_FLAVOR_OVERRIDABLE = unchecked((int)0x00000000),
        WBEM_FLAVOR_NOT_OVERRIDABLE = unchecked((int)0x00000010),
        WBEM_FLAVOR_MASK_PERMISSIONS = unchecked((int)0x00000010),
        WBEM_FLAVOR_ORIGIN_LOCAL = unchecked((int)0x00000000),
        WBEM_FLAVOR_ORIGIN_PROPAGATED = unchecked((int)0x00000020),
        WBEM_FLAVOR_ORIGIN_SYSTEM = unchecked((int)0x00000040),
        WBEM_FLAVOR_MASK_ORIGIN = unchecked((int)0x00000060),
        WBEM_FLAVOR_NOT_AMENDED = unchecked((int)0x00000000),
        WBEM_FLAVOR_AMENDED = unchecked((int)0x00000080),
        WBEM_FLAVOR_MASK_AMENDED = unchecked((int)0x00000080),
    }

    internal enum tag_WBEM_QUERY_FLAG_TYPE
    {
        WBEM_FLAG_DEEP = unchecked((int)0x00000000),
        WBEM_FLAG_SHALLOW = unchecked((int)0x00000001),
        WBEM_FLAG_PROTOTYPE = unchecked((int)0x00000002),
    }

    internal enum tag_CIMTYPE_ENUMERATION
    {
        CIM_ILLEGAL = unchecked((int)0x00000FFF),
        CIM_EMPTY = unchecked((int)0x00000000),
        CIM_SINT8 = unchecked((int)0x00000010),
        CIM_UINT8 = unchecked((int)0x00000011),
        CIM_SINT16 = unchecked((int)0x00000002),
        CIM_UINT16 = unchecked((int)0x00000012),
        CIM_SINT32 = unchecked((int)0x00000003),
        CIM_UINT32 = unchecked((int)0x00000013),
        CIM_SINT64 = unchecked((int)0x00000014),
        CIM_UINT64 = unchecked((int)0x00000015),
        CIM_REAL32 = unchecked((int)0x00000004),
        CIM_REAL64 = unchecked((int)0x00000005),
        CIM_BOOLEAN = unchecked((int)0x0000000B),
        CIM_STRING = unchecked((int)0x00000008),
        CIM_DATETIME = unchecked((int)0x00000065),
        CIM_REFERENCE = unchecked((int)0x00000066),
        CIM_CHAR16 = unchecked((int)0x00000067),
        CIM_OBJECT = unchecked((int)0x0000000D),
        CIM_FLAG_ARRAY = unchecked((int)0x00002000),
    }

    internal enum tag_WBEMSTATUS
    {
        WBEM_NO_ERROR = unchecked((int)0x00000000),
        WBEM_S_NO_ERROR = unchecked((int)0x00000000),
        WBEM_S_SAME = unchecked((int)0x00000000),
        WBEM_S_FALSE = unchecked((int)0x00000001),
        WBEM_S_ALREADY_EXISTS = unchecked((int)0x00040001),
        WBEM_S_RESET_TO_DEFAULT = unchecked((int)0x00040002),
        WBEM_S_DIFFERENT = unchecked((int)0x00040003),
        WBEM_S_TIMEDOUT = unchecked((int)0x00040004),
        WBEM_S_NO_MORE_DATA = unchecked((int)0x00040005),
        WBEM_S_OPERATION_CANCELLED = unchecked((int)0x00040006),
        WBEM_S_PENDING = unchecked((int)0x00040007),
        WBEM_S_DUPLICATE_OBJECTS = unchecked((int)0x00040008),
        WBEM_S_ACCESS_DENIED = unchecked((int)0x00040009),
        WBEM_S_PARTIAL_RESULTS = unchecked((int)0x00040010),
        WBEM_S_NO_POSTHOOK = unchecked((int)0x00040011),
        WBEM_S_POSTHOOK_WITH_BOTH = unchecked((int)0x00040012),
        WBEM_S_POSTHOOK_WITH_NEW = unchecked((int)0x00040013),
        WBEM_S_POSTHOOK_WITH_STATUS = unchecked((int)0x00040014),
        WBEM_S_POSTHOOK_WITH_OLD = unchecked((int)0x00040015),
        WBEM_S_REDO_PREHOOK_WITH_ORIGINAL_OBJECT = unchecked((int)0x00040016),
        WBEM_S_SOURCE_NOT_AVAILABLE = unchecked((int)0x00040017),
        WBEM_E_FAILED = unchecked((int)0x80041001),
        WBEM_E_NOT_FOUND = unchecked((int)0x80041002),
        WBEM_E_ACCESS_DENIED = unchecked((int)0x80041003),
        WBEM_E_PROVIDER_FAILURE = unchecked((int)0x80041004),
        WBEM_E_TYPE_MISMATCH = unchecked((int)0x80041005),
        WBEM_E_OUT_OF_MEMORY = unchecked((int)0x80041006),
        WBEM_E_INVALID_CONTEXT = unchecked((int)0x80041007),
        WBEM_E_INVALID_PARAMETER = unchecked((int)0x80041008),
        WBEM_E_NOT_AVAILABLE = unchecked((int)0x80041009),
        WBEM_E_CRITICAL_ERROR = unchecked((int)0x8004100A),
        WBEM_E_INVALID_STREAM = unchecked((int)0x8004100B),
        WBEM_E_NOT_SUPPORTED = unchecked((int)0x8004100C),
        WBEM_E_INVALID_SUPERCLASS = unchecked((int)0x8004100D),
        WBEM_E_INVALID_NAMESPACE = unchecked((int)0x8004100E),
        WBEM_E_INVALID_OBJECT = unchecked((int)0x8004100F),
        WBEM_E_INVALID_CLASS = unchecked((int)0x80041010),
        WBEM_E_PROVIDER_NOT_FOUND = unchecked((int)0x80041011),
        WBEM_E_INVALID_PROVIDER_REGISTRATION = unchecked((int)0x80041012),
        WBEM_E_PROVIDER_LOAD_FAILURE = unchecked((int)0x80041013),
        WBEM_E_INITIALIZATION_FAILURE = unchecked((int)0x80041014),
        WBEM_E_TRANSPORT_FAILURE = unchecked((int)0x80041015),
        WBEM_E_INVALID_OPERATION = unchecked((int)0x80041016),
        WBEM_E_INVALID_QUERY = unchecked((int)0x80041017),
        WBEM_E_INVALID_QUERY_TYPE = unchecked((int)0x80041018),
        WBEM_E_ALREADY_EXISTS = unchecked((int)0x80041019),
        WBEM_E_OVERRIDE_NOT_ALLOWED = unchecked((int)0x8004101A),
        WBEM_E_PROPAGATED_QUALIFIER = unchecked((int)0x8004101B),
        WBEM_E_PROPAGATED_PROPERTY = unchecked((int)0x8004101C),
        WBEM_E_UNEXPECTED = unchecked((int)0x8004101D),
        WBEM_E_ILLEGAL_OPERATION = unchecked((int)0x8004101E),
        WBEM_E_CANNOT_BE_KEY = unchecked((int)0x8004101F),
        WBEM_E_INCOMPLETE_CLASS = unchecked((int)0x80041020),
        WBEM_E_INVALID_SYNTAX = unchecked((int)0x80041021),
        WBEM_E_NONDECORATED_OBJECT = unchecked((int)0x80041022),
        WBEM_E_READ_ONLY = unchecked((int)0x80041023),
        WBEM_E_PROVIDER_NOT_CAPABLE = unchecked((int)0x80041024),
        WBEM_E_CLASS_HAS_CHILDREN = unchecked((int)0x80041025),
        WBEM_E_CLASS_HAS_INSTANCES = unchecked((int)0x80041026),
        WBEM_E_QUERY_NOT_IMPLEMENTED = unchecked((int)0x80041027),
        WBEM_E_ILLEGAL_NULL = unchecked((int)0x80041028),
        WBEM_E_INVALID_QUALIFIER_TYPE = unchecked((int)0x80041029),
        WBEM_E_INVALID_PROPERTY_TYPE = unchecked((int)0x8004102A),
        WBEM_E_VALUE_OUT_OF_RANGE = unchecked((int)0x8004102B),
        WBEM_E_CANNOT_BE_SINGLETON = unchecked((int)0x8004102C),
        WBEM_E_INVALID_CIM_TYPE = unchecked((int)0x8004102D),
        WBEM_E_INVALID_METHOD = unchecked((int)0x8004102E),
        WBEM_E_INVALID_METHOD_PARAMETERS = unchecked((int)0x8004102F),
        WBEM_E_SYSTEM_PROPERTY = unchecked((int)0x80041030),
        WBEM_E_INVALID_PROPERTY = unchecked((int)0x80041031),
        WBEM_E_CALL_CANCELLED = unchecked((int)0x80041032),
        WBEM_E_SHUTTING_DOWN = unchecked((int)0x80041033),
        WBEM_E_PROPAGATED_METHOD = unchecked((int)0x80041034),
        WBEM_E_UNSUPPORTED_PARAMETER = unchecked((int)0x80041035),
        WBEM_E_MISSING_PARAMETER_ID = unchecked((int)0x80041036),
        WBEM_E_INVALID_PARAMETER_ID = unchecked((int)0x80041037),
        WBEM_E_NONCONSECUTIVE_PARAMETER_IDS = unchecked((int)0x80041038),
        WBEM_E_PARAMETER_ID_ON_RETVAL = unchecked((int)0x80041039),
        WBEM_E_INVALID_OBJECT_PATH = unchecked((int)0x8004103A),
        WBEM_E_OUT_OF_DISK_SPACE = unchecked((int)0x8004103B),
        WBEM_E_BUFFER_TOO_SMALL = unchecked((int)0x8004103C),
        WBEM_E_UNSUPPORTED_PUT_EXTENSION = unchecked((int)0x8004103D),
        WBEM_E_UNKNOWN_OBJECT_TYPE = unchecked((int)0x8004103E),
        WBEM_E_UNKNOWN_PACKET_TYPE = unchecked((int)0x8004103F),
        WBEM_E_MARSHAL_VERSION_MISMATCH = unchecked((int)0x80041040),
        WBEM_E_MARSHAL_INVALID_SIGNATURE = unchecked((int)0x80041041),
        WBEM_E_INVALID_QUALIFIER = unchecked((int)0x80041042),
        WBEM_E_INVALID_DUPLICATE_PARAMETER = unchecked((int)0x80041043),
        WBEM_E_TOO_MUCH_DATA = unchecked((int)0x80041044),
        WBEM_E_SERVER_TOO_BUSY = unchecked((int)0x80041045),
        WBEM_E_INVALID_FLAVOR = unchecked((int)0x80041046),
        WBEM_E_CIRCULAR_REFERENCE = unchecked((int)0x80041047),
        WBEM_E_UNSUPPORTED_CLASS_UPDATE = unchecked((int)0x80041048),
        WBEM_E_CANNOT_CHANGE_KEY_INHERITANCE = unchecked((int)0x80041049),
        WBEM_E_CANNOT_CHANGE_INDEX_INHERITANCE = unchecked((int)0x80041050),
        WBEM_E_TOO_MANY_PROPERTIES = unchecked((int)0x80041051),
        WBEM_E_UPDATE_TYPE_MISMATCH = unchecked((int)0x80041052),
        WBEM_E_UPDATE_OVERRIDE_NOT_ALLOWED = unchecked((int)0x80041053),
        WBEM_E_UPDATE_PROPAGATED_METHOD = unchecked((int)0x80041054),
        WBEM_E_METHOD_NOT_IMPLEMENTED = unchecked((int)0x80041055),
        WBEM_E_METHOD_DISABLED = unchecked((int)0x80041056),
        WBEM_E_REFRESHER_BUSY = unchecked((int)0x80041057),
        WBEM_E_UNPARSABLE_QUERY = unchecked((int)0x80041058),
        WBEM_E_NOT_EVENT_CLASS = unchecked((int)0x80041059),
        WBEM_E_MISSING_GROUP_WITHIN = unchecked((int)0x8004105A),
        WBEM_E_MISSING_AGGREGATION_LIST = unchecked((int)0x8004105B),
        WBEM_E_PROPERTY_NOT_AN_OBJECT = unchecked((int)0x8004105C),
        WBEM_E_AGGREGATING_BY_OBJECT = unchecked((int)0x8004105D),
        WBEM_E_UNINTERPRETABLE_PROVIDER_QUERY = unchecked((int)0x8004105F),
        WBEM_E_BACKUP_RESTORE_WINMGMT_RUNNING = unchecked((int)0x80041060),
        WBEM_E_QUEUE_OVERFLOW = unchecked((int)0x80041061),
        WBEM_E_PRIVILEGE_NOT_HELD = unchecked((int)0x80041062),
        WBEM_E_INVALID_OPERATOR = unchecked((int)0x80041063),
        WBEM_E_LOCAL_CREDENTIALS = unchecked((int)0x80041064),
        WBEM_E_CANNOT_BE_ABSTRACT = unchecked((int)0x80041065),
        WBEM_E_AMENDED_OBJECT = unchecked((int)0x80041066),
        WBEM_E_CLIENT_TOO_SLOW = unchecked((int)0x80041067),
        WBEM_E_NULL_SECURITY_DESCRIPTOR = unchecked((int)0x80041068),
        WBEM_E_TIMED_OUT = unchecked((int)0x80041069),
        WBEM_E_INVALID_ASSOCIATION = unchecked((int)0x8004106A),
        WBEM_E_AMBIGUOUS_OPERATION = unchecked((int)0x8004106B),
        WBEM_E_QUOTA_VIOLATION = unchecked((int)0x8004106C),
        WBEM_E_RESERVED_001 = unchecked((int)0x8004106D),
        WBEM_E_RESERVED_002 = unchecked((int)0x8004106E),
        WBEM_E_UNSUPPORTED_LOCALE = unchecked((int)0x8004106F),
        WBEM_E_HANDLE_OUT_OF_DATE = unchecked((int)0x80041070),
        WBEM_E_CONNECTION_FAILED = unchecked((int)0x80041071),
        WBEM_E_INVALID_HANDLE_REQUEST = unchecked((int)0x80041072),
        WBEM_E_PROPERTY_NAME_TOO_WIDE = unchecked((int)0x80041073),
        WBEM_E_CLASS_NAME_TOO_WIDE = unchecked((int)0x80041074),
        WBEM_E_METHOD_NAME_TOO_WIDE = unchecked((int)0x80041075),
        WBEM_E_QUALIFIER_NAME_TOO_WIDE = unchecked((int)0x80041076),
        WBEM_E_RERUN_COMMAND = unchecked((int)0x80041077),
        WBEM_E_DATABASE_VER_MISMATCH = unchecked((int)0x80041078),
        WBEM_E_VETO_DELETE = unchecked((int)0x80041079),
        WBEM_E_VETO_PUT = unchecked((int)0x8004107A),
        WBEM_E_INVALID_LOCALE = unchecked((int)0x80041080),
        WBEM_E_PROVIDER_SUSPENDED = unchecked((int)0x80041081),
        WBEM_E_SYNCHRONIZATION_REQUIRED = unchecked((int)0x80041082),
        WBEM_E_NO_SCHEMA = unchecked((int)0x80041083),
        WBEM_E_PROVIDER_ALREADY_REGISTERED = unchecked((int)0x80041084),
        WBEM_E_PROVIDER_NOT_REGISTERED = unchecked((int)0x80041085),
        WBEM_E_FATAL_TRANSPORT_ERROR = unchecked((int)0x80041086),
        WBEM_E_ENCRYPTED_CONNECTION_REQUIRED = unchecked((int)0x80041087),
        WBEM_E_PROVIDER_TIMED_OUT = unchecked((int)0x80041088),
        WBEM_E_NO_KEY = unchecked((int)0x80041089),
        WBEMESS_E_REGISTRATION_TOO_BROAD = unchecked((int)0x80042001),
        WBEMESS_E_REGISTRATION_TOO_PRECISE = unchecked((int)0x80042002),
        WBEMMOF_E_EXPECTED_QUALIFIER_NAME = unchecked((int)0x80044001),
        WBEMMOF_E_EXPECTED_SEMI = unchecked((int)0x80044002),
        WBEMMOF_E_EXPECTED_OPEN_BRACE = unchecked((int)0x80044003),
        WBEMMOF_E_EXPECTED_CLOSE_BRACE = unchecked((int)0x80044004),
        WBEMMOF_E_EXPECTED_CLOSE_BRACKET = unchecked((int)0x80044005),
        WBEMMOF_E_EXPECTED_CLOSE_PAREN = unchecked((int)0x80044006),
        WBEMMOF_E_ILLEGAL_CONSTANT_VALUE = unchecked((int)0x80044007),
        WBEMMOF_E_EXPECTED_TYPE_IDENTIFIER = unchecked((int)0x80044008),
        WBEMMOF_E_EXPECTED_OPEN_PAREN = unchecked((int)0x80044009),
        WBEMMOF_E_UNRECOGNIZED_TOKEN = unchecked((int)0x8004400A),
        WBEMMOF_E_UNRECOGNIZED_TYPE = unchecked((int)0x8004400B),
        WBEMMOF_E_EXPECTED_PROPERTY_NAME = unchecked((int)0x8004400C),
        WBEMMOF_E_TYPEDEF_NOT_SUPPORTED = unchecked((int)0x8004400D),
        WBEMMOF_E_UNEXPECTED_ALIAS = unchecked((int)0x8004400E),
        WBEMMOF_E_UNEXPECTED_ARRAY_INIT = unchecked((int)0x8004400F),
        WBEMMOF_E_INVALID_AMENDMENT_SYNTAX = unchecked((int)0x80044010),
        WBEMMOF_E_INVALID_DUPLICATE_AMENDMENT = unchecked((int)0x80044011),
        WBEMMOF_E_INVALID_PRAGMA = unchecked((int)0x80044012),
        WBEMMOF_E_INVALID_NAMESPACE_SYNTAX = unchecked((int)0x80044013),
        WBEMMOF_E_EXPECTED_CLASS_NAME = unchecked((int)0x80044014),
        WBEMMOF_E_TYPE_MISMATCH = unchecked((int)0x80044015),
        WBEMMOF_E_EXPECTED_ALIAS_NAME = unchecked((int)0x80044016),
        WBEMMOF_E_INVALID_CLASS_DECLARATION = unchecked((int)0x80044017),
        WBEMMOF_E_INVALID_INSTANCE_DECLARATION = unchecked((int)0x80044018),
        WBEMMOF_E_EXPECTED_DOLLAR = unchecked((int)0x80044019),
        WBEMMOF_E_CIMTYPE_QUALIFIER = unchecked((int)0x8004401A),
        WBEMMOF_E_DUPLICATE_PROPERTY = unchecked((int)0x8004401B),
        WBEMMOF_E_INVALID_NAMESPACE_SPECIFICATION = unchecked((int)0x8004401C),
        WBEMMOF_E_OUT_OF_RANGE = unchecked((int)0x8004401D),
        WBEMMOF_E_INVALID_FILE = unchecked((int)0x8004401E),
        WBEMMOF_E_ALIASES_IN_EMBEDDED = unchecked((int)0x8004401F),
        WBEMMOF_E_NULL_ARRAY_ELEM = unchecked((int)0x80044020),
        WBEMMOF_E_DUPLICATE_QUALIFIER = unchecked((int)0x80044021),
        WBEMMOF_E_EXPECTED_FLAVOR_TYPE = unchecked((int)0x80044022),
        WBEMMOF_E_INCOMPATIBLE_FLAVOR_TYPES = unchecked((int)0x80044023),
        WBEMMOF_E_MULTIPLE_ALIASES = unchecked((int)0x80044024),
        WBEMMOF_E_INCOMPATIBLE_FLAVOR_TYPES2 = unchecked((int)0x80044025),
        WBEMMOF_E_NO_ARRAYS_RETURNED = unchecked((int)0x80044026),
        WBEMMOF_E_MUST_BE_IN_OR_OUT = unchecked((int)0x80044027),
        WBEMMOF_E_INVALID_FLAGS_SYNTAX = unchecked((int)0x80044028),
        WBEMMOF_E_EXPECTED_BRACE_OR_BAD_TYPE = unchecked((int)0x80044029),
        WBEMMOF_E_UNSUPPORTED_CIMV22_QUAL_VALUE = unchecked((int)0x8004402A),
        WBEMMOF_E_UNSUPPORTED_CIMV22_DATA_TYPE = unchecked((int)0x8004402B),
        WBEMMOF_E_INVALID_DELETEINSTANCE_SYNTAX = unchecked((int)0x8004402C),
        WBEMMOF_E_INVALID_QUALIFIER_SYNTAX = unchecked((int)0x8004402D),
        WBEMMOF_E_QUALIFIER_USED_OUTSIDE_SCOPE = unchecked((int)0x8004402E),
        WBEMMOF_E_ERROR_CREATING_TEMP_FILE = unchecked((int)0x8004402F),
        WBEMMOF_E_ERROR_INVALID_INCLUDE_FILE = unchecked((int)0x80044030),
        WBEMMOF_E_INVALID_DELETECLASS_SYNTAX = unchecked((int)0x80044031),
    }

    internal enum tag_WBEM_CONNECT_OPTIONS
    {
        WBEM_FLAG_CONNECT_REPOSITORY_ONLY = 0X40,
        WBEM_FLAG_CONNECT_USE_MAX_WAIT = 0X80,
    }

    internal enum tag_WBEM_PATH_STATUS_FLAG
    {
        WBEMPATH_INFO_ANON_LOCAL_MACHINE = unchecked((int)0x00000001),
        WBEMPATH_INFO_HAS_MACHINE_NAME = unchecked((int)0x00000002),
        WBEMPATH_INFO_IS_CLASS_REF = unchecked((int)0x00000004),
        WBEMPATH_INFO_IS_INST_REF = unchecked((int)0x00000008),
        WBEMPATH_INFO_HAS_SUBSCOPES = unchecked((int)0x00000010),
        WBEMPATH_INFO_IS_COMPOUND = unchecked((int)0x00000020),
        WBEMPATH_INFO_HAS_V2_REF_PATHS = unchecked((int)0x00000040),
        WBEMPATH_INFO_HAS_IMPLIED_KEY = unchecked((int)0x00000080),
        WBEMPATH_INFO_CONTAINS_SINGLETON = unchecked((int)0x00000100),
        WBEMPATH_INFO_V1_COMPLIANT = unchecked((int)0x00000200),
        WBEMPATH_INFO_V2_COMPLIANT = unchecked((int)0x00000400),
        WBEMPATH_INFO_CIM_COMPLIANT = unchecked((int)0x00000800),
        WBEMPATH_INFO_IS_SINGLETON = unchecked((int)0x00001000),
        WBEMPATH_INFO_IS_PARENT = unchecked((int)0x00002000),
        WBEMPATH_INFO_SERVER_NAMESPACE_ONLY = unchecked((int)0x00004000),
        WBEMPATH_INFO_NATIVE_PATH = unchecked((int)0x00008000),
        WBEMPATH_INFO_WMI_PATH = unchecked((int)0x00010000),
        WBEMPATH_INFO_PATH_HAD_SERVER = unchecked((int)0x00020000),
    }

    internal enum tag_WBEM_PATH_CREATE_FLAG
    {
        WBEMPATH_CREATE_ACCEPT_RELATIVE = unchecked((int)0x00000001),
        WBEMPATH_CREATE_ACCEPT_ABSOLUTE = unchecked((int)0x00000002),
        WBEMPATH_CREATE_ACCEPT_ALL = unchecked((int)0x00000004),
        WBEMPATH_TREAT_SINGLE_IDENT_AS_NS = unchecked((int)0x00000008),
    }

    internal enum tag_WBEM_GET_TEXT_FLAGS
    {
        WBEMPATH_COMPRESSED = unchecked((int)0x00000001),
        WBEMPATH_GET_RELATIVE_ONLY = unchecked((int)0x00000002),
        WBEMPATH_GET_SERVER_TOO = unchecked((int)0x00000004),
        WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY = unchecked((int)0x00000008),
        WBEMPATH_GET_NAMESPACE_ONLY = unchecked((int)0x00000010),
        WBEMPATH_GET_ORIGINAL = unchecked((int)0x00000020),
    }
    #endregion

    #region Structs

    #endregion

    [ClassInterfaceAttribute((short)0x0000)]
    [GuidAttribute("674B6698-EE92-11D0-AD71-00C04FD8FDFF")]
    //[TypeLibTypeAttribute(0x0202)]
    [ComImport]
    internal class WbemContext
    {
    }

    [GuidAttribute("9A653086-174F-11D2-B5F9-00104B703EFD")]
    [ClassInterfaceAttribute((short)0x0000)]
    //[TypeLibTypeAttribute(0x0002)]
    [ComImport]
    internal class WbemClassObject
    {
    }

    [ClassInterfaceAttribute((short)0x0000)]
    //[TypeLibTypeAttribute(0x0002)]
    [GuidAttribute("EB87E1BD-3233-11D2-AEC9-00C04FB68820")]
    [ComImport]
    internal class WbemStatusCodeText
    {
    }

    [ClassInterfaceAttribute((short)0x0000)]
    //[TypeLibTypeAttribute(0x0202)]
    [GuidAttribute("8D1C559D-84F0-4BB3-A7D5-56A7435A9BA6")]
    [ComImport]
    internal class WbemObjectTextSrc
    {
    }

    [ClassInterfaceAttribute((short)0x0000)]
    [GuidAttribute("CF4CC405-E2C5-4DDD-B3CE-5E7582D8C9FA")]
    //[TypeLibTypeAttribute(0x0202)]
    [ComImport]
    internal class WbemDefPath
    {
    }

    internal static class MTAHelper
    {
        private sealed class MTARequest
        {
            public AutoResetEvent evtDone = new AutoResetEvent(false);
            public Type typeToCreate;
            public object createdObject;
            public Exception exception;

            public MTARequest(Type typeToCreate)
            {
                this.typeToCreate = typeToCreate;
            }
        }

        private static readonly ArrayList reqList = new ArrayList(3);
        private static readonly object critSec = new object();

        private static readonly AutoResetEvent evtGo = new AutoResetEvent(false); // tells the worker to create an object on our behalf

        private static bool workerThreadInitialized;
        // Initialize worker thread
        // This is not done in a static constructor so that we don't do this in an MTA only application
        private static void InitWorkerThread()
        {
            // Create the worker thread
            Thread thread = new Thread(new ThreadStart(WorkerThread));
            thread.SetApartmentState(ApartmentState.MTA);
            thread.IsBackground = true;
            thread.Start();
        }

        // This method will create an instance of the requeted type in the MTA
        public static object CreateInMTA(Type type)
        {
            // If we are currently in the MTA, we can directly create the object
            if (IsNoContextMTA())
                return Activator.CreateInstance(type);

            // We need to create the object in the MTA by using a worker thread
            // that lives in the MTA.  Make sure only one person uses this worker
            // thread at a time
            MTARequest myReq = new MTARequest(type);
            int ndx;

            lock (critSec)
            {
                // Make sure worker thread is initialized
                if (workerThreadInitialized == false)
                {
                    InitWorkerThread();
                    workerThreadInitialized = true;
                }

                ndx = reqList.Add(myReq);

                if (evtGo.Set() == false)
                {
                    reqList.RemoveAt(ndx);
                    throw new ManagementException(SR.WorkerThreadWakeupFailed);
                }
            }

            // release the lock so that worker thread can work on the request
            myReq.evtDone.WaitOne();

            if (myReq.exception != null)
            {
                throw myReq.exception;
            }

            return myReq.createdObject;
        }

        private static void WorkerThread()
        {
            // The worker thread will be a background thread, so we never have
            // to worry about killing it.  There is no chance that we will
            // be terminated inside the guts of the worker because we are
            // always waited on until we complete.  In other words, when the
            // process goes away, we are really assured that we will be in the
            // evtGo.WaitOne() block.
            while (true)
            {
                // Wait for someone who wants us to create an object
                evtGo.WaitOne();

                // get requests from the request queue. Since two Set within short time on evtGo can wake this thread only once
                // workerthread should check until we empty all the results. Even if we consume the request that is not set,
                // workerthread will wake up one more time unnecessarily and do nothing
                while (true)
                {
                    MTARequest reqToProcess = null;
                    lock (critSec)
                    {
                        if (reqList.Count > 0)
                        {
                            reqToProcess = (MTARequest)reqList[0];
                            reqList.RemoveAt(0);
                        }
                        else
                        {
                            break;  // break the inner while true
                        }
                    }

                    try
                    {
                        reqToProcess.createdObject = Activator.CreateInstance(reqToProcess.typeToCreate);
                    }
                    catch (Exception e)
                    {
                        reqToProcess.exception = e;
                    }
                    finally
                    {
                        // Tell the caller that we are done creating the object
                        reqToProcess.evtDone.Set();
                    }
                }
            }
        }

        // Interfaces that we need to use
        private static readonly Guid IID_IObjectContext = new Guid("51372AE0-CAE7-11CF-BE81-00AA00A2FA25");
        private static readonly Guid IID_IComThreadingInfo = new Guid("000001ce-0000-0000-C000-000000000046");

        // A variable that is initialized once to tell us if we are on
        // a Win2k platform or above.
        private static readonly bool CanCallCoGetObjectContext = IsWindows2000OrHigher();

        // This method will tell us if the calling thread is in the MTA and we are not in a 'context'
        public static bool IsNoContextMTA()
        {
            // If the runtime says we are not an MTA thread, we'll trust it and return false
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
                return false;

            // We know that 'ApartmentState' says we are MTA
            // If we are not on Win2k or above, we don't have to worry about 'context' or the neutral apartment
            // We know we are safely in the MTA and return true
            if (!CanCallCoGetObjectContext)
                return true;


            // We now will use IComThreadingInfo to make sure we are not in the neutral apartment, and
            // we check for IObjectContext to see if we have a 'context'
            IntPtr pComThreadingInfo = IntPtr.Zero;
            IntPtr pObjectContext = IntPtr.Zero;
            try
            {
                // If we CANNOT call CoGetObjectContext, assume we are not in the 'no context MTA' for safety
                // (NOTE: This call is expected to always succeed)
                if (0 != Interop.Ole32.CoGetObjectContext(IID_IComThreadingInfo, out pComThreadingInfo))
                    return false;

                WmiNetUtilsHelper.APTTYPE aptType;

                // If we CANNOT get the apartment type, assume we are not in the 'no context MTA' for safety
                // (NOTE: This call is expected to always succeed)
                if (0 != WmiNetUtilsHelper.GetCurrentApartmentType_f(3, pComThreadingInfo, out aptType))
                    return false;

                // If we are not in the MTA, return false
                if (aptType != WmiNetUtilsHelper.APTTYPE.APTTYPE_MTA)
                    return false;

                // If we CAN get to the IObjectContext interface, we have a 'context'
#pragma warning disable CS9191 // The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
                if (0 == Marshal.QueryInterface(pComThreadingInfo, ref Unsafe.AsRef(in IID_IObjectContext), out pObjectContext))
#pragma warning restore CS9191
                    return false;
            }
            finally
            {
                if (pComThreadingInfo != IntPtr.Zero)
                    Marshal.Release(pComThreadingInfo);
                if (pObjectContext != IntPtr.Zero)
                    Marshal.Release(pObjectContext);
            }

            // We made it past all the tests
            return true;
        }

        private static bool IsWindows2000OrHigher()
        {
            // If we are on Win2k or above, we are OK
            // - Platform == Win32NT and OS version >= 5.0.0.0
            OperatingSystem os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT && os.Version >= new Version(5, 0))
                return true;
            return false;
        }
    }


    /// <summary>
    /// The ThreadDispatch class allows clients to very easily spawn a thread, specify a worker delegate to be called from the
    /// spawned thread and wait until thread finishes work. This is important in cases where a new thread needs to be spawned but
    /// the main thread has to wait until the spawned thread finishes. As an example of this, in WMI we spawn MTA threads and create
    /// various objects in these threads to make sure we avoid marshaling cost.
    /// If the worker thread returns a value it is stored in the ThreadDispatch object and accessible to clients via the Result property.
    /// Also, any exception thrown is propagated from worker thread to main thread (by rethrowing orinal exception):
    ///
    ///     ThreadDispatch disp = new ThreadDispatch ( new ThreadDispatch.ThreadWorkerMethod  ( Class1.Func ) ) ;
    ///     disp.Start ( ) ;
    ///
    /// Four different delegate types are supported:
    ///
    ///     1. Delegate with no parameter and no return value.
    ///     2. Delegate with no parameter and return value.
    ///     3. Delegate with parameter and no return value.
    ///     4. Delegate with parameter and return value.
    ///
    /// </summary>
    internal sealed class ThreadDispatch
    {
        #region Private fields
        private Thread thread;
        private Exception exception;                                                        // Exception thrown from worker method (if any). Exposed as property.
        private ThreadWorkerMethodWithReturn threadWorkerMethodWithReturn;                  // Delegate to be called for worker method with return value
        private ThreadWorkerMethodWithReturnAndParam threadWorkerMethodWithReturnAndParam;  // Delegate to be called for worker method with return value and parameter
        private ThreadWorkerMethod threadWorkerMethod;                                      // Delegate to be called for worker method
        private ThreadWorkerMethodWithParam threadWorkerMethodWithParam;                    // Delegate to be called for worker method with parameter
        private object threadReturn;                                                        // Return data from worker method.
        private object threadParams;                                                        // Parameters to be passed to worker method
        private bool backgroundThread;                                                      // Is this a background thread?
        private ApartmentState apartmentType;                                               // Apartment type of thread
        #endregion

        #region Properties
        /// <summary>
        /// [Get] Gets the exception associated with the operation performed by thread.
        /// This can be null if no exception has been thrown.
        /// </summary>
        public System.Exception Exception
        {
            get
            {
                return this.exception;
            }
        }

        /// <summary>
        /// [Get/Set] The parameter to be used by worker thread. This will typically be a 'this' reference for access to instance members.
        /// </summary>
        public object Parameter
        {
            get
            {
                return this.threadParams;
            }
            set
            {
                this.threadParams = value;
            }
        }

        /// <summary>
        /// [Get/Set] The background property of a thread. Defaults to false.
        /// </summary>
        public bool IsBackgroundThread
        {
            get
            {
                return this.backgroundThread;
            }
            set
            {
                this.backgroundThread = value;
            }
        }

        /// <summary>
        /// [Get] The result of the worker method called.
        /// </summary>
        public object Result
        {
            get
            {
                return this.threadReturn;
            }
        }


        /// <summary>
        /// [Get/Set] The thread apartment type
        /// </summary>
        public ApartmentState ApartmentType
        {
            get
            {
                return this.apartmentType;
            }
            set
            {
                this.apartmentType = value;
            }
        }
        #endregion

        #region Public Delegates
        /// <summary>
        /// Delegate declaration representing signature of worker method with return value.
        /// </summary>
        public delegate object ThreadWorkerMethodWithReturn();

        /// <summary>
        /// Delegate declaration representing signature of worker method with return value and parameter.
        /// </summary>
        public delegate object ThreadWorkerMethodWithReturnAndParam(object param);

        /// <summary>
        /// Delegate declaration representing signature of worker method with no return value and no parameter
        /// </summary>
        public delegate void ThreadWorkerMethod();

        /// <summary>
        /// Delegate declaration representing signature of worker method with parameter.
        /// </summary>
        public delegate void ThreadWorkerMethodWithParam(object param);
        #endregion

        #region Public methods
        /// <summary>
        /// Constructs a ThreadDispatch object with the thread worker method.
        /// Default settings on the ThreadDispatch object are:
        ///     1. Parameter is null
        ///     2. Thread ApartmentState will be MTA
        ///     3. Background thread status will be false
        /// </summary>
        /// <param name="workerMethod">Delegate to be called to perform the work</param>
        public ThreadDispatch(ThreadWorkerMethodWithReturn workerMethod) : this()
        {
            InitializeThreadState(null, workerMethod, ApartmentState.MTA, false);
        }

        /// <summary>
        /// Constructs a ThreadDispatch object with the thread worker method.
        /// Default settings on the ThreadDispatch object are:
        ///     1. Parameter is null
        ///     2. Thread ApartmentState will be MTA
        ///     3. Background thread status will be false
        /// </summary>
        /// <param name="workerMethod">Delegate to be called to perform the work</param>
        public ThreadDispatch(ThreadWorkerMethodWithReturnAndParam workerMethod) : this()
        {
            InitializeThreadState(null, workerMethod, ApartmentState.MTA, false);
        }

        /// <summary>
        /// Constructs a ThreadDispatch object with the thread worker method.
        /// Default settings on the ThreadDispatch object are:
        ///     1. Parameter is null
        ///     2. Thread ApartmentState will be MTA
        ///     3. Background thread status will be false
        /// </summary>
        /// <param name="workerMethod">Delegate to be called to perform the work</param>
        public ThreadDispatch(ThreadWorkerMethodWithParam workerMethod) : this()
        {
            InitializeThreadState(null, workerMethod, ApartmentState.MTA, false);
        }

        /// <summary>
        /// Constructs a ThreadDispatch object with the thread worker method.
        /// Default settings on the ThreadDispatch object are:
        ///     1. Parameter is null
        ///     2. Thread ApartmentState will be MTA
        ///     3. Background thread status will be false
        /// </summary>
        /// <param name="workerMethod">Delegate to be called to perform the work</param>
        public ThreadDispatch(ThreadWorkerMethod workerMethod) : this()
        {
            InitializeThreadState(null, workerMethod, ApartmentState.MTA, false);
        }

        /// <summary>
        /// Starts the thread execution and waits for thread to finish. If an exception occurs in the worker method
        /// this method rethrows the exception.
        /// </summary>
        public void Start()
        {
            this.exception = null;
            DispatchThread();
            if (this.Exception != null)
            {
                throw this.Exception;
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Initializes the ThreadDispatch instance with initial values. Note that this constructor is private to avoid
        /// declaring instances without specifying at least a worker delegate instance.
        /// </summary>
        private ThreadDispatch()
        {
            this.thread = null;
            this.exception = null;
            this.threadParams = null;
            this.threadWorkerMethodWithReturn = null;
            this.threadWorkerMethodWithReturnAndParam = null;
            this.threadWorkerMethod = null;
            this.threadWorkerMethodWithParam = null;
            this.threadReturn = null;
            this.backgroundThread = false;
            this.apartmentType = ApartmentState.MTA;
        }

        /// <summary>
        /// Initializes the thread state members.
        /// </summary>
        /// <param name="threadParams">Parameters to be passed to thread</param>
        /// <param name="workerMethod">The delegate to be called from thread</param>
        /// <param name="aptState">The apartment of the thread created</param>
        /// <param name="background">Thread is created as a background or not</param>
        private void InitializeThreadState(object threadParams, ThreadWorkerMethodWithReturn workerMethod, ApartmentState aptState, bool background)
        {
            this.threadParams = threadParams;
            this.threadWorkerMethodWithReturn = workerMethod;
            this.thread = new Thread(new ThreadStart(ThreadEntryPointMethodWithReturn));
            this.thread.SetApartmentState(aptState);
            this.backgroundThread = background;
        }

        /// <summary>
        /// Initializes the thread state members.
        /// </summary>
        /// <param name="threadParams">Parameters to be passed to thread</param>
        /// <param name="workerMethod">The delegate to be called from thread</param>
        /// <param name="aptState">The apartment of the thread created</param>
        /// <param name="background">Thread is created as a background or not</param>
        private void InitializeThreadState(object threadParams, ThreadWorkerMethodWithReturnAndParam workerMethod, ApartmentState aptState, bool background)
        {
            this.threadParams = threadParams;
            this.threadWorkerMethodWithReturnAndParam = workerMethod;
            this.thread = new Thread(new ThreadStart(ThreadEntryPointMethodWithReturnAndParam));
            this.thread.SetApartmentState(aptState);
            this.backgroundThread = background;
        }

        /// <summary>
        /// Initializes the thread state members.
        /// </summary>
        /// <param name="threadParams">Parameters to be passed to thread</param>
        /// <param name="workerMethod">The delegate to be called from thread</param>
        /// <param name="aptState">The apartment of the thread created</param>
        /// <param name="background">Thread is created as a background or not</param>
        private void InitializeThreadState(object threadParams, ThreadWorkerMethod workerMethod, ApartmentState aptState, bool background)
        {
            this.threadParams = threadParams;
            this.threadWorkerMethod = workerMethod;
            this.thread = new Thread(new ThreadStart(ThreadEntryPoint));
            this.thread.SetApartmentState(aptState);
            this.backgroundThread = background;
        }

        /// <summary>
        /// Initializes the thread state members.
        /// </summary>
        /// <param name="threadParams">Parameters to be passed to thread</param>
        /// <param name="workerMethod">The delegate to be called from thread</param>
        /// <param name="aptState">The apartment of the thread created</param>
        /// <param name="background">Thread is created as a background or not</param>
        private void InitializeThreadState(object threadParams, ThreadWorkerMethodWithParam workerMethod, ApartmentState aptState, bool background)
        {
            this.threadParams = threadParams;
            this.threadWorkerMethodWithParam = workerMethod;
            this.thread = new Thread(new ThreadStart(ThreadEntryPointMethodWithParam));
            this.thread.SetApartmentState(aptState);
            this.backgroundThread = background;
        }


        /// <summary>
        /// Starts the execution of the thread and waits until the threadFinished event is signaled before continuing
        /// </summary>
        private void DispatchThread()
        {
            this.thread.Start();
            this.thread.Join();
        }

        /// <summary>
        /// Entry point for the newly created thread. This method is wrapped in a try/catch block and captures any
        /// exceptions thrown from the worker method and re-throws the exception.
        /// The worker method for this thread entry point has no parameter and no return value.
        /// </summary>
        private void ThreadEntryPoint()
        {
            try
            {
                this.threadWorkerMethod();
            }
            catch (Exception e)
            {
                this.exception = e;
            }
        }

        /// <summary>
        /// Entry point for the newly created thread. This method is wrapped in a try/catch block and captures any
        /// exceptions thrown from the worker method and re-throws the exception.
        /// The worker method for this thread entry point takes a parameter and no return value.
        /// </summary>

        private void ThreadEntryPointMethodWithParam()
        {
            try
            {
                this.threadWorkerMethodWithParam(this.threadParams);
            }
            catch (Exception e)
            {
                this.exception = e;
            }
        }

        /// <summary>
        /// Entry point for the newly created thread. This method is wrapped in a try/catch block and captures any
        /// exceptions thrown from the worker method and re-throws the exception.
        /// The worker method for this thread entry point has no parameter but does return a value.
        /// </summary>

        private void ThreadEntryPointMethodWithReturn()
        {
            try
            {
                this.threadReturn = this.threadWorkerMethodWithReturn();
            }
            catch (Exception e)
            {
                this.exception = e;
            }
        }

        /// <summary>
        /// Entry point for the newly created thread. This method is wrapped in a try/catch block and captures any
        /// exceptions thrown from the worker method and re-throws the exception.
        /// The worker method for this thread entry point has a parameter and return value.
        /// </summary>

        private void ThreadEntryPointMethodWithReturnAndParam()
        {
            try
            {
                this.threadReturn = this.threadWorkerMethodWithReturnAndParam(this.threadParams);
            }
            catch (Exception e)
            {
                this.exception = e;
            }
        }
        #endregion
    }
}
