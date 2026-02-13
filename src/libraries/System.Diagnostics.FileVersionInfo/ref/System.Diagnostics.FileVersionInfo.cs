// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    /// <summary>
    /// Provides version information for a physical file on disk.
    /// </summary>
    public sealed partial class FileVersionInfo
    {
        internal FileVersionInfo() { }
        /// <summary>
        /// Gets the comments associated with the file.
        /// </summary>
        public string? Comments { get { throw null; } }
        /// <summary>
        /// Gets the name of the company that produced the file.
        /// </summary>
        public string? CompanyName { get { throw null; } }
        /// <summary>
        /// Gets the build number of the file.
        /// </summary>
        public int FileBuildPart { get { throw null; } }
        /// <summary>
        /// Gets the description of the file.
        /// </summary>
        public string? FileDescription { get { throw null; } }
        /// <summary>
        /// Gets the major part of the version number.
        /// </summary>
        public int FileMajorPart { get { throw null; } }
        /// <summary>
        /// Gets the minor part of the version number of the file.
        /// </summary>
        public int FileMinorPart { get { throw null; } }
        /// <summary>
        /// Gets the name of the file that this instance of <see cref="FileVersionInfo" /> describes.
        /// </summary>
        public string FileName { get { throw null; } }
        /// <summary>
        /// Gets the file private part number.
        /// </summary>
        public int FilePrivatePart { get { throw null; } }
        /// <summary>
        /// Gets the file version number.
        /// </summary>
        public string? FileVersion { get { throw null; } }
        /// <summary>
        /// Gets the internal name of the file, if one exists.
        /// </summary>
        public string? InternalName { get { throw null; } }
        /// <summary>
        /// Gets a value that specifies whether the file contains debugging information or is compiled with debugging features enabled.
        /// </summary>
        public bool IsDebug { get { throw null; } }
        /// <summary>
        /// Gets a value that specifies whether the file has been modified and is not identical to the original shipping file of the same version number.
        /// </summary>
        public bool IsPatched { get { throw null; } }
        /// <summary>
        /// Gets a value that specifies whether the file is a development version, rather than a commercially released product.
        /// </summary>
        public bool IsPreRelease { get { throw null; } }
        /// <summary>
        /// Gets a value that specifies whether the file was built using standard release procedures.
        /// </summary>
        public bool IsPrivateBuild { get { throw null; } }
        /// <summary>
        /// Gets a value that specifies whether the file is a special build.
        /// </summary>
        public bool IsSpecialBuild { get { throw null; } }
        /// <summary>
        /// Gets the default language string for the version info block.
        /// </summary>
        public string? Language { get { throw null; } }
        /// <summary>
        /// Gets all copyright notices that apply to the specified file.
        /// </summary>
        public string? LegalCopyright { get { throw null; } }
        /// <summary>
        /// Gets the trademarks and registered trademarks that apply to the file.
        /// </summary>
        public string? LegalTrademarks { get { throw null; } }
        /// <summary>
        /// Gets the name the file was created with.
        /// </summary>
        public string? OriginalFilename { get { throw null; } }
        /// <summary>
        /// Gets information about a private version of the file.
        /// </summary>
        public string? PrivateBuild { get { throw null; } }
        /// <summary>
        /// Gets the build number of the product this file is associated with.
        /// </summary>
        public int ProductBuildPart { get { throw null; } }
        /// <summary>
        /// Gets the major part of the version number for the product this file is associated with.
        /// </summary>
        public int ProductMajorPart { get { throw null; } }
        /// <summary>
        /// Gets the minor part of the version number for the product the file is associated with.
        /// </summary>
        public int ProductMinorPart { get { throw null; } }
        /// <summary>
        /// Gets the name of the product this file is distributed with.
        /// </summary>
        public string? ProductName { get { throw null; } }
        /// <summary>
        /// Gets the private part number of the product this file is associated with.
        /// </summary>
        public int ProductPrivatePart { get { throw null; } }
        /// <summary>
        /// Gets the version of the product this file is distributed with.
        /// </summary>
        public string? ProductVersion { get { throw null; } }
        /// <summary>
        /// Gets the special build information for the file.
        /// </summary>
        public string? SpecialBuild { get { throw null; } }
        /// <summary>
        /// Returns a <see cref="FileVersionInfo" /> representing the version information associated with the specified file.
        /// </summary>
        /// <param name="fileName">The fully qualified path and name of the file to retrieve the version information for.</param>
        /// <returns>A <see cref="FileVersionInfo" /> containing information about the file. If the file did not contain version information, the <see cref="FileVersionInfo" /> contains only the name of the file requested.</returns>
        public static System.Diagnostics.FileVersionInfo GetVersionInfo(string fileName) { throw null; }
        /// <summary>
        /// Returns a partial list of properties in the <see cref="FileVersionInfo" /> and their values.
        /// </summary>
        /// <returns>A list of the following properties in this class and their values: <see cref="FileName" />, <see cref="InternalName" />, <see cref="OriginalFilename" />, <see cref="FileVersion" />, <see cref="FileDescription" />, <see cref="ProductName" />, <see cref="ProductVersion" />, <see cref="IsDebug" />, <see cref="IsPatched" />, <see cref="IsPreRelease" />, <see cref="IsPrivateBuild" />, <see cref="IsSpecialBuild" />, <see cref="Language" />. If the file did not contain version information, this list will contain only the name of the requested file. Boolean values will be <see langword="false" />, and all other entries will be <see langword="null" />.</returns>
        public override string ToString() { throw null; }
    }
}
