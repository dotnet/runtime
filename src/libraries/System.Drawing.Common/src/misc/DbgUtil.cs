// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace System.Drawing.Internal
{
    internal sealed class DbgUtil
    {
        public const int
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
            FORMAT_MESSAGE_DEFAULT = FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM;

        [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetUserDefaultLCID();

        [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int FormatMessage(int dwFlags, HandleRef lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, HandleRef arguments);

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
        public static void AssertWin32(bool expression, string format, object arg1)
        {
#if DEBUG
            if (!expression)
            {
                object[] args = new object[] { arg1 };
                AssertWin32Impl(expression, format, args);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void AssertWin32(bool expression, string format, object arg1, object arg2)
        {
            if (!expression)
            {
                object[] args = new object[] { arg1, arg2 };
                AssertWin32Impl(expression, format, args);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertWin32(bool expression, string format, object arg1, object arg2, object arg3)
        {
            if (!expression)
            {
                object[] args = new object[] { arg1, arg2, arg3 };
                AssertWin32Impl(expression, format, args);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertWin32(bool expression, string format, object arg1, object arg2, object arg3, object arg4)
        {
            if (!expression)
            {
                object[] args = new object[] { arg1, arg2, arg3, arg4 };
                AssertWin32Impl(expression, format, args);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertWin32(bool expression, string format, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            if (!expression)
            {
                object[] args = new object[] { arg1, arg2, arg3, arg4, arg5 };
                AssertWin32Impl(expression, format, args);
            }
        }

        [Conditional("DEBUG")]
        private static void AssertWin32Impl(bool expression, string format, object[] args)
        {
            if (!expression)
            {
                string message = string.Format(CultureInfo.CurrentCulture, format, args);
                Debug.Fail($"{message}\r\nError: {GetLastErrorStr()}");
            }
        }

        // WARNING: Your PInvoke function needs to have the DllImport.SetLastError=true for this method
        // to work properly.  From the MSDN:
        // GetLastWin32Error exposes the Win32 GetLastError API method from Kernel32.DLL. This method exists 
        // because it is not safe to make a direct platform invoke call to GetLastError to obtain this information. 
        // If you want to access this error code, you must call GetLastWin32Error rather than writing your own 
        // platform invoke definition for GetLastError and calling it. The common language runtime can make 
        // internal calls to APIs that overwrite the operating system maintained GetLastError.
        //
        // You can only use this method to obtain error codes if you apply the System.Runtime.InteropServices.DllImportAttribute
        // to the method signature and set the SetLastError field to true.
        public static string GetLastErrorStr()
        {
            int MaxSize = 255;
            var buffer = new StringBuilder(MaxSize);
            string message = string.Empty;
            int err = 0;

            try
            {
                err = Marshal.GetLastWin32Error();

                int retVal = FormatMessage(
                    FORMAT_MESSAGE_DEFAULT,
                    new HandleRef(null, IntPtr.Zero),
                    err,
                    GetUserDefaultLCID(),
                    buffer,
                    MaxSize,
                    new HandleRef(null, IntPtr.Zero));

                message = retVal != 0 ? buffer.ToString() : "<error returned>";
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
                message = ex.ToString();
            }

            return $"0x{err:x8} - {message}";
        }

        /// <summary>
        /// Duplicated here from ClientUtils not to depend on that code because this class is to be compiled into
        /// System.Drawing and System.Windows.Forms.
        /// </summary>
        private static bool IsCriticalException(Exception ex)
        {
            return
                ex is StackOverflowException ||
                ex is OutOfMemoryException ||
                ex is ThreadAbortException;
        }
    }
}
