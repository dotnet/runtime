// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** RuntimeClass is the base class of all WinRT types
**
** 
===========================================================*/
namespace System.Runtime.InteropServices.WindowsRuntime {
    
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Runtime.CompilerServices;
    using System.Security;


    // Local definition of Windows.Foundation.IStringable
    [ComImport]
    [Guid("96369f54-8eb6-48f0-abce-c1b211e627c3")]
    [WindowsRuntimeImport]
    internal interface IStringable
    {
        string ToString();
    }

    internal class IStringableHelper
    {
        internal static string ToString(object obj)
        {
            // Check whether the type implements IStringable.
            IStringable stringableType = obj as IStringable;
            if (stringableType != null)
            {
                return stringableType.ToString();
            }                   
            
            return obj.ToString();
        }        
    }
    
    //
    // Base class for every WinRT class
    // We'll make it a ComImport and WindowsRuntimeImport in the type loader
    // as C# compiler won't allow putting code in ComImport type
    //
    internal abstract class RuntimeClass : __ComObject
    {
        //
        // Support for ToString/GetHashCode/Equals override
        //        
        [System.Security.SecurityCritical]
        [MethodImpl(MethodImplOptions.InternalCall)]            
        internal extern IntPtr GetRedirectedGetHashCodeMD();        
        
        [System.Security.SecurityCritical]
        [MethodImpl(MethodImplOptions.InternalCall)]            
        internal extern int RedirectGetHashCode(IntPtr pMD);        

        [System.Security.SecuritySafeCritical]
        public override int GetHashCode()
        {
            IntPtr pMD = GetRedirectedGetHashCodeMD();
            if (pMD == IntPtr.Zero)
                return base.GetHashCode();
            return RedirectGetHashCode(pMD);
        }

        [System.Security.SecurityCritical]
        [MethodImpl(MethodImplOptions.InternalCall)]            
        internal extern IntPtr GetRedirectedToStringMD();        

        [System.Security.SecurityCritical]
        [MethodImpl(MethodImplOptions.InternalCall)]            
        internal extern string RedirectToString(IntPtr pMD);

        [System.Security.SecuritySafeCritical]
        public override string ToString()
        {
            // Check whether the type implements IStringable.
            IStringable stringableType = this as IStringable;
            if (stringableType != null)
            {
                return stringableType.ToString();
            }
            else
            {
                IntPtr pMD = GetRedirectedToStringMD();

                if (pMD == IntPtr.Zero)
                    return base.ToString();

                return RedirectToString(pMD);
            }
        }

        [System.Security.SecurityCritical]
        [MethodImpl(MethodImplOptions.InternalCall)]            
        internal extern IntPtr GetRedirectedEqualsMD();        

        [System.Security.SecurityCritical]
        [MethodImpl(MethodImplOptions.InternalCall)]            
        internal extern bool RedirectEquals(object obj, IntPtr pMD);        

        [System.Security.SecuritySafeCritical]
        public override bool Equals(object obj)
        {
            IntPtr pMD = GetRedirectedEqualsMD();
            if (pMD == IntPtr.Zero)
                return base.Equals(obj);
            return RedirectEquals(obj, pMD);
        }
    }
}
