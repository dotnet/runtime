// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** 
** 
**
**
** Purpose: For Assembly-related custom attributes.
**
**
=============================================================================*/

namespace System.Reflection {

    using System;
    using System.Configuration.Assemblies;
    using System.Diagnostics.Contracts;

    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyCopyrightAttribute : Attribute 
    {
        private String m_copyright;

        public AssemblyCopyrightAttribute(String copyright)
        {
            m_copyright = copyright;
        }

        public String Copyright
        {
            get { return m_copyright; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyTrademarkAttribute : Attribute 
    {
        private String m_trademark;

        public AssemblyTrademarkAttribute(String trademark)
        {
            m_trademark = trademark;
        }

        public String Trademark
        {
            get { return m_trademark; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyProductAttribute : Attribute 
    {
        private String m_product;

        public AssemblyProductAttribute(String product)
        {
            m_product = product;
        }

        public String Product
        {
            get { return m_product; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyCompanyAttribute : Attribute 
    {
        private String m_company; 

        public AssemblyCompanyAttribute(String company)
        {
            m_company = company;
        }

        public String Company
        {
            get { return m_company; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyDescriptionAttribute : Attribute 
    {
        private String m_description; 

        public AssemblyDescriptionAttribute(String description)
        {
            m_description = description;
        }

        public String Description
        {
            get { return m_description; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyTitleAttribute : Attribute 
    {
        private String m_title;

        public AssemblyTitleAttribute(String title)
        {
            m_title = title;
        }

        public String Title
        {
            get { return m_title; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyConfigurationAttribute : Attribute 
    {
        private String m_configuration; 

        public AssemblyConfigurationAttribute(String configuration)
        {
            m_configuration = configuration;
        }

        public String Configuration
        {
            get { return m_configuration; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyDefaultAliasAttribute : Attribute 
    {
        private String m_defaultAlias;

        public AssemblyDefaultAliasAttribute(String defaultAlias)
        {
            m_defaultAlias = defaultAlias;
        }

        public String DefaultAlias
        {
            get { return m_defaultAlias; }
        }
    }
        

    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyInformationalVersionAttribute : Attribute 
    {
        private String m_informationalVersion;

        public AssemblyInformationalVersionAttribute(String informationalVersion)
        {
            m_informationalVersion = informationalVersion;
        }

        public String InformationalVersion
        {
            get { return m_informationalVersion; }
        }
    }   
    

    [AttributeUsage(AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyFileVersionAttribute : Attribute 
    {
        private String _version;

        public AssemblyFileVersionAttribute(String version)
        {
            if (version == null)
                throw new ArgumentNullException("version");
            Contract.EndContractBlock();
            _version = version;
        }

        public String Version {
            get { return _version; }
        }
    }
    

    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public unsafe sealed class AssemblyCultureAttribute : Attribute 
    {
        private String m_culture; 

        public AssemblyCultureAttribute(String culture)
        {
            m_culture = culture;
        }

        public String Culture
        {
            get { return m_culture; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public unsafe sealed class AssemblyVersionAttribute : Attribute 
    {
        private String m_version;

        public AssemblyVersionAttribute(String version)
        {
            m_version = version;
        }

        public String Version
        {
            get { return m_version; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyKeyFileAttribute : Attribute 
    {
        private String m_keyFile;

        public AssemblyKeyFileAttribute(String keyFile)
        {
            m_keyFile = keyFile;
        }

        public String KeyFile
        {
            get { return m_keyFile; }
        }
    }


    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyDelaySignAttribute : Attribute 
    {
        private bool m_delaySign; 

        public AssemblyDelaySignAttribute(bool delaySign)
        {
            m_delaySign = delaySign;
        }

        public bool DelaySign
        { get
            { return m_delaySign; }
        }
    }


    [AttributeUsage(AttributeTargets.Assembly, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public unsafe sealed class AssemblyAlgorithmIdAttribute : Attribute
    {
        private uint m_algId;

        public AssemblyAlgorithmIdAttribute(AssemblyHashAlgorithm algorithmId)
        {
            m_algId = (uint) algorithmId;
        }

        [CLSCompliant(false)]
        public AssemblyAlgorithmIdAttribute(uint algorithmId)
        {
            m_algId = algorithmId;
        }

        [CLSCompliant(false)]
        public uint AlgorithmId
        {
            get { return m_algId; }
        }
    }


    [AttributeUsage(AttributeTargets.Assembly, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public unsafe sealed class AssemblyFlagsAttribute : Attribute
    {
        private AssemblyNameFlags m_flags;

        [Obsolete("This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        [CLSCompliant(false)]
        public AssemblyFlagsAttribute(uint flags)
        {
            m_flags = (AssemblyNameFlags)flags;
        }

        [Obsolete("This property has been deprecated. Please use AssemblyFlags instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        [CLSCompliant(false)]
        public uint Flags
        {
            get { return (uint)m_flags; }
        }

        // This, of course, should be typed as AssemblyNameFlags.  The compat police don't allow such changes.
        public int AssemblyFlags
        {
            get { return (int)m_flags; }
        }

        [Obsolete("This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyFlagsAttribute(int assemblyFlags)
        {
            m_flags = (AssemblyNameFlags)assemblyFlags;
        }


        public AssemblyFlagsAttribute(AssemblyNameFlags assemblyFlags)
        {
            m_flags = assemblyFlags;
        }
    }
    
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true, Inherited=false)]
    public sealed class AssemblyMetadataAttribute : Attribute 
    {
        private String m_key;
        private String m_value;
		
        public AssemblyMetadataAttribute(string key, string value) 
        {
            m_key = key;
            m_value = value;
        }
        
        public string Key
        {
            get { return m_key; }
        }
		
        public string Value
        {
            get { return m_value;}
        }
    }   

#if FEATURE_STRONGNAME_MIGRATION
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple=false)]
    public sealed class AssemblySignatureKeyAttribute : Attribute
    {
        private String _publicKey;
        private String _countersignature;

        public AssemblySignatureKeyAttribute(String publicKey, String countersignature)
        {
            _publicKey = publicKey;
            _countersignature = countersignature;
        }

        public String PublicKey
        {
            get { return _publicKey; }
        }

        public String Countersignature
        {
            get { return _countersignature; }
        }
    }
#endif

    [AttributeUsage (AttributeTargets.Assembly, Inherited=false)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AssemblyKeyNameAttribute : Attribute 
    {
        private String m_keyName; 

        public AssemblyKeyNameAttribute(String keyName)
        {
            m_keyName = keyName;
        }

        public String KeyName
        {
            get { return m_keyName; }
        }
    }

}

