// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// Declares an array of attributes for a member and defines
    /// the properties and methods that give you access to the attributes in the array.
    /// All attributes must derive from <see cref='System.Attribute'/>.
    /// </summary>
    public abstract class MemberDescriptor
    {
        private readonly string? _name;
        private readonly string _displayName;
        private readonly int _nameHash;
        private AttributeCollection? _attributeCollection;
        private Attribute[]? _attributes;
        private Attribute[]? _originalAttributes;
        private bool _attributesFiltered;
        private bool _attributesFilled;
        private int _metadataVersion;
        private string? _category;
        private string? _description;
        private readonly object _lockCookie = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the specified <paramref name="name"/> and no attributes.
        /// </summary>
        protected MemberDescriptor(string name) : this(name, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the specified <paramref name="name"/> and <paramref name="attributes "/> array.
        /// </summary>
        protected MemberDescriptor(string name, Attribute[]? attributes)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.InvalidMemberName, nameof(name));
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
        /// Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the specified <see cref='System.ComponentModel.MemberDescriptor'/>.
        /// </summary>
        protected MemberDescriptor(MemberDescriptor descr)
        {
            ArgumentNullException.ThrowIfNull(descr);

            _name = descr.Name;
            _displayName = _name;
            _nameHash = _name?.GetHashCode() ?? 0;

            _attributes = new Attribute[descr.Attributes.Count];
            descr.Attributes.CopyTo(_attributes, 0);

            _attributesFiltered = true;

            _originalAttributes = _attributes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.MemberDescriptor'/> class with the name in the specified
        /// <see cref='System.ComponentModel.MemberDescriptor'/> and the attributes
        /// in both the old <see cref='System.ComponentModel.MemberDescriptor'/> and the <see cref='System.Attribute'/> array.
        /// </summary>
        protected MemberDescriptor(MemberDescriptor oldMemberDescriptor, Attribute[]? newAttributes)
        {
            ArgumentNullException.ThrowIfNull(oldMemberDescriptor);

            _name = oldMemberDescriptor.Name;
            _displayName = oldMemberDescriptor.DisplayName;
            _nameHash = _name.GetHashCode();

            List<Attribute> newList = new List<Attribute>();

            if (oldMemberDescriptor.Attributes.Count != 0)
            {
                foreach (Attribute o in oldMemberDescriptor.Attributes)
                {
                    newList.Add(o);
                }
            }

            if (newAttributes != null)
            {
                foreach (Attribute o in newAttributes)
                {
                    newList.Add(o);
                }
            }

            _attributes = new Attribute[newList.Count];
            newList.CopyTo(_attributes, 0);
            _attributesFiltered = false;

            _originalAttributes = _attributes;
        }

        /// <summary>
        /// Gets or sets an array of attributes.
        /// </summary>
        protected virtual Attribute[]? AttributeArray
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
        /// Gets the collection of attributes for this member.
        /// </summary>
        public virtual AttributeCollection Attributes
        {
            get
            {
                CheckAttributesValid();
                AttributeCollection? attrs = _attributeCollection;
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
        /// Gets the name of the category that the member belongs to, as specified
        /// in the <see cref='System.ComponentModel.CategoryAttribute'/>.
        /// </summary>
        public virtual string Category => _category ?? (_category = ((CategoryAttribute)Attributes[typeof(CategoryAttribute)]!).Category);

        /// <summary>
        /// Gets the description of the member as specified in the <see cref='System.ComponentModel.DescriptionAttribute'/>.
        /// </summary>
        public virtual string Description => _description ??
                                             (_description = ((DescriptionAttribute)Attributes[typeof(DescriptionAttribute)]!).Description);

        /// <summary>
        /// Gets a value indicating whether the member is browsable as specified in the
        /// <see cref='System.ComponentModel.BrowsableAttribute'/>.
        /// </summary>
        public virtual bool IsBrowsable => ((BrowsableAttribute)Attributes[typeof(BrowsableAttribute)]!).Browsable;

        /// <summary>
        /// Gets the name of the member.
        /// </summary>
        public virtual string Name => _name ?? "";

        /// <summary>
        /// Gets the hash code for the name of the member as specified in <see cref='string.GetHashCode()'/>.
        /// </summary>
        protected virtual int NameHashCode => _nameHash;

        /// <summary>
        /// Determines whether this member should be set only at
        /// design time as specified in the <see cref='System.ComponentModel.DesignOnlyAttribute'/>.
        /// </summary>
        public virtual bool DesignTimeOnly => (DesignOnlyAttribute.Yes.Equals(Attributes[typeof(DesignOnlyAttribute)]));

        /// <summary>
        /// Gets the name that can be displayed in a window like a properties window.
        /// </summary>
        public virtual string DisplayName
        {
            get
            {
                if (!(Attributes[typeof(DisplayNameAttribute)] is DisplayNameAttribute displayNameAttr) || displayNameAttr.IsDefaultAttribute())
                {
                    return _displayName;
                }
                return displayNameAttr.DisplayName;
            }
        }

        /// <summary>
        /// Called each time we access the attributes on
        /// this member descriptor to give deriving classes
        /// a chance to change them on the fly.
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
        /// Creates a collection of attributes using the
        /// array of attributes that you passed to the constructor.
        /// </summary>
        protected virtual AttributeCollection CreateAttributeCollection()
        {
            return new AttributeCollection(AttributeArray);
        }

        /// <summary>
        /// Compares this instance to the specified <see cref='System.ComponentModel.MemberDescriptor'/> to see if they are equivalent.
        /// NOTE: If you make a change here, you likely need to change GetHashCode() as well.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
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
                (_category != null && !mdObj._category!.Equals(_category)))
            {
                return false;
            }

            if ((mdObj._description == null) != (_description == null) ||
                (_description != null && !mdObj._description!.Equals(_description)))
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
                if (_attributes.Length != mdObj._attributes!.Length)
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
        /// In an inheriting class, adds the attributes of the inheriting class to the
        /// specified list of attributes in the parent class. For duplicate attributes,
        /// the last one added to the list will be kept.
        /// </summary>
        protected virtual void FillAttributes(IList attributeList)
        {
            ArgumentNullException.ThrowIfNull(attributeList);

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
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    list = new List<Attribute>(_attributes!);
                }

                var map = new Dictionary<object, int>();

                for (int i = 0; i < list.Count;)
                {
                    int savedIndex;
                    object? typeId = list[i]?.TypeId;
                    if (typeId == null)
                    {
                        list.RemoveAt(i);
                    }
                    else if (!map.TryGetValue(typeId, out savedIndex))
                    {
                        map.Add(typeId, i);
                        i++;
                    }
                    else
                    {
                        list[savedIndex] = list[i];
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
        /// Finds the given method through reflection. This method only looks for public methods.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "This method only looks for public methods by hard-coding publicOnly=true")]
        protected static MethodInfo? FindMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type componentClass,
            string name,
            Type[] args,
            Type returnType)
        {
            return FindMethod(componentClass, name, args, returnType, publicOnly: true);
        }

        /// <summary>
        /// Finds the given method through reflection.
        /// </summary>
        protected static MethodInfo? FindMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type componentClass,
            string name,
            Type[] args,
            Type returnType,
            bool publicOnly)
        {
            ArgumentNullException.ThrowIfNull(componentClass);

            MethodInfo? result;
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

        /// <summary>
        /// Try to keep this reasonable in [....] with Equals(). Specifically,
        /// if A.Equals(B) returns true, A &amp; B should have the same hash code.
        /// </summary>
        public override int GetHashCode() => _nameHash;

        /// <summary>
        /// This method returns the object that should be used during invocation of members.
        /// Normally the return value will be the same as the instance passed in. If
        /// someone associated another object with this instance, or if the instance is a
        /// custom type descriptor, GetInvocationTarget may return a different value.
        /// </summary>
        protected virtual object? GetInvocationTarget(Type type, object instance)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(instance);

            return TypeDescriptor.GetAssociation(type, instance);
        }

        /// <summary>
        /// Gets a component site for the given component.
        /// </summary>
        protected static ISite? GetSite(object? component) => (component as IComponent)?.Site;

        [Obsolete("MemberDescriptor.GetInvokee has been deprecated. Use GetInvocationTarget instead.")]
        protected static object GetInvokee(Type componentClass, object component)
        {
            ArgumentNullException.ThrowIfNull(componentClass);
            ArgumentNullException.ThrowIfNull(component);

            return TypeDescriptor.GetAssociation(componentClass, component);
        }
    }
}
