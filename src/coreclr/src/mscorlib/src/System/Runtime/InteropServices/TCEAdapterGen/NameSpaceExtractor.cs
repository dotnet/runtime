// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
