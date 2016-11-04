// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    ///     This attribute can be placed on a class to indicate what serialization
    ///     object should be used to serialize the class at design time if it is
    ///     being used as a root object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    [Obsolete("This attribute has been deprecated. Use DesignerSerializerAttribute instead.  For example, to specify a root designer for CodeDom, use DesignerSerializerAttribute(...,typeof(TypeCodeDomSerializer)).  http://go.microsoft.com/fwlink/?linkid=14202")]
    public sealed class RootDesignerSerializerAttribute : Attribute
    {
        private bool _reloadable;
        private string _serializerTypeName;
        private string _serializerBaseTypeName;
        private string _typeId;

        /// <summary>
        ///     Creates a new designer serialization attribute.
        /// </summary>
        public RootDesignerSerializerAttribute(Type serializerType, Type baseSerializerType, bool reloadable)
        {
            _serializerTypeName = serializerType.AssemblyQualifiedName;
            _serializerBaseTypeName = baseSerializerType.AssemblyQualifiedName;
            _reloadable = reloadable;
        }

        /// <summary>
        ///     Creates a new designer serialization attribute.
        /// </summary>
        public RootDesignerSerializerAttribute(string serializerTypeName, Type baseSerializerType, bool reloadable)
        {
            _serializerTypeName = serializerTypeName;
            _serializerBaseTypeName = baseSerializerType.AssemblyQualifiedName;
            _reloadable = reloadable;
        }

        /// <summary>
        ///     Creates a new designer serialization attribute.
        /// </summary>
        public RootDesignerSerializerAttribute(string serializerTypeName, string baseSerializerTypeName, bool reloadable)
        {
            _serializerTypeName = serializerTypeName;
            _serializerBaseTypeName = baseSerializerTypeName;
            _reloadable = reloadable;
        }

        /// <summary>
        ///     Indicates that this root serializer supports reloading.  If false, the design document
        ///     will not automatically perform a reload on behalf of the user.  It will be the user's
        ///     responsibility to reload the document themselves.
        /// </summary>
        public bool Reloadable
        {
            get
            {
                return _reloadable;
            }
        }

        /// <summary>
        ///     Retrieves the fully qualified type name of the serializer.
        /// </summary>
        public string SerializerTypeName
        {
            get
            {
                return _serializerTypeName;
            }
        }

        /// <summary>
        ///     Retrieves the fully qualified type name of the serializer base type.
        /// </summary>
        public string SerializerBaseTypeName
        {
            get
            {
                return _serializerBaseTypeName;
            }
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>
        ///       This defines a unique ID for this attribute type. It is used
        ///       by filtering algorithms to identify two attributes that are
        ///       the same type. For most attributes, this just returns the
        ///       Type instance for the attribute. EditorAttribute overrides
        ///       this to include the type of the editor base type.
        ///    </para>
        /// </summary>
        public override object TypeId
        {
            get
            {
                if (_typeId == null)
                {
                    string baseType = _serializerBaseTypeName;
                    int comma = baseType.IndexOf(',');
                    if (comma != -1)
                    {
                        baseType = baseType.Substring(0, comma);
                    }
                    _typeId = GetType().FullName + baseType;
                }
                return _typeId;
            }
        }
    }
}

