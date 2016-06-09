// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * Code related to the <assemblies> config section
 *
 * Copyright (c) 1999 Microsoft Corporation
 */

namespace System.CodeDom.Compiler
{
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Globalization;
    using Res = System.CodeDom.Compiler.Res_CodeDom;

    internal class CodeDomCompilationConfiguration
    {
        internal const String sectionName = "system.codedom";

        private static readonly char[] s_fieldSeparators = new char[] { ';' };

        // _compilerLanguages : Hashtable <string, CompilerInfo>
        internal Hashtable _compilerLanguages;

        // _compilerExtensions : Hashtable <string, CompilerInfo>
        internal Hashtable _compilerExtensions;
        internal ArrayList _allCompilerInfo;

        private static CodeDomCompilationConfiguration s_defaultInstance = new CodeDomCompilationConfiguration();

        internal static CodeDomCompilationConfiguration Default
        {
            get
            {
                return s_defaultInstance;
            }
        }
    }
}


