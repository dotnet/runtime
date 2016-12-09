//------------------------------------------------------------------------------
// <copyright file="UnsafeNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Interop {
    using System.Runtime.InteropServices;
    using System;
    using System.Security;
    using System.Security.Permissions;
    using System.Collections;
    using System.IO;
    using System.Text;

    [StructLayout(LayoutKind.Explicit)]
    internal struct Variant {
        [FieldOffset(0)] public ushort varType;
        [FieldOffset(2)] public ushort reserved1;
        [FieldOffset(4)] public ushort reserved2;
        [FieldOffset(6)] public ushort reserved3;
        [FieldOffset(8)] public short boolvalue; 
        [FieldOffset(8)] public IntPtr ptr1;
        [FieldOffset(12)] public IntPtr ptr2;
    }


    [
    ComVisible(false), 
    SuppressUnmanagedCodeSecurityAttribute()
    ]
    internal class UnsafeNativeMethods {        

        [DllImport(ExternDll.Activeds, ExactSpelling=true, EntryPoint="ADsOpenObject", CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int IntADsOpenObject(string path, string userName, string password, int flags, [In, Out] ref Guid iid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppObject);
        public static int ADsOpenObject(string path, string userName, string password, int flags, [In, Out] ref Guid iid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppObject) {
            try {
                return IntADsOpenObject(path, userName, password, flags, ref iid, out ppObject);
            }
            catch(EntryPointNotFoundException) {
                throw new InvalidOperationException(Res.GetString(Res.DSAdsiNotInstalled));
            }
        }
        
        [ComImport, Guid("FD8256D0-FD15-11CE-ABC4-02608C9E7553"), System.Runtime.InteropServices.InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IAds {
            string Name {
                [return: MarshalAs(UnmanagedType.BStr)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
            }
    
            string Class {
                [return: MarshalAs(UnmanagedType.BStr)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
            }
    
            string GUID {
                [return: MarshalAs(UnmanagedType.BStr)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
            }
    
            string ADsPath {
                [return: MarshalAs(UnmanagedType.BStr)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
            }
    
            string Parent {
                [return: MarshalAs(UnmanagedType.BStr)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
            }
    
            string Schema {
                [return: MarshalAs(UnmanagedType.BStr)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
            }
    
            [SuppressUnmanagedCodeSecurityAttribute()]
            void GetInfo();
    
            [SuppressUnmanagedCodeSecurityAttribute()]
            void SetInfo();
    
            [return: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
            Object Get(
                [In, MarshalAs(UnmanagedType.BStr)] 
                string bstrName);
    
            [SuppressUnmanagedCodeSecurityAttribute()]
            void Put(
                [In, MarshalAs(UnmanagedType.BStr)] 
                string bstrName, 
                [In, MarshalAs(UnmanagedType.Struct)]
                Object vProp);
    
            [SuppressUnmanagedCodeSecurityAttribute()][PreserveSig]
            int GetEx(
                [In, MarshalAs(UnmanagedType.BStr)] 
                String bstrName,
                [Out, MarshalAs(UnmanagedType.Struct)] 
                out object value);
    
            
            [SuppressUnmanagedCodeSecurityAttribute()]
            void PutEx(
                [In, MarshalAs(UnmanagedType.U4)] 
                int lnControlCode, 
                [In, MarshalAs(UnmanagedType.BStr)] 
                string bstrName, 
                [In, MarshalAs(UnmanagedType.Struct)]
                Object vProp);
    
            [SuppressUnmanagedCodeSecurityAttribute()]
            void GetInfoEx(
                [In, MarshalAs(UnmanagedType.Struct)]
                Object vProperties, 
                [In, MarshalAs(UnmanagedType.U4)] 
                int lnReserved);
        }
                             
        [ComImport, Guid("001677D0-FD16-11CE-ABC4-02608C9E7553"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IAdsContainer {
            int Count {
                [return: MarshalAs(UnmanagedType.U4)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
            }
    
            object _NewEnum {
                [return: MarshalAs(UnmanagedType.Interface)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
            }
    
            object Filter {
                [return: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
                set;
            }
    
            object Hints {
                [return: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
                set;
            }
    
            [return: MarshalAs(UnmanagedType.Interface)][SuppressUnmanagedCodeSecurityAttribute()]
            object GetObject(
                [In, MarshalAs(UnmanagedType.BStr)] 
                string className, 
                [In, MarshalAs(UnmanagedType.BStr)] 
                string relativeName);
    
            [return: MarshalAs(UnmanagedType.Interface)][SuppressUnmanagedCodeSecurityAttribute()]
            object Create(
                [In, MarshalAs(UnmanagedType.BStr)] 
                string className, 
                [In, MarshalAs(UnmanagedType.BStr)] 
                string relativeName);
    
            [SuppressUnmanagedCodeSecurityAttribute()]
            void Delete(
                [In, MarshalAs(UnmanagedType.BStr)] 
                string className, 
                [In, MarshalAs(UnmanagedType.BStr)] 
                string relativeName);
    
            [return: MarshalAs(UnmanagedType.Interface)][SuppressUnmanagedCodeSecurityAttribute()]
            object CopyHere(
                [In, MarshalAs(UnmanagedType.BStr)] 
                string sourceName, 
                [In, MarshalAs(UnmanagedType.BStr)] 
                string newName);
    
            [return: MarshalAs(UnmanagedType.Interface)][SuppressUnmanagedCodeSecurityAttribute()]
            object MoveHere(
                [In, MarshalAs(UnmanagedType.BStr)] 
                string sourceName, 
                [In, MarshalAs(UnmanagedType.BStr)] 
                string newName);
        }
        
        [ComImport, Guid("B2BD0902-8878-11D1-8C21-00C04FD8D503"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IAdsDeleteOps {
    
            [SuppressUnmanagedCodeSecurityAttribute()]
            void DeleteObject(int flags);
        }

        //
        // PropertyValue as a co-class that implements the IAdsPropertyValue interface
        //
        [ComImport, Guid("7b9e38b0-a97c-11d0-8534-00c04fd8d503")]
        public class PropertyValue
        {
        }

        [ComImport, Guid( "9068270B-0939-11D1-8BE1-00C04FD8D503" ), InterfaceTypeAttribute( ComInterfaceType.InterfaceIsDual )]
        public interface IADsLargeInteger
        {
            int HighPart
            {
                get;
                set;
            }
            int LowPart
            {
                get;
                set;
            }
        }

        [ComImport, Guid("79FA9AD0-A97C-11D0-8534-00C04FD8D503"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IAdsPropertyValue
        {
            [SuppressUnmanagedCodeSecurityAttribute()]
            void Clear();
            
            int ADsType
            {
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [SuppressUnmanagedCodeSecurityAttribute()]
                set;
            }

            string DNString
            {
                [return :MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param :MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string CaseExactString
            {
                [return :MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param :MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string CaseIgnoreString
            {
                [return :MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param :MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string PrintableString
            {
                [return :MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param :MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string NumericString
            {
                [return :MarshalAs(UnmanagedType.BStr)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param :MarshalAs(UnmanagedType.BStr)]
                set;
            }

            bool Boolean
            {
                get;
                set;
            }

            int Integer
            {
                get;
                set;
            }

            object OctetString
            {
                [return :MarshalAs(UnmanagedType.Struct)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param :MarshalAs(UnmanagedType.Struct)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                set;
            }          

            object SecurityDescriptor
            {
                [return :MarshalAs(UnmanagedType.Struct)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param :MarshalAs(UnmanagedType.Struct)]
                set;
            }

            object LargeInteger
            {
                [return :MarshalAs(UnmanagedType.Struct)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param :MarshalAs(UnmanagedType.Struct)]
                set;
            }

            object UTCTime
            {
                [return :MarshalAs(UnmanagedType.Struct)]
                [SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param :MarshalAs(UnmanagedType.Struct)]
                set;
            }
        }

        //
        // PropertyEntry as a co-class that implements the IAdsPropertyEntry interface
        //
        [ComImport, Guid("72d3edc2-a4c4-11d0-8533-00c04fd8d503")]
        public class PropertyEntry
        {
        }
        
        [ComImport, Guid("05792C8E-941F-11D0-8529-00C04FD8D503"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IAdsPropertyEntry {
    
            [SuppressUnmanagedCodeSecurityAttribute()]
            void Clear();
    
            string Name {
                [return: MarshalAs(UnmanagedType.BStr)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param: MarshalAs(UnmanagedType.BStr)][SuppressUnmanagedCodeSecurityAttribute()]
                set;
            }    
            
            int ADsType {   
                [SuppressUnmanagedCodeSecurityAttribute()]             
                get;             
                [SuppressUnmanagedCodeSecurityAttribute()]   
                set;
            }
    
            int ControlCode {   
                [SuppressUnmanagedCodeSecurityAttribute()]             
                get;
                [SuppressUnmanagedCodeSecurityAttribute()]                
                set;
            }
    
            object Values {
                [return: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
                [param: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
                set;
            }    
        }              
        
        [ComImport, Guid("C6F602B6-8F69-11D0-8528-00C04FD8D503"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IAdsPropertyList {
    
            int PropertyCount {
                [return: MarshalAs(UnmanagedType.U4)][SuppressUnmanagedCodeSecurityAttribute()]
                get;
            }
    
            [return: MarshalAs(UnmanagedType.I4)][SuppressUnmanagedCodeSecurityAttribute()][PreserveSig]
            int Next([Out, MarshalAs(UnmanagedType.Struct)] out object nextProp);
                
            void Skip([In] int cElements);
            
            [SuppressUnmanagedCodeSecurityAttribute()]    
            void Reset();
    
            [return: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
            object Item([In, MarshalAs(UnmanagedType.Struct)] object varIndex);
    
            [return: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
            object GetPropertyItem([In, MarshalAs(UnmanagedType.BStr)] string bstrName, int ADsType);
            
            [SuppressUnmanagedCodeSecurityAttribute()]    
            void PutPropertyItem([In, MarshalAs(UnmanagedType.Struct)] object varData);
                
            void ResetPropertyItem([In, MarshalAs(UnmanagedType.Struct)] object varEntry);
                
            void PurgePropertyList();
    
        }
                                             
        [ComImport, Guid("109BA8EC-92F0-11D0-A790-00C04FD8D5A8"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDirectorySearch {
            
            [SuppressUnmanagedCodeSecurityAttribute()]
            void SetSearchPreference(
                [In]
                IntPtr /*ads_searchpref_info * */pSearchPrefs,
                //ads_searchpref_info[] pSearchPrefs,
                int dwNumPrefs);
    
            [SuppressUnmanagedCodeSecurityAttribute()]
            void ExecuteSearch(
                [In, MarshalAs(UnmanagedType.LPWStr)]
                string pszSearchFilter,
                [In, MarshalAs(UnmanagedType.LPArray)]
                string[] pAttributeNames,
                [In]
                int dwNumberAttributes,
                [Out]
                out IntPtr hSearchResult);
    
            [SuppressUnmanagedCodeSecurityAttribute()]
            void AbandonSearch([In] IntPtr hSearchResult);
    
            [return: MarshalAs(UnmanagedType.U4)][SuppressUnmanagedCodeSecurityAttribute()][PreserveSig]
            int GetFirstRow([In] IntPtr hSearchResult);
    
            [return: MarshalAs(UnmanagedType.U4)][SuppressUnmanagedCodeSecurityAttribute()][PreserveSig]
            int GetNextRow([In] IntPtr hSearchResult);
    
            [return: MarshalAs(UnmanagedType.U4)][SuppressUnmanagedCodeSecurityAttribute()][PreserveSig]
            int GetPreviousRow([In] IntPtr hSearchResult);
    
            [return: MarshalAs(UnmanagedType.U4)][SuppressUnmanagedCodeSecurityAttribute()][PreserveSig]
            int GetNextColumnName(
                [In] IntPtr hSearchResult,
                [Out]
                IntPtr ppszColumnName);      
            
            [SuppressUnmanagedCodeSecurityAttribute()]
            void GetColumn(
                [In] IntPtr hSearchResult,
                [In] //, MarshalAs(UnmanagedType.LPWStr)]
                IntPtr /* char * */ szColumnName,
                [In]
                IntPtr pSearchColumn);    
         
            [SuppressUnmanagedCodeSecurityAttribute()]               
            void FreeColumn(
                [In]
                IntPtr pSearchColumn);
    
            [SuppressUnmanagedCodeSecurityAttribute()]               
            void CloseSearchHandle([In] IntPtr hSearchResult);
        }

        [ComImport, Guid("46F14FDA-232B-11D1-A808-00C04FD8D5A8"), System.Runtime.InteropServices.InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IAdsObjectOptions {
            [return: MarshalAs(UnmanagedType.Struct)][SuppressUnmanagedCodeSecurityAttribute()]
            object GetOption(int flag);

            [SuppressUnmanagedCodeSecurityAttribute()]
            void SetOption(int flag, [In, MarshalAs(UnmanagedType.Struct)] object varValue);
            
        }          

        // for boolean type, the default marshaller does not work, so need to have specific marshaller. For other types, use the
        // default marshaller which is more efficient        

        [ComImport, Guid("46f14fda-232b-11d1-a808-00c04fd8d5a8"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IAdsObjectOptions2 {
            [SuppressUnmanagedCodeSecurityAttribute()][PreserveSig]
            int GetOption(int flag, [Out, MarshalAs(UnmanagedType.Struct)] out object value);

            [SuppressUnmanagedCodeSecurityAttribute()]
            void SetOption(int option, Variant value);
            
        }

        // IDirecorySearch return codes  
        internal const int S_ADS_NOMORE_ROWS = 0x00005012;
        internal const int INVALID_FILTER = unchecked((int)0x8007203E);
        internal const int SIZE_LIMIT_EXCEEDED = unchecked((int)0x80072023);
                                
    }
}


