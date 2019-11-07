// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.CodeDom
{
    public class CodeNamespace : CodeObject
    {
        private string _name;
        private readonly CodeNamespaceImportCollection _imports = new CodeNamespaceImportCollection();
        private readonly CodeCommentStatementCollection _comments = new CodeCommentStatementCollection();
        private readonly CodeTypeDeclarationCollection _classes = new CodeTypeDeclarationCollection();

        private int _populated = 0x0;
        private const int ImportsCollection = 0x1;
        private const int CommentsCollection = 0x2;
        private const int TypesCollection = 0x4;

        public event EventHandler PopulateComments;
        public event EventHandler PopulateImports;
        public event EventHandler PopulateTypes;

        public CodeNamespace() { }

        public CodeNamespace(string name)
        {
            Name = name;
        }

        public CodeTypeDeclarationCollection Types
        {
            get
            {
                if ((_populated & TypesCollection) == 0)
                {
                    _populated |= TypesCollection;
                    PopulateTypes?.Invoke(this, EventArgs.Empty);
                }

                return _classes;
            }
        }

        public CodeNamespaceImportCollection Imports
        {
            get
            {
                if ((_populated & ImportsCollection) == 0)
                {
                    _populated |= ImportsCollection;
                    PopulateImports?.Invoke(this, EventArgs.Empty);
                }

                return _imports;
            }
        }

        public string Name
        {
            get => _name ?? string.Empty;
            set => _name = value;
        }

        public CodeCommentStatementCollection Comments
        {
            get
            {
                if ((_populated & CommentsCollection) == 0)
                {
                    _populated |= CommentsCollection;
                    PopulateComments?.Invoke(this, EventArgs.Empty);
                }

                return _comments;
            }
        }
    }
}
