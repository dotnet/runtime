// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    public sealed partial class FileVersionInfo
    {
        private FileVersionInfo(string fileName)
        {
            _fileName = fileName;

            // TODO: Implement this

            _companyName = string.Empty;
            _fileDescription = string.Empty;
            _fileVersion = string.Empty;
            _internalName = string.Empty;
            _legalCopyright = string.Empty;
            _originalFilename = string.Empty;
            _productName = string.Empty;
            _productVersion = string.Empty;
            _comments = string.Empty;
            _legalTrademarks = string.Empty;
            _privateBuild = string.Empty;
            _specialBuild = string.Empty;

            _language = string.Empty;

            _fileMajor = 0;
            _fileMinor = 0;
            _fileBuild = 0;
            _filePrivate = 0;
            _productMajor = 0;
            _productMinor = 0;
            _productBuild = 0;
            _productPrivate = 0;

            _isDebug = false;
            _isPatched = false;
            _isPrivateBuild = false;
            _isPreRelease = false;
            _isSpecialBuild = false;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
