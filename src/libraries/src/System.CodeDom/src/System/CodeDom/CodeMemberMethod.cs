// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeMemberMethod : CodeTypeMember
    {
        private readonly CodeParameterDeclarationExpressionCollection _parameters = new CodeParameterDeclarationExpressionCollection();
        private readonly CodeStatementCollection _statements = new CodeStatementCollection();
        private CodeTypeReference _returnType;
        private CodeTypeReferenceCollection _implementationTypes = null;
        private CodeAttributeDeclarationCollection _returnAttributes = null;
        private CodeTypeParameterCollection _typeParameters;

        private int _populated = 0x0;
        private const int ParametersCollection = 0x1;
        private const int StatementsCollection = 0x2;
        private const int ImplTypesCollection = 0x4;

        public event EventHandler PopulateParameters;
        public event EventHandler PopulateStatements;
        public event EventHandler PopulateImplementationTypes;

        public CodeTypeReference ReturnType
        {
            get => _returnType ?? (_returnType = new CodeTypeReference(typeof(void).FullName));
            set => _returnType = value;
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

        public CodeTypeReference PrivateImplementationType { get; set; }

        public CodeTypeReferenceCollection ImplementationTypes
        {
            get
            {
                if (_implementationTypes == null)
                {
                    _implementationTypes = new CodeTypeReferenceCollection();
                }

                if ((_populated & ImplTypesCollection) == 0)
                {
                    _populated |= ImplTypesCollection;
                    PopulateImplementationTypes?.Invoke(this, EventArgs.Empty);
                }

                return _implementationTypes;
            }
        }

        public CodeAttributeDeclarationCollection ReturnTypeCustomAttributes => _returnAttributes ?? (_returnAttributes = new CodeAttributeDeclarationCollection());

        public CodeTypeParameterCollection TypeParameters => _typeParameters ?? (_typeParameters = new CodeTypeParameterCollection());
    }
}
