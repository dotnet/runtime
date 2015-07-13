// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    class File_Create_str_i_fo : File_Create_str_i
    {
        #region Utilities

        protected const bool DefaultUseAsync = true;

        public override FileStream Create(string path)
        {
            return File.Create(path, DefaultBufferSize, DefaultUseAsync ? FileOptions.Asynchronous : FileOptions.None);
        }

        public override FileStream Create(string path, int bufferSize)
        {
            return File.Create(path, bufferSize, DefaultUseAsync ? FileOptions.Asynchronous : FileOptions.None);
        }

        #endregion
    }
}
