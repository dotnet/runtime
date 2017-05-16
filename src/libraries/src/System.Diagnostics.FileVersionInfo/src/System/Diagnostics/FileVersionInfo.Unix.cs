// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace System.Diagnostics
{
    public sealed partial class FileVersionInfo
    {
        private static readonly char[] s_versionSeparators = new char[] { '.' };

        private FileVersionInfo(string fileName)
        {
            _fileName = fileName;

            // For managed assemblies, read the file version information from the assembly's metadata.
            // This isn't quite what's done on Windows, which uses the Win32 GetFileVersionInfo to read
            // the Win32 resource information from the file, and the managed compiler uses these attributes
            // to fill in that resource information when compiling the assembly.  It's possible
            // that after compilation, someone could have modified the resource information such that it
            // no longer matches what was or wasn't in the assembly.  But that's a rare enough case
            // that this should match for all intents and purposes.  If this ever becomes a problem,
            // we can implement a full-fledged Win32 resource parser; that would also enable support
            // for native Win32 PE files on Unix, but that should also be an extremely rare case.
            if (!TryLoadManagedAssemblyMetadata())
            {
                // We could try to parse Executable and Linkable Format (ELF) files, but at present
                // for executables they don't store version information, which is typically just
                // available in the filename itself.  For now, we won't do anything special, but
                // we can add more cases here as we find need and opportunity.
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Attempt to load our fields from the metadata of the file, if it's a managed assembly.</summary>
        /// <returns>true if the file is a managed assembly; otherwise, false.</returns>
        private bool TryLoadManagedAssemblyMetadata()
        {
            // First make sure it's a file we can actually read from.  Only regular files are relevant,
            // and attempting to open and read from a file such as a named pipe file could cause us to
            // hang (waiting for someone else to open and write to the file).
            Interop.Sys.FileStatus fileStatus;
            if (Interop.Sys.Stat(_fileName, out fileStatus) != 0 ||
                (fileStatus.Mode & Interop.Sys.FileTypes.S_IFMT) != Interop.Sys.FileTypes.S_IFREG)
            {
                throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, _fileName), _fileName);
            }

            try
            {
                // Try to load the file using the managed metadata reader
                using (FileStream assemblyStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0x1000, useAsync: false))
                using (PEReader peReader = new PEReader(assemblyStream))
                {
                    if (peReader.HasMetadata)
                    {
                        MetadataReader metadataReader = peReader.GetMetadataReader();
                        if (metadataReader.IsAssembly)
                        {
                            LoadManagedAssemblyMetadata(metadataReader, peReader.PEHeaders.IsExe);
                            return true;
                        }
                    }
                }
            }
            catch (BadImageFormatException) { }
            return false;
        }

        /// <summary>Load our fields from the metadata of the file as represented by the provided metadata reader.</summary>
        /// <param name="metadataReader">The metadata reader for the CLI file this represents.</param>\
        /// <param name="isExe">true if the assembly represents an executable; false if it's a dll.</param>
        private void LoadManagedAssemblyMetadata(MetadataReader metadataReader, bool isExe)
        {
            AssemblyDefinition assemblyDefinition = metadataReader.GetAssemblyDefinition();

            // Set the internal and original names based on the assembly name.  We avoid using the
            // current filename for determinism and better alignment with behavior on Windows.
            string assemblyName = metadataReader.GetString(assemblyDefinition.Name);
            if (!assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                !assemblyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName += isExe ? ".exe" : ".dll";
            }
            _internalName = _originalFilename = assemblyName;

            // Set the product version based on the assembly's version (this may be overwritten 
            // later in the method).
            Version productVersion = assemblyDefinition.Version;
            _productVersion = productVersion.ToString();
            _productMajor = productVersion.Major;
            _productMinor = productVersion.Minor;
            _productBuild = productVersion.Build != -1 ? productVersion.Build : 0;
            _productPrivate = productVersion.Revision != -1 ? productVersion.Revision : 0;

            // "Language Neutral" is used on Win32 for unknown language identifiers.
            _language = "Language Neutral";

            // Set other fields to default values in case they're not overwritten by attributes
            _companyName = string.Empty;
            _comments = string.Empty;
            _fileDescription = " "; // this is what the managed compiler outputs when value isn't set
            _fileVersion = string.Empty;
            _legalCopyright = " "; // this is what the managed compiler outputs when value isn't set
            _legalTrademarks = string.Empty;
            _productName = string.Empty;
            _privateBuild = string.Empty;
            _specialBuild = string.Empty;

            // Be explicit about initialization to suppress warning about fields not being set
            _isDebug = false;
            _isPatched = false;
            _isPreRelease = false;
            _isPrivateBuild = false;
            _isSpecialBuild = false;

            bool sawAssemblyInformationalVersionAttribute = false;

            // Everything else is parsed from assembly attributes
            MetadataStringComparer comparer = metadataReader.StringComparer;
            foreach (CustomAttributeHandle attrHandle in assemblyDefinition.GetCustomAttributes())
            {
                CustomAttribute attr = metadataReader.GetCustomAttribute(attrHandle);
                StringHandle typeNamespaceHandle = default(StringHandle), typeNameHandle = default(StringHandle);
                if (TryGetAttributeName(metadataReader, attr, out typeNamespaceHandle, out typeNameHandle) &&
                    comparer.Equals(typeNamespaceHandle, "System.Reflection"))
                {
                    if (comparer.Equals(typeNameHandle, "AssemblyCompanyAttribute"))
                    {
                        GetStringAttributeArgumentValue(metadataReader, attr, ref _companyName);
                    }
                    else if (comparer.Equals(typeNameHandle, "AssemblyCopyrightAttribute"))
                    {
                        GetStringAttributeArgumentValue(metadataReader, attr, ref _legalCopyright);
                    }
                    else if (comparer.Equals(typeNameHandle, "AssemblyDescriptionAttribute"))
                    {
                        GetStringAttributeArgumentValue(metadataReader, attr, ref _comments);
                    }
                    else if (comparer.Equals(typeNameHandle, "AssemblyFileVersionAttribute"))
                    {
                        GetStringAttributeArgumentValue(metadataReader, attr, ref _fileVersion);
                        ParseVersion(_fileVersion, out _fileMajor, out _fileMinor, out _fileBuild, out _filePrivate);
                    }
                    else if (comparer.Equals(typeNameHandle, "AssemblyInformationalVersionAttribute"))
                    {
                        GetStringAttributeArgumentValue(metadataReader, attr, ref _productVersion);
                        ParseVersion(_productVersion, out _productMajor, out _productMinor, out _productBuild, out _productPrivate);
                        sawAssemblyInformationalVersionAttribute = true;
                    }
                    else if (comparer.Equals(typeNameHandle, "AssemblyProductAttribute"))
                    {
                        GetStringAttributeArgumentValue(metadataReader, attr, ref _productName);
                    }
                    else if (comparer.Equals(typeNameHandle, "AssemblyTrademarkAttribute"))
                    {
                        GetStringAttributeArgumentValue(metadataReader, attr, ref _legalTrademarks);
                    }
                    else if (comparer.Equals(typeNameHandle, "AssemblyTitleAttribute"))
                    {
                        GetStringAttributeArgumentValue(metadataReader, attr, ref _fileDescription);
                    }
                }
            }

            // When the managed compiler sees an [AssemblyVersion(...)] attribute, it uses that to set 
            // both the assembly version and the product version in the Win32 resources. If it doesn't 
            // see an [AssemblyVersion(...)], then it sets the assembly version to 0.0.0.0, however it 
            // sets the product version in the Win32 resources to whatever was defined in the 
            // [AssemblyFileVersionAttribute(...)] if there was one (unless there is an AssemblyInformationalVersionAttribute,
            // in which case it always uses that for the product version).  Without parsing the Win32 resources,
            // we can't differentiate these two cases, so given the rarity of explicitly setting an 
            // assembly's version number to 0.0.0.0, we assume that if it is 0.0.0.0 then the attribute 
            // wasn't specified and we use the file version.

            if (!sawAssemblyInformationalVersionAttribute && _productVersion == "0.0.0.0")
            {
                _productVersion = _fileVersion;
                _productMajor = _fileMajor;
                _productMinor = _fileMinor;
                _productBuild = _fileBuild;
                _productPrivate = _filePrivate;
            }
        }

        /// <summary>Parses the version into its constituent parts.</summary>
        private static void ParseVersion(string versionString, out int major, out int minor, out int build, out int priv)
        {
            // Relatively-forgiving parsing of a version:
            // - If there are more than four parts (separated by periods), all results are deemed 0
            // - If any part fails to parse completely as an integer, no further parts are parsed and are left as 0.
            // - If any part partially parses as an integer, that value is used for that part.
            // - Whitespace is treated like any other non-digit character and thus isn't ignored.
            // - Each component is parsed as a ushort, allowing for overflow.

            string[] parts = versionString.Split(s_versionSeparators);
            major = minor = build = priv = 0;
            if (parts.Length <= 4)
            {
                bool endedEarly;
                if (parts.Length > 0)
                {
                    major = ParseUInt16UntilNonDigit(parts[0], out endedEarly);
                    if (!endedEarly && parts.Length > 1)
                    {
                        minor = ParseUInt16UntilNonDigit(parts[1], out endedEarly);
                        if (!endedEarly && parts.Length > 2)
                        {
                            build = ParseUInt16UntilNonDigit(parts[2], out endedEarly);
                            if (!endedEarly && parts.Length > 3)
                            {
                                priv = ParseUInt16UntilNonDigit(parts[3], out endedEarly);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Parses a string as a UInt16 until it hits a non-digit.</summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The parsed value.</returns>
        private static ushort ParseUInt16UntilNonDigit(string s, out bool endedEarly)
        {
            endedEarly = false;
            ushort result = 0;

            for (int index = 0; index < s.Length; index++)
            {
                char c = s[index];
                if (c < '0' || c > '9')
                {
                    endedEarly = true;
                    break;
                }
                result = (ushort)((result * 10) + (c - '0')); // explicitly allow for overflow, as this is the behavior employed on Windows
            }

            return result;
        }

        /// <summary>Gets the name of an attribute.</summary>
        /// <param name="reader">The metadata reader.</param>
        /// <param name="attr">The attribute.</param>
        /// <param name="typeNamespaceHandle">The namespace of the attribute.</param>
        /// <param name="typeNameHandle">The name of the attribute.</param>
        /// <returns>true if the name could be retrieved; otherwise, false.</returns>
        private static bool TryGetAttributeName(MetadataReader reader, CustomAttribute attr, out StringHandle typeNamespaceHandle, out StringHandle typeNameHandle)
        {
            EntityHandle ctorHandle = attr.Constructor;
            switch (ctorHandle.Kind)
            {
                case HandleKind.MemberReference:
                    EntityHandle container = reader.GetMemberReference((MemberReferenceHandle)ctorHandle).Parent;
                    if (container.Kind == HandleKind.TypeReference)
                    {
                        TypeReference tr = reader.GetTypeReference((TypeReferenceHandle)container);
                        typeNamespaceHandle = tr.Namespace;
                        typeNameHandle = tr.Name;
                        return true;
                    }
                    break;

                case HandleKind.MethodDefinition:
                    MethodDefinition md = reader.GetMethodDefinition((MethodDefinitionHandle)ctorHandle);
                    TypeDefinition td = reader.GetTypeDefinition(md.GetDeclaringType());
                    typeNamespaceHandle = td.Namespace;
                    typeNameHandle = td.Name;
                    return true;
            }

            // Unusual case, potentially invalid IL
            typeNamespaceHandle = default(StringHandle);
            typeNameHandle = default(StringHandle);
            return false;
        }

        /// <summary>Gets the string argument value of an attribute with a single fixed string argument.</summary>
        /// <param name="reader">The metadata reader.</param>
        /// <param name="attr">The attribute.</param>
        /// <param name="value">The value parsed from the attribute, if it could be retrieved; otherwise, the value is left unmodified.</param>
        private static void GetStringAttributeArgumentValue(MetadataReader reader, CustomAttribute attr, ref string value)
        {
            EntityHandle ctorHandle = attr.Constructor;
            BlobHandle signature;
            switch (ctorHandle.Kind)
            {
                case HandleKind.MemberReference:
                    signature = reader.GetMemberReference((MemberReferenceHandle)ctorHandle).Signature;
                    break;
                case HandleKind.MethodDefinition:
                    signature = reader.GetMethodDefinition((MethodDefinitionHandle)ctorHandle).Signature;
                    break;
                default:
                    // Unusual case, potentially invalid IL
                    return;
            }

            BlobReader signatureReader = reader.GetBlobReader(signature);
            BlobReader valueReader = reader.GetBlobReader(attr.Value);

            const ushort Prolog = 1; // two-byte "prolog" defined by ECMA-335 (II.23.3) to be at the beginning of attribute value blobs
            if (valueReader.ReadUInt16() == Prolog)
            {
                SignatureHeader header = signatureReader.ReadSignatureHeader();
                int parameterCount;
                if (header.Kind == SignatureKind.Method &&                               // attr ctor must be a method
                    !header.IsGeneric &&                                                 // attr ctor must be non-generic
                    signatureReader.TryReadCompressedInteger(out parameterCount) &&      // read parameter count
                    parameterCount == 1 &&                                               // attr ctor must have 1 parameter
                    signatureReader.ReadSignatureTypeCode() == SignatureTypeCode.Void && // attr ctor return type must be void
                    signatureReader.ReadSignatureTypeCode() == SignatureTypeCode.String) // attr ctor first parameter must be string
                {
                    value = valueReader.ReadSerializedString();
                }
            }
        }

    }
}
