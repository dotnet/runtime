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

        internal CodeDomCompilationConfiguration()
        {
            // First time initialization. This must be kept consistent with machine.config.comments in that it
            // must initialize the config system as if that block was present.

            _compilerLanguages = new Hashtable(StringComparer.OrdinalIgnoreCase);
            _compilerExtensions = new Hashtable(StringComparer.OrdinalIgnoreCase);
            _allCompilerInfo = new ArrayList();

            CompilerInfo compilerInfo;
            CompilerParameters compilerParameters;
            String typeName;

            // C#
            compilerParameters = new CompilerParameters();
            compilerParameters.WarningLevel = 4;
            typeName = "Microsoft.CSharp.CSharpCodeProvider, " + AssemblyRef.System;
            compilerInfo = new CompilerInfo(compilerParameters, typeName);
            compilerInfo._compilerLanguages = new string[] { "c#", "cs", "csharp" };
            compilerInfo._compilerExtensions = new string[] { ".cs", "cs" };
            compilerInfo._providerOptions = new Dictionary<string, string>();
            compilerInfo._providerOptions[RedistVersionInfo.NameTag] = RedistVersionInfo.DefaultVersion;
            AddCompilerInfo(compilerInfo);

            // VB
            compilerParameters = new CompilerParameters();
            compilerParameters.WarningLevel = 4;
            typeName = "Microsoft.VisualBasic.VBCodeProvider, " + AssemblyRef.System;
            compilerInfo = new CompilerInfo(compilerParameters, typeName);
            compilerInfo._compilerLanguages = new string[] { "vb", "vbs", "visualbasic", "vbscript" };
            compilerInfo._compilerExtensions = new string[] { ".vb", "vb" };
            compilerInfo._providerOptions = new Dictionary<string, string>();
            compilerInfo._providerOptions[RedistVersionInfo.NameTag] = RedistVersionInfo.DefaultVersion;
            AddCompilerInfo(compilerInfo);

            // JScript
            compilerParameters = new CompilerParameters();
            compilerParameters.WarningLevel = 4;
            typeName = "Microsoft.JScript.JScriptCodeProvider, " + AssemblyRef.MicrosoftJScript;
            compilerInfo = new CompilerInfo(compilerParameters, typeName);
            compilerInfo._compilerLanguages = new string[] { "js", "jscript", "javascript" };
            compilerInfo._compilerExtensions = new string[] { ".js", "js" };
            compilerInfo._providerOptions = new Dictionary<string, string>();
            AddCompilerInfo(compilerInfo);

            //// C++
            //compilerParameters = new CompilerParameters();
            //compilerParameters.WarningLevel = 4;
            //typeName = "Microsoft.VisualC.CppCodeProvider, " + AssemblyRef.MicrosoftVisualCCppCodeProvider;
            //compilerInfo = new CompilerInfo(compilerParameters, typeName);
            //compilerInfo._compilerLanguages = new string[] {"c++", "mc", "cpp"};
            //compilerInfo._compilerExtensions = new string[] {".h", "h"};
            //compilerInfo._providerOptions = new Dictionary<string, string>();
            //AddCompilerInfo(compilerInfo);
        }

        private void AddCompilerInfo(CompilerInfo compilerInfo)
        {
            foreach (string language in compilerInfo._compilerLanguages)
            {
                _compilerLanguages[language] = compilerInfo;
            }

            foreach (string extension in compilerInfo._compilerExtensions)
            {
                _compilerExtensions[extension] = compilerInfo;
            }

            _allCompilerInfo.Add(compilerInfo);
        }
    }
}


