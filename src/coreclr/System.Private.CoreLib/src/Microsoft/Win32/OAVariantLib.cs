// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: This class only exists to provide support for
**          implementing IDispatch on managed objects. It is
**          used to provide OleAut style coercion rules.
**
**
===========================================================*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Win32
{
    internal static unsafe partial class OAVariantLib
    {
        #region Constants

        // Constants for VariantChangeType from OleAuto.h
        public const int LocalBool = 0x10;

        private static readonly Dictionary<Type, VarEnum> ClassTypes = new Dictionary<Type, VarEnum>
        {
            { typeof(bool), VarEnum.VT_BOOL },
            { typeof(char), VarEnum.VT_I2 },
            { typeof(sbyte), VarEnum.VT_I1 },
            { typeof(byte), VarEnum.VT_UI1 },
            { typeof(short), VarEnum.VT_I2 },
            { typeof(ushort), VarEnum.VT_UI2 },
            { typeof(int), VarEnum.VT_I4 },
            { typeof(uint), VarEnum.VT_UI4 },
            { typeof(long), VarEnum.VT_I8 },
            { typeof(ulong), VarEnum.VT_UI8 },
            { typeof(float), VarEnum.VT_R4 },
            { typeof(double), VarEnum.VT_R8 },
            { typeof(string), VarEnum.VT_BSTR },
            { typeof(DateTime), VarEnum.VT_DATE },
            { typeof(decimal), VarEnum.VT_DECIMAL },
        };

        #endregion


        #region Internal Methods

        /**
         * Changes a Variant from one type to another, calling the OLE
         * Automation VariantChangeTypeEx routine.  Note the legal types here are
         * restricted to the subset of what can be legally found in a VB
         * Variant and the types that CLR supports explicitly in the
         * CLR Variant class.
         */
        internal static object? ChangeType(object source, Type targetClass, short options, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(targetClass);
            ArgumentNullException.ThrowIfNull(culture);

            object? result = null;

            if (Variant.IsSystemDrawingColor(targetClass))
            {
                if (source is int || source is uint)
                {
                    uint sourceData = source is int ? (uint)(int)source : (uint)source;
                    // Int32/UInt32 can be converted to System.Drawing.Color
                    Variant.ConvertOleColorToSystemColor(ObjectHandleOnStack.Create(ref result), sourceData, targetClass.TypeHandle.Value);
                    Debug.Assert(result != null);
                    return result;
                }
            }

            if (!ClassTypes.TryGetValue(targetClass, out VarEnum vt))
            {
                throw new NotSupportedException(SR.NotSupported_ChangeType);
            }

            ComVariant vOp = ToOAVariant(source);
            ComVariant ret = default;

            int hr = Interop.OleAut32.VariantChangeTypeEx(&ret, &vOp, culture.LCID, options, (ushort)vt);

            using (vOp)
            using (ret)
            {
                if (hr < 0)
                {
                    OAFailed(hr);
                }

                result = FromOAVariant(ret);
                if (targetClass == typeof(char))
                {
                    result = (char)(uint)result!;
                }
            }

            return result;
        }

        private static void OAFailed(int hr)
        {
            switch (hr)
            {
                case HResults.COR_E_OUTOFMEMORY:
                    throw new OutOfMemoryException();
                case HResults.DISP_E_BADVARTYPE:
                    throw new NotSupportedException(SR.NotSupported_OleAutBadVarType);
                case HResults.DISP_E_DIVBYZERO:
                    throw new DivideByZeroException();
                case HResults.DISP_E_OVERFLOW:
                    throw new OverflowException();
                case HResults.DISP_E_TYPEMISMATCH:
                    throw new InvalidCastException(SR.InvalidCast_OATypeMismatch);
                case HResults.E_INVALIDARG:
                    throw new ArgumentException();
                default:
                    Debug.Fail("Unrecognized HResult - OAVariantLib routine failed in an unexpected way!");
                    throw Marshal.GetExceptionForHR(hr);
            }
        }

        private static ComVariant ToOAVariant(object input)
        {
            return input switch
            {
                string str => ComVariant.Create(str),
                DateTime dateTime => ComVariant.Create(dateTime),
                bool b => ComVariant.Create(b),
                decimal d => ComVariant.Create(d),
                sbyte i1 => ComVariant.Create(i1),
                byte u1 => ComVariant.Create(u1),
                short i2 => ComVariant.Create(i2),
                ushort u2 => ComVariant.Create(u2),
                int i4 => ComVariant.Create(i4),
                uint u4 => ComVariant.Create(u4),
                long i8 => ComVariant.Create(i8),
                ulong u8 => ComVariant.Create(u8),
                float r4 => ComVariant.Create(r4),
                double r8 => ComVariant.Create(r8),
                null => default,
                Missing => throw new NotSupportedException(SR.NotSupported_ChangeType),
                DBNull => ComVariant.Null,
                _ => GetComIPFromObjectRef(input) // Convert the object to an IDispatch/IUnknown pointer.
            };
        }

        private static ComVariant GetComIPFromObjectRef(object? obj)
        {
            IntPtr pUnk = GetIUnknownOrIDispatchForObject(ObjectHandleOnStack.Create(ref obj), out bool isIDispatch);
            return ComVariant.CreateRaw(isIDispatch ? VarEnum.VT_DISPATCH : VarEnum.VT_UNKNOWN, pUnk);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetIUnknownOrIDispatchForObject")]
        private static partial IntPtr GetIUnknownOrIDispatchForObject(ObjectHandleOnStack o, [MarshalAs(UnmanagedType.Bool)] out bool isIDispatch);

        private static object? FromOAVariant(ComVariant input) =>
            input.VarType switch
            {
                VarEnum.VT_BSTR => input.As<string>(),
                VarEnum.VT_DATE => input.As<DateTime>(),
                VarEnum.VT_BOOL => input.As<bool>(),
                VarEnum.VT_DECIMAL => input.As<decimal>(),
                VarEnum.VT_I1 => input.As<sbyte>(),
                VarEnum.VT_UI1 => input.As<byte>(),
                VarEnum.VT_I2 => input.As<short>(),
                VarEnum.VT_UI2 => input.As<ushort>(),
                VarEnum.VT_I4 or VarEnum.VT_INT => input.As<int>(),
                VarEnum.VT_UI4 or VarEnum.VT_UINT => input.As<uint>(),
                VarEnum.VT_I8 => input.As<long>(),
                VarEnum.VT_UI8 => input.As<ulong>(),
                VarEnum.VT_R4 => input.As<float>(),
                VarEnum.VT_R8 => input.As<double>(),
                _ => throw new NotSupportedException(SR.NotSupported_ChangeType),
            };

        #endregion
    }
}
