// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Specifies values for interacting with zip archive entries.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[remarks](~/includes/remarks/System.IO.Compression/ZipArchiveMode/ZipArchiveMode.md)]
    /// ]]></format></remarks>
    public enum ZipArchiveMode
    {
        /// <summary>Only reading archive entries is permitted.</summary>
        Read,
        /// <summary>Only creating new archive entries is permitted.</summary>
        Create,
        /// <summary>Both read and write operations are permitted for archive entries.</summary>
        Update
    }
}
