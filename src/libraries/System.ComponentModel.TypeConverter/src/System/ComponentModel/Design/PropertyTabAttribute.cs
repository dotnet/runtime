// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// Identifies the property tab or tabs that should be displayed for the
    /// specified class or classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class PropertyTabAttribute : Attribute
    {
        private Type[]? _tabClasses;
        private string[]? _tabClassNames;

        /// <summary>
        /// Basic constructor that creates a PropertyTabAttribute. Use this ctor to derive from this
        /// attribute and specify multiple tab types by calling InitializeArrays.
        /// </summary>
        public PropertyTabAttribute()
        {
            TabScopes = Array.Empty<PropertyTabScope>();
            _tabClassNames = Array.Empty<string>();
        }

        /// <summary>
        /// Basic constructor that creates a property tab attribute that will create a tab
        /// of the specified type.
        /// </summary>
        public PropertyTabAttribute(Type tabClass) : this(tabClass, PropertyTabScope.Component)
        {
        }

        /// <summary>
        /// Basic constructor that creates a property tab attribute that will create a tab
        /// of the specified type.
        /// </summary>
        public PropertyTabAttribute(
            // Using PublicParameterlessConstructor to preserve the type. See https://github.com/mono/linker/issues/1878
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string tabClassName)
            : this(tabClassName, PropertyTabScope.Component)
        {
        }

        /// <summary>
        /// Basic constructor that creates a property tab attribute that will create a tab
        /// of the specified type.
        /// </summary>
        public PropertyTabAttribute(Type tabClass, PropertyTabScope tabScope)
        {
            _tabClasses = new Type[] { tabClass };
            if (tabScope < PropertyTabScope.Document)
            {
                throw new ArgumentException(SR.PropertyTabAttributeBadPropertyTabScope, nameof(tabScope));
            }
            TabScopes = new PropertyTabScope[] { tabScope };
        }

        /// <summary>
        /// Basic constructor that creates a property tab attribute that will create a tab
        /// of the specified type.
        /// </summary>
        public PropertyTabAttribute(
            // Using PublicParameterlessConstructor to preserve the type. See https://github.com/mono/linker/issues/1878
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string tabClassName,
            PropertyTabScope tabScope)
        {
            _tabClassNames = new string[] { tabClassName };
            if (tabScope < PropertyTabScope.Document)
            {
                throw new ArgumentException(SR.PropertyTabAttributeBadPropertyTabScope, nameof(tabScope));
            }
            TabScopes = new PropertyTabScope[] { tabScope };
        }

        /// <summary>
        /// Gets the types of tab that this attribute specifies.
        /// </summary>
        public Type[] TabClasses
        {
            get
            {
                if (_tabClasses == null && _tabClassNames != null)
                {
                    InitializeTabClasses();
                }
                return _tabClasses!;
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The APIs that specify _tabClassNames are either marked with DynamicallyAccessedMembers or RequiresUnreferencedCode.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:TypeGetType",
            Justification = "The APIs that specify _tabClassNames are either marked with DynamicallyAccessedMembers or RequiresUnreferencedCode.")]
        [MemberNotNull(nameof(_tabClasses))]
        private void InitializeTabClasses()
        {
            Debug.Assert(_tabClassNames != null);
            _tabClasses = new Type[_tabClassNames.Length];
            for (int i = 0; i < _tabClassNames.Length; i++)
            {
                int commaIndex = _tabClassNames[i].IndexOf(',');
                string? className;
                string? assemblyName = null;

                if (commaIndex != -1)
                {
                    className = _tabClassNames[i].AsSpan(0, commaIndex).Trim().ToString();
                    assemblyName = _tabClassNames[i].AsSpan(commaIndex + 1).Trim().ToString();
                }
                else
                {
                    className = _tabClassNames[i];
                }

                _tabClasses[i] = Type.GetType(className, false)!;

                if (_tabClasses[i] == null)
                {
                    if (assemblyName != null)
                    {
                        Assembly a = Assembly.Load(assemblyName);
                        if (a != null)
                        {
                            _tabClasses[i] = a.GetType(className, true)!;
                        }
                    }
                    else
                    {
                        throw new TypeLoadException(SR.Format(SR.PropertyTabAttributeTypeLoadException, className));
                    }
                }
            }
        }

        protected string[]? TabClassNames => (string[]?)_tabClassNames?.Clone();

        /// <summary>
        /// Gets the scopes of tabs for this System.ComponentModel.Design.PropertyTabAttribute, from System.ComponentModel.Design.PropertyTabScope.
        /// </summary>
        public PropertyTabScope[] TabScopes { get; private set; }

        public override bool Equals([NotNullWhen(true)] object? other)
            => Equals(other as PropertyTabAttribute);

        public bool Equals([NotNullWhen(true)] PropertyTabAttribute? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (other.TabClasses.Length != TabClasses.Length || other.TabScopes.Length != TabScopes.Length)
                return false;

            for (int i = 0; i < TabClasses.Length; i++)
            {
                if (TabClasses[i] != other.TabClasses[i] || TabScopes[i] != other.TabScopes[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the hashcode for this object.
        /// </summary>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Utiliity function to set the types of tab classes this PropertyTabAttribute specifies.
        /// </summary>
        [RequiresUnreferencedCode("The Types referenced by tabClassNames may be trimmed.")]
        protected void InitializeArrays(string[]? tabClassNames, PropertyTabScope[]? tabScopes)
        {
            InitializeArrays(tabClassNames, null, tabScopes);
        }

        /// <summary>
        /// Utiliity function to set the types of tab classes this PropertyTabAttribute specifies.
        /// </summary>
        protected void InitializeArrays(Type[]? tabClasses, PropertyTabScope[]? tabScopes)
        {
            InitializeArrays(null, tabClasses, tabScopes);
        }

        private void InitializeArrays(string[]? tabClassNames, Type[]? tabClasses, PropertyTabScope[]? tabScopes)
        {
            if (tabClasses != null)
            {
                if (tabScopes != null && tabClasses.Length != tabScopes.Length)
                {
                    throw new ArgumentException(SR.PropertyTabAttributeArrayLengthMismatch);
                }
                _tabClasses = (Type[])tabClasses.Clone();
            }
            else if (tabClassNames != null)
            {
                if (tabScopes != null && tabClassNames.Length != tabScopes.Length)
                {
                    throw new ArgumentException(SR.PropertyTabAttributeArrayLengthMismatch);
                }
                _tabClassNames = (string[])tabClassNames.Clone();
                _tabClasses = null;
            }
            else if (_tabClasses == null && _tabClassNames == null)
            {
                throw new ArgumentException(SR.PropertyTabAttributeParamsBothNull);
            }

            if (tabScopes != null)
            {
                for (int i = 0; i < tabScopes.Length; i++)
                {
                    if (tabScopes[i] < PropertyTabScope.Document)
                    {
                        throw new ArgumentException(SR.PropertyTabAttributeBadPropertyTabScope);
                    }
                }
                TabScopes = (PropertyTabScope[])tabScopes.Clone();
            }
            else
            {
                TabScopes = new PropertyTabScope[tabClasses!.Length];

                for (int i = 0; i < TabScopes.Length; i++)
                {
                    TabScopes[i] = PropertyTabScope.Component;
                }
            }
        }
    }
}
