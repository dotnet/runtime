// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.CodeDom
{
    public class CodeMemberMethod : CodeTypeMember
    {
        private readonly CodeParameterDeclarationExpressionCollection _parameters = new CodeParameterDeclarationExpressionCollection();
        private readonly CodeStatementCollection _statements = new CodeStatementCollection();
        private CodeTypeReferenceCollection _implementationTypes;

        private int _populated;
        private static long s_methodIdentityVersion;

        internal static long MethodIdentityVersion => Volatile.Read(ref s_methodIdentityVersion);
        internal static void InvalidateMethodIdentity() => Interlocked.Increment(ref s_methodIdentityVersion);
        internal bool IsMethodIdentityTracked => (_populated & MethodIdentityTracked) != 0;
        internal void TrackMethodIdentity() => _populated |= MethodIdentityTracked;
        internal void OnMethodIdentityChanged()
        {
            if (IsMethodIdentityTracked)
                InvalidateMethodIdentity();
        }

        private const int ParametersCollection = 0x1;
        private const int StatementsCollection = 0x2;
        private const int ImplTypesCollection = 0x4;
        private const int MethodIdentityTracked = 0x8;

        public event EventHandler PopulateParameters;
        public event EventHandler PopulateStatements;
        public event EventHandler PopulateImplementationTypes;

        public CodeTypeReference ReturnType
        {
            get => field ??= new CodeTypeReference(typeof(void).FullName);
            set => field = value;
        }

        public CodeStatementCollection Statements
        {
            get
            {
                if ((_populated & StatementsCollection) == 0)
                {
                    _populated |= StatementsCollection;
                    PopulateStatements?.Invoke(this, EventArgs.Empty);
                }

                return _statements;
            }
        }

        public CodeParameterDeclarationExpressionCollection Parameters
        {
            get
            {
                if ((_populated & ParametersCollection) == 0)
                {
                    _populated |= ParametersCollection;
                    PopulateParameters?.Invoke(this, EventArgs.Empty);
                }

                return _parameters;
            }
        }

        public CodeTypeReference PrivateImplementationType
        {
            get;
            set
            {
                bool changed = !ReferenceEquals(field, value);
                field = value;
                if (changed)
                    OnMethodIdentityChanged();
            }
        }

        public CodeTypeReferenceCollection ImplementationTypes
        {
            get
            {
                _implementationTypes ??= new CodeTypeReferenceCollection();

                if ((_populated & ImplTypesCollection) == 0)
                {
                    _populated |= ImplTypesCollection;
                    PopulateImplementationTypes?.Invoke(this, EventArgs.Empty);
                }

                return _implementationTypes;
            }
        }

        public CodeAttributeDeclarationCollection ReturnTypeCustomAttributes => field ??= new CodeAttributeDeclarationCollection();

        public CodeTypeParameterCollection TypeParameters => field ??= new CodeTypeParameterCollection();
    }
}
