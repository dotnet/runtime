//------------------------------------------------------------------------------
// <copyright file="DirectoryEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.DirectoryServices {

    using System;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;
    using System.DirectoryServices.Interop;
    using System.ComponentModel;
    using System.Threading;
    using System.Reflection;
    using System.Security.Permissions;
    using System.DirectoryServices.Design;
    using System.Globalization;
    using System.Net;
    
    /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry"]/*' />
    /// <devdoc>
    ///    <para> Encapsulates a node or an object in the Active Directory hierarchy.</para>
    /// </devdoc>
    [    
    DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true),
    TypeConverterAttribute(typeof(DirectoryEntryConverter)),     
    EnvironmentPermission(SecurityAction.Assert, Unrestricted=true),
    SecurityPermission(SecurityAction.Assert, Flags=SecurityPermissionFlag.UnmanagedCode),
    DSDescriptionAttribute(Res.DirectoryEntryDesc)
    ]
    public class DirectoryEntry : Component {

        private string path = "";
        private UnsafeNativeMethods.IAds adsObject;
        private bool useCache = true;
        private bool cacheFilled;        
// disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        internal bool propertiesAlreadyEnumerated = false;
#pragma warning restore 0414
        private bool justCreated = false;   // 'true' if newly created entry was not yet stored by CommitChanges().
        private bool disposed = false;
        private AuthenticationTypes authenticationType = AuthenticationTypes.Secure;
        private NetworkCredential credentials;
        private DirectoryEntryConfiguration options;

        private PropertyCollection propertyCollection = null;        
        internal bool allowMultipleChange = false;
        private bool userNameIsNull = false;
        private bool passwordIsNull = false;
        private bool objectSecurityInitialized = false;
        private bool objectSecurityModified = false;
        private ActiveDirectorySecurity objectSecurity = null;
        private static string securityDescriptorProperty = "ntSecurityDescriptor";

        
        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.DirectoryEntry"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.DirectoryServices.DirectoryEntry'/>class.
        ///    </para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectoryEntry() {  
            options = new DirectoryEntryConfiguration(this);
        }
        
        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.DirectoryEntry1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.DirectoryServices.DirectoryEntry'/> class that will bind
        ///       to the directory entry at <paramref name="path"/>.
        ///    </para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectoryEntry(string path)  :this() {
            Path = path;               
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.DirectoryEntry2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.DirectoryServices.DirectoryEntry'/> class.
        ///    </para>
        /// </devdoc>        
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectoryEntry(string path, string username, string password) : this(path, username, password, AuthenticationTypes.Secure) {
        }
        
        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.DirectoryEntry3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.DirectoryServices.DirectoryEntry'/> class.
        ///    </para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectoryEntry(string path, string username, string password, AuthenticationTypes authenticationType) : this(path) {
            this.credentials = new NetworkCredential(username, password); 
            if(username == null)
                userNameIsNull = true;

            if(password == null)
                passwordIsNull = true;
                
            this.authenticationType = authenticationType;
        }

        internal DirectoryEntry(string path, bool useCache, string username, string password, AuthenticationTypes authenticationType)  {
            this.path = path;            
            this.useCache = useCache;
            this.credentials = new NetworkCredential(username, password);      
            if(username == null)
                userNameIsNull = true;

            if(password == null)
                passwordIsNull = true;
                
            this.authenticationType = authenticationType;
            
            options = new DirectoryEntryConfiguration(this);
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.DirectoryEntry4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.DirectoryServices.DirectoryEntry'/> class that will bind
        ///       to the native Active Directory object which is passed in.
        ///    </para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectoryEntry(object adsObject)
            : this(adsObject, true, null, null, AuthenticationTypes.Secure, true) {
        }

        internal DirectoryEntry(object adsObject, bool useCache, string username, string password, AuthenticationTypes authenticationType)
            : this(adsObject, useCache, username, password, authenticationType, false) {
         }

        
        internal DirectoryEntry(object adsObject, bool useCache, string username, string password, AuthenticationTypes authenticationType, bool AdsObjIsExternal) {
            this.adsObject = adsObject as UnsafeNativeMethods.IAds;
            if (this.adsObject == null)
                throw new ArgumentException(Res.GetString(Res.DSDoesNotImplementIADs));
            
            // GetInfo is not needed here. ADSI executes an implicit GetInfo when GetEx 
            // is called on the PropertyValueCollection. 0x800704BC error might be returned 
            // on some WinNT entries, when iterating through 'Users' group members.
            // if (forceBind)
            //     this.adsObject.GetInfo();                
            path = this.adsObject.ADsPath;
            this.useCache = useCache;
            
            this.authenticationType = authenticationType;
            this.credentials = new NetworkCredential(username, password);       
            if(username == null)
                userNameIsNull = true;

            if(password == null)
                passwordIsNull = true;
                
            if (!useCache)
                CommitChanges();
            
            options = new DirectoryEntryConfiguration(this);
            
            // We are starting from an already bound connection so make sure the options are set properly.
            // If this is an externallly managed com object then we don't want to change it's current behavior
            if (!AdsObjIsExternal)
            {
               InitADsObjectOptions();
            }
        }
                                 
        internal UnsafeNativeMethods.IAds AdsObject {
            get {
                Bind();
                 return adsObject;
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.AuthenticationType"]/*' />
        [
            DefaultValue(AuthenticationTypes.Secure),
            DSDescriptionAttribute(Res.DSAuthenticationType)
        ]
        public AuthenticationTypes AuthenticationType {
            get {
                return authenticationType;
            }
            set {
                if (authenticationType == value)
                    return;

                authenticationType = value;
                Unbind();
            }
        }
        
        private bool Bound {
            get {
                return adsObject != null;
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Children"]/*' />
        /// <devdoc>
        /// <para>Gets a <see cref='System.DirectoryServices.DirectoryEntries'/>
        /// containing the child entries of this node in the Active
        /// Directory hierarchy.</para>
        /// </devdoc>
        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSChildren)
        ]
        public DirectoryEntries Children {
            get {
                return new DirectoryEntries(this);
            }
        }

        internal UnsafeNativeMethods.IAdsContainer ContainerObject {
            get {
                Bind();
                return (UnsafeNativeMethods.IAdsContainer) adsObject;
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Guid"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the globally unique identifier of the <see cref='System.DirectoryServices.DirectoryEntry'/>.
        ///    </para>
        /// </devdoc>
        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSGuid)
        ]
        public Guid Guid {
            get {
                string guid = NativeGuid;
                if (guid.Length == 32) {
                    // oddly, the value comes back as a string with no dashes from LDAP
                    byte[] intGuid = new byte[16];
                    for ( int j = 0; j < 16 ; j++) {
                        intGuid[j] = Convert.ToByte( new String(new char[] { guid[j*2], guid[j*2+1] }) , 16);
                    }
                    return new Guid(intGuid);
                    // return new Guid(guid.Substring(0, 8) + "-" + guid.Substring(8, 4) + "-" + guid.Substring(12, 4) + "-" + guid.Substring(16, 4) + "-" + guid.Substring(20));
                }
                else
                    return new Guid(guid);
            }
        }

        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSObjectSecurity)
        ]
        public ActiveDirectorySecurity ObjectSecurity 
        {
            get 
            {
                if (!objectSecurityInitialized)
                {
                    objectSecurity = GetObjectSecurityFromCache();
                    objectSecurityInitialized = true;
                }

                return objectSecurity;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                
                objectSecurity = value;
                objectSecurityInitialized = true;
                objectSecurityModified = true;

                CommitIfNotCaching();
            }
        }

        internal bool IsContainer {
            get {
                Bind();
                return adsObject is UnsafeNativeMethods.IAdsContainer;
            }
        }

        internal bool JustCreated {
            get {
                return justCreated;
            }
            set {
                justCreated = value;
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Name"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the relative name of the object as named with the
        ///       underlying directory service.
        ///    </para>
        /// </devdoc>
        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSName)
        ]
        public string Name {
            get {
                Bind();
                string tmpName = adsObject.Name;
                GC.KeepAlive(this);
                return tmpName;
            }
        }
       
        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.NativeGuid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [
            Browsable(false), 
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSNativeGuid)            
        ]
        public string NativeGuid {
            get {
                FillCache("GUID");
                string tmpGuid = adsObject.GUID;
                GC.KeepAlive(this);
                return tmpGuid;
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.NativeObject"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the native Active Directory Services Interface (ADSI) object.
        ///    </para>
        /// </devdoc>
        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSNativeObject)
        ]
        public object NativeObject {
            get {
                Bind();
                return adsObject;
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Parent"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets this
        ///       entry's parent entry in the Active Directory hierarchy.
        ///    </para>
        /// </devdoc>
        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSParent)
        ]
        public DirectoryEntry Parent {
            get {
                Bind();
                return new DirectoryEntry(adsObject.Parent, UsePropertyCache, GetUsername(), GetPassword(), AuthenticationType);
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Password"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the password to use when authenticating the client.</para>
        /// </devdoc>
        [
            DSDescriptionAttribute(Res.DSPassword),            
            DefaultValue(null),
            Browsable(false)
        ]
        public string Password {            
            set {                                    
                if (value == GetPassword())
                    return;                
                                   
                if (this.credentials == null) 
                {
                    this.credentials = new NetworkCredential();
                    // have not set it yet
                    userNameIsNull = true;
                }

                if(value == null)
                    passwordIsNull = true;
                else
                    passwordIsNull = false;

                this.credentials.Password = value;             
                                    
                Unbind();
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Path"]/*' />
        /// <devdoc>
        /// <para>Gets or sets the path for this <see cref='System.DirectoryServices.DirectoryEntry'/>.</para>
        /// </devdoc>
        [
            DefaultValue(""),
            DSDescriptionAttribute(Res.DSPath),
            TypeConverter("System.Diagnostics.Design.StringValueConverter, " + AssemblyRef.SystemDesign),
            SettingsBindable(true)
        ]
        public string Path {
            get {                                                 
                return path;                
            }
            set {
                if (value == null)
                    value = "";

                if (System.DirectoryServices.ActiveDirectory.Utils.Compare(path, value) == 0)
                    return;

                path = value;
                Unbind();
            }
        }
        
        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Properties"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets a <see cref='System.DirectoryServices.PropertyCollection'/>
        ///       of properties set on this object.
        ///    </para>
        /// </devdoc>
        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSProperties)
        ]
        public PropertyCollection Properties {
           get {
                if(this.propertyCollection == null)
                {                    
                    this.propertyCollection = new PropertyCollection(this);
                }

                return this.propertyCollection;


            }
        }
        
        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.SchemaClassName"]/*' />
        /// <devdoc>
        /// <para>Gets the name of the schema used for this <see cref='System.DirectoryServices.DirectoryEntry'/>.</para>
        /// </devdoc>
        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSSchemaClassName)
        ]
        public string SchemaClassName {
            get {
                Bind();
                string tmpClass = adsObject.Class;
                GC.KeepAlive(this);
                return tmpClass;
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.SchemaEntry"]/*' />
        /// <devdoc>
        /// <para>Gets the <see cref='System.DirectoryServices.DirectoryEntry'/> that holds schema information for this 
        ///    entry. An entry's <see cref='System.DirectoryServices.DirectoryEntry.SchemaClassName'/>
        ///    determines what properties are valid for it.</para>
        /// </devdoc>
        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSSchemaEntry)
        ]
        public DirectoryEntry SchemaEntry {
            get {
                Bind();
                return new DirectoryEntry(adsObject.Schema, UsePropertyCache, GetUsername(), GetPassword(), AuthenticationType);
            }
        }

        // By default changes to properties are done locally to
        // a cache and reading property values is cached after
        // the first read.  Setting this to false will cause the
        // cache to be committed after each operation.
        //
        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.UsePropertyCache"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether the cache should be committed after each
        ///       operation.
        ///    </para>
        /// </devdoc>
        [
            DefaultValue(true),
            DSDescriptionAttribute(Res.DSUsePropertyCache)
        ]
        public bool UsePropertyCache {
            get {
                return useCache;
            }
            set {
                if (value == useCache)
                    return;

                // auto-commit when they set this to false.
                if (!value)
                    CommitChanges();

                cacheFilled = false;    // cache mode has been changed
                useCache = value;
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Username"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the username to use when authenticating the client.</para>
        /// </devdoc>
        [
            DSDescriptionAttribute(Res.DSUsername),            
            TypeConverter("System.Diagnostics.Design.StringValueConverter, " + AssemblyRef.SystemDesign),
            DefaultValue(null),
            Browsable(false)
        ]
        public string Username {
            get {
                if (this.credentials == null || userNameIsNull) 
                    return null;
                  
                return this.credentials.UserName;
            }
            set {                                    
                if (value == GetUsername())
                    return;
                                    
                if (this.credentials == null) 
                {
                    this.credentials = new NetworkCredential();
                    passwordIsNull = true;
                }

                if(value == null)
                    userNameIsNull = true;
                else
                    userNameIsNull = false;

                this.credentials.UserName = value;        
                                    
                Unbind();
            }            
        }

        [
            Browsable(false),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
            DSDescriptionAttribute(Res.DSOptions),
            ComVisible(false)
        ]
        public DirectoryEntryConfiguration Options{
            get {
                // only LDAP provider supports IADsObjectOptions, so make the check here
                if (!(AdsObject is UnsafeNativeMethods.IAdsObjectOptions))
                    return null;               
                
                return options;
            }
        }       

        internal void InitADsObjectOptions() {

            if(adsObject is UnsafeNativeMethods.IAdsObjectOptions2) {

                //--------------------------------------------
                // Check if ACCUMULATE_MODIFICATION is available
                //--------------------------------------------
                object o = null;
                int unmanagedResult = 0;
                // check whether the new option is available

                // 8 is ADS_OPTION_ACCUMULATIVE_MODIFICATION
                unmanagedResult = ((UnsafeNativeMethods.IAdsObjectOptions2)adsObject).GetOption(8, out o);
                if(unmanagedResult != 0)
                {
                    // rootdse does not support this option and invalid parameter due to without accumulative change fix in ADSI
                   if((unmanagedResult == unchecked((int)0x80004001)) || (unmanagedResult == unchecked((int)0x80005008)))
                   {
                       return;
                   }
                   else
                   {
                       throw COMExceptionHelper.CreateFormattedComException(unmanagedResult);
                   }
               }
               
               // the new option is available, set it so we get the new PutEx behavior that will allow multiple changes
               Variant value = new Variant();
               value.varType = 11; //VT_BOOL
               value.boolvalue = -1;
               ((UnsafeNativeMethods.IAdsObjectOptions2)adsObject).SetOption(8, value);                   
               
               allowMultipleChange = true;
             }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Bind"]/*' />
        /// <devdoc>
        /// Binds to the ADs object (if not already bound).
        /// </devdoc>
        private void Bind() {
            Bind(true);
        }
        
        internal void Bind(bool throwIfFail) {
            //Cannot rebind after the object has been disposed, since finalization has been suppressed.

            if (this.disposed)
                throw new ObjectDisposedException(GetType().Name);
            
            if (adsObject == null) {                                                                                                                                        
                string pathToUse = Path; 
                if (pathToUse == null || pathToUse.Length == 0) {
                    // get the default naming context. This should be the default root for the search.
                    DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE", true, null, null, AuthenticationTypes.Secure);
                
                    //SECREVIEW: Looking at the root of the DS will demand browse permissions
                    //                     on "*" or "LDAP://RootDSE".
                    string defaultNamingContext = (string) rootDSE.Properties["defaultNamingContext"][0];
                    rootDSE.Dispose();
                                    
                    pathToUse = "LDAP://" + defaultNamingContext;                                                                                
                }                                                
            
                // Ensure we've got a thread model set, else CoInitialize() won't have been called.
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.Unknown)
                    Thread.CurrentThread.SetApartmentState(ApartmentState.MTA);

                Guid g = new Guid("00000000-0000-0000-c000-000000000046"); // IID_IUnknown
                object value = null;                
                int hr = UnsafeNativeMethods.ADsOpenObject(pathToUse, GetUsername(), GetPassword(), (int)authenticationType, ref g, out value);

                if (hr != 0) {
                    if (throwIfFail) 
                        throw COMExceptionHelper.CreateFormattedComException(hr);    
                                            
                }
                else {
                    adsObject = (UnsafeNativeMethods.IAds) value;
                }

               InitADsObjectOptions();
                     
           }

        }


        // Create new entry with the same data, but different IADs object, and grant it Browse Permission.
        internal DirectoryEntry CloneBrowsable() {            
            DirectoryEntry newEntry = new DirectoryEntry(this.Path, this.UsePropertyCache, this.GetUsername(), this.GetPassword(), this.AuthenticationType);            
            return newEntry;
        }


        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Close"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Closes the <see cref='System.DirectoryServices.DirectoryEntry'/>
        ///       and releases any system resources associated with this component.
        ///    </para>
        /// </devdoc>
        public void Close() {
            Unbind();
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.CommitChanges"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Saves any
        ///       changes to the entry in the directory store.
        ///    </para>
        /// </devdoc>
        public void CommitChanges() {
            if ( justCreated ) {     
                // Note: Permissions Demand is not necessary here, because entry has already been created with appr. permissions. 
                // Write changes regardless of Caching mode to finish construction of a new entry.
                try
                {
                    //
                    // Write the security descriptor to the cache
                    //
                    SetObjectSecurityInCache();

                    adsObject.SetInfo();    
                }
                catch(COMException e)
                {  
                    throw COMExceptionHelper.CreateFormattedComException(e);
                }
                justCreated = false;
                objectSecurityInitialized = false;
                objectSecurityModified = false;

                // we need to refresh that properties table.
                this.propertyCollection = null;                
                return;
            }
            if (!useCache) {

                // unless we have modified the existing security descriptor (in-place) through ObjectSecurity property
                // there is nothing to do
                if ((objectSecurity == null) || (!objectSecurity.IsModified()))
                {
                    return;
                }
            }

            if (!Bound)
                return;
            
            try
            {
                //
                // Write the security descriptor to the cache
                //
                SetObjectSecurityInCache();
                adsObject.SetInfo();
                objectSecurityInitialized = false;
                objectSecurityModified = false;
            }
            catch(COMException e)
            {  
                throw COMExceptionHelper.CreateFormattedComException(e);
            }
            // we need to refresh that properties table.
            this.propertyCollection = null;
        }
        
        internal void CommitIfNotCaching() {

            if ( justCreated )  
                return;   // Do not write changes, beacuse the entry is just under construction until CommitChanges() is called.
                            
            if (useCache)
                return;

            if (!Bound)
                return;

            // do full demand before we commit changes back to the server
            new DirectoryServicesPermission(PermissionState.Unrestricted).Demand();

            try
            {
                //
                // Write the security descriptor to the cache
                //
                SetObjectSecurityInCache();

                adsObject.SetInfo();
                objectSecurityInitialized = false;
                objectSecurityModified = false;
            }
            catch(COMException e)
            {  
                throw COMExceptionHelper.CreateFormattedComException(e);
            }
            // we need to refresh that properties table.
            this.propertyCollection = null;

        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>Creates a copy of this entry as a child of the given parent.</para>
        /// </devdoc>
        public DirectoryEntry CopyTo(DirectoryEntry newParent) {
            return CopyTo(newParent, null);
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.CopyTo1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Creates a copy of this entry as a child of the given parent and
        ///       gives it a new name.
        ///    </para>
        /// </devdoc>
        public DirectoryEntry CopyTo(DirectoryEntry newParent, string newName) {                    
            if (!newParent.IsContainer)
                throw new InvalidOperationException(Res.GetString(Res.DSNotAContainer, newParent.Path));               
            
            object copy = null;
            try
            {
                copy = newParent.ContainerObject.CopyHere(Path, newName);
            }
            catch(COMException e)
            {  
                throw COMExceptionHelper.CreateFormattedComException(e);
            }
            return new DirectoryEntry(copy, newParent.UsePropertyCache, GetUsername(), GetPassword(), AuthenticationType);
        }        
                                                             
        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.DeleteTree"]/*' />
        /// <devdoc>
        ///    <para>Deletes this entry and its entire subtree from the
        ///       Active Directory hierarchy.</para>
        /// </devdoc>
        public void DeleteTree() {
            if (!(AdsObject is UnsafeNativeMethods.IAdsDeleteOps))
                throw new InvalidOperationException(Res.GetString(Res.DSCannotDelete));
                
            UnsafeNativeMethods.IAdsDeleteOps entry = (UnsafeNativeMethods.IAdsDeleteOps) AdsObject;
            try
            {
                entry.DeleteObject(0);
            }
            catch(COMException e)
            {  
                throw COMExceptionHelper.CreateFormattedComException(e);
            }

            GC.KeepAlive(this);
        }
       
        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Dispose"]/*' />
        /// <devdoc>        
        /// </devdoc>
        protected override void Dispose(bool disposing) {
            // no managed object to free
            
            // free own state (unmanaged objects)
            if(!this.disposed)
            {
                Unbind();
                this.disposed = true;
            }
            
            base.Dispose(disposing);
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Exists"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Searches the directory store at the given
        ///       path to see whether an entry exists.
        ///    </para>
        /// </devdoc>        
        public static bool Exists(string path) {
            DirectoryEntry entry = new DirectoryEntry(path);
            try {
                entry.Bind(true);       // throws exceptions (possibly can break applications) 
                return entry.Bound; 
            }    
            catch ( System.Runtime.InteropServices.COMException e ) {
                if ( e.ErrorCode == unchecked((int)0x80072030) ||
                     e.ErrorCode == unchecked((int)0x80070003) ||   // ERROR_DS_NO_SUCH_OBJECT and path not found (not found in strict sense)
                     e.ErrorCode == unchecked((int)0x800708AC))     // Group name could not be found
                    return false;    
                throw;
            }
            finally {
                entry.Dispose();    
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.FillCache"]/*' />
        /// <devdoc>
        /// If UsePropertyCache is true, calls GetInfo the first time it's necessary.
        /// If it's false, calls GetInfoEx on the given property name.
        /// </devdoc>
        internal void FillCache(string propertyName) {
            if (UsePropertyCache) {
                if (cacheFilled)
                    return;

                RefreshCache();
                cacheFilled = true;
            }
            else {
                Bind();
                try
                {
                    if ( propertyName.Length > 0 )
                        adsObject.GetInfoEx(new object[] { propertyName }, 0);
                    else
                        adsObject.GetInfo();  // [alexvec]
                }
                catch(COMException e)
                {  
                    throw COMExceptionHelper.CreateFormattedComException(e);
                }
            }
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Invoke"]/*' />
        /// <devdoc>
        ///    <para>Calls
        ///       a method on the native Active Directory.</para>
        /// </devdoc>
        public object Invoke(string methodName, params object[] args) {
            object target = this.NativeObject;
            Type type = target.GetType();
            object result = null;
            try
            {
                result = type.InvokeMember(methodName, BindingFlags.InvokeMethod, null, target, args, CultureInfo.InvariantCulture);
                GC.KeepAlive(this);
            }
            catch(COMException e)
            {
                throw COMExceptionHelper.CreateFormattedComException(e);
            }
            catch(TargetInvocationException e)
            {
                if(e.InnerException != null)
                {
                    if(e.InnerException is COMException)
                    {
                        COMException inner = (COMException) e.InnerException;
                        throw new TargetInvocationException(e.Message, COMExceptionHelper.CreateFormattedComException(inner));
                    }
                }                

                throw e;
            }
            
            if (result is UnsafeNativeMethods.IAds)
            	
                return new DirectoryEntry(result, UsePropertyCache, GetUsername(), GetPassword(), AuthenticationType);
            else
                return result;
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.InvokeGet"]/*' />
        /// <devdoc>
        ///    <para>Reads
        ///       a property on the native Active Directory object.</para>
        /// </devdoc>
        [ComVisible(false)]
        public object InvokeGet(string propertyName) {            
            object target = this.NativeObject;
            Type type = target.GetType();
            object result = null;
            try
            {
                result = type.InvokeMember(propertyName, BindingFlags.GetProperty, null, target, null, CultureInfo.InvariantCulture);
                GC.KeepAlive(this);
            }
            catch(COMException e)
            {
                throw COMExceptionHelper.CreateFormattedComException(e);
            }
            catch(TargetInvocationException e)
            {
                if(e.InnerException != null)
                {
                    if(e.InnerException is COMException)
                    {
                        COMException inner = (COMException) e.InnerException;
                        throw new TargetInvocationException(e.Message, COMExceptionHelper.CreateFormattedComException(inner));
                    }
                }    

                throw e;
            }

            return result;
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.InvokeSet"]/*' />
        /// <devdoc>
        ///    <para>Sets
        ///       a property on the native Active Directory object.</para>
        /// </devdoc>
        [ComVisible(false)]
        public void InvokeSet(string propertyName, params object[] args) {            
            object target = this.NativeObject;
            Type type = target.GetType();
            try
            {                
                type.InvokeMember(propertyName, BindingFlags.SetProperty, null, target, args, CultureInfo.InvariantCulture);
                GC.KeepAlive(this);
            }
            catch(COMException e)
            {
                throw COMExceptionHelper.CreateFormattedComException(e);
            }
            catch(TargetInvocationException e)
            {
                if(e.InnerException != null)
                {
                    if(e.InnerException is COMException)
                    {
                        COMException inner = (COMException) e.InnerException;
                        throw new TargetInvocationException(e.Message, COMExceptionHelper.CreateFormattedComException(inner));
                    }
                }                

                throw e;
            }
            
        }


        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.MoveTo"]/*' />
        /// <devdoc>
        ///    <para>Moves this entry to the given parent.</para>
        /// </devdoc>
        public void MoveTo(DirectoryEntry newParent) {
            MoveTo(newParent, null);
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.MoveTo1"]/*' />
        /// <devdoc>
        ///    <para>Moves this entry to the given parent, and gives it a new name.</para>
        /// </devdoc>
        public void MoveTo(DirectoryEntry newParent, string newName) {
            object newEntry = null;
            if (!(newParent.AdsObject is UnsafeNativeMethods.IAdsContainer))
                throw new InvalidOperationException(Res.GetString(Res.DSNotAContainer, newParent.Path));
            try
            {
                if(AdsObject.ADsPath.StartsWith("WinNT:", StringComparison.Ordinal))
                {
                    // get the ADsPath instead of using Path as ADsPath for the case that "WinNT://computername" is passed in while we need "WinNT://domain/computer"
                    string childPath = AdsObject.ADsPath;                                        
                    string parentPath = newParent.AdsObject.ADsPath;

                    // we know ADsPath does not end with object type qualifier like ",computer" so it is fine to compare with whole newparent's adspath
                    // for the case that child has different components from newparent in the aspects other than case, we don't do any processing, just let ADSI decide in case future adsi change
                    if(System.DirectoryServices.ActiveDirectory.Utils.Compare(childPath, 0, parentPath.Length, parentPath, 0, parentPath.Length) == 0)
                    {
                        uint compareFlags = System.DirectoryServices.ActiveDirectory.Utils.NORM_IGNORENONSPACE | 
									System.DirectoryServices.ActiveDirectory.Utils.NORM_IGNOREKANATYPE | 
									System.DirectoryServices.ActiveDirectory.Utils.NORM_IGNOREWIDTH | 
									System.DirectoryServices.ActiveDirectory.Utils.SORT_STRINGSORT;
                        // work around the ADSI case sensitive bug: only case is different, replace with newparent's path
                        if(System.DirectoryServices.ActiveDirectory.Utils.Compare(childPath, 0, parentPath.Length, parentPath, 0, parentPath.Length, compareFlags) != 0)
                        {
                            childPath = parentPath + childPath.Substring(parentPath.Length);     
                        }
                    }
                    
                    newEntry = newParent.ContainerObject.MoveHere(childPath, newName);
                }
                else
                {
                    newEntry = newParent.ContainerObject.MoveHere(Path, newName);
                }
            }
            catch(COMException e)
            {  
                throw COMExceptionHelper.CreateFormattedComException(e);
            }

            if (Bound)
                System.Runtime.InteropServices.Marshal.ReleaseComObject(adsObject);     // release old handle
             
            this.adsObject = (UnsafeNativeMethods.IAds) newEntry;
            path = this.adsObject.ADsPath;

            // Reset the options on the ADSI object since there were lost when the new object was created.
            InitADsObjectOptions();

            if (!useCache)
                CommitChanges();
            else
                RefreshCache();     // in ADSI cache is lost after moving
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.RefreshCache"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Loads the property values for this directory entry into
        ///       the property cache.
        ///    </para>
        /// </devdoc>
        public void RefreshCache() {
            Bind();
            try
            {
                adsObject.GetInfo();
            }
            catch(COMException e)
            {  
                throw COMExceptionHelper.CreateFormattedComException(e);
            }

            cacheFilled = true;
            // we need to refresh that properties table.
            this.propertyCollection = null;

            // need to refresh the objectSecurity property
            objectSecurityInitialized = false;
            objectSecurityModified = false;
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.RefreshCache1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Loads the values of the specified properties into the
        ///       property cache.
        ///    </para>
        /// </devdoc>
        public void RefreshCache(string[] propertyNames) {
            Bind();
            
            //Consider, V2, jruiz: there shouldn't be any marshaling issues
            //by just doing: AdsObject.GetInfoEx(object[]propertyNames, 0);
            Object[] names = new Object[propertyNames.Length];
            for (int i = 0; i < propertyNames.Length; i++)
                names[i] = propertyNames[i];
            try
            {
                AdsObject.GetInfoEx(names, 0);
            }
            catch(COMException e)
            {  
                throw COMExceptionHelper.CreateFormattedComException(e);
            }

            // this is a half-lie, but oh well. Without it, this method is pointless.
            cacheFilled = true;
            // we need to partially refresh that properties table.
            if(this.propertyCollection != null && propertyNames != null)
            {
                for(int i = 0; i < propertyNames.Length; i++)
                {
                    if(propertyNames[i] != null)
                    {
                        string name = propertyNames[i].ToLower(CultureInfo.InvariantCulture);
                        this.propertyCollection.valueTable.Remove(name);

                        // also need to consider the range retrieval case
                        string[] results = name.Split(new char[]{';'});
                        if(results.Length != 1)
                        {
                            string rangeName = "";
                            for(int count = 0; count < results.Length; count++)
                            {
                                if(!results[count].StartsWith("range=", StringComparison.Ordinal))
                                {
                                    rangeName += results[count];
                                    rangeName += ";";
                                }
                            }

                            // remove the last ';' character
                            rangeName = rangeName.Remove(rangeName.Length - 1, 1);

                            this.propertyCollection.valueTable.Remove(rangeName);
                        }

                        // if this is "ntSecurityDescriptor" we should refresh the objectSecurity property
                        if (String.Compare(propertyNames[i], securityDescriptorProperty, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            objectSecurityInitialized = false;
                            objectSecurityModified = false;
                        }
                           
                    }
                }
            }
            
        }

        /// <include file='doc\DirectoryEntry.uex' path='docs/doc[@for="DirectoryEntry.Rename"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Changes the name of this entry.
        ///    </para>
        /// </devdoc>
        public void Rename(string newName) {
            MoveTo(Parent, newName);
        }

        private void Unbind() {
            if ( adsObject != null )
                System.Runtime.InteropServices.Marshal.ReleaseComObject(adsObject);
            adsObject = null;
            // we need to release that properties table.
            this.propertyCollection = null;

            // need to refresh the objectSecurity property
            objectSecurityInitialized = false;
            objectSecurityModified = false;
        }

        internal string GetUsername()
        {
            if (this.credentials == null || userNameIsNull) 
                return null;
                  
            return this.credentials.UserName;
        }

        internal string GetPassword()
        {
            if (this.credentials == null || passwordIsNull) 
                return null;
                  
            return this.credentials.Password;
        }

        private ActiveDirectorySecurity GetObjectSecurityFromCache() 
        {
            try
            {
                //
                // This property is the managed version of the "ntSecurityDescriptor"
                // attribute. In order to build an ActiveDirectorySecurity object from it
                // we need to get the binary form of the security descriptor.
                // If we use IADs::Get to get the IADsSecurityDescriptor interface and then 
                // convert to raw form, there would be a performance overhead (because of 
                // sid lookups and reverse lookups during conversion).
                // So to get the security descriptor in binary form, we use 
                // IADsPropertyList::GetPropertyItem
                //
                
                //
                // GetPropertyItem does not implicitly fill the property cache
                // so we need to fill it explicitly (for an existing entry)
                //
                if (!JustCreated)
                {
                    SecurityMasks securityMasksUsedInRetrieval;
                    
                    //
                    // To ensure that we honor the security masks while retrieving
                    // the security descriptor, we will retrieve the "ntSecurityDescriptor" each time
                    // while initializing the ObjectSecurity property
                    //
                    securityMasksUsedInRetrieval = this.Options.SecurityMasks;
                    RefreshCache(new string[] {securityDescriptorProperty});

                    //
                    // Get the IAdsPropertyList interface
                    // (Check that the IAdsPropertyList interface is supported)
                    //
                    if (!(NativeObject is UnsafeNativeMethods.IAdsPropertyList))
                        throw new NotSupportedException(Res.GetString(Res.DSPropertyListUnsupported));

                    UnsafeNativeMethods.IAdsPropertyList list = (UnsafeNativeMethods.IAdsPropertyList)NativeObject;

                    UnsafeNativeMethods.IAdsPropertyEntry propertyEntry = (UnsafeNativeMethods.IAdsPropertyEntry)list.GetPropertyItem(securityDescriptorProperty, (int)AdsType.ADSTYPE_OCTET_STRING);
                    GC.KeepAlive(this);

                    //
                    // Create a new ActiveDirectorySecurity object from the binary form
                    // of the security descriptor
                    //
                    object[] values = (object[])propertyEntry.Values;

                    //
                    // This should never happen. It indicates that there is a problem in ADSI's property cache logic.
                    //
                    if (values.Length < 1)
                    {
                        Debug.Fail("ntSecurityDescriptor property exists in cache but has no values.");
                        throw new InvalidOperationException(Res.GetString(Res.DSSDNoValues));
                    }

                    //
                    // Do not support more than one security descriptor
                    //
                    if (values.Length > 1)
                    {
                        throw new NotSupportedException(Res.GetString(Res.DSMultipleSDNotSupported));
                    }

                    UnsafeNativeMethods.IAdsPropertyValue propertyValue = (UnsafeNativeMethods.IAdsPropertyValue)values[0];
                    return new ActiveDirectorySecurity((byte[])propertyValue.OctetString, securityMasksUsedInRetrieval);

                }
                else
                {
                    //
                    // Newly created directory entry
                    //

                    return null;
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x8000500D))    //  property not found exception
                    return null;
                else
                    throw;
            }
        }

        private void SetObjectSecurityInCache() 
        {
            if ((objectSecurity != null) && (objectSecurityModified || objectSecurity.IsModified()))
            {  	
                UnsafeNativeMethods.IAdsPropertyValue sDValue = (UnsafeNativeMethods.IAdsPropertyValue)new UnsafeNativeMethods.PropertyValue();

                sDValue.ADsType = (int)AdsType.ADSTYPE_OCTET_STRING;
                sDValue.OctetString = objectSecurity.GetSecurityDescriptorBinaryForm();

                UnsafeNativeMethods.IAdsPropertyEntry newSDEntry = (UnsafeNativeMethods.IAdsPropertyEntry)new UnsafeNativeMethods.PropertyEntry();

                newSDEntry.Name = securityDescriptorProperty;
                newSDEntry.ADsType = (int)AdsType.ADSTYPE_OCTET_STRING;
                newSDEntry.ControlCode = (int)AdsPropertyOperation.Update;
                newSDEntry.Values = new object[] { sDValue };

                ((UnsafeNativeMethods.IAdsPropertyList)NativeObject).PutPropertyItem( newSDEntry );  
            }
        }
    }
    
}
