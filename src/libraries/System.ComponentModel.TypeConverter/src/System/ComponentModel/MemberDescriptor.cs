// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Collections;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>
    ///       Declares an array of attributes for a member and defines
    ///       the properties and methods that give you access to the attributes in the array.
    ///       All attributes must derive from <see cref='System.Attribute'/>.
    ///    </para>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class MemberDescriptor
    {
        private string _name;
        private string _displayName;
        private int _nameHash;
        private AttributeCollection _attributeCollection;
        private Attribute[] _attributes;
        private Attribute[] _originalAttributes;
        private bool _attributesFiltered = false;
        private bool _attributesFilled = false;
        private int _metadataVersion;
        private string _category;
        private string _description;
        private object _lockCookie = new object();


        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the specified <paramref name="name
        ///       "/> and no
        ///       attributes.
        ///    </para>
        /// </devdoc>
        protected MemberDescriptor(string name) : this(name, null)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the specified <paramref name="name"/>
        ///       and <paramref name="attributes "/>
        ///       array.
        ///    </para>
        /// </devdoc>
        protected MemberDescriptor(string name, Attribute[] attributes)
        {
            try
            {
                if (name == null || name.Length == 0)
                {
                    throw new ArgumentException(SR.GetString(SR.InvalidMemberName));
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
            catch (Exception t)
            {
                Debug.Fail(t.ToString());
                throw t;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the specified <see cref='System.ComponentModel.MemberDescriptor'/>.
        ///    </para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the name in the specified
        ///    <see cref='System.ComponentModel.MemberDescriptor'/> and the attributes 
        ///       in both the old <see cref='System.ComponentModel.MemberDescriptor'/> and the <see cref='System.Attribute'/> array.
        ///    </para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>
        ///       Gets or sets an array of
        ///       attributes.
        ///    </para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>
        ///       Gets the collection of attributes for this member.
        ///    </para>
        /// </devdoc>
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


        /// <devdoc>
        ///    <para>
        ///       Gets the name of the category that the
        ///       member
        ///       belongs to, as specified in the <see cref='System.ComponentModel.CategoryAttribute'/>.
        ///    </para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>
        ///       Gets the description of
        ///       the member as specified in the <see cref='System.ComponentModel.DescriptionAttribute'/>.
        ///    </para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether the member is browsable as specified in the
        ///    <see cref='System.ComponentModel.BrowsableAttribute'/>. 
        ///    </para>
        /// </devdoc>
        public virtual bool IsBrowsable
        {
            get
            {
                return ((BrowsableAttribute)Attributes[typeof(BrowsableAttribute)]).Browsable;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the
        ///       name of the member.
        ///    </para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>
        ///       Gets the hash
        ///       code for the name of the member as specified in <see cref='System.String.GetHashCode'/>.
        ///    </para>
        /// </devdoc>
        protected virtual int NameHashCode
        {
            get
            {
                return _nameHash;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Determines whether this member should be set only at
        ///       design time as specified in the <see cref='System.ComponentModel.DesignOnlyAttribute'/>.
        ///    </para>
        /// </devdoc>
        public virtual bool DesignTimeOnly
        {
            get
            {
                return (DesignOnlyAttribute.Yes.Equals(Attributes[typeof(DesignOnlyAttribute)]));
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the name that can be displayed in a window like a
        ///       properties window.
        ///    </para>
        /// </devdoc>
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

        /// <devdoc>
        ///     Called each time we access the attribtes on
        ///     this member descriptor to give deriving classes
        ///     a chance to change them on the fly.
        /// </devdoc>
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

        /// <include file='doc\MemberDescriptor.uex' path='docs/doc[@for="MemberDescriptor.CreateAttributeCollection"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Creates a collection of attributes using the
        ///       array of attributes that you passed to the constructor.
        ///    </para>
        /// </devdoc>
        protected virtual AttributeCollection CreateAttributeCollection()
        {
            return new AttributeCollection(AttributeArray);
        }

        /// <devdoc>
        ///    <para>
        ///       Compares this instance to the specified <see cref='System.ComponentModel.MemberDescriptor'/> to see if they are equivalent.
        ///       NOTE: If you make a change here, you likely need to change GetHashCode() as well.
        ///    </para>
        /// </devdoc>
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
                (_description != null && !mdObj._category.Equals(_description)))
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

        /// <devdoc>
        ///    <para>
        ///       In an inheriting class, adds the attributes of the inheriting class to the
        ///       specified list of attributes in the parent class.  For duplicate attributes,
        ///       the last one added to the list will be kept.
        ///    </para>
        /// </devdoc>
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
                IList list;

                if (!_attributesFilled)
                {
                    list = new ArrayList();
                    try
                    {
                        FillAttributes(list);
                    }
                    catch (System.Threading.ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        Debug.Fail(_name + ">>" + e.ToString());
                    }
                }
                else
                {
                    list = new ArrayList(_attributes);
                }

                Hashtable hash = new Hashtable(list.Count);

                foreach (Attribute attr in list)
                {
                    hash[attr.TypeId] = attr;
                }

                Attribute[] newAttributes = new Attribute[hash.Values.Count];
                hash.Values.CopyTo(newAttributes, 0);

                lock (_lockCookie)
                {
                    _attributes = newAttributes;
                    _attributesFiltered = true;
                    _attributesFilled = true;
                    _metadataVersion = TypeDescriptor.MetadataVersion;
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Finds the given method through reflection.  This method only looks for public methods.
        ///    </para>
        /// </devdoc>
        protected static MethodInfo FindMethod(Type componentClass, string name, Type[] args, Type returnType)
        {
            return FindMethod(componentClass, name, args, returnType, true);
        }

        /// <devdoc>
        ///    <para>
        ///       Finds the given method through reflection.
        ///    </para>
        /// </devdoc>
        protected static MethodInfo FindMethod(Type componentClass, string name, Type[] args, Type returnType, bool publicOnly)
        {
            MethodInfo result = null;

            if (publicOnly)
            {
                result = componentClass.GetMethod(name, args);
            }
            else
            {
                result = componentClass.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
            }
            if (result != null && !result.ReturnType.IsEquivalentTo(returnType))
            {
                result = null;
            }
            return result;
        }

        /// <devdoc>
        ///     Try to keep this reasonable in [....] with Equals(). Specifically, 
        ///     if A.Equals(B) returns true, A & B should have the same hash code.
        /// </devdoc>
        public override int GetHashCode()
        {
            return _nameHash;
        }

        /// <devdoc>
        ///     This method returns the object that should be used during invocation of members.
        ///     Normally the return value will be the same as the instance passed in.  If
        ///     someone associated another object with this instance, or if the instance is a
        ///     custom type descriptor, GetInvocationTarget may return a different value.
        /// </devdoc>
        protected virtual object GetInvocationTarget(Type type, object instance)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            return TypeDescriptor.GetAssociation(type, instance);
        }

        /// <devdoc>
        ///    <para>
        ///       Gets a component site
        ///       for the given component.
        ///    </para>
        /// </devdoc>
        protected static ISite GetSite(object component)
        {
            if (!(component is IComponent))
            {
                return null;
            }

            return ((IComponent)component).Site;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the component
        ///       that a method should be invoked on.
        ///    </para>
        /// </devdoc>
        [Obsolete("This method has been deprecated. Use GetInvocationTarget instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        protected static object GetInvokee(Type componentClass, object component)
        {
            if (componentClass == null)
            {
                throw new ArgumentNullException("componentClass");
            }

            if (component == null)
            {
                throw new ArgumentNullException("component");
            }

            return TypeDescriptor.GetAssociation(componentClass, component);
        }
    }
}
