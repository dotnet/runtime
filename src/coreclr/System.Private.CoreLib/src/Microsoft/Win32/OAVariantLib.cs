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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Win32
{
    internal static unsafe partial class OAVariantLib
    {
        #region Constants

        // Constants for VariantChangeType from OleAuto.h
        public const int NoValueProp = 0x01;
        public const int AlphaBool = 0x02;
        public const int NoUserOverride = 0x04;
        public const int CalendarHijri = 0x08;
        public const int LocalBool = 0x10;

        private static readonly Dictionary<Type, VarEnum> ClassTypes = new Dictionary<Type, VarEnum>
        {
            { typeof(Empty), VarEnum.VT_EMPTY },
            { typeof(void), VarEnum.VT_VOID },
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
            { typeof(object), VarEnum.VT_UNKNOWN },
            { typeof(decimal), VarEnum.VT_DECIMAL },
            { typeof(DBNull), VarEnum.VT_NULL },
        };

        #endregion


        #region Internal Methods

#pragma warning disable CS8500

        /**
         * Changes a Variant from one type to another, calling the OLE
         * Automation VariantChangeTypeEx routine.  Note the legal types here are
         * restricted to the subset of what can be legally found in a VB
         * Variant and the types that CLR supports explicitly in the
         * CLR Variant class.
         */
        internal static object ChangeType(object source, Type targetClass, short options, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(targetClass);
            ArgumentNullException.ThrowIfNull(culture);

            object? result = null;

            TypeHandle th = ((RuntimeType)targetClass).GetNativeTypeHandle();
            Debug.Assert(!th.IsTypeDesc);
            if (Variant.IsSystemDrawingColor(th.AsMethodTable()))
            {
                if (source.GetType() == typeof(int) || source.GetType() == typeof(uint))
                {
                    uint sourceData = source.GetType() == typeof(int) ? (uint)(int)source : (uint)source;
                    // Int32/UInt32 can be converted to System.Drawing.Color
                    ConvertOleColorToSystemColor(ObjectHandleOnStack.Create(ref result), sourceData);
                    Debug.Assert(result != null);
                    return result;
                }
            }

            VarEnum vt = GetVTFromClass(targetClass);

            ComVariant vOp = ToOAVariant(source);
            ComVariant ret = default;

            int hr = VariantChangeTypeEx(ref ret, ref vOp, culture.LCID, options, (ushort)vt);

            using (vOp)
            using (ret)
            {
                if (hr < 0)
                    OAFailed(hr);

                result = FromOAVariant(ret);
                if (targetClass == typeof(char))
                {
                    result = (char)(uint)result;
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
                case unchecked((int)0x80020008): // DISP_E_BADVARTYPE
                    throw new NotSupportedException(SR.NotSupported_OleAutBadVarType);
                case HResults.COR_E_DIVIDEBYZERO:
                    throw new DivideByZeroException();
                case HResults.COR_E_OVERFLOW:
                    throw new OverflowException();
                case HResults.TYPE_E_TYPEMISMATCH:
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

        }

        private static object FromOAVariant(ComVariant input)
        {

        }

        private static VarEnum GetVTFromClass(Type type)
        {
            if (ClassTypes.TryGetValue(type, out VarEnum vt))
                return vt;

            throw new NotSupportedException(SR.NotSupported_ChangeType);
        }

        [LibraryImport(Interop.Libraries.OleAut32)]
        private static partial int VariantChangeTypeEx(ref ComVariant pVarRes, ref ComVariant pVarSrc, int lcid, short wFlags, ushort vt);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Variant_ConvertOleColorToSystemColor")]
        private static partial void ConvertOleColorToSystemColor(ObjectHandleOnStack objret, uint value);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "OAVariant_ChangeType")]
        private static partial void ChangeType(Variant* result, Variant* source, int lcid, IntPtr typeHandle, int cvType, short flags);

#pragma warning restore CS8500

#endregion
    }
}
