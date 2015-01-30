// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: This class only exists to provide support for 
**          implenting IDispatch on managed objects. It is 
**          used to provide OleAut style coercion rules.
**
** 
===========================================================*/
namespace Microsoft.Win32 {
    
    using System;
    using System.Diagnostics.Contracts;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using CultureInfo = System.Globalization.CultureInfo;

    internal static class OAVariantLib
    {
        #region Constants

        // Constants for VariantChangeType from OleAuto.h
        public const int NoValueProp = 0x01;
        public const int AlphaBool = 0x02;
        public const int NoUserOverride = 0x04;
        public const int CalendarHijri = 0x08;
        public const int LocalBool = 0x10;
        
        internal static readonly Type [] ClassTypes = {
            typeof(Empty),
            typeof(void),
            typeof(Boolean),
            typeof(Char),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(Single),
            typeof(Double),
            typeof(String),
            typeof(void),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(Object),
            typeof(Decimal),
            null,  // Enums - what do we do here?
            typeof(Missing),
            typeof(DBNull),
        };

    	// Keep these numbers in sync w/ the above array.
        private const int CV_OBJECT=0x12;
        
        #endregion

        
        #region Internal Methods

        /**
         * Changes a Variant from one type to another, calling the OLE
         * Automation VariantChangeTypeEx routine.  Note the legal types here are
         * restricted to the subset of what can be legally found in a VB
         * Variant and the types that CLR supports explicitly in the 
         * CLR Variant class.  
         */
        [System.Security.SecurityCritical]  // auto-generated
        internal static Variant ChangeType(Variant source, Type targetClass, short options, CultureInfo culture)
        {
            if (targetClass == null)
                throw new ArgumentNullException("targetClass");
            if (culture == null)
                throw new ArgumentNullException("culture");
            Variant result = new Variant ();
            ChangeTypeEx(ref result, ref source, 
#if FEATURE_USE_LCID
                         culture.LCID, 
#else
        // @CORESYSTODO: what does CoreSystem expect for this argument?
                        0,
#endif
                         targetClass.TypeHandle.Value, GetCVTypeFromClass(targetClass), options);
            return result;
        }       

        #endregion


        #region Private Helpers

        private static int GetCVTypeFromClass(Type ctype) 
        {
            Contract.Requires(ctype != null);
#if _DEBUG
            BCLDebug.Assert(ClassTypes[CV_OBJECT] == typeof(Object), "OAVariantLib::ClassTypes[CV_OBJECT] == Object.class");
#endif
    
            int cvtype=-1;
            for (int i=0; i<ClassTypes.Length; i++) {
                if (ctype.Equals(ClassTypes[i])) {
                    cvtype=i;
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

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ChangeTypeEx(ref Variant result, ref Variant source, int lcid, IntPtr typeHandle, int cvType, short flags);

        #endregion
    }
}
