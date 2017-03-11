// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Globalization;
using System.Data.SqlClient;
using System.Text;
using System.Diagnostics;


namespace System.Data
{
    internal static partial class LocalDBAPI
    {
        private const string const_localDbPrefix = @"(localdb)\";


        // check if name is in format (localdb)\<InstanceName - not empty> and return instance name if it is
        internal static string GetLocalDbInstanceNameFromServerName(string serverName)
        {
            if (serverName == null)
                return null;
            serverName = serverName.TrimStart(); // it can start with spaces if specified in quotes
            if (!serverName.StartsWith(const_localDbPrefix, StringComparison.OrdinalIgnoreCase))
                return null;
            string instanceName = serverName.Substring(const_localDbPrefix.Length).Trim();
            if (instanceName.Length == 0)
                return null;
            else
                return instanceName;
        }


        internal static void ReleaseDLLHandles()
        {
            s_userInstanceDLLHandle = IntPtr.Zero;
            s_localDBFormatMessage = null;
        }



        //This is copy of handle that SNI maintains, so we are responsible for freeing it - therefore there we are not using SafeHandle
        private static IntPtr s_userInstanceDLLHandle = IntPtr.Zero;

        private static readonly object s_dllLock = new object();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private delegate int LocalDBFormatMessageDelegate(int hrLocalDB, UInt32 dwFlags, UInt32 dwLanguageId, StringBuilder buffer, ref UInt32 buflen);

        private static LocalDBFormatMessageDelegate s_localDBFormatMessage = null;

        private static LocalDBFormatMessageDelegate LocalDBFormatMessage
        {
            get
            {
                if (s_localDBFormatMessage == null)
                {
                    lock (s_dllLock)
                    {
                        if (s_localDBFormatMessage == null)
                        {
                            IntPtr functionAddr = SafeNativeMethods.GetProcAddress(UserInstanceDLLHandle, "LocalDBFormatMessage");

                            if (functionAddr == IntPtr.Zero)
                            {
                                int hResult = Marshal.GetLastWin32Error();
                                throw CreateLocalDBException(errorMessage: SR.LocalDB_MethodNotFound);
                            }
                            s_localDBFormatMessage = Marshal.GetDelegateForFunctionPointer<LocalDBFormatMessageDelegate>(functionAddr);
                        }
                    }
                }
                return s_localDBFormatMessage;
            }
        }

        private const UInt32 const_LOCALDB_TRUNCATE_ERR_MESSAGE = 1;// flag for LocalDBFormatMessage that indicates that message can be truncated if it does not fit in the buffer
        private const int const_ErrorMessageBufferSize = 1024;      // Buffer size for Local DB error message 1K will be enough for all messages


        internal static string GetLocalDBMessage(int hrCode)
        {
            Debug.Assert(hrCode < 0, "HRCode does not indicate error");
            try
            {
                StringBuilder buffer = new StringBuilder((int)const_ErrorMessageBufferSize);
                UInt32 len = (UInt32)buffer.Capacity;


                // First try for current culture                
                int hResult = LocalDBFormatMessage(hrLocalDB: hrCode, dwFlags: const_LOCALDB_TRUNCATE_ERR_MESSAGE, dwLanguageId: (uint)CultureInfo.CurrentCulture.LCID,
                                                 buffer: buffer, buflen: ref len);
                if (hResult >= 0)
                    return buffer.ToString();
                else
                {
                    // Message is not available for current culture, try default 
                    buffer = new StringBuilder((int)const_ErrorMessageBufferSize);
                    len = (UInt32)buffer.Capacity;
                    hResult = LocalDBFormatMessage(hrLocalDB: hrCode, dwFlags: const_LOCALDB_TRUNCATE_ERR_MESSAGE, dwLanguageId: 0 /* thread locale with fallback to English */,
                                                 buffer: buffer, buflen: ref len);
                    if (hResult >= 0)
                        return buffer.ToString();
                    else
                        return string.Format(CultureInfo.CurrentCulture, "{0} (0x{1:X}).", SR.LocalDB_UnobtainableMessage, hResult);
                }
            }
            catch (SqlException exc)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0} ({1}).", SR.LocalDB_UnobtainableMessage, exc.Message);
            }
        }


        private static SqlException CreateLocalDBException(string errorMessage, string instance = null, int localDbError = 0, int sniError = 0)
        {
            Debug.Assert((localDbError == 0) || (sniError == 0), "LocalDB error and SNI error cannot be specified simultaneously");
            Debug.Assert(!string.IsNullOrEmpty(errorMessage), "Error message should not be null or empty");
            SqlErrorCollection collection = new SqlErrorCollection();

            int errorCode = (localDbError == 0) ? sniError : localDbError;

            if (sniError != 0)
            {
                string sniErrorMessage = SQL.GetSNIErrorMessage(sniError);
                errorMessage = String.Format((IFormatProvider)null, "{0} (error: {1} - {2})",
                         errorMessage, sniError, sniErrorMessage);
            }

            collection.Add(new SqlError(errorCode, 0, TdsEnums.FATAL_ERROR_CLASS, instance, errorMessage, null, 0));

            if (localDbError != 0)
                collection.Add(new SqlError(errorCode, 0, TdsEnums.FATAL_ERROR_CLASS, instance, GetLocalDBMessage(localDbError), null, 0));

            SqlException exc = SqlException.CreateException(collection, null);

            exc._doNotReconnect = true;

            return exc;
        }
    }
}
