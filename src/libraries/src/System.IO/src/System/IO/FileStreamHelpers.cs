// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Diagnostics;

namespace System.IO
{
    // Use reflection to access the implementation of FileStream in System.IO.FileSystem.dll. While far from ideal,
    // we do this to avoid having to directly reference FileSystem as doing so introduces a dependency cycle. A more
    // permanent solution would be to merge the IO and FileSystem assemblies.
    internal static class FileStreamHelpers
    {
        private const int FileMode_Create = 2;
        private const int FileMode_Open = 3;
        private const int FileMode_Append = 6;
        private const int FileAccess_Read = 1;
        private const int FileAccess_Write = 2;
        private const int FileShare_Read = 1;

        private static FileOpenDelegate s_fileOpen;
        private delegate Stream FileOpenDelegate(string path, int fileMode, int fileAccess, int fileShare);

        public static Stream CreateFileStream(string path, bool write, bool append)
        {
            if (s_fileOpen == null)
            {
                s_fileOpen = GetFileOpenFunction();
            }
            int filemode = !write ? FileMode_Open : append ? FileMode_Append : FileMode_Create;
            int fileaccess = write ? FileAccess_Write : FileAccess_Read;
            return s_fileOpen(path, filemode, fileaccess, FileShare_Read);
        }

        private static FileOpenDelegate GetFileOpenFunction()
        {
            Type fileSystemType = Type.GetType("System.IO.File, System.IO.FileSystem, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
            var methodInfos = fileSystemType?.GetTypeInfo().GetDeclaredMethods("Open");
            if (methodInfos != null)
            {
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    var methodParams = methodInfo.GetParameters();
                    if (methodParams?.Length == 4 &&
                        methodParams[0].Name == "path" &&
                        methodParams[1].Name == "mode" &&
                        methodParams[2].Name == "access" &&
                        methodParams[3].Name == "share")
                    {
                        return (FileOpenDelegate)methodInfo.CreateDelegate(typeof(FileOpenDelegate));
                    }
                }
            }
            Debug.Fail("Could not access the File.Open function via reflection into System.IO.FileSystem version 4.0.1.0.");
            return null;
        }
    }
}