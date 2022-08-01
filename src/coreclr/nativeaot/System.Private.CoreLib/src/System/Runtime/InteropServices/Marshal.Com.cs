// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;

using Internal.Reflection.Augments;

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        internal static bool IsBuiltInComSupported => false;

        private const int DISP_E_PARAMNOTFOUND = unchecked((int)0x80020004);

        public static int GetHRForException(Exception? e)
        {
            return PInvokeMarshal.GetHRForException(e);
        }

        public static bool AreComObjectsAvailableForCleanup() => false;

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr CreateAggregatedObject(IntPtr pOuter, object o)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        [SupportedOSPlatform("windows")]
        public static object BindToMoniker(string monikerName)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static void CleanupUnusedObjectsInCurrentContext()
        {
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o) where T : notnull
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object? CreateWrapperOfType(object? o, Type t)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static TWrapper CreateWrapperOfType<T, TWrapper>(T? o)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static void ChangeWrapperHandleStrength(object otp, bool fIsWeak)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static int FinalReleaseComObject(object o)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr GetComInterfaceForObject(object o, Type T)
        {
            if (o is null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            if (T is null)
            {
                throw new ArgumentNullException(nameof(T));
            }

            return ComWrappers.ComInterfaceForObject(o, T.GUID);
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr GetComInterfaceForObject(object o, Type T, CustomQueryInterfaceMode mode)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr GetComInterfaceForObject<T, TInterface>([DisallowNull] T o)
        {
            return GetComInterfaceForObject(o!, typeof(T));
        }

        [SupportedOSPlatform("windows")]
        public static object? GetComObjectData(object obj, object key)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr GetIDispatchForObject(object o)
        {
            if (o is null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            return ComWrappers.ComInterfaceForObject(o, new Guid(0x00020400, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46) /* IID_IDispatch */);
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr GetIUnknownForObject(object o)
        {
            return ComWrappers.ComInterfaceForObject(o);
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static unsafe void GetNativeVariantForObject(object? obj, IntPtr pDstNativeVariant)
        {
            if (pDstNativeVariant == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(pDstNativeVariant));
            }

            Variant* data = (Variant*)pDstNativeVariant;
            if (obj == null)
            {
                data->VariantType = VarEnum.VT_EMPTY;
                return;
            }

            switch (obj)
            {
                // Int and String most used types.
                case int value:
                    data->AsI4 = value;
                    break;
                case string value:
                    data->AsBstr = value;
                    break;

                case bool value:
                    data->AsBool = value;
                    break;
                case byte value:
                    data->AsUi1 = value;
                    break;
                case sbyte value:
                    data->AsI1 = value;
                    break;
                case short value:
                    data->AsI2 = value;
                    break;
                case ushort value:
                    data->AsUi2 = value;
                    break;
                case uint value:
                    data->AsUi4 = value;
                    break;
                case long value:
                    data->AsI8 = value;
                    break;
                case ulong value:
                    data->AsUi8 = value;
                    break;
                case float value:
                    data->AsR4 = value;
                    break;
                case double value:
                    data->AsR8 = value;
                    break;
                case DateTime value:
                    data->AsDate = value;
                    break;
                case decimal value:
                    data->AsDecimal = value;
                    break;
                case char value:
                    data->AsUi2 = value;
                    break;
                case BStrWrapper value:
                    data->AsBstr = value.WrappedObject;
                    break;
#pragma warning disable 0618 // CurrencyWrapper is obsolete
                case CurrencyWrapper value:
                    data->AsCy = value.WrappedObject;
                    break;
#pragma warning restore 0618
                case UnknownWrapper value:
                    data->AsUnknown = value.WrappedObject;
                    break;
                case DispatchWrapper value:
                    data->AsDispatch = value.WrappedObject;
                    break;
                case ErrorWrapper value:
                    data->AsError = value.ErrorCode;
                    break;
                case VariantWrapper value:
                    throw new ArgumentException();
                case DBNull value:
                    data->SetAsNULL();
                    break;
                case Missing value:
                    data->AsError = DISP_E_PARAMNOTFOUND;
                    break;
                case IConvertible value:
                    switch (value.GetTypeCode())
                    {
                        case TypeCode.Empty:
                            data->VariantType = VarEnum.VT_EMPTY;
                            break;
                        case TypeCode.Object:
                            data->AsUnknown = value;
                            break;
                        case TypeCode.DBNull:
                            data->SetAsNULL();
                            break;
                        case TypeCode.Boolean:
                            data->AsBool = value.ToBoolean(null);
                            break;
                        case TypeCode.Char:
                            data->AsUi2 = value.ToChar(null);
                            break;
                        case TypeCode.SByte:
                            data->AsI1 = value.ToSByte(null);
                            break;
                        case TypeCode.Byte:
                            data->AsUi1 = value.ToByte(null);
                            break;
                        case TypeCode.Int16:
                            data->AsI2 = value.ToInt16(null);
                            break;
                        case TypeCode.UInt16:
                            data->AsUi2 = value.ToUInt16(null);
                            break;
                        case TypeCode.Int32:
                            data->AsI4 = value.ToInt32(null);
                            break;
                        case TypeCode.UInt32:
                            data->AsUi4 = value.ToUInt32(null);
                            break;
                        case TypeCode.Int64:
                            data->AsI8 = value.ToInt64(null);
                            break;
                        case TypeCode.UInt64:
                            data->AsUi8 = value.ToUInt64(null);
                            break;
                        case TypeCode.Single:
                            data->AsR4 = value.ToSingle(null);
                            break;
                        case TypeCode.Double:
                            data->AsR8 = value.ToDouble(null);
                            break;
                        case TypeCode.Decimal:
                            data->AsDecimal = value.ToDecimal(null);
                            break;
                        case TypeCode.DateTime:
                            data->AsDate = value.ToDateTime(null);
                            break;
                        case TypeCode.String:
                            data->AsBstr = value.ToString();
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;
                case CriticalHandle:
                    throw new ArgumentException();
                case SafeHandle:
                    throw new ArgumentException();
                case Array:
                    // SAFEARRAY implementation goes here.
                    throw new NotSupportedException("VT_ARRAY");
                case ValueType:
                    throw new NotSupportedException("VT_RECORD");
                default:
                    data->AsDispatch = obj;
                    break;
            }
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void GetNativeVariantForObject<T>(T? obj, IntPtr pDstNativeVariant)
        {
            GetNativeVariantForObject((object?)obj, pDstNativeVariant);
        }

        [SupportedOSPlatform("windows")]
        public static object GetTypedObjectForIUnknown(IntPtr pUnk, Type t)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static object GetObjectForIUnknown(IntPtr pUnk)
        {
            return ComWrappers.ComObjectForInterface(pUnk);
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static unsafe object? GetObjectForNativeVariant(IntPtr pSrcNativeVariant)
        {
            if (pSrcNativeVariant == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(pSrcNativeVariant));
            }

            Variant* data = (Variant*)pSrcNativeVariant;

            if (data->IsEmpty)
            {
                return null;
            }

            switch (data->VariantType)
            {
                case VarEnum.VT_NULL:
                    return DBNull.Value;

                case VarEnum.VT_I1: return data->AsI1;
                case VarEnum.VT_I2: return data->AsI2;
                case VarEnum.VT_I4: return data->AsI4;
                case VarEnum.VT_I8: return data->AsI8;
                case VarEnum.VT_UI1: return data->AsUi1;
                case VarEnum.VT_UI2: return data->AsUi2;
                case VarEnum.VT_UI4: return data->AsUi4;
                case VarEnum.VT_UI8: return data->AsUi8;
                case VarEnum.VT_INT: return data->AsInt;
                case VarEnum.VT_UINT: return data->AsUint;
                case VarEnum.VT_BOOL: return data->AsBool;
                case VarEnum.VT_ERROR: return data->AsError;
                case VarEnum.VT_R4: return data->AsR4;
                case VarEnum.VT_R8: return data->AsR8;
                case VarEnum.VT_DECIMAL: return data->AsDecimal;
                case VarEnum.VT_CY: return data->AsCy;
                case VarEnum.VT_DATE: return data->AsDate;
                case VarEnum.VT_BSTR: return data->AsBstr;
                case VarEnum.VT_UNKNOWN: return data->AsUnknown;
                case VarEnum.VT_DISPATCH: return data->AsDispatch;

                default:
                    // Other VARIANT types not supported yet.
                    throw new NotSupportedException();
            }
        }

        [return: MaybeNull]
        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static T GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object?[] GetObjectsForNativeVariants(IntPtr aSrcNativeVariant, int cVars)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static T[] GetObjectsForNativeVariants<T>(IntPtr aSrcNativeVariant, int cVars)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static int GetStartComSlot(Type t)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static int GetEndComSlot(Type t)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        internal static Type? GetTypeFromCLSID(Guid clsid, string? server, bool throwOnError)
        {
            return ReflectionAugments.ReflectionCoreCallbacks.GetTypeFromCLSID(clsid, server, throwOnError);
        }

        [SupportedOSPlatform("windows")]
        public static string GetTypeInfoName(ITypeInfo typeInfo)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static object GetUniqueObjectForIUnknown(IntPtr unknown)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static bool IsComObject(object o)
        {
            if (o is null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            return false;
        }

        public static bool IsTypeVisibleFromCom(Type t)
        {
            if (t is null)
            {
                throw new ArgumentNullException(nameof(t));
            }
            return false;
        }

        [SupportedOSPlatform("windows")]
        public static int ReleaseComObject(object o)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static bool SetComObjectData(object obj, object key, object? data)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }
    }
}
