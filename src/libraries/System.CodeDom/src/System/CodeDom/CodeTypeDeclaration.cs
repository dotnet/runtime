// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.CodeDom
{
    public class CodeTypeDeclaration : CodeTypeMember
    {
        private readonly CodeTypeReferenceCollection _baseTypes = new CodeTypeReferenceCollection();
        private readonly CodeTypeMemberCollection _members = new CodeTypeMemberCollection();
        private bool _isEnum;
        private bool _isStruct;
        private int _populated;
        private const int BaseTypesCollection = 0x1;
        private const int MembersCollection = 0x2;
        private CodeTypeParameterCollection _typeParameters;

        public event EventHandler PopulateBaseTypes;
        public event EventHandler PopulateMembers;

        public CodeTypeDeclaration() { }

        public CodeTypeDeclaration(string name)
        {
            Name = name;
        }

        public TypeAttributes TypeAttributes { get; set; } = TypeAttributes.Public | TypeAttributes.Class;

        public CodeTypeReferenceCollection BaseTypes
        {
            get
            {
                if ((_populated & BaseTypesCollection) == 0)
                {
                    _populated |= BaseTypesCollection;
                    PopulateBaseTypes?.Invoke(this, EventArgs.Empty);
                }

                return _baseTypes;
            }
        }

        public bool IsClass
        {
            get => (TypeAttributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class && !_isEnum && !_isStruct;
            set
            {
                if (value)
                {
                    TypeAttributes &= ~TypeAttributes.ClassSemanticsMask;
                    TypeAttributes |= TypeAttributes.Class;
                    _isStruct = false;
                    _isEnum = false;
                }
            }
        }

        public bool IsStruct
        {
            get => _isStruct;
            set
            {
                if (value)
                {
                    TypeAttributes &= ~TypeAttributes.ClassSemanticsMask;
                    _isEnum = false;
                }

                _isStruct = value;
            }
        }

        public bool IsEnum
        {
            get => _isEnum;
            set
            {
                if (value)
                {
                    TypeAttributes &= ~TypeAttributes.ClassSemanticsMask;
                    _isStruct = false;
                }

                _isEnum = value;
            }
        }

        public bool IsInterface
        {
            get => (TypeAttributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
            set
            {
                if (value)
                {
                    TypeAttributes &= ~TypeAttributes.ClassSemanticsMask;
                    TypeAttributes |= TypeAttributes.Interface;
                    _isStruct = false;
                    _isEnum = false;
                }
                else
                {
                    TypeAttributes &= ~TypeAttributes.Interface;
                }
            }
        }

        public bool IsPartial { get; set; }

        public CodeTypeMemberCollection Members
        {
            get
            {
                if ((_populated & MembersCollection) == 0)
                {
                    _populated |= MembersCollection;
                    PopulateMembers?.Invoke(this, EventArgs.Empty);
                }

                return _members;
            }
        }

        public CodeTypeParameterCollection TypeParameters => _typeParameters ??= new CodeTypeParameterCollection();
    }
}
