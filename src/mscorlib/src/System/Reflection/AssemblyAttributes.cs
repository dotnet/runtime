// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Assemblies;

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyCopyrightAttribute : Attribute
    {
        public AssemblyCopyrightAttribute(string copyright)
        {
            Copyright = copyright;
        }

        public string Copyright { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyTrademarkAttribute : Attribute
    {
        public AssemblyTrademarkAttribute(string trademark)
        {
            Trademark = trademark;
        }

        public string Trademark { get; }
    }


    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyProductAttribute : Attribute
    {
        public AssemblyProductAttribute(string product)
        {
            Product = product;
        }

        public string Product { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyCompanyAttribute : Attribute
    {
        public AssemblyCompanyAttribute(string company)
        {
            Company = company;
        }

        public string Company { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyDescriptionAttribute : Attribute
    {
        public AssemblyDescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyTitleAttribute : Attribute
    {
        public AssemblyTitleAttribute(string title)
        {
            Title = title;
        }

        public string Title { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyConfigurationAttribute : Attribute
    {
        public AssemblyConfigurationAttribute(string configuration)
        {
            Configuration = configuration;
        }

        public string Configuration { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyDefaultAliasAttribute : Attribute
    {
        public AssemblyDefaultAliasAttribute(string defaultAlias)
        {
            DefaultAlias = defaultAlias;
        }

        public string DefaultAlias { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyInformationalVersionAttribute : Attribute
    {
        public AssemblyInformationalVersionAttribute(string informationalVersion)
        {
            InformationalVersion = informationalVersion;
        }

        public string InformationalVersion { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyFileVersionAttribute : Attribute
    {
        public AssemblyFileVersionAttribute(string version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));
            Version = version;
        }

        public string Version { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public unsafe sealed class AssemblyCultureAttribute : Attribute
    {
        public AssemblyCultureAttribute(string culture)
        {
            Culture = culture;
        }

        public string Culture { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public unsafe sealed class AssemblyVersionAttribute : Attribute
    {
        public AssemblyVersionAttribute(string version)
        {
            Version = version;
        }

        public string Version { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyKeyFileAttribute : Attribute
    {
        public AssemblyKeyFileAttribute(string keyFile)
        {
            KeyFile = keyFile;
        }

        public string KeyFile { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyDelaySignAttribute : Attribute
    {
        public AssemblyDelaySignAttribute(bool delaySign)
        {
            DelaySign = delaySign;
        }

        public bool DelaySign { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public unsafe sealed class AssemblyAlgorithmIdAttribute : Attribute
    {
        public AssemblyAlgorithmIdAttribute(AssemblyHashAlgorithm algorithmId)
        {
            AlgorithmId = (uint)algorithmId;
        }

        [CLSCompliant(false)]
        public AssemblyAlgorithmIdAttribute(uint algorithmId)
        {
            AlgorithmId = algorithmId;
        }

        [CLSCompliant(false)]
        public uint AlgorithmId { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public unsafe sealed class AssemblyFlagsAttribute : Attribute
    {
        private AssemblyNameFlags _flags;

        [Obsolete("This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        [CLSCompliant(false)]
        public AssemblyFlagsAttribute(uint flags)
        {
            _flags = (AssemblyNameFlags)flags;
        }

        [Obsolete("This property has been deprecated. Please use AssemblyFlags instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        [CLSCompliant(false)]
        public uint Flags
        {
            get { return (uint)_flags; }
        }

        public int AssemblyFlags
        {
            get { return (int)_flags; }
        }

        [Obsolete("This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyFlagsAttribute(int assemblyFlags)
        {
            _flags = (AssemblyNameFlags)assemblyFlags;
        }

        public AssemblyFlagsAttribute(AssemblyNameFlags assemblyFlags)
        {
            _flags = assemblyFlags;
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class AssemblyMetadataAttribute : Attribute
    {
        public AssemblyMetadataAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class AssemblySignatureKeyAttribute : Attribute
    {
        public AssemblySignatureKeyAttribute(string publicKey, string countersignature)
        {
            PublicKey = publicKey;
            Countersignature = countersignature;
        }

        public string PublicKey { get; }

        public string Countersignature { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyKeyNameAttribute : Attribute
    {
        public AssemblyKeyNameAttribute(string keyName)
        {
            KeyName = keyName;
        }

        public string KeyName { get; }
    }
}

