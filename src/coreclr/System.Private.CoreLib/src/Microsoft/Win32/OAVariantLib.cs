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

namespace Microsoft.Win32
{
    internal static class OAVariantLib
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
            ChangeTypeEx(ref result, ref source,
                         culture.LCID,
                         targetClass.TypeHandle.Value, GetCVTypeFromClass(targetClass), options);
            return result;
        }

        #endregion


        #region Private Helpers

        private static int GetCVTypeFromClass(Type ctype)
        {
            Debug.Assert(ctype != null);
            Debug.Assert(ClassTypes[CV_OBJECT] == typeof(object), "OAVariantLib::ClassTypes[CV_OBJECT] == Object.class");

            int cvtype = -1;
            for (int i = 0; i < ClassTypes.Length; i++)
            {
                if (ctype.Equals(ClassTypes[i]))
                {
                    cvtype = i;
                    break;
                }
            }

            // OleAut Binder works better if unrecognized
            // types were changed to Object.  So don't throw here.
            if (cvtype == -1)
                cvtype = CV_OBJECT;

            return cvtype;
        }

        #endregion


        #region Private FCalls

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void ChangeTypeEx(ref Variant result, ref Variant source, int lcid, IntPtr typeHandle, int cvType, short flags);

        #endregion
    }
}
