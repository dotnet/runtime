// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.TCEAdapterGen {

    using System;
    internal static class NameSpaceExtractor
    {
        private static char NameSpaceSeperator = '.';
        
        public static String ExtractNameSpace(String FullyQualifiedTypeName)
        {
            int TypeNameStartPos = FullyQualifiedTypeName.LastIndexOf(NameSpaceSeperator);
            if (TypeNameStartPos == -1)
                return "";
            else
                return FullyQualifiedTypeName.Substring(0, TypeNameStartPos);
         }
    }
}
