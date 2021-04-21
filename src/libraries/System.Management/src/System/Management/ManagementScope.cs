// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.ComponentModel.Design.Serialization;
using System.Security;
using Microsoft.Win32;

namespace System.Management
{
    internal static class CompatSwitches
    {
        internal const string DotNetVersion = "v4.0.30319";
        private const string RegKeyLocation = @"SOFTWARE\Microsoft\.NETFramework\" + DotNetVersion;

        private static readonly object s_syncLock = new object();
        private static int s_allowManagementObjectQI;

        private const string c_WMIDisableCOMSecurity = "WMIDisableCOMSecurity";

        public static bool AllowIManagementObjectQI
        {
            get
            {
                if (s_allowManagementObjectQI == 0)
                {
                    lock (s_syncLock)
                    {
                        if (s_allowManagementObjectQI == 0)
                        {
                            s_allowManagementObjectQI = GetSwitchValueFromRegistry() == true ? 1 : -1;
                        }
                    }
                }

                return s_allowManagementObjectQI == 1 ? true : false;
            }
        }

        private static bool GetSwitchValueFromRegistry()
        {
            RegistryKey s_switchesRegKey = null;
            try
            {
                s_switchesRegKey = Registry.LocalMachine.OpenSubKey(RegKeyLocation);

                if (s_switchesRegKey == null)
                {
                    return false;
                }

                return ((int)s_switchesRegKey.GetValue(c_WMIDisableCOMSecurity, -1 /* default */) == 1);
            }

            // ignore exceptions so that we don't crash the process if we can't read the switch value
            catch (Exception e)
            {
                if (e is StackOverflowException ||
                    e is OutOfMemoryException ||
                    e is System.Threading.ThreadAbortException ||
                    e is AccessViolationException)
                    throw;
            }
            finally
            {
                // dispose of the key
                if (s_switchesRegKey != null)
                {
                    s_switchesRegKey.Dispose();
                }
            }

            // if for any reason we cannot retrieve the value of the switch from the Registry,
            // fallback to 'false' which is the secure behavior
            return false;
        }
    }

    internal static class WmiNetUtilsHelper
    {
        internal delegate int ResetSecurity(IntPtr hToken);
        internal delegate int SetSecurity([In][Out] ref bool pNeedtoReset, [In][Out] ref IntPtr pHandle);
        internal delegate int BlessIWbemServices([MarshalAs(UnmanagedType.Interface)] IWbemServices pIUnknown,
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                        IntPtr password,
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority,
                                                                        int impersonationLevel,
                                                                        int authenticationLevel);
        internal delegate int BlessIWbemServicesObject([MarshalAs(UnmanagedType.IUnknown)] object pIUnknown,
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                        IntPtr password,
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority,
                                                                        int impersonationLevel,
                                                                        int authenticationLevel);

        internal delegate int GetPropertyHandle(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszPropertyName, [Out] out int pType, [Out] out int plHandle);
        internal delegate int WritePropertyValue(int vFunc, IntPtr pWbemClassObject, [In] int lHandle, [In] int lNumBytes, [In][MarshalAs(UnmanagedType.LPWStr)] string str);
        internal delegate int GetQualifierSet(int vFunc, IntPtr pWbemClassObject, [Out] out IntPtr ppQualSet);
        internal delegate int Get(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName, [In] int lFlags, [In][Out] ref object pVal, [In][Out] ref int pType, [In][Out] ref int plFlavor);
        internal delegate int Put(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName, [In] int lFlags, [In] ref object pVal, [In] int Type);
        internal delegate int Delete(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName);
        internal delegate int GetNames(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszQualifierName, [In] int lFlags, [In] ref object pQualifierVal, [Out][MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]  out string[] pNames);
        internal delegate int BeginEnumeration(int vFunc, IntPtr pWbemClassObject, [In] int lEnumFlags);
        internal delegate int Next(int vFunc, IntPtr pWbemClassObject, [In] int lFlags, [In][Out][MarshalAs(UnmanagedType.BStr)]  ref string strName, [In][Out] ref object pVal, [In][Out] ref int pType, [In][Out] ref int plFlavor);
        internal delegate int EndEnumeration(int vFunc, IntPtr pWbemClassObject);
        internal delegate int GetPropertyQualifierSet(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszProperty, [Out] out IntPtr ppQualSet);
        internal delegate int Clone(int vFunc, IntPtr pWbemClassObject, [Out] out IntPtr ppCopy);
        internal delegate int GetObjectText(int vFunc, IntPtr pWbemClassObject, [In] int lFlags, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrObjectText);
        internal delegate int SpawnDerivedClass(int vFunc, IntPtr pWbemClassObject, [In] int lFlags, [Out] out IntPtr ppNewClass);
        internal delegate int SpawnInstance(int vFunc, IntPtr pWbemClassObject, [In] int lFlags, [Out] out IntPtr ppNewInstance);
        internal delegate int CompareTo(int vFunc, IntPtr pWbemClassObject, [In] int lFlags, [In] IntPtr pCompareTo);
        internal delegate int GetPropertyOrigin(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrClassName);
        internal delegate int InheritsFrom(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string strAncestor);
        internal delegate int GetMethod(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName, [In] int lFlags, [Out]out IntPtr ppInSignature, [Out] out IntPtr ppOutSignature);
        internal delegate int PutMethod(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName, [In] int lFlags, [In] IntPtr pInSignature, [In] IntPtr pOutSignature);
        internal delegate int DeleteMethod(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName);
        internal delegate int BeginMethodEnumeration(int vFunc, IntPtr pWbemClassObject, [In] int lEnumFlags);
        internal delegate int NextMethod(int vFunc, IntPtr pWbemClassObject, [In] int lFlags, [Out][MarshalAs(UnmanagedType.BStr)] out string pstrName, [Out] out IntPtr ppInSignature, [Out] out IntPtr ppOutSignature);
        internal delegate int EndMethodEnumeration(int vFunc, IntPtr pWbemClassObject);
        internal delegate int GetMethodQualifierSet(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszMethod, [Out] out IntPtr ppQualSet);
        internal delegate int GetMethodOrigin(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszMethodName, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrClassName);
        internal delegate int QualifierSet_Get(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName, [In] int lFlags, [In][Out] ref object pVal, [In][Out] ref int plFlavor);
        internal delegate int QualifierSet_Put(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName, [In] ref object pVal, [In] int lFlavor);
        internal delegate int QualifierSet_Delete(int vFunc, IntPtr pWbemClassObject, [In][MarshalAs(UnmanagedType.LPWStr)]  string wszName);
        internal delegate int QualifierSet_GetNames(int vFunc, IntPtr pWbemClassObject, [In] int lFlags, [Out][MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]  out string[] pNames);
        internal delegate int QualifierSet_BeginEnumeration(int vFunc, IntPtr pWbemClassObject, [In] int lFlags);
        internal delegate int QualifierSet_Next(int vFunc, IntPtr pWbemClassObject, [In] int lFlags, [Out][MarshalAs(UnmanagedType.BStr)]  out string pstrName, [Out] out object pVal, [Out] out int plFlavor);
        internal delegate int QualifierSet_EndEnumeration(int vFunc, IntPtr pWbemClassObject);
        internal delegate int GetCurrentApartmentType(int vFunc, IntPtr pComThreadingInfo, [Out] out APTTYPE aptType);
        internal delegate void VerifyClientKey();
        internal delegate int GetDemultiplexedStub([In, MarshalAs(UnmanagedType.IUnknown)]object pIUnknown, [In]bool isLocal, [Out, MarshalAs(UnmanagedType.IUnknown)]out object ppIUnknown);
        internal delegate int CreateInstanceEnumWmi([In][MarshalAs(UnmanagedType.BStr)]  string strFilter,
                                                                                           [In] int lFlags,
                                                                                           [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx,
                                                                                           [Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum,
                                                                                           [In] int impLevel,
                                                                                           [In] int authnLevel,
                                                                                           [In] [MarshalAs(UnmanagedType.Interface)] IWbemServices pCurrentNamespace,
                                                                                           [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                                           [In]IntPtr strPassword,
                                                                                           [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority
                                                                                           );
        internal delegate int CreateClassEnumWmi([In][MarshalAs(UnmanagedType.BStr)]  string strSuperclass,
                                                                                       [In] int lFlags,
                                                                                       [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx,
                                                                                       [Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum,
                                                                                       [In] int impLevel,
                                                                                       [In] int authnLevel,
                                                                                       [In] [MarshalAs(UnmanagedType.Interface)] IWbemServices pCurrentNamespace,
                                                                                       [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                                       [In]IntPtr strPassword,
                                                                                       [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority
                                                                                       );
        internal delegate int ExecQueryWmi([In][MarshalAs(UnmanagedType.BStr)]  string strQueryLanguage,
                                                                           [In][MarshalAs(UnmanagedType.BStr)]  string strQuery,
                                                                           [In] int lFlags,
                                                                           [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx,
                                                                           [Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum,
                                                                           [In] int impLevel,
                                                                           [In] int authnLevel,
                                                                           [In] [MarshalAs(UnmanagedType.Interface)] IWbemServices pCurrentNamespace,
                                                                           [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                           [In]IntPtr strPassword,
                                                                           [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority
                                                                           );
        internal delegate int ExecNotificationQueryWmi([In][MarshalAs(UnmanagedType.BStr)]  string strQueryLanguage,
                                                                                                [In][MarshalAs(UnmanagedType.BStr)]  string strQuery,
                                                                                                [In] int lFlags,
                                                                                                [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx,
                                                                                                [Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum,
                                                                                                [In] int impLevel,
                                                                                                [In] int authnLevel,
                                                                                                [In] [MarshalAs(UnmanagedType.Interface)] IWbemServices pCurrentNamespace,
                                                                                                [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                                                [In]IntPtr strPassword,
                                                                                                [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority
                                                                                                );
        internal delegate int PutInstanceWmi([In] IntPtr pInst,
                                                                            [In] int lFlags,
                                                                            [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx,
                                                                            [In] IntPtr ppCallResult,
                                                                            [In] int impLevel,
                                                                            [In] int authnLevel,
                                                                            [In] [MarshalAs(UnmanagedType.Interface)] IWbemServices pCurrentNamespace,
                                                                            [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                            [In]IntPtr strPassword,
                                                                            [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority
                                                                            );
        internal delegate int PutClassWmi([In] IntPtr pObject,
                                                                        [In] int lFlags,
                                                                        [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx,
                                                                        [In] IntPtr ppCallResult,
                                                                        [In] int impLevel,
                                                                        [In] int authnLevel,
                                                                        [In] [MarshalAs(UnmanagedType.Interface)] IWbemServices pCurrentNamespace,
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                        [In]IntPtr strPassword,
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority
                                                                        );
        internal delegate int CloneEnumWbemClassObject(
                                                                [Out][MarshalAs(UnmanagedType.Interface)]  out IEnumWbemClassObject ppEnum,
                                                                [In] int impLevel,
                                                                [In] int authnLevel,
                                                                [In] [MarshalAs(UnmanagedType.Interface)] IEnumWbemClassObject pCurrentEnumWbemClassObject,
                                                                [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                [In]IntPtr strPassword,
                                                                [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority
                                                                );
        internal delegate int ConnectServerWmi(
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strNetworkResource,
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strUser,
                                                                        [In]  IntPtr strPassword,
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strLocale,
                                                                        [In] int lSecurityFlags,
                                                                        [In][MarshalAs(UnmanagedType.BStr)]  string strAuthority,
                                                                        [In][MarshalAs(UnmanagedType.Interface)]  IWbemContext pCtx,
                                                                        [Out][MarshalAs(UnmanagedType.Interface)]  out IWbemServices ppNamespace,
                                                                        int impersonationLevel,
                                                                        int authenticationLevel);

        internal delegate IntPtr GetErrorInfo();

        internal delegate int Initialize([In]bool AllowIManagementObjectQI);




        // 'Apartment Type' returned by IComThreadingInfo::GetCurrentApartmentType()
        internal enum APTTYPE
        {
            APTTYPE_CURRENT = -1,
            APTTYPE_STA = 0,
            APTTYPE_MTA = 1,
            APTTYPE_NA = 2,
            APTTYPE_MAINSTA = 3
        }
        internal static ResetSecurity ResetSecurity_f;
        internal static SetSecurity SetSecurity_f;
        internal static BlessIWbemServices BlessIWbemServices_f;
        internal static BlessIWbemServicesObject BlessIWbemServicesObject_f;
        internal static GetPropertyHandle GetPropertyHandle_f27;
        internal static WritePropertyValue WritePropertyValue_f28;
        internal static GetQualifierSet GetQualifierSet_f;
        internal static Get Get_f;
        internal static Put Put_f;
        internal static Delete Delete_f;
        internal static GetNames GetNames_f;
        internal static BeginEnumeration BeginEnumeration_f;
        internal static Next Next_f;
        internal static EndEnumeration EndEnumeration_f;
        internal static GetPropertyQualifierSet GetPropertyQualifierSet_f;
        internal static Clone Clone_f;
        internal static GetObjectText GetObjectText_f;
        internal static SpawnDerivedClass SpawnDerivedClass_f;
        internal static SpawnInstance SpawnInstance_f;
        internal static CompareTo CompareTo_f;
        internal static GetPropertyOrigin GetPropertyOrigin_f;
        internal static InheritsFrom InheritsFrom_f;
        internal static GetMethod GetMethod_f;
        internal static PutMethod PutMethod_f;
        internal static DeleteMethod DeleteMethod_f;
        internal static BeginMethodEnumeration BeginMethodEnumeration_f;
        internal static NextMethod NextMethod_f;
        internal static EndMethodEnumeration EndMethodEnumeration_f;
        internal static GetMethodQualifierSet GetMethodQualifierSet_f;
        internal static GetMethodOrigin GetMethodOrigin_f;
        internal static QualifierSet_Get QualifierGet_f;
        internal static QualifierSet_Put QualifierPut_f;
        internal static QualifierSet_Delete QualifierDelete_f;
        internal static QualifierSet_GetNames QualifierGetNames_f;
        internal static QualifierSet_BeginEnumeration QualifierBeginEnumeration_f;
        internal static QualifierSet_Next QualifierNext_f;
        internal static QualifierSet_EndEnumeration QualifierEndEnumeration_f;
        internal static GetCurrentApartmentType GetCurrentApartmentType_f;
        internal static VerifyClientKey VerifyClientKey_f;
        internal static Clone Clone_f12;
        internal static GetDemultiplexedStub GetDemultiplexedStub_f;
        internal static CreateInstanceEnumWmi CreateInstanceEnumWmi_f;
        internal static CreateClassEnumWmi CreateClassEnumWmi_f;
        internal static ExecQueryWmi ExecQueryWmi_f;
        internal static ExecNotificationQueryWmi ExecNotificationQueryWmi_f;
        internal static PutInstanceWmi PutInstanceWmi_f;
        internal static PutClassWmi PutClassWmi_f;
        internal static CloneEnumWbemClassObject CloneEnumWbemClassObject_f;
        internal static ConnectServerWmi ConnectServerWmi_f;
        internal static GetErrorInfo GetErrorInfo_f;
        internal static Initialize Initialize_f;

        static WmiNetUtilsHelper()
        {
            RegistryKey netFrameworkSubKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework\");
            string netFrameworkInstallRoot = (string)netFrameworkSubKey?.GetValue("InstallRoot");

            if (netFrameworkInstallRoot == null)
            {
                // In some Windows versions, like Nano Server, the .NET Framework is not installed by default.
                // It is possible that general failure to access the registry get to this code branch but it is
                // very unlikely.
                // Load PNSE delegates. This way it will throw PNSE when methods are used not when type is loaded.
                LoadPlatformNotSupportedDelegates(SR.PlatformNotSupported_FullFrameworkRequired);
                return;
            }

            string wminet_utilsPath = Path.Combine(
                netFrameworkInstallRoot,
                CompatSwitches.DotNetVersion, // The same value is hard coded on Environment.Version and quirks for WMI
                "wminet_utils.dll");

            IntPtr hModule = Interop.Kernel32.LoadLibrary(wminet_utilsPath);
            if (hModule == IntPtr.Zero)
            {
                // This is unlikely, so having the TypeInitializationException wrapping it is fine.
                throw new Win32Exception(Marshal.GetLastWin32Error(), SR.Format(SR.LoadLibraryFailed, wminet_utilsPath));
            }

            if (LoadDelegate(ref ResetSecurity_f, hModule, "ResetSecurity") &&
                LoadDelegate(ref SetSecurity_f, hModule, "SetSecurity") &&
                LoadDelegate(ref BlessIWbemServices_f, hModule, "BlessIWbemServices") &&
                LoadDelegate(ref BlessIWbemServicesObject_f, hModule, "BlessIWbemServicesObject") &&
                LoadDelegate(ref GetPropertyHandle_f27, hModule, "GetPropertyHandle") &&
                LoadDelegate(ref WritePropertyValue_f28, hModule, "WritePropertyValue") &&
                LoadDelegate(ref Clone_f12, hModule, "Clone") &&
                LoadDelegate(ref VerifyClientKey_f, hModule, "VerifyClientKey") &&
                LoadDelegate(ref GetQualifierSet_f, hModule, "GetQualifierSet") &&
                LoadDelegate(ref Get_f, hModule, "Get") &&
                LoadDelegate(ref Put_f, hModule, "Put") &&
                LoadDelegate(ref Delete_f, hModule, "Delete") &&
                LoadDelegate(ref GetNames_f, hModule, "GetNames") &&
                LoadDelegate(ref BeginEnumeration_f, hModule, "BeginEnumeration") &&
                LoadDelegate(ref Next_f, hModule, "Next") &&
                LoadDelegate(ref EndEnumeration_f, hModule, "EndEnumeration") &&
                LoadDelegate(ref GetPropertyQualifierSet_f, hModule, "GetPropertyQualifierSet") &&
                LoadDelegate(ref Clone_f, hModule, "Clone") &&
                LoadDelegate(ref GetObjectText_f, hModule, "GetObjectText") &&
                LoadDelegate(ref SpawnDerivedClass_f, hModule, "SpawnDerivedClass") &&
                LoadDelegate(ref SpawnInstance_f, hModule, "SpawnInstance") &&
                LoadDelegate(ref CompareTo_f, hModule, "CompareTo") &&
                LoadDelegate(ref GetPropertyOrigin_f, hModule, "GetPropertyOrigin") &&
                LoadDelegate(ref InheritsFrom_f, hModule, "InheritsFrom") &&
                LoadDelegate(ref GetMethod_f, hModule, "GetMethod") &&
                LoadDelegate(ref PutMethod_f, hModule, "PutMethod") &&
                LoadDelegate(ref DeleteMethod_f, hModule, "DeleteMethod") &&
                LoadDelegate(ref BeginMethodEnumeration_f, hModule, "BeginMethodEnumeration") &&
                LoadDelegate(ref NextMethod_f, hModule, "NextMethod") &&
                LoadDelegate(ref EndMethodEnumeration_f, hModule, "EndMethodEnumeration") &&
                LoadDelegate(ref GetMethodQualifierSet_f, hModule, "GetMethodQualifierSet") &&
                LoadDelegate(ref GetMethodOrigin_f, hModule, "GetMethodOrigin") &&
                LoadDelegate(ref QualifierGet_f, hModule, "QualifierSet_Get") &&
                LoadDelegate(ref QualifierPut_f, hModule, "QualifierSet_Put") &&
                LoadDelegate(ref QualifierDelete_f, hModule, "QualifierSet_Delete") &&
                LoadDelegate(ref QualifierGetNames_f, hModule, "QualifierSet_GetNames") &&
                LoadDelegate(ref QualifierBeginEnumeration_f, hModule, "QualifierSet_BeginEnumeration") &&
                LoadDelegate(ref QualifierNext_f, hModule, "QualifierSet_Next") &&
                LoadDelegate(ref QualifierEndEnumeration_f, hModule, "QualifierSet_EndEnumeration") &&
                LoadDelegate(ref GetCurrentApartmentType_f, hModule, "GetCurrentApartmentType") &&
                LoadDelegate(ref GetDemultiplexedStub_f, hModule, "GetDemultiplexedStub") &&
                LoadDelegate(ref CreateInstanceEnumWmi_f, hModule, "CreateInstanceEnumWmi") &&
                LoadDelegate(ref CreateClassEnumWmi_f, hModule, "CreateClassEnumWmi") &&
                LoadDelegate(ref ExecQueryWmi_f, hModule, "ExecQueryWmi") &&
                LoadDelegate(ref ExecNotificationQueryWmi_f, hModule, "ExecNotificationQueryWmi") &&
                LoadDelegate(ref PutInstanceWmi_f, hModule, "PutInstanceWmi") &&
                LoadDelegate(ref PutClassWmi_f, hModule, "PutClassWmi") &&
                LoadDelegate(ref CloneEnumWbemClassObject_f, hModule, "CloneEnumWbemClassObject") &&
                LoadDelegate(ref ConnectServerWmi_f, hModule, "ConnectServerWmi") &&
                LoadDelegate(ref GetErrorInfo_f, hModule, "GetErrorInfo") &&
                LoadDelegate(ref Initialize_f, hModule, "Initialize"))
            {
                // All required delegates were loaded.
                Initialize_f(CompatSwitches.AllowIManagementObjectQI);
            }
            else
            {
                LoadPlatformNotSupportedDelegates(SR.Format(SR.PlatformNotSupported_FrameworkUpdatedRequired, wminet_utilsPath));
            }
        }
        private static bool LoadDelegate<TDelegate>(ref TDelegate delegate_f, IntPtr hModule, string procName) where TDelegate : class
        {
            IntPtr procAddr = Interop.Kernel32.GetProcAddress(hModule, procName);
            return procAddr != IntPtr.Zero &&
                (delegate_f = Marshal.GetDelegateForFunctionPointer<TDelegate>(procAddr)) != null;
        }

        private static void LoadPlatformNotSupportedDelegates(string exceptionMessage)
        {
            ResetSecurity_f = (_) => throw new PlatformNotSupportedException(exceptionMessage);
            SetSecurity_f = (ref bool _, ref IntPtr __) => throw new PlatformNotSupportedException(exceptionMessage);
            BlessIWbemServices_f = (_, __, ___, ____, _____, ______) => throw new PlatformNotSupportedException(exceptionMessage);
            BlessIWbemServicesObject_f = (_, __, ___, ____, _____, ______) => throw new PlatformNotSupportedException(exceptionMessage);
            GetPropertyHandle_f27 = (int _, IntPtr __, string ___, out int ____, out int _____) => throw new PlatformNotSupportedException(exceptionMessage);
            WritePropertyValue_f28 = (_, __, ___, ____, _____) => throw new PlatformNotSupportedException(exceptionMessage);
            Clone_f12 = (int _, IntPtr __, out IntPtr ___) => throw new PlatformNotSupportedException(exceptionMessage);
            VerifyClientKey_f = () => throw new PlatformNotSupportedException(exceptionMessage);
            GetQualifierSet_f = (int _, IntPtr __, out IntPtr ___) => throw new PlatformNotSupportedException(exceptionMessage);
            Get_f = (int _, IntPtr __, string ___, int ____, ref object _____, ref int ______, ref int _______) => throw new PlatformNotSupportedException(exceptionMessage);
            Put_f = (int _, IntPtr __, string ___, int ____, ref object _____, int ______) => throw new PlatformNotSupportedException(exceptionMessage);
            Delete_f = (_, __, ___) => throw new PlatformNotSupportedException(exceptionMessage);
            GetNames_f = (int _, IntPtr __, string ___, int ____, ref object _____, out string[] ______) => throw new PlatformNotSupportedException(exceptionMessage);
            BeginEnumeration_f = (_, __, ___) => throw new PlatformNotSupportedException(exceptionMessage);
            Next_f = (int _, IntPtr __, int ___, ref string ____, ref object _____, ref int ______, ref int _______) => throw new PlatformNotSupportedException(exceptionMessage);
            EndEnumeration_f = (_, __) => throw new PlatformNotSupportedException(exceptionMessage);
            GetPropertyQualifierSet_f = (int _, IntPtr __, string ___, out IntPtr ____) => throw new PlatformNotSupportedException(exceptionMessage);
            Clone_f = (int _, IntPtr __, out IntPtr ___) => throw new PlatformNotSupportedException(exceptionMessage);
            GetObjectText_f = (int _, IntPtr __, int ___, out string ____) => throw new PlatformNotSupportedException(exceptionMessage);
            SpawnDerivedClass_f = (int _, IntPtr __, int ___, out IntPtr ____) => throw new PlatformNotSupportedException(exceptionMessage);
            SpawnInstance_f = (int _, IntPtr __, int ___, out IntPtr ____) => throw new PlatformNotSupportedException(exceptionMessage);
            CompareTo_f = (_, __, ___, ____) => throw new PlatformNotSupportedException(exceptionMessage);
            GetPropertyOrigin_f = (int _, IntPtr __, string ___, out string ____) => throw new PlatformNotSupportedException(exceptionMessage);
            InheritsFrom_f = (int _, IntPtr __, string ___) => throw new PlatformNotSupportedException(exceptionMessage);
            GetMethod_f = (int _, IntPtr __, string ___, int ____, out IntPtr _____, out IntPtr ______) => throw new PlatformNotSupportedException(exceptionMessage);
            PutMethod_f = (_, __, ___, ____, _____, ______) => throw new PlatformNotSupportedException(exceptionMessage);
            DeleteMethod_f = (_, __, ___) => throw new PlatformNotSupportedException(exceptionMessage);
            BeginMethodEnumeration_f = (_, __, ___) => throw new PlatformNotSupportedException(exceptionMessage);
            NextMethod_f = (int _, IntPtr __, int ___, out string ____, out IntPtr _____, out IntPtr ______) => throw new PlatformNotSupportedException(exceptionMessage);
            EndMethodEnumeration_f = (_, __) => throw new PlatformNotSupportedException(exceptionMessage);
            GetMethodQualifierSet_f = (int _, IntPtr __, string ___, out IntPtr ____) => throw new PlatformNotSupportedException(exceptionMessage);
            GetMethodOrigin_f = (int _, IntPtr __, string ___, out string ____) => throw new PlatformNotSupportedException(exceptionMessage);
            QualifierGet_f = (int _, IntPtr __, string ___, int ____, ref object _____, ref int ______) => throw new PlatformNotSupportedException(exceptionMessage);
            QualifierPut_f = (int _, IntPtr __, string ___, ref object ____, int _____) => throw new PlatformNotSupportedException(exceptionMessage);
            QualifierDelete_f = (_, __, ___) => throw new PlatformNotSupportedException(exceptionMessage);
            QualifierGetNames_f = (int _, IntPtr __, int ___, out string[] ____) => throw new PlatformNotSupportedException(exceptionMessage);
            QualifierBeginEnumeration_f = (_, __, ___) => throw new PlatformNotSupportedException(exceptionMessage);
            QualifierNext_f = (int _, IntPtr __, int ___, out string ____, out object _____, out int ______) => throw new PlatformNotSupportedException(exceptionMessage);
            QualifierEndEnumeration_f = (_, __) => throw new PlatformNotSupportedException(exceptionMessage);
            GetCurrentApartmentType_f = (int _, IntPtr __, out APTTYPE ___) => throw new PlatformNotSupportedException(exceptionMessage);
            GetDemultiplexedStub_f = (object _, bool __, out object ___) => throw new PlatformNotSupportedException(exceptionMessage);
            CreateInstanceEnumWmi_f = (string _, int __, IWbemContext ___, out IEnumWbemClassObject ____, int _____, int ______, IWbemServices _______, string ________, IntPtr _________, string __________) => throw new PlatformNotSupportedException(exceptionMessage);
            CreateClassEnumWmi_f = (string _, int __, IWbemContext ___, out IEnumWbemClassObject ____, int _____, int ______, IWbemServices _______, string ________, IntPtr _________, string __________) => throw new PlatformNotSupportedException(exceptionMessage);
            ExecQueryWmi_f = (string _, string __, int ___, IWbemContext ____, out IEnumWbemClassObject _____, int ______, int _______, IWbemServices ________, string _________, IntPtr __________, string ___________) => throw new PlatformNotSupportedException(exceptionMessage);
            ExecNotificationQueryWmi_f = (string _, string __, int ___, IWbemContext ____, out IEnumWbemClassObject _____, int ______, int _______, IWbemServices ________, string _________, IntPtr __________, string ___________) => throw new PlatformNotSupportedException(exceptionMessage);
            PutInstanceWmi_f = (_, __, ___, ____, _____, ______, _______, ________, _________, __________) => throw new PlatformNotSupportedException(exceptionMessage);
            PutClassWmi_f = (_, __, ___, ____, _____, ______, _______, ________, _________, __________) => throw new PlatformNotSupportedException(exceptionMessage);
            CloneEnumWbemClassObject_f = (out IEnumWbemClassObject _, int __, int ____, IEnumWbemClassObject _____, string ______, IntPtr _______, string ________) => throw new PlatformNotSupportedException(exceptionMessage);
            ConnectServerWmi_f = (string _, string __, IntPtr ___, string ____, int _____, string ______, IWbemContext _______, out IWbemServices ________, int _________, int __________) => throw new PlatformNotSupportedException(exceptionMessage);
            GetErrorInfo_f = () => throw new PlatformNotSupportedException(exceptionMessage);
            Initialize_f = (_) => throw new PlatformNotSupportedException(exceptionMessage);
        }
    }

    /// <summary>
    ///    <para>Represents a scope for management operations. In v1.0 the scope defines the WMI namespace in which management operations are performed.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates how to connect to root/default namespace
    /// // using ManagmentScope object.
    /// class Sample_ManagementScope
    /// {
    ///     public static int Main(string[] args)
    ///     {
    ///         ManagementScope scope = new ManagementScope("root\\default");
    ///         scope.Connect();
    ///         ManagementClass newClass = new ManagementClass(
    ///             scope,
    ///             new ManagementPath(),
    ///             null);
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates how to connect to root/default namespace
    /// ' using ManagmentScope object.
    /// Class Sample_ManagementScope
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim scope As New ManagementScope("root\default")
    ///         scope.Connect()
    ///         Dim newClass As New ManagementClass(scope, _
    ///             New ManagementPath(), _
    ///             Nothing)
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    [TypeConverter(typeof(ManagementScopeConverter))]
    public class ManagementScope : ICloneable
    {
        private ManagementPath validatedPath;
        private IWbemServices wbemServices;
        private ConnectionOptions options;
        internal event IdentifierChangedEventHandler IdentifierChanged;
        internal bool IsDefaulted; //used to tell whether the current scope has been created from the default
        //scope or not - this information is used to tell whether it can be overridden
        //when a new path is set or not.

        //Fires IdentifierChanged event
        private void FireIdentifierChanged()
        {
            if (IdentifierChanged != null)
                IdentifierChanged(this, null);
        }

        //Called when IdentifierChanged() event fires
        private void HandleIdentifierChange(object sender,
            IdentifierChangedEventArgs args)
        {
            // Since our object has changed we had better signal to ourself that
            // an connection needs to be established
            wbemServices = null;

            //Something inside ManagementScope changed, we need to fire an event
            //to the parent object
            FireIdentifierChanged();
        }

        // Private path property accessor which performs minimal validation on the
        // namespace path. IWbemPath cannot differentiate between a class or a name-
        // space if path separators are not present in the path.  Therefore, IWbemPath
        // will allow a namespace of "rootBeer" vs "root".  Since it is established
        // that the scope path is indeed a namespace path, we perform this validation.
        private ManagementPath prvpath
        {
            get
            {
                return validatedPath;
            }
            set
            {
                if (value != null)
                {
                    string pathValue = value.Path;
                    if (!ManagementPath.IsValidNamespaceSyntax(pathValue))
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)tag_WBEMSTATUS.WBEM_E_INVALID_NAMESPACE);
                }

                validatedPath = value;
            }
        }

        internal IWbemServices GetIWbemServices()
        {
            IWbemServices localCopy = wbemServices;
            //IWbemServices is always created in MTA context. Only if call is made through non MTA context we need to use IWbemServices in right context.
            // Lets start by assuming that we'll return the RCW that we already have. When WMINet_Utils.dll wraps the real COM proxy, credentials don't get
            // lost when the CLR marshals the wrapped object to a different COM apartment. The wrap was added to prevent marshalling of IManagedObject from native
            // to managed code.

            if (CompatSwitches.AllowIManagementObjectQI)
            {
                // Get an IUnknown for this apartment
                IntPtr pUnk = Marshal.GetIUnknownForObject(wbemServices);

                // Get an 'IUnknown RCW' for this apartment
                object unknown = Marshal.GetObjectForIUnknown(pUnk);

                // Release the ref count on the IUnknwon
                Marshal.Release(pUnk);

                // See if we are in the same apartment as where the original IWbemServices lived
                // If we are in a different apartment, give the caller an RCW generated just for their
                // apartment, and set the proxy blanket appropriately
                if (!object.ReferenceEquals(unknown, wbemServices))
                {
                    // We need to set the proxy blanket on 'unknown' or else the QI for IWbemServices may
                    // fail if we are running under a local user account.  The QI has to be done by
                    // someone who is a member of the 'Everyone' group on the target machine, or DCOM
                    // won't let the call through.
                    SecurityHandler securityHandler = GetSecurityHandler();
                    securityHandler.SecureIUnknown(unknown);

                    // Now, we can QI and secure the IWbemServices
                    localCopy = (IWbemServices)unknown;

                    // We still need to bless the IWbemServices in this apartment
                    securityHandler.Secure(localCopy);
                }
            }

            return localCopy; // STRANGE: Why does it still work if I return 'wbemServices'?
        }

        /// <summary>
        /// <para> Gets or sets a value indicating whether the <see cref='System.Management.ManagementScope'/> is currently bound to a
        ///    WMI server and namespace.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if a connection is alive (bound
        ///    to a server and namespace); otherwise, <see langword='false'/>.</para>
        /// </value>
        /// <remarks>
        ///    <para> A scope is disconnected after creation until someone
        ///       explicitly calls <see cref='System.Management.ManagementScope.Connect'/>(), or uses the scope for any
        ///       operation that requires a live connection. Also, the scope is
        ///       disconnected from the previous connection whenever the identifying properties of the scope are
        ///       changed.</para>
        /// </remarks>
        public bool IsConnected
        {
            get
            {
                return (null != wbemServices);
            }
        }

        //Internal constructor
        internal ManagementScope(ManagementPath path, IWbemServices wbemServices,
            ConnectionOptions options)
        {
            if (null != path)
                this.Path = path;

            if (null != options)
            {
                this.Options = options;
            }

            // We set this.wbemServices after setting Path and Options
            // because the latter operations can cause wbemServices to be NULLed.
            this.wbemServices = wbemServices;
        }

        internal ManagementScope(ManagementPath path, ManagementScope scope)
            : this(path, (null != scope) ? scope.options : null) { }

        internal static ManagementScope _Clone(ManagementScope scope)
        {
            return ManagementScope._Clone(scope, null);
        }

        internal static ManagementScope _Clone(ManagementScope scope, IdentifierChangedEventHandler handler)
        {
            ManagementScope scopeTmp = new ManagementScope(null, null, null);

            // Wire up change handler chain. Use supplied handler, if specified;
            // otherwise, default to that of the scope argument.
            if (handler != null)
                scopeTmp.IdentifierChanged = handler;
            else if (scope != null)
                scopeTmp.IdentifierChanged = new IdentifierChangedEventHandler(scope.HandleIdentifierChange);

            // Set scope path.
            if (scope == null)
            {
                // No path specified. Default.
                scopeTmp.prvpath = ManagementPath._Clone(ManagementPath.DefaultPath, new IdentifierChangedEventHandler(scopeTmp.HandleIdentifierChange));
                scopeTmp.IsDefaulted = true;

                scopeTmp.wbemServices = null;
                scopeTmp.options = null;
            }
            else
            {
                if (scope.prvpath == null)
                {
                    // No path specified. Default.
                    scopeTmp.prvpath = ManagementPath._Clone(ManagementPath.DefaultPath, new IdentifierChangedEventHandler(scopeTmp.HandleIdentifierChange));
                    scopeTmp.IsDefaulted = true;
                }
                else
                {
                    // Use scope-supplied path.
                    scopeTmp.prvpath = ManagementPath._Clone(scope.prvpath, new IdentifierChangedEventHandler(scopeTmp.HandleIdentifierChange));
                    scopeTmp.IsDefaulted = scope.IsDefaulted;
                }

                scopeTmp.wbemServices = scope.wbemServices;
                if (scope.options != null)
                    scopeTmp.options = ConnectionOptions._Clone(scope.options, new IdentifierChangedEventHandler(scopeTmp.HandleIdentifierChange));
            }

            return scopeTmp;
        }

        //Default constructor
        /// <overload>
        ///    Initializes a new instance
        ///    of the <see cref='System.Management.ManagementScope'/> class.
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementScope'/> class, with default values. This is the
        ///    default constructor.</para>
        /// </summary>
        /// <remarks>
        ///    <para> If the object doesn't have any
        ///       properties set before connection, it will be initialized with default values
        ///       (for example, the local machine and the root\cimv2 namespace).</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementScope s = new ManagementScope();
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementScope()
        ///    </code>
        /// </example>
        public ManagementScope() :
            this(new ManagementPath(ManagementPath.DefaultPath.Path))
        {
            //Flag that this scope uses the default path
            IsDefaulted = true;
        }

        //Parameterized constructors
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementScope'/> class representing
        ///    the specified scope path.</para>
        /// </summary>
        /// <param name='path'>A <see cref='System.Management.ManagementPath'/> containing the path to a server and namespace for the <see cref='System.Management.ManagementScope'/>.</param>
        /// <example>
        ///    <code lang='C#'>ManagementScope s = new ManagementScope(new ManagementPath("\\\\MyServer\\root\\default"));
        ///    </code>
        ///    <code lang='VB'>Dim p As New ManagementPath("\\MyServer\root\default")
        /// Dim s As New ManagementScope(p)
        ///    </code>
        /// </example>
        public ManagementScope(ManagementPath path) : this(path, (ConnectionOptions)null) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementScope'/> class representing the specified scope
        ///    path.</para>
        /// </summary>
        /// <param name='path'>The server and namespace path for the <see cref='System.Management.ManagementScope'/>.</param>
        /// <example>
        ///    <code lang='C#'>ManagementScope s = new ManagementScope("\\\\MyServer\\root\\default");
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementScope("\\MyServer\root\default")
        ///    </code>
        /// </example>
        public ManagementScope(string path) : this(new ManagementPath(path), (ConnectionOptions)null) { }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementScope'/> class representing the specified scope path,
        ///    with the specified options.</para>
        /// </summary>
        /// <param name='path'>The server and namespace for the <see cref='System.Management.ManagementScope'/>.</param>
        /// <param name=' options'>A <see cref='System.Management.ConnectionOptions'/> containing options for the connection.</param>
        /// <example>
        ///    <code lang='C#'>ConnectionOptions opt = new ConnectionOptions();
        /// opt.Username = "Me";
        /// opt.Password = "MyPassword";
        /// ManagementScope s = new ManagementScope("\\\\MyServer\\root\\default", opt);
        ///    </code>
        ///    <code lang='VB'>Dim opt As New ConnectionOptions()
        /// opt.Username = "Me"
        /// opt.Password = "MyPassword"
        /// Dim s As New ManagementScope("\\MyServer\root\default", opt);
        ///    </code>
        /// </example>
        public ManagementScope(string path, ConnectionOptions options) : this(new ManagementPath(path), options) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementScope'/> class representing the specified scope path,
        ///    with the specified options.</para>
        /// </summary>
        /// <param name='path'>A <see cref='System.Management.ManagementPath'/> containing the path to the server and namespace for the <see cref='System.Management.ManagementScope'/>.</param>
        /// <param name=' options'>The <see cref='System.Management.ConnectionOptions'/> containing options for the connection.</param>
        /// <example>
        ///    <code lang='C#'>ConnectionOptions opt = new ConnectionOptions();
        /// opt.Username = "Me";
        /// opt.Password = "MyPassword";
        ///
        /// ManagementPath p = new ManagementPath("\\\\MyServer\\root\\default");
        /// ManagementScope = new ManagementScope(p, opt);
        ///    </code>
        ///    <code lang='VB'>Dim opt As New ConnectionOptions()
        /// opt.UserName = "Me"
        /// opt.Password = "MyPassword"
        ///
        /// Dim p As New ManagementPath("\\MyServer\root\default")
        /// Dim s As New ManagementScope(p, opt)
        ///    </code>
        /// </example>
        public ManagementScope(ManagementPath path, ConnectionOptions options)
        {
            if (null != path)
                this.prvpath = ManagementPath._Clone(path, new IdentifierChangedEventHandler(HandleIdentifierChange));
            else
                this.prvpath = ManagementPath._Clone(null);

            if (null != options)
            {
                this.options = ConnectionOptions._Clone(options, new IdentifierChangedEventHandler(HandleIdentifierChange));
            }
            else
                this.options = null;

            IsDefaulted = false; //assume that this scope is not initialized by the default path
        }

        /// <summary>
        ///    <para> Gets or sets options for making the WMI connection.</para>
        /// </summary>
        /// <value>
        /// <para>The valid <see cref='System.Management.ConnectionOptions'/>
        /// containing options for the WMI connection.</para>
        /// </value>
        /// <example>
        ///    <code lang='C#'>//This constructor creates a scope object with default options
        /// ManagementScope s = new ManagementScope("root\\MyApp");
        ///
        /// //Change default connection options -
        /// //In this example, set the system privileges to enabled for operations that require system privileges.
        /// s.Options.EnablePrivileges = true;
        ///    </code>
        ///    <code lang='VB'>'This constructor creates a scope object with default options
        /// Dim s As New ManagementScope("root\\MyApp")
        ///
        /// 'Change default connection options -
        /// 'In this example, set the system privileges to enabled for operations that require system privileges.
        /// s.Options.EnablePrivileges = True
        ///    </code>
        /// </example>
        public ConnectionOptions Options
        {
            get
            {
                if (options == null)
                    return options = ConnectionOptions._Clone(null, new IdentifierChangedEventHandler(HandleIdentifierChange));
                else
                    return options;
            }
            set
            {
                if (null != value)
                {
                    if (null != options)
                        options.IdentifierChanged -= new IdentifierChangedEventHandler(HandleIdentifierChange);

                    options = ConnectionOptions._Clone((ConnectionOptions)value, new IdentifierChangedEventHandler(HandleIdentifierChange));

                    //the options property has changed so act like we fired the event
                    HandleIdentifierChange(this, null);
                }
                else
                    throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// <para>Gets or sets the path for the <see cref='System.Management.ManagementScope'/>.</para>
        /// </summary>
        /// <value>
        /// <para> A <see cref='System.Management.ManagementPath'/> containing
        ///    the path to a server and namespace.</para>
        /// </value>
        /// <example>
        ///    <code lang='C#'>ManagementScope s = new ManagementScope();
        /// s.Path = new ManagementPath("root\\MyApp");
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementScope()
        /// s.Path = New ManagementPath("root\MyApp")
        ///    </code>
        /// </example>
        public ManagementPath Path
        {
            get
            {
                if (prvpath == null)
                    return prvpath = ManagementPath._Clone(null);
                else
                    return prvpath;
            }
            set
            {
                if (null != value)
                {
                    if (null != prvpath)
                        prvpath.IdentifierChanged -= new IdentifierChangedEventHandler(HandleIdentifierChange);

                    IsDefaulted = false; //someone is specifically setting the scope path so it's not defaulted any more

                    prvpath = ManagementPath._Clone((ManagementPath)value, new IdentifierChangedEventHandler(HandleIdentifierChange));

                    //the path property has changed so act like we fired the event
                    HandleIdentifierChange(this, null);
                }
                else
                    throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        ///    <para>Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        /// <para>A new copy of the <see cref='System.Management.ManagementScope'/>.</para>
        /// </returns>
        public ManagementScope Clone()
        {
            return ManagementScope._Clone(this);
        }

        /// <summary>
        ///    <para>Clone a copy of this object.</para>
        /// </summary>
        /// <returns>
        ///    A new copy of this object.
        ///    object.
        /// </returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// <para>Connects this <see cref='System.Management.ManagementScope'/> to the actual WMI
        ///    scope.</para>
        /// </summary>
        /// <remarks>
        ///    <para>This method is called implicitly when the
        ///       scope is used in an operation that requires it to be connected. Calling it
        ///       explicitly allows the user to control the time of connection.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementScope s = new ManagementScope("root\\MyApp");
        ///
        /// //Explicit call to connect the scope object to the WMI namespace
        /// s.Connect();
        ///
        /// //The following doesn't do any implicit scope connections because s is already connected.
        /// ManagementObject o = new ManagementObject(s, "Win32_LogicalDisk='C:'", null);
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementScope("root\\MyApp")
        ///
        /// 'Explicit call to connect the scope object to the WMI namespace
        /// s.Connect()
        ///
        /// 'The following doesn't do any implicit scope connections because s is already connected.
        /// Dim o As New ManagementObject(s, "Win32_LogicalDisk=""C:""", null)
        ///    </code>
        /// </example>
        public void Connect()
        {
            Initialize();
        }

        internal void Initialize()
        {
            //If the path is not set yet we can't do it
            if (null == prvpath)
                throw new InvalidOperationException();


            /*
             * If we're not connected yet, this is the time to do it... We lock
             * the state to prevent 2 threads simultaneously doing the same
             * connection. To avoid taking the lock unnecessarily we examine
             * isConnected first
             */
            if (!IsConnected)
            {
                lock (this)
                {
                    if (!IsConnected)
                    {
                        // The locator cannot be marshalled across apartments, so we must create the locator
                        // and get the IWbemServices from an MTA thread
                        if (!MTAHelper.IsNoContextMTA())
                        {
                            //
                            // Ensure we are able to trap exceptions from worker thread.
                            //
                            ThreadDispatch disp = new ThreadDispatch(new ThreadDispatch.ThreadWorkerMethodWithParam(InitializeGuts));
                            disp.Parameter = this;
                            disp.Start();
                        }
                        else
                            InitializeGuts(this);
                    }
                }
            }
        }

        private void InitializeGuts(object o)
        {
            ManagementScope threadParam = (ManagementScope)o;

            if (null == threadParam.options)
            {
                threadParam.Options = new ConnectionOptions();
            }

            string nsPath = threadParam.prvpath.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY);

            // If no namespace specified, fill in the default one
            if ((null == nsPath) || (0 == nsPath.Length))
            {
                // NB: we use a special method to set the namespace
                // path here as we do NOT want to trigger an
                // IdentifierChanged event as a result of this set

                nsPath = threadParam.prvpath.SetNamespacePath(ManagementPath.DefaultPath.Path, out _);
            }

            int status = (int)ManagementStatus.NoError;
            threadParam.wbemServices = null;

            //If we're on XP or higher, always use the "max_wait" flag to avoid hanging
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {

                if (((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)) || (Environment.OSVersion.Version.Major >= 6))
                {
                    threadParam.options.Flags |= (int)tag_WBEM_CONNECT_OPTIONS.WBEM_FLAG_CONNECT_USE_MAX_WAIT;
                }
            }

            try
            {
                status = GetSecuredConnectHandler().ConnectNSecureIWbemServices(nsPath, ref threadParam.wbemServices);
            }
            catch (COMException e)
            {
                ManagementException.ThrowWithExtendedInfo(e);
            }

            if ((status & 0xfffff000) == 0x80041000)
            {
                ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
            }
            else if ((status & 0x80000000) != 0)
            {
                Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

        internal SecurityHandler GetSecurityHandler()
        {
            return new SecurityHandler(this);
        }
        internal SecuredConnectHandler GetSecuredConnectHandler()
        {
            return new SecuredConnectHandler(this);
        }
        internal SecuredIEnumWbemClassObjectHandler GetSecuredIEnumWbemClassObjectHandler(IEnumWbemClassObject pEnumWbemClassObject)
        {
            return new SecuredIEnumWbemClassObjectHandler(this, pEnumWbemClassObject);
        }
        internal SecuredIWbemServicesHandler GetSecuredIWbemServicesHandler(IWbemServices pWbemServiecs)
        {
            return new SecuredIWbemServicesHandler(this, pWbemServiecs);
        }

    }//ManagementScope

    internal sealed class SecuredIEnumWbemClassObjectHandler
    {
        private readonly IEnumWbemClassObject pEnumWbemClassObjectsecurityHelper;
        private readonly ManagementScope scope;
        internal SecuredIEnumWbemClassObjectHandler(ManagementScope theScope, IEnumWbemClassObject pEnumWbemClassObject)
        {
            this.scope = theScope;
            pEnumWbemClassObjectsecurityHelper = pEnumWbemClassObject;
        }
        internal int Reset_()
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pEnumWbemClassObjectsecurityHelper.Reset_();
            return status;
        }
        internal int Next_(int lTimeout, uint uCount, IWbemClassObject_DoNotMarshal[] ppOutParams, ref uint puReturned)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pEnumWbemClassObjectsecurityHelper.Next_(lTimeout, uCount, ppOutParams, out puReturned);
            return status;
        }
        internal int NextAsync_(uint uCount, IWbemObjectSink pSink)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pEnumWbemClassObjectsecurityHelper.NextAsync_(uCount, pSink);
            return status;
        }
        internal int Clone_(ref IEnumWbemClassObject ppEnum)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            if (null != scope)
            {
                IntPtr password = scope.Options.GetPassword();
                status = WmiNetUtilsHelper.CloneEnumWbemClassObject_f(
                    out ppEnum,
                    (int)scope.Options.Authentication,
                    (int)scope.Options.Impersonation,
                    pEnumWbemClassObjectsecurityHelper,
                    scope.Options.Username,
                    password,
                    scope.Options.Authority);
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
            }
            return status;
        }
        internal int Skip_(int lTimeout, uint nCount)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pEnumWbemClassObjectsecurityHelper.Skip_(lTimeout, nCount);
            return status;
        }
    }


    internal sealed class SecuredConnectHandler
    {
        private readonly ManagementScope scope;

        internal SecuredConnectHandler(ManagementScope theScope)
        {
            this.scope = theScope;
        }
        internal int ConnectNSecureIWbemServices(string path, ref IWbemServices pServices)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            if (null != scope)
            {
                bool needToReset = false;
                IntPtr handle = IntPtr.Zero;

                try
                {
                    if (scope.Options.EnablePrivileges && !CompatSwitches.AllowIManagementObjectQI)
                    {
                        WmiNetUtilsHelper.SetSecurity_f(ref needToReset, ref handle);
                    }

                    IntPtr password = scope.Options.GetPassword();
                    status = WmiNetUtilsHelper.ConnectServerWmi_f(
                        path,
                        scope.Options.Username,
                        password,
                        scope.Options.Locale,
                        scope.Options.Flags,
                        scope.Options.Authority,
                        scope.Options.GetContext(),
                        out pServices,
                        (int)scope.Options.Impersonation,
                        (int)scope.Options.Authentication);
                    System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
                }
                finally
                {
                    if (needToReset)
                    {
                        needToReset = false;
                        WmiNetUtilsHelper.ResetSecurity_f(handle);
                    }
                }
            }
            return status;
        }
    }

    internal sealed class SecuredIWbemServicesHandler
    {
        private readonly IWbemServices pWbemServiecsSecurityHelper;
        private readonly ManagementScope scope;
        internal SecuredIWbemServicesHandler(ManagementScope theScope, IWbemServices pWbemServiecs)
        {
            this.scope = theScope;
            pWbemServiecsSecurityHelper = pWbemServiecs;
        }
        internal int OpenNamespace_(string strNamespace, int lFlags, ref IWbemServices ppWorkingNamespace, IntPtr ppCallResult)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_NOT_SUPPORTED;
            //This should go through WMINET_utils layer and ppWorkingNamespace should be secured. See implementation of CreateInstanceEnum method.
            return status;
        }

        internal int CancelAsyncCall_(IWbemObjectSink pSink)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.CancelAsyncCall_(pSink);
            return status;
        }
        internal int QueryObjectSink_(int lFlags, ref IWbemObjectSink ppResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.QueryObjectSink_(lFlags, out ppResponseHandler);
            return status;
        }
        internal int GetObject_(string strObjectPath, int lFlags, IWbemContext pCtx, ref IWbemClassObjectFreeThreaded ppObject, IntPtr ppCallResult)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.GetObject_(strObjectPath, lFlags, pCtx, out ppObject, ppCallResult);
            return status;
        }

        internal int GetObjectAsync_(string strObjectPath, int lFlags, IWbemContext pCtx, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.GetObjectAsync_(strObjectPath, lFlags, pCtx, pResponseHandler);
            return status;
        }
        internal int PutClass_(IWbemClassObjectFreeThreaded pObject, int lFlags, IWbemContext pCtx, IntPtr ppCallResult)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            if (null != scope)
            {
                IntPtr password = scope.Options.GetPassword();
                status = WmiNetUtilsHelper.PutClassWmi_f(pObject,
                    lFlags,
                    pCtx,
                    ppCallResult,
                    (int)scope.Options.Authentication,
                    (int)scope.Options.Impersonation,
                    pWbemServiecsSecurityHelper,
                    scope.Options.Username,
                    password,
                    scope.Options.Authority);
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
            }
            return status;
        }
        internal int PutClassAsync_(IWbemClassObjectFreeThreaded pObject, int lFlags, IWbemContext pCtx, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.PutClassAsync_(pObject, lFlags, pCtx, pResponseHandler);
            return status;
        }
        internal int DeleteClass_(string strClass, int lFlags, IWbemContext pCtx, IntPtr ppCallResult)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.DeleteClass_(strClass, lFlags, pCtx, ppCallResult);
            return status;
        }
        internal int DeleteClassAsync_(string strClass, int lFlags, IWbemContext pCtx, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.DeleteClassAsync_(strClass, lFlags, pCtx, pResponseHandler);
            return status;
        }
        internal int CreateClassEnum_(string strSuperClass, int lFlags, IWbemContext pCtx, ref IEnumWbemClassObject ppEnum)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            if (null != scope)
            {
                IntPtr password = scope.Options.GetPassword();
                status = WmiNetUtilsHelper.CreateClassEnumWmi_f(strSuperClass,
                    lFlags,
                    pCtx,
                    out ppEnum,
                    (int)scope.Options.Authentication,
                    (int)scope.Options.Impersonation,
                    pWbemServiecsSecurityHelper,
                    scope.Options.Username,
                    password,
                    scope.Options.Authority);
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
            }
            return status;
        }
        internal int CreateClassEnumAsync_(string strSuperClass, int lFlags, IWbemContext pCtx, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.CreateClassEnumAsync_(strSuperClass, lFlags, pCtx, pResponseHandler);
            return status;
        }
        internal int PutInstance_(IWbemClassObjectFreeThreaded pInst, int lFlags, IWbemContext pCtx, IntPtr ppCallResult)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            if (null != scope)
            {
                IntPtr password = scope.Options.GetPassword();
                status = WmiNetUtilsHelper.PutInstanceWmi_f(pInst,
                    lFlags,
                    pCtx,
                    ppCallResult,
                    (int)scope.Options.Authentication,
                    (int)scope.Options.Impersonation,
                    pWbemServiecsSecurityHelper,
                    scope.Options.Username,
                    password,
                    scope.Options.Authority);
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
            }
            return status;
        }
        internal int PutInstanceAsync_(IWbemClassObjectFreeThreaded pInst, int lFlags, IWbemContext pCtx, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.PutInstanceAsync_(pInst, lFlags, pCtx, pResponseHandler);
            return status;
        }
        internal int DeleteInstance_(string strObjectPath, int lFlags, IWbemContext pCtx, IntPtr ppCallResult)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.DeleteInstance_(strObjectPath, lFlags, pCtx, ppCallResult);
            return status;
        }
        internal int DeleteInstanceAsync_(string strObjectPath, int lFlags, IWbemContext pCtx, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.DeleteInstanceAsync_(strObjectPath, lFlags, pCtx, pResponseHandler);
            return status;
        }

        internal int CreateInstanceEnum_(string strFilter, int lFlags, IWbemContext pCtx, ref IEnumWbemClassObject ppEnum)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            if (null != scope)
            {
                IntPtr password = scope.Options.GetPassword();
                status = WmiNetUtilsHelper.CreateInstanceEnumWmi_f(strFilter,
                    lFlags,
                    pCtx,
                    out ppEnum,
                    (int)scope.Options.Authentication,
                    (int)scope.Options.Impersonation,
                    pWbemServiecsSecurityHelper,
                    scope.Options.Username,
                    password,
                    scope.Options.Authority);
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
            }
            return status;
        }
        internal int CreateInstanceEnumAsync_(string strFilter, int lFlags, IWbemContext pCtx, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.CreateInstanceEnumAsync_(strFilter, lFlags, pCtx, pResponseHandler);
            return status;
        }
        internal int ExecQuery_(string strQueryLanguage, string strQuery, int lFlags, IWbemContext pCtx, ref IEnumWbemClassObject ppEnum)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            if (null != scope)
            {
                IntPtr password = scope.Options.GetPassword();
                status = WmiNetUtilsHelper.ExecQueryWmi_f(strQueryLanguage,
                    strQuery,
                    lFlags,
                    pCtx,
                    out ppEnum,
                    (int)scope.Options.Authentication,
                    (int)scope.Options.Impersonation,
                    pWbemServiecsSecurityHelper,
                    scope.Options.Username,
                    password,
                    scope.Options.Authority);
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
            }
            return status;
        }
        internal int ExecQueryAsync_(string strQueryLanguage, string strQuery, int lFlags, IWbemContext pCtx, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.ExecQueryAsync_(strQueryLanguage, strQuery, lFlags, pCtx, pResponseHandler);
            return status;
        }
        internal int ExecNotificationQuery_(string strQueryLanguage, string strQuery, int lFlags, IWbemContext pCtx, ref IEnumWbemClassObject ppEnum)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            if (null != scope)
            {
                IntPtr password = scope.Options.GetPassword();
                status = WmiNetUtilsHelper.ExecNotificationQueryWmi_f(strQueryLanguage,
                    strQuery,
                    lFlags,
                    pCtx,
                    out ppEnum,
                    (int)scope.Options.Authentication,
                    (int)scope.Options.Impersonation,
                    pWbemServiecsSecurityHelper,
                    scope.Options.Username,
                    password,
                    scope.Options.Authority);
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
            }
            return status;
        }
        internal int ExecNotificationQueryAsync_(string strQueryLanguage, string strQuery, int lFlags, IWbemContext pCtx, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.ExecNotificationQueryAsync_(strQueryLanguage, strQuery, lFlags, pCtx, pResponseHandler);
            return status;
        }
        internal int ExecMethod_(string strObjectPath, string strMethodName, int lFlags, IWbemContext pCtx, IWbemClassObjectFreeThreaded pInParams, ref IWbemClassObjectFreeThreaded ppOutParams, IntPtr ppCallResult)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.ExecMethod_(strObjectPath, strMethodName, lFlags, pCtx, pInParams, out ppOutParams, ppCallResult);
            return status;
        }
        internal int ExecMethodAsync_(string strObjectPath, string strMethodName, int lFlags, IWbemContext pCtx, IWbemClassObjectFreeThreaded pInParams, IWbemObjectSink pResponseHandler)
        {
            int status = (int)tag_WBEMSTATUS.WBEM_E_FAILED;
            status = pWbemServiecsSecurityHelper.ExecMethodAsync_(strObjectPath, strMethodName, lFlags, pCtx, pInParams, pResponseHandler);
            return status;
        }
    }


    internal sealed class SecurityHandler
    {
        private bool needToReset;
        private readonly IntPtr handle;
        private readonly ManagementScope scope;

        internal SecurityHandler(ManagementScope theScope)
        {
            this.scope = theScope;
            if (null != scope)
            {
                if (scope.Options.EnablePrivileges)
                {
                    WmiNetUtilsHelper.SetSecurity_f(ref needToReset, ref handle);
                }
            }
        }

        internal void Reset()
        {
            if (needToReset)
            {
                needToReset = false;

                if (null != scope)
                {
                    WmiNetUtilsHelper.ResetSecurity_f(handle);
                }
            }

        }


        internal void Secure(IWbemServices services)
        {
            if (null != scope)
            {
                IntPtr password = scope.Options.GetPassword();
                int status = WmiNetUtilsHelper.BlessIWbemServices_f
                    (
                    services,
                    scope.Options.Username,
                    password,
                    scope.Options.Authority,
                    (int)scope.Options.Impersonation,
                    (int)scope.Options.Authentication
                    );
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
                if (status < 0)
                {
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        internal void SecureIUnknown(object unknown)
        {
            // We use a hack to call BlessIWbemServices with an IUnknown instead of an IWbemServices
            // In VNext, we should really change the implementation of WMINet_Utils.dll so that it has
            // a method which explicitly takes an IUnknown.  We rely on the fact that the implementation
            // of BlessIWbemServices actually casts the first parameter to IUnknown before blessing
            if (null != scope)
            {
                IntPtr password = scope.Options.GetPassword();
                int status = WmiNetUtilsHelper.BlessIWbemServicesObject_f
                    (
                    unknown,
                    scope.Options.Username,
                    password,
                    scope.Options.Authority,
                    (int)scope.Options.Impersonation,
                    (int)scope.Options.Authentication
                    );
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(password);
                if (status < 0)
                {
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }


    } //SecurityHandler


    /// <summary>
    /// Converts a String to a ManagementScope
    /// </summary>
    internal sealed class ManagementScopeConverter : ExpandableObjectConverter
    {

        /// <summary>
        /// Determines if this converter can convert an object in the given source type to the native type of the converter.
        /// </summary>
        /// <param name='context'>An ITypeDescriptorContext that provides a format context.</param>
        /// <param name='sourceType'>A Type that represents the type you wish to convert from.</param>
        /// <returns>
        ///    <para>true if this converter can perform the conversion; otherwise, false.</para>
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if ((sourceType == typeof(ManagementScope)))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object to the given destination type using the context.
        /// </summary>
        /// <param name='context'>An ITypeDescriptorContext that provides a format context.</param>
        /// <param name='destinationType'>A Type that represents the type you wish to convert to.</param>
        /// <returns>
        ///    <para>true if this converter can perform the conversion; otherwise, false.</para>
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if ((destinationType == typeof(InstanceDescriptor)))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///      Converts the given object to another type.  The most common types to convert
        ///      are to and from a string object.  The default implementation will make a call
        ///      to ToString on the object if the object is valid and if the destination
        ///      type is string.  If this cannot convert to the desitnation type, this will
        ///      throw a NotSupportedException.
        /// </summary>
        /// <param name='context'>An ITypeDescriptorContext that provides a format context.</param>
        /// <param name='culture'>A CultureInfo object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed.</param>
        /// <param name='value'>The Object to convert.</param>
        /// <param name='destinationType'>The Type to convert the value parameter to.</param>
        /// <returns>An Object that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {

            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (value is ManagementScope && destinationType == typeof(InstanceDescriptor))
            {
                ManagementScope obj = ((ManagementScope)(value));
                ConstructorInfo ctor = typeof(ManagementScope).GetConstructor(new Type[] { typeof(string) });
                if (ctor != null)
                {
                    return new InstanceDescriptor(ctor, new object[] { obj.Path.Path });
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
