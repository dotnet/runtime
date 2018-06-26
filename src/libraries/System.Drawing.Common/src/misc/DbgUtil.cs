// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace System.Drawing.Internal
{
    internal sealed class DbgUtil
    {
        /// <summary>
        /// Call this method from your Dispose(bool) to assert that unmanaged resources has been explicitly disposed.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertFinalization(object obj, bool disposing)
        {
#if GDI_FINALIZATION_WATCH
            if( disposing || AppDomain.CurrentDomain.IsFinalizingForUnload() )
            {
                return;
            }

            try 
            {
                BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static | BindingFlags.Instance;
                FieldInfo allocSiteFld = obj.GetType().GetField("AllocationSite", bindingFlags);
                string allocationSite = allocSiteFld != null ? allocSiteFld.GetValue( obj ).ToString() : "<Allocation site unavailable>";
                
                // ignore ojects created by WindowsGraphicsCacheManager.
                if( allocationSite.Contains("WindowsGraphicsCacheManager") )
                {
                    return;
                }
    
                Debug.Fail("Object Disposed through finalization - it should be explicitly disposed.");
                Debug.WriteLine("Allocation stack:\r\n" + allocationSite); 
            } 
            catch(Exception ex)
            {
                try
                {
                    Debug.WriteLine("Exception thrown while trying to get allocation stack: " + ex); 
                }
                catch
                {
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertWin32(bool expression, string format, params object[] args)
        {
            if (!expression)
            {
                var e = new Win32Exception();
                string message = string.Format(CultureInfo.CurrentCulture, format, args);
                Debug.Fail($"{message}\r\nError: 0x{e.NativeErrorCode:x8} - {e.Message}");
            }
        }
    }
}
