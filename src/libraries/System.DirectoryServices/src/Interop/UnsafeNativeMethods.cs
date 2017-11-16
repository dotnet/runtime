// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security;

namespace System.DirectoryServices.Interop
{
#pragma warning disable BCL0015 // CoreFxPort

    [StructLayout(LayoutKind.Explicit)]
    internal struct Variant
    {
        [FieldOffset(0)]
        public ushort varType;
        [FieldOffset(2)]
        public ushort reserved1;
        [FieldOffset(4)]
        public ushort reserved2;
        [FieldOffset(6)]
        public ushort reserved3;
        [FieldOffset(8)]
        public short boolvalue;
        [FieldOffset(8)]
        public IntPtr ptr1;
        [FieldOffset(12)]
        public IntPtr ptr2;
    }

    [SuppressUnmanagedCodeSecurity]
    internal class UnsafeNativeMethods
    {
        [DllImport(ExternDll.Activeds, ExactSpelling = true, EntryPoint = "ADsOpenObject", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int IntADsOpenObject(string path, string userName, string password, int flags, [In, Out] ref Guid iid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppObject);

        public static int ADsOpenObject(string path, string userName, string password, int flags, [In, Out] ref Guid iid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppObject)
        {
            try
            {
                return IntADsOpenObject(path, userName, password, flags, ref iid, out ppObject);
            }
            catch (EntryPointNotFoundException)
            {
                throw new InvalidOperationException(SR.DSAdsiNotInstalled);
            }
        }

        [ComImport, Guid("FD8256D0-FD15-11CE-ABC4-02608C9E7553")]
        public interface IAds
        {
            string Name
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
            }

            string Class
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
            }

            string GUID
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
            }

            string ADsPath
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
            }

            string Parent
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
            }

            string Schema
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
            }

            [SuppressUnmanagedCodeSecurity]
            void GetInfo();

            [SuppressUnmanagedCodeSecurity]
            void SetInfo();

            object Get([In, MarshalAs(UnmanagedType.BStr)] string bstrName);

            [SuppressUnmanagedCodeSecurity]
            void Put([In, MarshalAs(UnmanagedType.BStr)] string bstrName, [In] object vProp);

            [SuppressUnmanagedCodeSecurity]
            [PreserveSig]
            int GetEx([In, MarshalAs(UnmanagedType.BStr)] string bstrName, [Out] out object value);

            [SuppressUnmanagedCodeSecurity]
            void PutEx(
                [In, MarshalAs(UnmanagedType.U4)] int lnControlCode,
                [In, MarshalAs(UnmanagedType.BStr)] string bstrName,
                [In] object vProp);

            [SuppressUnmanagedCodeSecurity]
            void GetInfoEx([In] object vProperties, [In, MarshalAs(UnmanagedType.U4)] int lnReserved);
        }

        [ComImport, Guid("001677D0-FD16-11CE-ABC4-02608C9E7553")]
        public interface IAdsContainer
        {
            int Count
            {
                [return: MarshalAs(UnmanagedType.U4)]
                [SuppressUnmanagedCodeSecurity]
                get;
            }

            object _NewEnum
            {
                [return: MarshalAs(UnmanagedType.Interface)]
                [SuppressUnmanagedCodeSecurity]
                get;
            }

            object Filter { get; set; }

            object Hints { get; set; }

            [return: MarshalAs(UnmanagedType.Interface)]
            [SuppressUnmanagedCodeSecurity]
            object GetObject(
                [In, MarshalAs(UnmanagedType.BStr)] string className,
                [In, MarshalAs(UnmanagedType.BStr)] string relativeName);

            [return: MarshalAs(UnmanagedType.Interface)]
            [SuppressUnmanagedCodeSecurity]
            object Create(
                [In, MarshalAs(UnmanagedType.BStr)] string className,
                [In, MarshalAs(UnmanagedType.BStr)] string relativeName);

            [SuppressUnmanagedCodeSecurity]
            void Delete(
                [In, MarshalAs(UnmanagedType.BStr)] string className,
                [In, MarshalAs(UnmanagedType.BStr)] string relativeName);

            [return: MarshalAs(UnmanagedType.Interface)]
            [SuppressUnmanagedCodeSecurity]
            object CopyHere(
                [In, MarshalAs(UnmanagedType.BStr)] string sourceName,
                [In, MarshalAs(UnmanagedType.BStr)] string newName);

            [return: MarshalAs(UnmanagedType.Interface)]
            [SuppressUnmanagedCodeSecurity]
            object MoveHere(
                [In, MarshalAs(UnmanagedType.BStr)] string sourceName,
                [In, MarshalAs(UnmanagedType.BStr)] string newName);
        }

        [ComImport, Guid("B2BD0902-8878-11D1-8C21-00C04FD8D503")]
        public interface IAdsDeleteOps
        {
            [SuppressUnmanagedCodeSecurity]
            void DeleteObject(int flags);
        }

        /// <summary>
        /// PropertyValue as a co-class that implements the IAdsPropertyValue interface.
        /// </summary>
        [ComImport, Guid("7b9e38b0-a97c-11d0-8534-00c04fd8d503")]
        public class PropertyValue
        {
        }

        [ComImport, Guid("9068270B-0939-11D1-8BE1-00C04FD8D503")]
        public interface IADsLargeInteger
        {
            int HighPart { get; set; }
            int LowPart { get; set; }
        }

        [ComImport, Guid("79FA9AD0-A97C-11D0-8534-00C04FD8D503")]
        public interface IAdsPropertyValue
        {
            [SuppressUnmanagedCodeSecurity]
            void Clear();

            int ADsType
            {
                [SuppressUnmanagedCodeSecurity]
                get;
                [SuppressUnmanagedCodeSecurity]
                set;
            }

            string DNString
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string CaseExactString
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string CaseIgnoreString
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string PrintableString
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string NumericString
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            bool Boolean { get; set; }

            int Integer { get; set; }

            object OctetString
            {
                [SuppressUnmanagedCodeSecurity]
                get;

                [SuppressUnmanagedCodeSecurity]
                set;
            }

            object SecurityDescriptor
            {
                [SuppressUnmanagedCodeSecurity]
                get;

                set;
            }

            object LargeInteger
            {
                [SuppressUnmanagedCodeSecurity]
                get;

                set;
            }

            object UTCTime
            {
                [SuppressUnmanagedCodeSecurity]
                get;

                set;
            }
        }

        /// <summary>
        ///  PropertyEntry as a co-class that implements the IAdsPropertyEntry interface.
        /// </summary>
        [ComImport, Guid("72D3EDC2-A4C4-11D0-8533-00C04FD8D503")]
        public class PropertyEntry
        {
        }

        [ComImport, Guid("05792C8E-941F-11D0-8529-00C04FD8D503")]
        public interface IAdsPropertyEntry
        {
            [SuppressUnmanagedCodeSecurity]
            void Clear();

            string Name
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurity]
                set;
            }

            int ADsType
            {
                [SuppressUnmanagedCodeSecurity]
                get;
                [SuppressUnmanagedCodeSecurity]
                set;
            }

            int ControlCode
            {
                [SuppressUnmanagedCodeSecurity]
                get;
                [SuppressUnmanagedCodeSecurity]
                set;
            }

            object Values { get; set;  }
        }

        [ComImport, Guid("C6F602B6-8F69-11D0-8528-00C04FD8D503")]
        public interface IAdsPropertyList
        {
            int PropertyCount
            {
                [return: MarshalAs(UnmanagedType.U4)]
                [SuppressUnmanagedCodeSecurity]
                get;
            }

            [return: MarshalAs(UnmanagedType.I4)]
            [SuppressUnmanagedCodeSecurity]
            [PreserveSig]
            int Next([Out] out object nextProp);

            void Skip([In] int cElements);

            [SuppressUnmanagedCodeSecurity]
            void Reset();

            object Item([In] object varIndex);

            object GetPropertyItem([In, MarshalAs(UnmanagedType.BStr)] string bstrName, int ADsType);

            [SuppressUnmanagedCodeSecurity]
            void PutPropertyItem([In] object varData);

            void ResetPropertyItem([In] object varEntry);

            void PurgePropertyList();
        }

        [ComImport, Guid("109BA8EC-92F0-11D0-A790-00C04FD8D5A8"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDirectorySearch
        {
            [SuppressUnmanagedCodeSecurity]
            void SetSearchPreference([In] IntPtr /*ads_searchpref_info * */pSearchPrefs, int dwNumPrefs);

            [SuppressUnmanagedCodeSecurity]
            void ExecuteSearch(
                [In, MarshalAs(UnmanagedType.LPWStr)] string pszSearchFilter,
                [In, MarshalAs(UnmanagedType.LPArray)] string[] pAttributeNames,
                [In] int dwNumberAttributes,
                [Out] out IntPtr hSearchResult);

            [SuppressUnmanagedCodeSecurity]
            void AbandonSearch([In] IntPtr hSearchResult);

            [return: MarshalAs(UnmanagedType.U4)]
            [SuppressUnmanagedCodeSecurity]
            [PreserveSig]
            int GetFirstRow([In] IntPtr hSearchResult);

            [return: MarshalAs(UnmanagedType.U4)]
            [SuppressUnmanagedCodeSecurity]
            [PreserveSig]
            int GetNextRow([In] IntPtr hSearchResult);

            [return: MarshalAs(UnmanagedType.U4)]
            [SuppressUnmanagedCodeSecurity]
            [PreserveSig]
            int GetPreviousRow([In] IntPtr hSearchResult);

            [return: MarshalAs(UnmanagedType.U4)]
            [SuppressUnmanagedCodeSecurity]
            [PreserveSig]
            int GetNextColumnName(
                [In] IntPtr hSearchResult,
                [Out] IntPtr ppszColumnName);

            [SuppressUnmanagedCodeSecurity]
            void GetColumn(
                [In] IntPtr hSearchResult,
                [In] IntPtr /* char * */ szColumnName,
                [In] IntPtr pSearchColumn);

            [SuppressUnmanagedCodeSecurity]
            void FreeColumn([In] IntPtr pSearchColumn);

            [SuppressUnmanagedCodeSecurity]
            void CloseSearchHandle([In] IntPtr hSearchResult);
        }

        [ComImport, Guid("46F14FDA-232B-11D1-A808-00C04FD8D5A8")]
        public interface IAdsObjectOptions
        {
            object GetOption(int flag);

            [SuppressUnmanagedCodeSecurity]
            void SetOption(int flag, [In] object varValue);
        }

        /// <summary>
        /// For boolean type, the default marshaller does not work, so need to have specific marshaller. For other types, use the
        /// default marshaller which is more efficient. There is no such interface on the type library this is the same as IAdsObjectOptions
        /// with a different signature.
        /// </summary>
        [ComImport, Guid("46F14FDA-232B-11D1-A808-00C04FD8D5A8")]
        public interface IAdsObjectOptions2
        {
            [SuppressUnmanagedCodeSecurity]
            [PreserveSig]
            int GetOption(int flag, [Out] out object value);

            [SuppressUnmanagedCodeSecurity]
            void SetOption(int option, Variant value);
        }

        // IDirecorySearch return codes  
        internal const int S_ADS_NOMORE_ROWS = 0x00005012;
        internal const int INVALID_FILTER = unchecked((int)0x8007203E);
        internal const int SIZE_LIMIT_EXCEEDED = unchecked((int)0x80072023);
    }
}
