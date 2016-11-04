// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Microsoft.VisualBasic
{
    public class VBCodeProvider : CodeDomProvider
    {
        private VBCodeGenerator _generator;

        public VBCodeProvider()
        {
            _generator = new VBCodeGenerator();
        }

        public VBCodeProvider(IDictionary<string, string> providerOptions)
        {
            if (providerOptions == null)
            {
                throw new ArgumentNullException(nameof(providerOptions));
            }

            _generator = new VBCodeGenerator(providerOptions);
        }

        public override string FileExtension => "vb";

        public override LanguageOptions LanguageOptions => LanguageOptions.CaseInsensitive;

        [Obsolete("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeGenerator CreateGenerator() => _generator;

        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeCompiler CreateCompiler() => _generator;

        public override TypeConverter GetConverter(Type type) =>
            type == typeof(MemberAttributes) ? VBMemberAttributeConverter.Default :
            type == typeof(TypeAttributes) ? VBTypeAttributeConverter.Default :
            base.GetConverter(type);

        public override void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options) =>
            _generator.GenerateCodeFromMember(member, writer, options);
    }
}
