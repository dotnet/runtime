// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System.Diagnostics;
    using System;
    using Microsoft.Win32;
    using System.Collections;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <devdoc>
    ///    <para>
    ///       Represents a
    ///       class or nested class.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeTypeDeclaration : CodeTypeMember
    {
        private TypeAttributes _attributes = Reflection.TypeAttributes.Public | Reflection.TypeAttributes.Class;
        private CodeTypeReferenceCollection _baseTypes = new CodeTypeReferenceCollection();
        private CodeTypeMemberCollection _members = new CodeTypeMemberCollection();

        private bool _isEnum;
        private bool _isStruct;
        private int _populated = 0x0;
        private const int BaseTypesCollection = 0x1;
        private const int MembersCollection = 0x2;

        // Need to be made optionally serializable
        private CodeTypeParameterCollection _typeParameters;
        private bool _isPartial = false;


        /// <devdoc>
        ///    <para>
        ///       An event that will be fired the first time the BaseTypes Collection is accessed.  
        ///    </para>
        /// </devdoc>
        public event EventHandler PopulateBaseTypes;

        /// <devdoc>
        ///    <para>
        ///       An event that will be fired the first time the Members Collection is accessed.  
        ///    </para>
        /// </devdoc>
        public event EventHandler PopulateMembers;


        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeTypeDeclaration'/>.
        ///    </para>
        /// </devdoc>
        public CodeTypeDeclaration()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeTypeDeclaration'/> with the specified name.
        ///    </para>
        /// </devdoc>
        public CodeTypeDeclaration(string name)
        {
            Name = name;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the attributes of the class.
        ///    </para>
        /// </devdoc>
        public TypeAttributes TypeAttributes
        {
            get
            {
                return _attributes;
            }
            set
            {
                _attributes = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the base types of the class.
        ///    </para>
        /// </devdoc>
        public CodeTypeReferenceCollection BaseTypes
        {
            get
            {
                if (0 == (_populated & BaseTypesCollection))
                {
                    _populated |= BaseTypesCollection;
                    if (PopulateBaseTypes != null) PopulateBaseTypes(this, EventArgs.Empty);
                }
                return _baseTypes;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value
        ///       indicating whether the class is a class.
        ///    </para>
        /// </devdoc>
        public bool IsClass
        {
            get
            {
                return (_attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class && !_isEnum && !_isStruct;
            }
            set
            {
                if (value)
                {
                    _attributes &= ~TypeAttributes.ClassSemanticsMask;
                    _attributes |= TypeAttributes.Class;
                    _isStruct = false;
                    _isEnum = false;
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value
        ///       indicating whether the class is a struct.
        ///    </para>
        /// </devdoc>
        public bool IsStruct
        {
            get
            {
                return _isStruct;
            }
            set
            {
                if (value)
                {
                    _attributes &= ~TypeAttributes.ClassSemanticsMask;
                    _isStruct = true;
                    _isEnum = false;
                }
                else
                {
                    _isStruct = false;
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value
        ///       indicating whether the class is an enumeration.
        ///    </para>
        /// </devdoc>
        public bool IsEnum
        {
            get
            {
                return _isEnum;
            }
            set
            {
                if (value)
                {
                    _attributes &= ~TypeAttributes.ClassSemanticsMask;
                    _isStruct = false;
                    _isEnum = true;
                }
                else
                {
                    _isEnum = false;
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value
        ///       indicating whether the class is an interface.
        ///    </para>
        /// </devdoc>
        public bool IsInterface
        {
            get
            {
                return (_attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
            }
            set
            {
                if (value)
                {
                    _attributes &= ~TypeAttributes.ClassSemanticsMask;
                    _attributes |= TypeAttributes.Interface;
                    _isStruct = false;
                    _isEnum = false;
                }
                else
                {
                    _attributes &= ~TypeAttributes.Interface;
                }
            }
        }

        public bool IsPartial
        {
            get
            {
                return _isPartial;
            }
            set
            {
                _isPartial = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the class member collection members.
        ///    </para>
        /// </devdoc>
        public CodeTypeMemberCollection Members
        {
            get
            {
                if (0 == (_populated & MembersCollection))
                {
                    _populated |= MembersCollection;
                    if (PopulateMembers != null) PopulateMembers(this, EventArgs.Empty);
                }
                return _members;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public CodeTypeParameterCollection TypeParameters
        {
            get
            {
                if (_typeParameters == null)
                {
                    _typeParameters = new CodeTypeParameterCollection();
                }
                return _typeParameters;
            }
        }
    }
}
