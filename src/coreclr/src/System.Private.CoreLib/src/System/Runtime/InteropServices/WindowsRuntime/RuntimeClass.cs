// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** RuntimeClass is the base class of all WinRT types
**
** 
===========================================================*/

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
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
        internal static string? ToString(object obj)
        {
            if (obj is IGetProxyTarget proxy)
                obj = proxy.GetTarget();

            // Check whether the type implements IStringable.
            if (obj is IStringable stringableType)
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
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern IntPtr GetRedirectedGetHashCodeMD();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int RedirectGetHashCode(IntPtr pMD);

        public override int GetHashCode()
        {
            IntPtr pMD = GetRedirectedGetHashCodeMD();
            if (pMD == IntPtr.Zero)
                return base.GetHashCode();
            return RedirectGetHashCode(pMD);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern IntPtr GetRedirectedToStringMD();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern string RedirectToString(IntPtr pMD);

        public override string ToString()
        {
            // Check whether the type implements IStringable.
            if (this is IStringable stringableType)
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern IntPtr GetRedirectedEqualsMD();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern bool RedirectEquals(object? obj, IntPtr pMD);

        public override bool Equals(object? obj)
        {
            IntPtr pMD = GetRedirectedEqualsMD();
            if (pMD == IntPtr.Zero)
                return base.Equals(obj);
            return RedirectEquals(obj, pMD);
        }
    }
}
