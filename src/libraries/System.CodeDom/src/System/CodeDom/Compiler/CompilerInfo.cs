// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.CodeDom.Compiler
{
    public sealed class CompilerInfo
    {
        internal readonly IDictionary<string, string> _providerOptions = new Dictionary<string, string>();
        internal string _codeDomProviderTypeName; // This can never by null
        internal CompilerParameters _compilerParams; // This can never by null
        internal string[] _compilerLanguages; // This can never by null
        internal string[] _compilerExtensions; // This can never by null
        private Type _type;

        private CompilerInfo() { } // Not createable

        public string[] GetLanguages() => (string[])_compilerLanguages.Clone();

        public string[] GetExtensions() => (string[])_compilerExtensions.Clone();

        public Type CodeDomProviderType
        {
            get
            {
                if (_type == null)
                {
                    lock (this)
                    {
                        if (_type == null)
                        {
                            _type = Type.GetType(_codeDomProviderTypeName);
                        }
                    }
                }

                return _type;
            }
        }

        public bool IsCodeDomProviderTypeValid => Type.GetType(_codeDomProviderTypeName) != null;

        public CodeDomProvider CreateProvider()
        {
            // if the provider defines an IDictionary<string, string> ctor and
            // provider options have been provided then call that and give it the 
            // provider options dictionary.  Otherwise call the normal one.

            Debug.Assert(_providerOptions != null, "Created CompilerInfo w/ null _providerOptions");

            if (_providerOptions.Count > 0)
            {
                ConstructorInfo ci = CodeDomProviderType.GetConstructor(new Type[] { typeof(IDictionary<string, string>) });
                if (ci != null)
                {
                    return (CodeDomProvider)ci.Invoke(new object[] { _providerOptions });
                }
            }

            return (CodeDomProvider)Activator.CreateInstance(CodeDomProviderType);
        }

        public CodeDomProvider CreateProvider(IDictionary<string, string> providerOptions)
        {
            if (providerOptions == null)
            {
                throw new ArgumentNullException(nameof(providerOptions));
            }

            ConstructorInfo constructor = CodeDomProviderType.GetConstructor(new Type[] { typeof(IDictionary<string, string>) });
            if (constructor != null)
            {
                return (CodeDomProvider)constructor.Invoke(new object[] { providerOptions });
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.Provider_does_not_support_options, CodeDomProviderType));
            }
        }

        public CompilerParameters CreateDefaultCompilerParameters() => CloneCompilerParameters();

        internal CompilerInfo(CompilerParameters compilerParams, string codeDomProviderTypeName)
        {
            _codeDomProviderTypeName = codeDomProviderTypeName;
            _compilerParams = compilerParams ?? new CompilerParameters();
        }

        public override int GetHashCode() => _codeDomProviderTypeName.GetHashCode();

        public override bool Equals(object o)
        {
            return
                o is CompilerInfo other &&
                CodeDomProviderType == other.CodeDomProviderType &&
                CompilerParams.WarningLevel == other.CompilerParams.WarningLevel &&
                CompilerParams.IncludeDebugInformation == other.CompilerParams.IncludeDebugInformation &&
                CompilerParams.CompilerOptions == other.CompilerParams.CompilerOptions;
        }

        private CompilerParameters CloneCompilerParameters()
        {
            CompilerParameters copy = new CompilerParameters();
            copy.IncludeDebugInformation = _compilerParams.IncludeDebugInformation;
            copy.TreatWarningsAsErrors = _compilerParams.TreatWarningsAsErrors;
            copy.WarningLevel = _compilerParams.WarningLevel;
            copy.CompilerOptions = _compilerParams.CompilerOptions;
            return copy;
        }

        internal CompilerParameters CompilerParams => _compilerParams;
    }
}
