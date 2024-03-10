// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

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
        [return: NotNullIfNotNull(nameof(o))]
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
            ArgumentNullException.ThrowIfNull(o);
            ArgumentNullException.ThrowIfNull(T);

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
            ArgumentNullException.ThrowIfNull(o);

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
            ArgumentNullException.ThrowIfNull(pDstNativeVariant);

            ComVariant* data = (ComVariant*)pDstNativeVariant;
            if (obj == null)
            {
                *data = default;
                return;
            }

            switch (obj)
            {
                // Int and String most used types.
                case int value:
                    *data = ComVariant.Create(value);
                    break;
                case string value:
                    *data = ComVariant.Create(new BStrWrapper(value));
                    break;

                case bool value:
                    *data = ComVariant.Create(value);
                    break;
                case byte value:
                    *data = ComVariant.Create(value);
                    break;
                case sbyte value:
                    *data = ComVariant.Create(value);
                    break;
                case short value:
                    *data = ComVariant.Create(value);
                    break;
                case ushort value:
                    *data = ComVariant.Create(value);
                    break;
                case uint value:
                    *data = ComVariant.Create(value);
                    break;
                case long value:
                    *data = ComVariant.Create(value);
                    break;
                case ulong value:
                    *data = ComVariant.Create(value);
                    break;
                case float value:
                    *data = ComVariant.Create(value);
                    break;
                case double value:
                    *data = ComVariant.Create(value);
                    break;
                case DateTime value:
                    *data = ComVariant.Create(value);
                    break;
                case decimal value:
                    *data = ComVariant.Create(value);
                    break;
                case char value:
                    *data = ComVariant.Create(value);
                    break;
                case BStrWrapper value:
                    *data = ComVariant.Create(value);
                    break;
#pragma warning disable 0618 // CurrencyWrapper is obsolete
                case CurrencyWrapper value:
                    *data = ComVariant.Create(value);
                    break;
#pragma warning restore 0618
                case UnknownWrapper value:
                    *data = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN, GetIUnknownForObject(value.WrappedObject));
                    break;
                case DispatchWrapper value:
                    *data = ComVariant.CreateRaw(VarEnum.VT_DISPATCH, GetIDispatchForObject(value.WrappedObject));
                    break;
                case ErrorWrapper value:
                    *data = ComVariant.Create(value);
                    break;
                case VariantWrapper value:
                    throw new ArgumentException(null, nameof(obj));
                case DBNull value:
                    *data = ComVariant.Null;
                    break;
                case Missing value:
                    *data = ComVariant.CreateRaw(VarEnum.VT_ERROR, DISP_E_PARAMNOTFOUND);
                    break;
                case IConvertible value:
                    switch (value.GetTypeCode())
                    {
                        case TypeCode.Empty:
                            *data = default;
                            break;
                        case TypeCode.Object:
                            *data = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN, GetIUnknownForObject(value));
                            break;
                        case TypeCode.DBNull:
                            *data = ComVariant.Null;
                            break;
                        case TypeCode.Boolean:
                            *data = ComVariant.Create(value.ToBoolean(null));
                            break;
                        case TypeCode.Char:
                            *data = ComVariant.Create(value.ToChar(null));
                            break;
                        case TypeCode.SByte:
                            *data = ComVariant.Create(value.ToSByte(null));
                            break;
                        case TypeCode.Byte:
                            *data = ComVariant.Create(value.ToByte(null));
                            break;
                        case TypeCode.Int16:
                            *data = ComVariant.Create(value.ToInt16(null));
                            break;
                        case TypeCode.UInt16:
                            *data = ComVariant.Create(value.ToUInt16(null));
                            break;
                        case TypeCode.Int32:
                            *data = ComVariant.Create(value.ToInt32(null));
                            break;
                        case TypeCode.UInt32:
                            *data = ComVariant.Create(value.ToUInt32(null));
                            break;
                        case TypeCode.Int64:
                            *data = ComVariant.Create(value.ToInt64(null));
                            break;
                        case TypeCode.UInt64:
                            *data = ComVariant.Create(value.ToUInt64(null));
                            break;
                        case TypeCode.Single:
                            *data = ComVariant.Create(value.ToSingle(null));
                            break;
                        case TypeCode.Double:
                            *data = ComVariant.Create(value.ToDouble(null));
                            break;
                        case TypeCode.Decimal:
                            *data = ComVariant.Create(value.ToDecimal(null));
                            break;
                        case TypeCode.DateTime:
                            *data = ComVariant.Create(value.ToDateTime(null));
                            break;
                        case TypeCode.String:
                            *data = ComVariant.Create(new BStrWrapper(value.ToString(null)));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;
                case CriticalHandle:
                    throw new ArgumentException(null, nameof(obj));
                case SafeHandle:
                    throw new ArgumentException(null, nameof(obj));
                case Array:
                    // SAFEARRAY implementation goes here.
                    throw new NotSupportedException("VT_ARRAY");
                case ValueType:
                    throw new NotSupportedException("VT_RECORD");
                default:
                    *data = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN, GetIDispatchForObject(obj));
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
            ArgumentNullException.ThrowIfNull(pSrcNativeVariant);

            ComVariant* data = (ComVariant*)pSrcNativeVariant;

            switch (data->VarType)
            {
                case VarEnum.VT_EMPTY:
                    return null;
                case VarEnum.VT_NULL:
                    return DBNull.Value;

                case VarEnum.VT_I1: return data->As<sbyte>();
                case VarEnum.VT_I2: return data->As<short>();
                case VarEnum.VT_I4: return data->As<int>();
                case VarEnum.VT_I8: return data->As<long>();
                case VarEnum.VT_UI1: return data->As<byte>();
                case VarEnum.VT_UI2: return data->As<ushort>();
                case VarEnum.VT_UI4: return data->As<uint>();
                case VarEnum.VT_UI8: return data->As<ulong>();
                case VarEnum.VT_INT: return data->As<int>();
                case VarEnum.VT_UINT: return data->As<uint>();
                case VarEnum.VT_BOOL: return data->As<short>() != -1;
                case VarEnum.VT_ERROR: return data->As<int>();
                case VarEnum.VT_R4: return data->As<float>();
                case VarEnum.VT_R8: return data->As<double>();
                case VarEnum.VT_DECIMAL: return data->As<decimal>();
                case VarEnum.VT_CY: return decimal.FromOACurrency(data->GetRawDataRef<long>());
                case VarEnum.VT_DATE: return data->As<DateTime>();
                case VarEnum.VT_BSTR: return PtrToStringBSTR(data->GetRawDataRef<nint>());
                case VarEnum.VT_UNKNOWN: return GetObjectForIUnknown(data->GetRawDataRef<nint>());
                case VarEnum.VT_DISPATCH: return GetObjectForIUnknown(data->GetRawDataRef<nint>());

                default:
                    // Other VARIANT types not supported yet.
                    throw new NotSupportedException();
            }
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static T? GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
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

#pragma warning disable IDE0060
        internal static Type? GetTypeFromCLSID(Guid clsid, string? server, bool throwOnError)
        {
            throw new NotSupportedException(SR.PlatformNotSupported_ComInterop);
        }
#pragma warning restore

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
            ArgumentNullException.ThrowIfNull(o);
            return false;
        }

        public static bool IsTypeVisibleFromCom(Type t)
        {
            ArgumentNullException.ThrowIfNull(t);
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
