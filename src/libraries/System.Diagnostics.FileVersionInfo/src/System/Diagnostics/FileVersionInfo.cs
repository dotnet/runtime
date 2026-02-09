// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;

namespace System.Diagnostics
{
    /// <summary>
    /// Provides version information for a physical file on disk.
    /// </summary>
    /// <remarks>
    /// <format type="text/markdown"><![CDATA[
    /// ## Remarks
    ///  Typically, a version number is displayed as "major number.minor number.build number.private part number". A file version number is a 64-bit number that holds the version number for a file as follows:
    ///
    /// - The first 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMajorPart%2A> number.
    ///
    /// - The next 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMinorPart%2A> number.
    ///
    /// - The third set of 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileBuildPart%2A> number.
    ///
    /// - The last 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FilePrivatePart%2A> number.
    ///
    ///  Use the <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> method of this class to get a <xref:System.Diagnostics.FileVersionInfo> containing information about a file, then look at the properties for information about the file. The <xref:System.Diagnostics.FileVersionInfo.FileVersion%2A> property provides version information about the file. The <xref:System.Diagnostics.FileVersionInfo.ProductMajorPart%2A>, <xref:System.Diagnostics.FileVersionInfo.ProductMinorPart%2A>, <xref:System.Diagnostics.FileVersionInfo.ProductBuildPart%2A>, <xref:System.Diagnostics.FileVersionInfo.ProductPrivatePart%2A>, and <xref:System.Diagnostics.FileVersionInfo.ProductVersion%2A> properties provide version information for the product that the specified file is a part of. Call <xref:System.Diagnostics.FileVersionInfo.ToString%2A> to get a partial list of properties and their values for this file.
    ///
    ///  The <xref:System.Diagnostics.FileVersionInfo> properties are based on version resource information built into the file. Version resources are often built into binary files such as .exe or .dll files; text files do not have version resource information.
    ///
    ///  Version resources are typically specified in a Win32 resource file, or in assembly attributes. For example the <xref:System.Diagnostics.FileVersionInfo.IsDebug%2A> property reflects the `VS_FF_DEBUG` flag value in the file's `VS_FIXEDFILEINFO` block, which is built from the `VERSIONINFO` resource in a Win32 resource file.  For more information about specifying version resources in a Win32 resource file, see "About Resource Files" and "VERSIONINFO Resource" in the Platform SDK. For more information about specifying version resources in a .NET module, see the [Setting Assembly Attributes](/dotnet/standard/assembly/set-attributes) topic.
    ///
    /// > [!NOTE]
    /// >  This class makes a link demand at the class level that applies to all members. A <xref:System.Security.SecurityException> is thrown when the immediate caller does not have full trust permission. For details about link demands, see [Link Demands](/dotnet/framework/misc/link-demands).
    ///
    ///
    ///
    /// ## Examples
    ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the file description and version number to the console.
    ///
    ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/Overview/source.cs" id="Snippet1":::
    ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/Overview/source.vb" id="Snippet1":::
    /// ]]></format>
    /// </remarks>
    public sealed partial class FileVersionInfo
    {
        private readonly string _fileName;

        private string? _companyName;
        private string? _fileDescription;
        private string? _fileVersion;
        private string? _internalName;
        private string? _legalCopyright;
        private string? _originalFilename;
        private string? _productName;
        private string? _productVersion;
        private string? _comments;
        private string? _legalTrademarks;
        private string? _privateBuild;
        private string? _specialBuild;
        private string? _language;
        private int _fileMajor;
        private int _fileMinor;
        private int _fileBuild;
        private int _filePrivate;
        private int _productMajor;
        private int _productMinor;
        private int _productBuild;
        private int _productPrivate;
        private bool _isDebug;
        private bool _isPatched;
        private bool _isPrivateBuild;
        private bool _isPreRelease;
        private bool _isSpecialBuild;

        /// <summary>
        /// Gets the comments associated with the file.
        /// </summary>
        /// <returns>The comments associated with the file or <see langword="null" /> if the file did not contain version information.</returns>
        /// <remarks>
        /// <format type="text/markdown"><![CDATA[
        /// ## Remarks
        ///  This property contains additional information that can be displayed for diagnostic purposes.
        ///
        ///
        ///
        /// ## Examples
        ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the comments in a text box. This code assumes `textBox1` has been instantiated.
        ///
        ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/Comments/source.cs" id="Snippet1":::
        ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/Comments/source.vb" id="Snippet1":::
        /// ]]></format>
        /// </remarks>
        public string? Comments
        {
            get { return _comments; }
        }

        /// <summary>
        /// Gets the name of the company that produced the file.
        /// </summary>
        /// <returns>The name of the company that produced the file or <see langword="null" /> if the file did not contain version information.</returns>
        /// <remarks>
        /// <format type="text/markdown"><![CDATA[
        /// ## Examples
        ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the <xref:System.Diagnostics.FileVersionInfo.CompanyName%2A> in a text box. This code assumes `textBox1` has been instantiated.
        ///
        ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/CompanyName/source.cs" id="Snippet1":::
        ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/CompanyName/source.vb" id="Snippet1":::
        /// ]]></format>
        /// </remarks>
        public string? CompanyName
        {
            get { return _companyName; }
        }

        /// <summary>
        /// Gets the build number of the file.
        /// </summary>
        /// <returns>A value representing the build number of the file or 0 (zero) if the file did not contain version information.</returns>
        /// <remarks>
        /// <format type="text/markdown"><![CDATA[
        /// ## Remarks
        ///  Typically, a version number is displayed as "major number.minor number.build number.private part number". A file version number is a 64-bit number that holds the version number for a file as follows:
        ///
        /// - The first 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMajorPart%2A> number.
        ///
        /// - The next 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMinorPart%2A> number.
        ///
        /// - The third set of 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileBuildPart%2A> number.
        ///
        /// - The last 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FilePrivatePart%2A> number.
        ///
        ///  This property gets the third set of 16 bits.
        ///
        ///
        ///
        /// ## Examples
        ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the <xref:System.Diagnostics.FileVersionInfo.FileBuildPart%2A> in a text box. This code assumes `textBox1` has been instantiated.
        ///
        ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/FileBuildPart/source.cs" id="Snippet1":::
        ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/FileBuildPart/source.vb" id="Snippet1":::
        /// ]]></format>
        /// </remarks>
        public int FileBuildPart
        {
            get { return _fileBuild; }
        }

        /// <summary>
        /// Gets the description of the file.
        /// </summary>
        /// <returns>The description of the file or <see langword="null" /> if the file did not contain version information.</returns>
        /// <remarks>
        /// <format type="text/markdown"><![CDATA[
        /// ## Examples
        ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the <xref:System.Diagnostics.FileVersionInfo.FileDescription%2A> in a text box. This code assumes `textBox1` has been instantiated.
        ///
        ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/FileDescription/source.cs" id="Snippet1":::
        ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/FileDescription/source.vb" id="Snippet1":::
        /// ]]></format>
        /// </remarks>
        public string? FileDescription
        {
            get { return _fileDescription; }
        }

        /// <summary>
        /// Gets the major part of the version number.
        /// </summary>
        /// <returns>A value representing the major part of the version number or 0 (zero) if the file did not contain version information.</returns>
        /// <remarks>
        /// <format type="text/markdown"><![CDATA[
        /// ## Remarks
        ///  Typically, a version number is displayed as "major number.minor number.build number.private part number". A file version number is a 64-bit number that holds the version number for a file as follows:
        ///
        /// - The first 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMajorPart%2A> number.
        ///
        /// - The next 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMinorPart%2A> number.
        ///
        /// - The third set of 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileBuildPart%2A> number.
        ///
        /// - The last 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FilePrivatePart%2A> number.
        ///
        ///  This property gets the first set of 16 bits.
        ///
        ///
        ///
        /// ## Examples
        ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the <xref:System.Diagnostics.FileVersionInfo.FileMajorPart%2A> in a text box. This code assumes `textBox1` has been instantiated.
        ///
        ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/FileMajorPart/source.cs" id="Snippet1":::
        ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/FileMajorPart/source.vb" id="Snippet1":::
        /// ]]></format>
        /// </remarks>
        public int FileMajorPart
        {
            get { return _fileMajor; }
        }

        /// <summary>
        /// Gets the minor part of the version number of the file.
        /// </summary>
        /// <returns>A value representing the minor part of the version number of the file or 0 (zero) if the file did not contain version information.</returns>
        /// <remarks>
        /// <format type="text/markdown"><![CDATA[
        /// ## Remarks
        ///  Typically, a version number is displayed as "major number.minor number.build number.private part number". A file version number is a 64-bit number that holds the version number for a file as follows:
        ///
        /// - The first 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMajorPart%2A> number.
        ///
        /// - The next 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMinorPart%2A> number.
        ///
        /// - The third set of 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileBuildPart%2A> number.
        ///
        /// - The last 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FilePrivatePart%2A> number.
        ///
        ///  This property gets the second set of 16 bits.
        ///
        ///
        ///
        /// ## Examples
        ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the <xref:System.Diagnostics.FileVersionInfo.FileMinorPart%2A> in a text box. This code assumes `textBox1` has been instantiated.
        ///
        ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/FileMinorPart/source.cs" id="Snippet1":::
        ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/FileMinorPart/source.vb" id="Snippet1":::
        /// ]]></format>
        /// </remarks>
        public int FileMinorPart
        {
            get { return _fileMinor; }
        }

        /// <summary>
        /// Gets the name of the file that this instance of <see cref="FileVersionInfo" /> describes.
        /// </summary>
        /// <returns>The name of the file described by this instance of <see cref="FileVersionInfo" />.</returns>
        /// <remarks>
        /// <format type="text/markdown"><![CDATA[
        /// ## Examples
        ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the <xref:System.Diagnostics.FileVersionInfo.FileName%2A> in a text box. This code assumes `textBox1` has been instantiated.
        ///
        ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/FileName/source.cs" id="Snippet1":::
        ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/FileName/source.vb" id="Snippet1":::
        /// ]]></format>
        /// </remarks>
        public string FileName
        {
            get { return _fileName; }
        }

        /// <summary>
        /// Gets the file private part number.
        /// </summary>
        /// <returns>A value representing the file private part number or 0 (zero) if the file did not contain version information.</returns>
        /// <remarks>
        /// <format type="text/markdown"><![CDATA[
        /// ## Remarks
        ///  Typically, a version number is displayed as "major number.minor number.build number.private part number". A file version number is a 64-bit number that holds the version number for a file as follows:
        ///
        /// - The first 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMajorPart%2A> number.
        ///
        /// - The next 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMinorPart%2A> number.
        ///
        /// - The third set of 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileBuildPart%2A> number.
        ///
        /// - The last 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FilePrivatePart%2A> number.
        ///
        ///  This property gets the last set of 16 bits.
        ///
        ///
        ///
        /// ## Examples
        ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the <xref:System.Diagnostics.FileVersionInfo.FilePrivatePart%2A> in a text box. This code assumes `textBox1` has been instantiated.
        ///
        ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/FilePrivatePart/source.cs" id="Snippet1":::
        ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/FilePrivatePart/source.vb" id="Snippet1":::
        /// ]]></format>
        /// </remarks>
        public int FilePrivatePart
        {
            get { return _filePrivate; }
        }

        /// <summary>
        /// Gets the file version number.
        /// </summary>
        /// <returns>The version number of the file or <see langword="null" /> if the file did not contain version information.</returns>
        /// <remarks>
        /// <format type="text/markdown"><![CDATA[
        /// ## Remarks
        ///  Typically, a version number is displayed as "major number.minor number.build number.private part number". A file version number is a 64-bit number that holds the version number for a file as follows:
        ///
        /// - The first 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMajorPart%2A> number.
        ///
        /// - The next 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileMinorPart%2A> number.
        ///
        /// - The third set of 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FileBuildPart%2A> number.
        ///
        /// - The last 16 bits are the <xref:System.Diagnostics.FileVersionInfo.FilePrivatePart%2A> number.
        ///
        ///
        ///
        /// ## Examples
        ///  The following example calls <xref:System.Diagnostics.FileVersionInfo.GetVersionInfo%2A> to get the <xref:System.Diagnostics.FileVersionInfo> for the Notepad. Then it prints the file description and version number in a text box. This code assumes `textBox1` has been instantiated.
        ///
        ///  :::code language="csharp" source="~/snippets/csharp/System.Diagnostics/FileVersionInfo/Overview/source.cs" id="Snippet1":::
        ///  :::code language="vb" source="~/snippets/visualbasic/System.Diagnostics/FileVersionInfo/Overview/source.vb" id="Snippet1":::
        /// ]]></format>
        /// </remarks>
        public string? FileVersion
        {
            get { return _fileVersion; }
        }

        /// <summary>
        /// Gets the internal name of the file, if one exists.
        /// </summary>
        /// <returns>The internal name of the file. If none exists, this property will contain the original name of the file without the extension.</returns>
        public string? InternalName
        {
            get { return _internalName; }
        }

        /// <summary>
        /// Gets a value that specifies whether the file contains debugging information or is compiled with debugging features enabled.
        /// </summary>
        /// <returns><see langword="true" /> if the file contains debugging information or is compiled with debugging features enabled; otherwise, <see langword="false" />.</returns>
        public bool IsDebug
        {
            get { return _isDebug; }
        }

        /// <summary>
        /// Gets a value that specifies whether the file has been modified and is not identical to the original shipping file of the same version number.
        /// </summary>
        /// <returns><see langword="true" /> if the file is patched; otherwise, <see langword="false" />.</returns>
        public bool IsPatched
        {
            get { return _isPatched; }
        }

        /// <summary>
        /// Gets a value that specifies whether the file was built using standard release procedures.
        /// </summary>
        /// <returns><see langword="true" /> if the file is a private build; <see langword="false" /> if the file was built using standard release procedures or if the file did not contain version information.</returns>
        public bool IsPrivateBuild
        {
            get { return _isPrivateBuild; }
        }

        /// <summary>
        /// Gets a value that specifies whether the file is a development version, rather than a commercially released product.
        /// </summary>
        /// <returns><see langword="true" /> if the file is prerelease; otherwise, <see langword="false" />.</returns>
        public bool IsPreRelease
        {
            get { return _isPreRelease; }
        }

        /// <summary>
        /// Gets a value that specifies whether the file is a special build.
        /// </summary>
        /// <returns><see langword="true" /> if the file is a special build; otherwise, <see langword="false" />.</returns>
        public bool IsSpecialBuild
        {
            get { return _isSpecialBuild; }
        }

        /// <summary>
        /// Gets the default language string for the version info block.
        /// </summary>
        /// <returns>The description string for the Microsoft Language Identifier in the version resource or <see langword="null" /> if the file did not contain version information.</returns>
        public string? Language
        {
            get { return _language; }
        }

        /// <summary>
        /// Gets all copyright notices that apply to the specified file.
        /// </summary>
        /// <returns>The copyright notices that apply to the specified file.</returns>
        public string? LegalCopyright
        {
            get { return _legalCopyright; }
        }

        /// <summary>
        /// Gets the trademarks and registered trademarks that apply to the file.
        /// </summary>
        /// <returns>The trademarks and registered trademarks that apply to the file or <see langword="null" /> if the file did not contain version information.</returns>
        public string? LegalTrademarks
        {
            get { return _legalTrademarks; }
        }

        /// <summary>
        /// Gets the name the file was created with.
        /// </summary>
        /// <returns>The name the file was created with or <see langword="null" /> if the file did not contain version information.</returns>
        public string? OriginalFilename
        {
            get { return _originalFilename; }
        }

        /// <summary>
        /// Gets information about a private version of the file.
        /// </summary>
        /// <returns>Information about a private version of the file or <see langword="null" /> if the file did not contain version information.</returns>
        public string? PrivateBuild
        {
            get { return _privateBuild; }
        }

        /// <summary>
        /// Gets the build number of the product this file is associated with.
        /// </summary>
        /// <returns>A value representing the build number of the product this file is associated with or 0 (zero) if the file did not contain version information.</returns>
        public int ProductBuildPart
        {
            get { return _productBuild; }
        }

        /// <summary>
        /// Gets the major part of the version number for the product this file is associated with.
        /// </summary>
        /// <returns>A value representing the major part of the product version number or 0 (zero) if the file did not contain version information.</returns>
        public int ProductMajorPart
        {
            get { return _productMajor; }
        }

        /// <summary>
        /// Gets the minor part of the version number for the product the file is associated with.
        /// </summary>
        /// <returns>A value representing the minor part of the product version number or 0 (zero) if the file did not contain version information.</returns>
        public int ProductMinorPart
        {
            get { return _productMinor; }
        }

        /// <summary>
        /// Gets the name of the product this file is distributed with.
        /// </summary>
        /// <returns>The name of the product this file is distributed with or <see langword="null" /> if the file did not contain version information.</returns>
        public string? ProductName
        {
            get { return _productName; }
        }

        /// <summary>
        /// Gets the private part number of the product this file is associated with.
        /// </summary>
        /// <returns>A value representing the private part number of the product this file is associated with or 0 (zero) if the file did not contain version information.</returns>
        public int ProductPrivatePart
        {
            get { return _productPrivate; }
        }

        /// <summary>
        /// Gets the version of the product this file is distributed with.
        /// </summary>
        /// <returns>The version of the product this file is distributed with or <see langword="null" /> if the file did not contain version information.</returns>
        public string? ProductVersion
        {
            get { return _productVersion; }
        }

        /// <summary>
        /// Gets the special build information for the file.
        /// </summary>
        /// <returns>The special build information for the file or <see langword="null" /> if the file did not contain version information.</returns>
        public string? SpecialBuild
        {
            get { return _specialBuild; }
        }

        /// <summary>
        /// Returns a <see cref="FileVersionInfo" /> representing the version information associated with the specified file.
        /// </summary>
        /// <param name="fileName">The fully qualified path and name of the file to retrieve the version information for.</param>
        /// <exception cref="FileNotFoundException">The file specified cannot be found.</exception>
        /// <returns>A <see cref="FileVersionInfo" /> containing information about the file. If the file did not contain version information, the <see cref="FileVersionInfo" /> contains only the name of the file requested.</returns>
        public static FileVersionInfo GetVersionInfo(string fileName)
        {
            // Check if fileName is a full path. Relative paths can cause confusion if the local file has the .dll extension,
            // as .dll search paths can take over & look for system .dll's in that case.
            if (!Path.IsPathFullyQualified(fileName))
            {
                fileName = Path.GetFullPath(fileName);
            }
            // Check for the existence of the file. File.Exists returns false if Read permission is denied.
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }

            return new FileVersionInfo(fileName);
        }

        /// <summary>
        /// Returns a partial list of properties in the <see cref="FileVersionInfo" /> and their values.
        /// </summary>
        /// <returns>A list of the following properties in this class and their values:
        ///
        /// <see cref="FileName" />, <see cref="InternalName" />, <see cref="OriginalFilename" />, <see cref="FileVersion" />, <see cref="FileDescription" />, <see cref="ProductName" />, <see cref="ProductVersion" />, <see cref="IsDebug" />, <see cref="IsPatched" />, <see cref="IsPreRelease" />, <see cref="IsPrivateBuild" />, <see cref="IsSpecialBuild" />,
        ///
        /// <see cref="Language" />.
        ///
        /// If the file did not contain version information, this list will contain only the name of the requested file. Boolean values will be <see langword="false" />, and all other entries will be <see langword="null" />.</returns>
        public override string ToString()
        {
            // An initial capacity of 512 was chosen because it is large enough to cover
            // the size of the static strings with enough capacity left over to cover
            // average length property values.
            var sb = new StringBuilder(512);
            sb.Append("File:             ").AppendLine(FileName);
            sb.Append("InternalName:     ").AppendLine(InternalName);
            sb.Append("OriginalFilename: ").AppendLine(OriginalFilename);
            sb.Append("FileVersion:      ").AppendLine(FileVersion);
            sb.Append("FileDescription:  ").AppendLine(FileDescription);
            sb.Append("Product:          ").AppendLine(ProductName);
            sb.Append("ProductVersion:   ").AppendLine(ProductVersion);
            sb.Append("Debug:            ").AppendLine(IsDebug.ToString());
            sb.Append("Patched:          ").AppendLine(IsPatched.ToString());
            sb.Append("PreRelease:       ").AppendLine(IsPreRelease.ToString());
            sb.Append("PrivateBuild:     ").AppendLine(IsPrivateBuild.ToString());
            sb.Append("SpecialBuild:     ").AppendLine(IsSpecialBuild.ToString());
            sb.Append("Language:         ").AppendLine(Language);
            return sb.ToString();
        }
    }
}
