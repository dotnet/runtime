// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
** 
**
**
** Purpose: Used for binding and retrieving info about an assembly
**
**
===========================================================*/
namespace System.Reflection {
    using System;
    using System.IO;
    using System.Configuration.Assemblies;
    using System.Runtime.CompilerServices;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_AssemblyName))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyName : _AssemblyName, ICloneable, ISerializable, IDeserializationCallback
    {
        //
        // READ ME
        // If you modify any of these fields, you must also update the 
        // AssemblyBaseObject structure in object.h
        //
        private String          _Name;                  // Name
        private byte[]          _PublicKey;
        private byte[]          _PublicKeyToken;
        private CultureInfo     _CultureInfo;
        private String          _CodeBase;              // Potential location to get the file
        private Version         _Version;
        
        private StrongNameKeyPair            _StrongNameKeyPair;

        private SerializationInfo m_siInfo; //A temporary variable which we need during deserialization.

        private byte[]                _HashForControl;
        private AssemblyHashAlgorithm _HashAlgorithm;
        private AssemblyHashAlgorithm _HashAlgorithmForControl;

        private AssemblyVersionCompatibility _VersionCompatibility;
        private AssemblyNameFlags            _Flags;
       
        public AssemblyName()
        { 
            _HashAlgorithm = AssemblyHashAlgorithm.None;
            _VersionCompatibility = AssemblyVersionCompatibility.SameMachine;
            _Flags = AssemblyNameFlags.None;
        }
    
        // Set and get the name of the assembly. If this is a weak Name
        // then it optionally contains a site. For strong assembly names, 
        // the name partitions up the strong name's namespace
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        public Version Version
        {
            get { 
                return _Version;
            }
            set { 
                _Version = value;
            }
        }

        // Locales, internally the LCID is used for the match.
        public CultureInfo CultureInfo
        {
            get {
                return _CultureInfo;
            }
            set { 
                _CultureInfo = value; 
            }
        }

        public String CultureName
        {
            get {
                return (_CultureInfo == null) ? null : _CultureInfo.Name;
            }
            set {
                _CultureInfo = (value == null) ? null : new CultureInfo(value);
            }
        }
    
        public String CodeBase
        {
#if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
#endif
            get { return _CodeBase; }
#if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
#endif
            set { _CodeBase = value; }
        }
    
        public String EscapedCodeBase
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (_CodeBase == null)
                    return null;
                else
                    return EscapeCodeBase(_CodeBase);
            }
        }
    
        public ProcessorArchitecture  ProcessorArchitecture
        {
            get {
                int x = (((int)_Flags) & 0x70) >> 4;
                if(x > 5) 
                    x = 0;
                return (ProcessorArchitecture)x;
            }
            set {
                int x = ((int)value) & 0x07;
                if(x <= 5) {
                    _Flags = (AssemblyNameFlags)((int)_Flags & 0xFFFFFF0F);
                    _Flags |= (AssemblyNameFlags)(x << 4);
                }
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public AssemblyContentType ContentType
        {
            get
            {
                int x = (((int)_Flags) & 0x00000E00) >> 9;
                if (x > 1)
                    x = 0;
                return (AssemblyContentType)x;
            }
            set
            {
                int x = ((int)value) & 0x07;
                if (x <= 1)
                {
                    _Flags = (AssemblyNameFlags)((int)_Flags & 0xFFFFF1FF);
                    _Flags |= (AssemblyNameFlags)(x << 9);
                }
            }
        }
         
        

        // Make a copy of this assembly name.
        public Object Clone()
        {
            AssemblyName name = new AssemblyName();
            name.Init(_Name,
                      _PublicKey,
                      _PublicKeyToken,
                      _Version,
                      _CultureInfo,
                      _HashAlgorithm,
                      _VersionCompatibility,
                      _CodeBase,
                      _Flags,
                      _StrongNameKeyPair);
            name._HashForControl=_HashForControl;
            name._HashAlgorithmForControl=_HashAlgorithmForControl;
            return name;
        }

        /*
         * Get the AssemblyName for a given file. This will only work
         * if the file contains an assembly manifest. This method causes
         * the file to be opened and closed.
         */
        [System.Security.SecuritySafeCritical]  // auto-generated
        static public AssemblyName GetAssemblyName(String assemblyFile)
        {
            if(assemblyFile == null)
                throw new ArgumentNullException("assemblyFile");
            Contract.EndContractBlock();

            // Assembly.GetNameInternal() will not demand path discovery 
            //  permission, so do that first.
            String fullPath = Path.GetFullPathInternal(assemblyFile);
            new FileIOPermission( FileIOPermissionAccess.PathDiscovery, fullPath ).Demand();
            return nGetFileInformation(fullPath);
        }
    
        internal void SetHashControl(byte[] hash, AssemblyHashAlgorithm hashAlgorithm)
        {
             _HashForControl=hash;
             _HashAlgorithmForControl=hashAlgorithm;
        }

        // The public key that is used to verify an assemblies
        // inclusion into the namespace. If the public key associated
        // with the namespace cannot verify the assembly the assembly
        // will fail to load.
        public byte[] GetPublicKey()
        {
            return _PublicKey;
        }

        public void SetPublicKey(byte[] publicKey)
        {
            _PublicKey = publicKey;

            if (publicKey == null)
                _Flags &= ~AssemblyNameFlags.PublicKey;
            else
                _Flags |= AssemblyNameFlags.PublicKey;
        }

        // The compressed version of the public key formed from a truncated hash.
        // Will throw a SecurityException if _PublicKey is invalid
        [System.Security.SecuritySafeCritical]  // auto-generated
        public byte[] GetPublicKeyToken()
        {
            if (_PublicKeyToken == null)
                _PublicKeyToken = nGetPublicKeyToken();
            return _PublicKeyToken;
        }

        public void SetPublicKeyToken(byte[] publicKeyToken)
        {
            _PublicKeyToken = publicKeyToken;
        }

        // Flags modifying the name. So far the only flag is PublicKey, which
        // indicates that a full public key and not the compressed version is
        // present. 
        // Processor Architecture flags are set only through ProcessorArchitecture
        // property and can't be set or retrieved directly
        // Content Type flags are set only through ContentType property and can't be 
        // set or retrieved directly
        public AssemblyNameFlags Flags
        {
            get { return (AssemblyNameFlags)((uint)_Flags & 0xFFFFF10F); }
            set {
                _Flags &= unchecked((AssemblyNameFlags)0x00000EF0);
                _Flags |= (value & unchecked((AssemblyNameFlags)0xFFFFF10F));
            }
        }

        public AssemblyHashAlgorithm HashAlgorithm
        {
            get { return _HashAlgorithm; }
            set { _HashAlgorithm = value; }
        }
        
        public AssemblyVersionCompatibility VersionCompatibility
        {
            get { return _VersionCompatibility; }
            set { _VersionCompatibility = value; }
        }

        public StrongNameKeyPair KeyPair
        {
            get { return _StrongNameKeyPair; }
            set { _StrongNameKeyPair = value; }
        }
       
        public String FullName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return nToString();
            }
        }
    
        // Returns the stringized version of the assembly name.
        public override String ToString()
        {
            String s = FullName;
            if(s == null) 
                return base.ToString();
            else 
                return s;
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            //Allocate the serialization info and serialize our static data.
            info.AddValue("_Name", _Name);
            info.AddValue("_PublicKey", _PublicKey, typeof(byte[]));
            info.AddValue("_PublicKeyToken", _PublicKeyToken, typeof(byte[]));
#if FEATURE_USE_LCID
            info.AddValue("_CultureInfo", (_CultureInfo == null) ? -1 :_CultureInfo.LCID);
#endif
            info.AddValue("_CodeBase", _CodeBase);
            info.AddValue("_Version", _Version);
            info.AddValue("_HashAlgorithm", _HashAlgorithm, typeof(AssemblyHashAlgorithm));
            info.AddValue("_HashAlgorithmForControl", _HashAlgorithmForControl, typeof(AssemblyHashAlgorithm));
            info.AddValue("_StrongNameKeyPair", _StrongNameKeyPair, typeof(StrongNameKeyPair));
            info.AddValue("_VersionCompatibility", _VersionCompatibility, typeof(AssemblyVersionCompatibility));
            info.AddValue("_Flags", _Flags, typeof(AssemblyNameFlags));
            info.AddValue("_HashForControl",_HashForControl,typeof(byte[]));
       }

        public void OnDeserialization(Object sender)
        {
            // Deserialization has already been performed
            if (m_siInfo == null)
                return;

            _Name = m_siInfo.GetString("_Name");
            _PublicKey = (byte[]) m_siInfo.GetValue("_PublicKey", typeof(byte[]));
            _PublicKeyToken = (byte[]) m_siInfo.GetValue("_PublicKeyToken", typeof(byte[]));
#if FEATURE_USE_LCID
            int lcid = (int)m_siInfo.GetInt32("_CultureInfo");
            if (lcid != -1)
                _CultureInfo = new CultureInfo(lcid);
#endif

            _CodeBase = m_siInfo.GetString("_CodeBase");
            _Version = (Version) m_siInfo.GetValue("_Version", typeof(Version));
            _HashAlgorithm = (AssemblyHashAlgorithm) m_siInfo.GetValue("_HashAlgorithm", typeof(AssemblyHashAlgorithm));
            _StrongNameKeyPair = (StrongNameKeyPair) m_siInfo.GetValue("_StrongNameKeyPair", typeof(StrongNameKeyPair));
            _VersionCompatibility = (AssemblyVersionCompatibility)m_siInfo.GetValue("_VersionCompatibility", typeof(AssemblyVersionCompatibility));
            _Flags = (AssemblyNameFlags) m_siInfo.GetValue("_Flags", typeof(AssemblyNameFlags));

            try {
                _HashAlgorithmForControl = (AssemblyHashAlgorithm) m_siInfo.GetValue("_HashAlgorithmForControl", typeof(AssemblyHashAlgorithm));
                _HashForControl = (byte[]) m_siInfo.GetValue("_HashForControl", typeof(byte[]));    
            }
            catch (SerializationException) { // RTM did not have these defined
                _HashAlgorithmForControl = AssemblyHashAlgorithm.None;
                _HashForControl = null;
            }

            m_siInfo = null;
        }

        // Constructs a new AssemblyName during deserialization.
        internal AssemblyName(SerializationInfo info, StreamingContext context)
        {
            //The graph is not valid until OnDeserialization() has been called.
            m_siInfo = info; 
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public AssemblyName(String assemblyName)
        {
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();
            if ((assemblyName.Length == 0) ||
                (assemblyName[0] == '\0'))
                throw new ArgumentException(Environment.GetResourceString("Format_StringZeroLength"));

            _Name = assemblyName;
            nInit();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        static public bool ReferenceMatchesDefinition(AssemblyName reference,
                                                             AssemblyName definition)
        {
            // Optimization for common use case
            if (Object.ReferenceEquals(reference, definition))
            {
                return true;
            }
            return ReferenceMatchesDefinitionInternal(reference, definition, true);
        }

        
        /// "parse" tells us to parse the simple name of the assembly as if it was the full name
        /// almost never the right thing to do, but needed for compat
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern bool ReferenceMatchesDefinitionInternal(AssemblyName reference,
                                                                     AssemblyName definition,
                                                                     bool parse);  



        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void nInit(out RuntimeAssembly assembly, bool forIntrospection, bool raiseResolveEvent);

        [System.Security.SecurityCritical]  // auto-generated
        internal void nInit()
        {
            RuntimeAssembly dummy = null;
            nInit(out dummy, false, false);
        }

        internal void SetProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm)
        {
            ProcessorArchitecture = CalculateProcArchIndex(pek, ifm, _Flags);
        }

        internal static ProcessorArchitecture CalculateProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm, AssemblyNameFlags flags)
        {
            if (((uint)flags & 0xF0) == 0x70)
                return ProcessorArchitecture.None;

            if ((pek & System.Reflection.PortableExecutableKinds.PE32Plus) == System.Reflection.PortableExecutableKinds.PE32Plus)
            {
                switch (ifm)
                {
                    case System.Reflection.ImageFileMachine.IA64:
                        return ProcessorArchitecture.IA64;
                    case System.Reflection.ImageFileMachine.AMD64:
                        return ProcessorArchitecture.Amd64;
                    case System.Reflection.ImageFileMachine.I386:
                        if ((pek & System.Reflection.PortableExecutableKinds.ILOnly) == System.Reflection.PortableExecutableKinds.ILOnly)
                            return ProcessorArchitecture.MSIL;
                        break;
                }
            }
            else
            {
                if (ifm == System.Reflection.ImageFileMachine.I386)
                {
                    if ((pek & System.Reflection.PortableExecutableKinds.Required32Bit) == System.Reflection.PortableExecutableKinds.Required32Bit)
                        return ProcessorArchitecture.X86;

                    if ((pek & System.Reflection.PortableExecutableKinds.ILOnly) == System.Reflection.PortableExecutableKinds.ILOnly)
                        return ProcessorArchitecture.MSIL;

                    return ProcessorArchitecture.X86;
                }
                if (ifm == System.Reflection.ImageFileMachine.ARM)
                {
                    return ProcessorArchitecture.Arm;
                }
            }
            return ProcessorArchitecture.None;
        }

        internal void Init(String name, 
                           byte[] publicKey,
                           byte[] publicKeyToken,
                           Version version,
                           CultureInfo cultureInfo,
                           AssemblyHashAlgorithm hashAlgorithm,
                           AssemblyVersionCompatibility versionCompatibility,
                           String codeBase,
                           AssemblyNameFlags flags,
                           StrongNameKeyPair keyPair) // Null if ref, matching Assembly if def
        {
            _Name = name;

            if (publicKey != null) {
                _PublicKey = new byte[publicKey.Length];
                Array.Copy(publicKey, _PublicKey, publicKey.Length);
            }
    
            if (publicKeyToken != null) {
                _PublicKeyToken = new byte[publicKeyToken.Length];
                Array.Copy(publicKeyToken, _PublicKeyToken, publicKeyToken.Length);
            }
    
            if (version != null)
                _Version = (Version) version.Clone();

            _CultureInfo = cultureInfo;
            _HashAlgorithm = hashAlgorithm;
            _VersionCompatibility = versionCompatibility;
            _CodeBase = codeBase;
            _Flags = flags;
            _StrongNameKeyPair = keyPair;
        }

#if !FEATURE_CORECLR
        void _AssemblyName.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _AssemblyName.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _AssemblyName.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _AssemblyName.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif

#if FEATURE_APTCA
        internal string GetNameWithPublicKey()
        {
            byte[] key = GetPublicKey();

            // The following string should not be localized because it is used in security decisions.
            return Name + ", PublicKey=" + System.Security.Util.Hex.EncodeHexString(key);
        }
#endif

        // This call opens and closes the file, but does not add the
        // assembly to the domain.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern AssemblyName nGetFileInformation(String s);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern String nToString();

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern byte[] nGetPublicKeyToken();
    
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern String EscapeCodeBase(String codeBase);
    }
}
