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
        public const int NoValueProp = 0x01;
        public const int AlphaBool = 0x02;
        public const int NoUserOverride = 0x04;
        public const int CalendarHijri = 0x08;
        public const int LocalBool = 0x10;

        internal static readonly Type?[] ClassTypes = {
            typeof(Empty),
            typeof(void),
            typeof(bool),
            typeof(char),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(void),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(object),
            typeof(decimal),
            null,  // Enums - what do we do here?
            typeof(Missing),
            typeof(DBNull),
        };

        // Keep these numbers in sync w/ the above array.
        private const int CV_OBJECT = 0x12;

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
        internal static Variant ChangeType(Variant source, Type targetClass, short options, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(targetClass);
            ArgumentNullException.ThrowIfNull(culture);

            Variant result = default;
            bool converted = false;

            TypeHandle th = ((RuntimeType)targetClass).GetNativeTypeHandle();
            Debug.Assert(!th.IsTypeDesc);
            if (Variant.IsSystemDrawingColor(th.AsMethodTable()))
            {
                if (source.CVType is Variant.CV_I4 or Variant.CV_U4)
                {
                    // Int32/UInt32 can be converted to System.Drawing.Color
                    object? ret = null;
                    ConvertOleColorToSystemColor(ObjectHandleOnStack.Create(ref ret), (uint)source._data);
                    Debug.Assert(ret != null);
                    result._objref = ret;
                    result._flags = Variant.CV_OBJECT;

                    converted = true;
                }
            }

            if (!converted)
            {
                VarEnum vt = source.CVType switch
                {
                    Variant.CV_EMPTY => VarEnum.VT_EMPTY,
                    Variant.CV_VOID => VarEnum.VT_VOID,
                    Variant.CV_BOOLEAN => VarEnum.VT_BOOL,
                    Variant.CV_CHAR => VarEnum.VT_UI2,
                    Variant.CV_I1 => VarEnum.VT_I1,
                    Variant.CV_U1 => VarEnum.VT_UI1,
                    Variant.CV_I2 => VarEnum.VT_I2,
                    Variant.CV_U2 => VarEnum.VT_UI2,
                    Variant.CV_I4 => VarEnum.VT_I4,
                    Variant.CV_U4 => VarEnum.VT_UI4,
                    Variant.CV_I8 => VarEnum.VT_UI8,
                    Variant.CV_R4 => VarEnum.VT_R4,
                    Variant.CV_R8 => VarEnum.VT_R8,
                    Variant.CV_STRING => VarEnum.VT_BSTR,
                    // Variant.CV_PTR => INVALID_MAPPING,
                    Variant.CV_DATETIME => VarEnum.VT_DATE,
                    // Variant.CV_TIMESPAN => INVALID_MAPPING,
                    Variant.CV_OBJECT => VarEnum.VT_UNKNOWN,
                    Variant.CV_DECIMAL => VarEnum.VT_DECIMAL,
                    Variant.CV_CURRENCY => VarEnum.VT_CY,
                    // Variant.CV_ENUM => INVALID_MAPPING,
                    // Variant.CV_MISSING => INVALID_MAPPING,
                    Variant.CV_NULL => VarEnum.VT_NULL,
                    _ => throw new NotSupportedException(SR.NotSupported_ChangeType)
                };

                ComVariant vOp = ToOAVariant(source);
                ComVariant ret = default;

                int hr = VariantChangeTypeEx(ref ret, ref vOp, culture.LCID, options, (ushort)vt);

                using (vOp)
                using (ret)
                {
                    if (hr < 0)
                        OAFailed(hr);

                    result = FromOAVariant(ret);
                    if (source.CVType == Variant.CV_CHAR)
                    {
                        result._flags = Variant.CV_CHAR;
                    }
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

        private static ComVariant ToOAVariant(Variant input)
        {

        }

        private static Variant FromOAVariant(ComVariant input)
        {

        }

        [LibraryImport(Interop.Libraries.OleAut32)]
        private static partial int VariantChangeTypeEx(ref ComVariant pVarRes, ref ComVariant pVarSrc, int lcid, short wFlags, ushort vt);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Variant_ConvertOleColorToSystemColor")]
        private static partial void ConvertOleColorToSystemColor(ObjectHandleOnStack objret, uint value);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "OAVariant_ChangeType")]
        private static partial void ChangeType(Variant* result, Variant* source, int lcid, IntPtr typeHandle, int cvType, short flags);

#pragma warning restore CS8500

        #endregion


        #region Private Helpers

        private static int GetCVTypeFromClass(Type ctype)
        {
            Debug.Assert(ctype != null);
            Debug.Assert(ClassTypes[CV_OBJECT] == typeof(object), "OAVariantLib::ClassTypes[CV_OBJECT] == Object.class");

            // OleAut Binder works better if unrecognized
            // types were changed to Object.
            int cvtype = CV_OBJECT;

            for (int i = 0; i < ClassTypes.Length; i++)
            {
                if (ctype.Equals(ClassTypes[i]))
                {
                    cvtype = i;
                    break;
                }
            }

            return cvtype;
        }

        #endregion
    }
}
