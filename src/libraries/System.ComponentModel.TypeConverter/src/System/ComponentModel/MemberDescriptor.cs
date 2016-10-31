// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///       Declares an array of attributes for a member and defines
    ///       the properties and methods that give you access to the attributes in the array.
    ///       All attributes must derive from <see cref='System.Attribute'/>.
    ///    </para>
    /// </summary>
    public abstract class MemberDescriptor
    {
        private readonly string _name;
        private readonly string _displayName;
        private readonly int _nameHash;
        private AttributeCollection _attributeCollection;
        private Attribute[] _attributes;
        private Attribute[] _originalAttributes;
        private bool _attributesFiltered = false;
        private bool _attributesFilled = false;
        private int _metadataVersion;
        private string _category;
        private string _description;
        private object _lockCookie = new object();

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the specified <paramref name="name"/> and no attributes.
        ///    </para>
        /// </summary>
        protected MemberDescriptor(string name) : this(name, null)
        {
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the specified <paramref name="name"/> and <paramref name="attributes "/> array.
        ///    </para>
        /// </summary>
        protected MemberDescriptor(string name, Attribute[] attributes)
        {
            if (name == null || name.Length == 0)
            {
                throw new ArgumentException(SR.InvalidMemberName);
            }

            _name = name;
            _displayName = name;
            _nameHash = name.GetHashCode();

            if (attributes != null)
            {
                _attributes = attributes;
                _attributesFiltered = false;
            }

            _originalAttributes = _attributes;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the specified <see cref='System.ComponentModel.MemberDescriptor'/>.
        ///    </para>
        /// </summary>
        protected MemberDescriptor(MemberDescriptor descr)
        {
            _name = descr.Name;
            _displayName = _name;
            _nameHash = _name.GetHashCode();

            _attributes = new Attribute[descr.Attributes.Count];
            descr.Attributes.CopyTo(_attributes, 0);

            _attributesFiltered = true;

            _originalAttributes = _attributes;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the name in the specified
        ///    <see cref='System.ComponentModel.MemberDescriptor'/> and the attributes 
        ///       in both the old <see cref='System.ComponentModel.MemberDescriptor'/> and the <see cref='System.Attribute'/> array.
        ///    </para>
        /// </summary>
        protected MemberDescriptor(MemberDescriptor oldMemberDescriptor, Attribute[] newAttributes)
        {
            _name = oldMemberDescriptor.Name;
            _displayName = oldMemberDescriptor.DisplayName;
            _nameHash = _name.GetHashCode();

            ArrayList newArray = new ArrayList();

            if (oldMemberDescriptor.Attributes.Count != 0)
            {
                foreach (object o in oldMemberDescriptor.Attributes)
                {
                    newArray.Add(o);
                }
            }

            if (newAttributes != null)
            {
                foreach (object o in newAttributes)
                {
                    newArray.Add(o);
                }
            }

            _attributes = new Attribute[newArray.Count];
            newArray.CopyTo(_attributes, 0);
            _attributesFiltered = false;

            _originalAttributes = _attributes;
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets an array of
        ///       attributes.
        ///    </para>
        /// </summary>
        protected virtual Attribute[] AttributeArray
        {
            get
            {
                CheckAttributesValid();
                FilterAttributesIfNeeded();
                return _attributes;
            }
            set
            {
                lock (_lockCookie)
                {
                    _attributes = value;
                    _originalAttributes = value;
                    _attributesFiltered = false;
                    _attributeCollection = null;
                }
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the collection of attributes for this member.
        ///    </para>
        /// </summary>
        public virtual AttributeCollection Attributes
        {
            get
            {
                CheckAttributesValid();
                AttributeCollection attrs = _attributeCollection;
                if (attrs == null)
                {
                    lock (_lockCookie)
                    {
                        attrs = CreateAttributeCollection();
                        _attributeCollection = attrs;
                    }
                }
                return attrs;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the category that the member belongs to, as specified 
        ///       in the <see cref='System.ComponentModel.CategoryAttribute'/>.
        ///    </para>
        /// </summary>
        public virtual string Category
        {
            get
            {
                if (_category == null)
                {
                    _category = ((CategoryAttribute)Attributes[typeof(CategoryAttribute)]).Category;
                }
                return _category;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the description of
        ///       the member as specified in the <see cref='System.ComponentModel.DescriptionAttribute'/>.
        ///    </para>
        /// </summary>
        public virtual string Description
        {
            get
            {
                if (_description == null)
                {
                    _description = ((DescriptionAttribute)Attributes[typeof(DescriptionAttribute)]).Description;
                }
                return _description;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets a value indicating whether the member is browsable as specified in the
        ///    <see cref='System.ComponentModel.BrowsableAttribute'/>. 
        ///    </para>
        /// </summary>
        public virtual bool IsBrowsable
        {
            get
            {
                return ((BrowsableAttribute)Attributes[typeof(BrowsableAttribute)]).Browsable;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the
        ///       name of the member.
        ///    </para>
        /// </summary>
        public virtual string Name
        {
            get
            {
                if (_name == null)
                {
                    return "";
                }
                return _name;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the hash
        ///       code for the name of the member as specified in <see cref='System.String.GetHashCode'/>.
        ///    </para>
        /// </summary>
        protected virtual int NameHashCode
        {
            get
            {
                return _nameHash;
            }
        }

        /// <summary>
        ///    <para>
        ///       Determines whether this member should be set only at
        ///       design time as specified in the <see cref='System.ComponentModel.DesignOnlyAttribute'/>.
        ///    </para>
        /// </summary>
        public virtual bool DesignTimeOnly
        {
            get
            {
                return (DesignOnlyAttribute.Yes.Equals(Attributes[typeof(DesignOnlyAttribute)]));
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the name that can be displayed in a window like a
        ///       properties window.
        ///    </para>
        /// </summary>
        public virtual string DisplayName
        {
            get
            {
                DisplayNameAttribute displayNameAttr = Attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute;
                if (displayNameAttr == null || displayNameAttr.IsDefaultAttribute())
                {
                    return _displayName;
                }
                return displayNameAttr.DisplayName;
            }
        }

        /// <summary>
        ///     Called each time we access the attribtes on
        ///     this member descriptor to give deriving classes
        ///     a chance to change them on the fly.
        /// </summary>
        private void CheckAttributesValid()
        {
            if (_attributesFiltered)
            {
                if (_metadataVersion != TypeDescriptor.MetadataVersion)
                {
                    _attributesFilled = false;
                    _attributesFiltered = false;
                    _attributeCollection = null;
                }
            }
        }

        /// <summary>
        ///    <para>
        ///       Creates a collection of attributes using the
        ///       array of attributes that you passed to the constructor.
        ///    </para>
        /// </summary>
        protected virtual AttributeCollection CreateAttributeCollection()
        {
            return new AttributeCollection(AttributeArray);
        }

        /// <summary>
        ///    <para>
        ///       Compares this instance to the specified <see cref='System.ComponentModel.MemberDescriptor'/> to see if they are equivalent.
        ///       NOTE: If you make a change here, you likely need to change GetHashCode() as well.
        ///    </para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            MemberDescriptor mdObj = (MemberDescriptor)obj;
            FilterAttributesIfNeeded();
            mdObj.FilterAttributesIfNeeded();

            if (mdObj._nameHash != _nameHash)
            {
                return false;
            }

            if ((mdObj._category == null) != (_category == null) ||
                (_category != null && !mdObj._category.Equals(_category)))
            {
                return false;
            }

            if ((mdObj._description == null) != (_description == null) ||
                (_description != null && !mdObj._description.Equals(_description)))
            {
                return false;
            }

            if ((mdObj._attributes == null) != (_attributes == null))
            {
                return false;
            }

            bool sameAttrs = true;

            if (_attributes != null)
            {
                if (_attributes.Length != mdObj._attributes.Length)
                {
                    return false;
                }
                for (int i = 0; i < _attributes.Length; i++)
                {
                    if (!_attributes[i].Equals(mdObj._attributes[i]))
                    {
                        sameAttrs = false;
                        break;
                    }
                }
            }
            return sameAttrs;
        }

        /// <summary>
        ///    <para>
        ///       In an inheriting class, adds the attributes of the inheriting class to the
        ///       specified list of attributes in the parent class.  For duplicate attributes,
        ///       the last one added to the list will be kept.
        ///    </para>
        /// </summary>
        protected virtual void FillAttributes(IList attributeList)
        {
            if (_originalAttributes != null)
            {
                foreach (Attribute attr in _originalAttributes)
                {
                    attributeList.Add(attr);
                }
            }
        }

        private void FilterAttributesIfNeeded()
        {
            if (!_attributesFiltered)
            {
                List<Attribute> list;

                if (!_attributesFilled)
                {
                    list = new List<Attribute>();
                    try
                    {
                        FillAttributes(list);
                    }
                    catch (Exception e)
                    {
                        Debug.Fail($"{_name}>>{e}");
                    }
                }
                else
                {
                    list = new List<Attribute>(_attributes);
                }

                var set = new HashSet<object>();

                for (int i = 0; i < list.Count;)
                {
                    if (set.Add(list[i].TypeId))
                    {
                        ++i;
                    }
                    else
                    {
                        list.RemoveAt(i);
                    }
                }

                Attribute[] newAttributes = list.ToArray();

                lock (_lockCookie)
                {
                    _attributes = newAttributes;
                    _attributesFiltered = true;
                    _attributesFilled = true;
                    _metadataVersion = TypeDescriptor.MetadataVersion;
                }
            }
        }

        /// <summary>
        ///    <para>
        ///       Finds the given method through reflection.  This method only looks for public methods.
        ///    </para>
        /// </summary>
        protected static MethodInfo FindMethod(Type componentClass, string name, Type[] args, Type returnType)
        {
            return FindMethod(componentClass, name, args, returnType, true);
        }

        /// <summary>
        ///    <para>
        ///       Finds the given method through reflection.
        ///    </para>
        /// </summary>
        protected static MethodInfo FindMethod(Type componentClass, string name, Type[] args, Type returnType, bool publicOnly)
        {
            MethodInfo result = null;

            if (publicOnly)
            {
                result = componentClass.GetTypeInfo().GetMethod(name, args);
            }
            else
            {
                // The original impementation requires the method https://msdn.microsoft.com/en-us/library/5fed8f59(v=vs.110).aspx which is not 
                // available on .NET Core. The replacement will use the default BindingFlags, which may miss some methods that had been found
                // on .NET Framework.
                result = componentClass.GetTypeInfo().GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
            }
            if (result != null && !result.ReturnType.GetTypeInfo().IsEquivalentTo(returnType))
            {
                result = null;
            }
            return result;
        }

        /// <summary>
        ///     Try to keep this reasonable in [....] with Equals(). Specifically, 
        ///     if A.Equals(B) returns true, A & B should have the same hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return _nameHash;
        }

        /// <summary>
        ///     This method returns the object that should be used during invocation of members.
        ///     Normally the return value will be the same as the instance passed in.  If
        ///     someone associated another object with this instance, or if the instance is a
        ///     custom type descriptor, GetInvocationTarget may return a different value.
        /// </summary>
        protected virtual object GetInvocationTarget(Type type, object instance)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return TypeDescriptor.GetAssociation(type, instance);
        }

        /// <summary>
        ///    <para>
        ///       Gets a component site
        ///       for the given component.
        ///    </para>
        /// </summary>
        protected static ISite GetSite(object component)
        {
            if (!(component is IComponent))
            {
                return null;
            }

            return ((IComponent)component).Site;
        }

        [Obsolete("This method has been deprecated. Use GetInvocationTarget instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        protected static object GetInvokee(Type componentClass, object component) {

            if (componentClass == null)
            {
                throw new ArgumentNullException(nameof(componentClass));
            }

            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            return TypeDescriptor.GetAssociation(componentClass, component);
        }
    }
}
