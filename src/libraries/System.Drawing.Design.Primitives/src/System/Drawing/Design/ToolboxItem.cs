// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Security.Permissions;

namespace System.Drawing.Design
{
    /// <summary>
    /// Provides a base implementation of a toolbox item.
    /// </summary>
    public class ToolboxItem : ISerializable
    {
        private static TraceSwitch s_toolboxItemPersist = new TraceSwitch(SR.ToolboxPersisting, SR.WriteData);

        private static object s_eventComponentsCreated = new object();
        private static object s_eventComponentsCreating = new object();

        private bool _locked;
        private ConcurrentDictionary<object, object> _properties;
        private ToolboxComponentsCreatedEventHandler _componentsCreatedEvent;
        private ToolboxComponentsCreatingEventHandler _componentsCreatingEvent;

        /// <summary>
        /// Initializes a new instance of the ToolboxItem class.
        /// </summary>
        public ToolboxItem() { }

        /// <summary>
        /// Initializes a new instance of the ToolboxItem class using the specified type.
        /// </summary>
        public ToolboxItem(Type toolType) : this()
        {
            Initialize(toolType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.Design.ToolboxItem'/>
        /// class using the specified serialization information.
        /// </summary>
        protected ToolboxItem(SerializationInfo info, StreamingContext context) : this()
        {
            throw new PlatformNotSupportedException(SR.SerializationOfThisObjectIsNotSupported);
        }

        /// <summary>
        /// The assembly name for this toolbox item. The assembly name describes the assembly
        /// information needed to load the toolbox item's type.
        /// </summary>
        public AssemblyName AssemblyName
        {
            get
            {
                return (AssemblyName)Properties[nameof(AssemblyName)];
            }
            set
            {
                Properties[nameof(AssemblyName)] = value;
            }
        }

        /// <summary>
        /// The assembly name for this toolbox item. The assembly name describes the assembly
        /// information needed to load the toolbox item's type.
        /// </summary>
        public AssemblyName[] DependentAssemblies
        {
            get
            {
                AssemblyName[] names = (AssemblyName[])Properties[nameof(DependentAssemblies)];
                return (AssemblyName[])names?.Clone();
            }
            set
            {
                Properties[nameof(DependentAssemblies)] = value.Clone();
            }
        }

        /// <summary>
        /// Gets or sets the bitmap that will be used on the toolbox for this item. 
        /// Use this property on the design surface as this bitmap is scaled according to the current the DPI setting.
        /// </summary>
        public Bitmap Bitmap
        {
            get
            {
                return (Bitmap)Properties[nameof(Bitmap)];
            }
            set
            {
                Properties[nameof(Bitmap)] = value;
            }
        }

        /// <summary>
        /// Gets or sets the original bitmap that will be used on the toolbox for this item.
        /// This bitmap should be 16x16 pixel and should be used in the Visual Studio toolbox, not on the design surface.
        /// </summary>
        public Bitmap OriginalBitmap
        {
            get
            {
                return (Bitmap)Properties[nameof(OriginalBitmap)];
            }
            set
            {
                Properties[nameof(OriginalBitmap)] = value;
            }
        }

        /// <summary>
        /// Gets or sets the company name for this <see cref='System.Drawing.Design.ToolboxItem'/>.
        /// This defaults to the companyname attribute retrieved from type.Assembly, if set.
        /// </summary>
        public string Company
        {
            get
            {
                return (string)Properties[nameof(Company)];
            }
            set
            {
                Properties[nameof(Company)] = value;
            }
        }

        /// <summary>
        /// The Component Type is ".Net Component" -- unless otherwise specified by a derived toolboxitem
        /// </summary>
        public virtual string ComponentType
        {
            get
            {
                return SR.DotNET_ComponentType;
            }
        }

        /// <summary>
        /// Description is a free-form, multiline capable text description that will be displayed in the tooltip
        /// for the toolboxItem.  It defaults to the path of the assembly that contains the item, but can be overridden.
        /// </summary>
        public string Description
        {
            get
            {
                return (string)Properties[nameof(Description)];
            }
            set
            {
                Properties[nameof(Description)] = value;
            }
        }

        /// <summary>
        /// Gets or sets the display name for this <see cref='System.Drawing.Design.ToolboxItem'/>.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return (string)Properties[nameof(DisplayName)];
            }
            set
            {
                Properties[nameof(DisplayName)] = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter for this toolbox item.  The filter is a collection of
        /// ToolboxItemFilterAttribute objects.
        /// </summary>
        public ICollection Filter
        {
            get
            {
                return (ICollection)Properties[nameof(Filter)];
            }
            set
            {
                Properties[nameof(Filter)] = value;
            }
        }

        /// <summary>
        /// If true, it indicates that this toolbox item should not be stored in
        /// any toolbox database when an application that is providing a toolbox
        /// closes down.  This property defaults to false.
        /// </summary>
        public bool IsTransient
        {
            get
            {
                return (bool)Properties[nameof(IsTransient)];
            }
            set
            {
                Properties[nameof(IsTransient)] = value;
            }
        }

        /// <summary>
        /// Determines if this toolbox item is locked.  Once locked, a toolbox item will
        /// not accept any changes to its properties.
        /// </summary>
        public virtual bool Locked
        {
            get
            {
                return _locked;
            }
        }

        /// <summary>
        /// The properties dictionary is a set of name/value pairs.  The keys are property
        /// names and the values are property values.  This dictionary becomes read-only
        /// after the toolbox item has been locked.
        /// Values in the properties dictionary are validated through ValidateProperty
        /// and default values are obtained from GetDefalutProperty.
        /// </summary>
        public IDictionary Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new ConcurrentDictionary<object, object>();
                }
                return _properties;
            }
        }

        /// <summary>
        /// Gets or sets the fully qualified name of the type this toolbox item will create.
        /// </summary>
        public string TypeName
        {
            get
            {
                return (string)Properties[nameof(TypeName)];
            }
            set
            {
                Properties[nameof(TypeName)] = value;
            }
        }

        /// <summary>
        /// Gets the version for this toolboxitem.  It defaults to AssemblyName.Version unless
        /// overridden in a derived toolboxitem.  This can be overridden to return an empty string
        /// to suppress its display in the toolbox tooltip.
        /// </summary>
        public virtual string Version
        {
            get
            {
                if (AssemblyName != null)
                {
                    return AssemblyName.Version.ToString();
                }
                return string.Empty;
            }
        }


        /// <summary>
        /// Occurs when components are created.
        /// </summary>
        public event ToolboxComponentsCreatedEventHandler ComponentsCreated
        {
            add
            {
                _componentsCreatedEvent += value;
            }
            remove
            {
                _componentsCreatedEvent -= value;
            }
        }

        /// <summary>
        /// Occurs before components are created.
        /// </summary>
        public event ToolboxComponentsCreatingEventHandler ComponentsCreating
        {
            add
            {
                _componentsCreatingEvent += value;
            }
            remove
            {
                _componentsCreatingEvent -= value;
            }
        }

        /// <summary>
        /// This method checks that the toolbox item is currently unlocked (read-write) and
        /// throws an appropriate exception if it isn't.
        /// </summary>
        protected void CheckUnlocked()
        {
            if (Locked)
                throw new InvalidOperationException(SR.ToolboxItemLocked);
        }

        /// <summary>
        /// Creates objects from the type contained in this toolbox item.
        /// </summary>
        /// <returns></returns>
        public IComponent[] CreateComponents()
        {
            return CreateComponents(null);
        }

        /// <summary>
        /// Creates objects from the type contained in this toolbox item.  If designerHost is non-null
        /// this will also add them to the designer.
        /// </summary>
        public IComponent[] CreateComponents(IDesignerHost host)
        {
            OnComponentsCreating(new ToolboxComponentsCreatingEventArgs(host));
            IComponent[] comps = CreateComponentsCore(host, new Hashtable());
            if (comps != null && comps.Length > 0)
            {
                OnComponentsCreated(new ToolboxComponentsCreatedEventArgs(comps));
            }
            return comps;
        }

        /// <summary>
        /// Creates objects from the type contained in this toolbox item.  If designerHost is non-null
        /// this will also add them to the designer.
        /// </summary>
        /// <returns></returns>
        public IComponent[] CreateComponents(IDesignerHost host, IDictionary defaultValues)
        {
            OnComponentsCreating(new ToolboxComponentsCreatingEventArgs(host));
            IComponent[] comps = CreateComponentsCore(host, defaultValues);
            if (comps != null && comps.Length > 0)
            {
                OnComponentsCreated(new ToolboxComponentsCreatedEventArgs(comps));
            }
            return comps;
        }

        /// <summary>
        /// Creates objects from the type contained in this toolbox item.  If designerHost is non-null
        /// this will also add them to the designer.
        /// </summary>
        protected virtual IComponent[] CreateComponentsCore(IDesignerHost host)
        {
            ArrayList comps = new ArrayList();

            Type createType = GetType(host, AssemblyName, TypeName, true);
            if (createType != null)
            {
                if (host != null)
                {
                    comps.Add(host.CreateComponent(createType));
                }
                else if (typeof(IComponent).IsAssignableFrom(createType))
                {
                    comps.Add(TypeDescriptor.CreateInstance(null, createType, null, null));
                }
            }

            IComponent[] temp = new IComponent[comps.Count];
            comps.CopyTo(temp, 0);
            return temp;
        }

        /// <summary>
        /// Creates objects from the type contained in this toolbox item.  If designerHost is non-null
        /// this will also add them to the designer.
        /// </summary>
        protected virtual IComponent[] CreateComponentsCore(IDesignerHost host, IDictionary defaultValues)
        {
            IComponent[] components = CreateComponentsCore(host);

            if (host != null)
            {
                for (int i = 0; i < components.Length; i++)
                {
                    IComponentInitializer init = host.GetDesigner(components[i]) as IComponentInitializer;
                    if (init != null)
                    {
                        try
                        {
                            init.InitializeNewComponent(defaultValues);
                        }
                        catch
                        {
                            for (int index = 0; index < components.Length; index++)
                            {
                                host.DestroyComponent(components[index]);
                            }
                            throw;
                        }
                    }
                }
            }

            return components;
        }

        /// <summary>
        ///  Check if two AssemblyName instances are equivalent
        /// </summary>
        private static bool AreAssemblyNamesEqual(AssemblyName name1, AssemblyName name2)
        {
            return name1 == name2 ||
                   (name1 != null && name2 != null && name1.FullName == name2.FullName);
        }


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

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            ToolboxItem otherItem = (ToolboxItem)obj;

            return TypeName == otherItem.TypeName &&
                   AreAssemblyNamesEqual(AssemblyName, otherItem.AssemblyName) &&
                   DisplayName == otherItem.DisplayName;
        }


        public override int GetHashCode()
        {
            int hash = TypeName?.GetHashCode() ?? 0;
            return unchecked(hash ^ DisplayName.GetHashCode());
        }

        /// <summary>
        /// Filters a property value before returning it.  This allows a property to always clone values,
        ///    or to provide a default value when none exists.
        /// </summary>
        protected virtual object FilterPropertyValue(string propertyName, object value)
        {
            switch (propertyName)
            {
                case nameof(AssemblyName):
                    if (value != null) value = ((AssemblyName)value).Clone();

                    break;

                case nameof(DisplayName):
                case nameof(TypeName):
                    if (value == null) value = string.Empty;

                    break;

                case nameof(Filter):
                    if (value == null) value = new ToolboxItemFilterAttribute[0];

                    break;

                case nameof(IsTransient):
                    if (value == null) value = false;

                    break;
            }
            return value;
        }

        /// <summary>
        /// Allows access to the type associated with the toolbox item.
        /// The designer host is used to access an implementation of ITypeResolutionService.
        /// However, the loaded type is not added to the list of references in the designer host.
        /// </summary>
        public Type GetType(IDesignerHost host)
        {
            return GetType(host, AssemblyName, TypeName, false);
        }

        /// <summary>
        /// This utility function can be used to load a type given a name.  AssemblyName and
        ///     designer host can be null, but if they are present they will be used to help
        ///     locate the type.  If reference is true, the given assembly name will be added
        ///     to the designer host's set of references.
        /// </summary>
        protected virtual Type GetType(IDesignerHost host, AssemblyName assemblyName, string typeName, bool reference)
        {
            ITypeResolutionService ts = null;
            Type type = null;

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            ts = (ITypeResolutionService)host?.GetService(typeof(ITypeResolutionService));

            if (ts != null)
            {
                if (reference)
                {
                    if (assemblyName != null)
                    {
                        ts.ReferenceAssembly(assemblyName);
                        type = ts.GetType(typeName);
                    }
                    else
                    {
                        // Just try loading the type.  If we succeed, then use this as the
                        // reference.
                        type = ts.GetType(typeName);
                        if (type == null)
                        {
                            type = Type.GetType(typeName);
                        }
                        if (type != null)
                        {
                            ts.ReferenceAssembly(type.Assembly.GetName());
                        }
                    }
                }
                else
                {
                    if (assemblyName != null)
                    {
                        Assembly a = ts.GetAssembly(assemblyName);
                        if (a != null)
                        {
                            type = a.GetType(typeName);
                        }
                    }

                    if (type == null)
                    {
                        type = ts.GetType(typeName);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(typeName))
                {
                    if (assemblyName != null)
                    {
                        Assembly a = null;
                        try
                        {
                            a = Assembly.Load(assemblyName);
                        }
                        catch (FileNotFoundException)
                        {
                        }
                        catch (BadImageFormatException)
                        {
                        }
                        catch (IOException)
                        {
                        }

                        if (a == null && assemblyName.CodeBase != null && assemblyName.CodeBase.Length > 0)
                        {
                            try
                            {
                                a = Assembly.LoadFrom(assemblyName.CodeBase);
                            }
                            catch (FileNotFoundException)
                            {
                            }
                            catch (BadImageFormatException)
                            {
                            }
                            catch (IOException)
                            {
                            }
                        }

                        if (a != null)
                        {
                            type = a.GetType(typeName);
                        }
                    }

                    if (type == null)
                    {
                        type = Type.GetType(typeName, false);
                    }
                }
            }

            return type;
        }

        /// <summary>
        /// Given an assemblyname and type, this method searches referenced assemblies from t.Assembly
        /// looking for a similar name.
        /// </summary>
        private AssemblyName GetNonRetargetedAssemblyName(Type type, AssemblyName policiedAssemblyName)
        {
            if (type == null || policiedAssemblyName == null)
                return null;

            //if looking for myself, just return it. (not a reference)
            if (type.Assembly.FullName == policiedAssemblyName.FullName)
            {
                return policiedAssemblyName;
            }

            //first search for an exact match -- we prefer this over a partial match.
            foreach (AssemblyName name in type.Assembly.GetReferencedAssemblies())
            {
                if (name.FullName == policiedAssemblyName.FullName)
                    return name;
            }

            //next search for a partial match -- we just compare the Name portions (ignore version and publickey)
            foreach (AssemblyName name in type.Assembly.GetReferencedAssemblies())
            {
                if (name.Name == policiedAssemblyName.Name)
                    return name;
            }

            //finally, the most expensive -- its possible that retargeting policy is on an assembly whose name changes
            // an example of this is the device System.Windows.Forms.Datagrid.dll
            // in this case, we need to try to load each device assemblyname through policy to see if it results
            // in assemblyname.
            foreach (AssemblyName name in type.Assembly.GetReferencedAssemblies())
            {
                Assembly a = null;

                try
                {
                    a = Assembly.Load(name);
                    if (a != null && a.FullName == policiedAssemblyName.FullName)
                        return name;
                }
                catch
                {
                    //ignore all exceptions and just fall through if it fails (it shouldn't, but who knows).
                }
            }
            return null;
        }

        /// <summary>
        /// Initializes a toolbox item with a given type.  A locked toolbox item cannot be initialized.
        /// </summary>
        public virtual void Initialize(Type type)
        {
            CheckUnlocked();

            if (type != null)
            {
                TypeName = type.FullName;
                AssemblyName assemblyName = type.Assembly.GetName(true);
                if (type.Assembly.GlobalAssemblyCache)
                {
                    assemblyName.CodeBase = null;
                }

                Dictionary<string, AssemblyName> parents = new Dictionary<string, AssemblyName>();
                Type parentType = type;
                while (parentType != null)
                {
                    AssemblyName policiedname = parentType.Assembly.GetName(true);

                    AssemblyName aname = GetNonRetargetedAssemblyName(type, policiedname);

                    if (aname != null && !parents.ContainsKey(aname.FullName))
                    {
                        parents[aname.FullName] = aname;
                    }
                    parentType = parentType.BaseType;
                }

                AssemblyName[] parentAssemblies = new AssemblyName[parents.Count];
                int i = 0;
                foreach (AssemblyName an in parents.Values)
                {
                    parentAssemblies[i++] = an;
                }

                this.DependentAssemblies = parentAssemblies;

                AssemblyName = assemblyName;
                DisplayName = type.Name;

                //if the Type is a reflectonly type, these values must be set through a config object or manually
                //after construction.
                if (!type.Assembly.ReflectionOnly)
                {
                    object[] companyattrs = type.Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);
                    if (companyattrs != null && companyattrs.Length > 0)
                    {
                        AssemblyCompanyAttribute company = companyattrs[0] as AssemblyCompanyAttribute;
                        if (company != null && company.Company != null)
                        {
                            Company = company.Company;
                        }
                    }

                    //set the description based off the description attribute of the given type.
                    DescriptionAttribute descattr = (DescriptionAttribute)TypeDescriptor.GetAttributes(type)[typeof(DescriptionAttribute)];
                    if (descattr != null)
                    {
                        this.Description = descattr.Description;
                    }

                    ToolboxBitmapAttribute attr = (ToolboxBitmapAttribute)TypeDescriptor.GetAttributes(type)[typeof(ToolboxBitmapAttribute)];
                    if (attr != null)
                    {
                        Bitmap = attr.GetImage(type, false) as Bitmap;
                    }

                    bool filterContainsType = false;
                    ArrayList array = new ArrayList();
                    foreach (Attribute a in TypeDescriptor.GetAttributes(type))
                    {
                        ToolboxItemFilterAttribute ta = a as ToolboxItemFilterAttribute;
                        if (ta != null)
                        {
                            if (ta.FilterString.Equals(TypeName))
                            {
                                filterContainsType = true;
                            }
                            array.Add(ta);
                        }
                    }

                    if (!filterContainsType)
                    {
                        array.Add(new ToolboxItemFilterAttribute(TypeName));
                    }

                    Filter = (ToolboxItemFilterAttribute[])array.ToArray(typeof(ToolboxItemFilterAttribute));
                }
            }
        }

        /// <summary>
        /// Locks this toolbox item.  Locking a toolbox item makes it read-only and 
        /// prevents any changes to its properties.
        /// </summary>
        public virtual void Lock()
        {
            _locked = true;
        }

        /// <summary>
        /// Raises the OnComponentsCreated event. This
        /// will be called when this <see cref='System.Drawing.Design.ToolboxItem'/> creates a component.
        /// </summary>
        protected virtual void OnComponentsCreated(ToolboxComponentsCreatedEventArgs args)
        {
            _componentsCreatedEvent?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the OnCreateComponentsInvoked event. This
        /// will be called before this <see cref='System.Drawing.Design.ToolboxItem'/> creates a component.
        /// </summary>
        protected virtual void OnComponentsCreating(ToolboxComponentsCreatingEventArgs args)
        {
            _componentsCreatingEvent?.Invoke(this, args);
        }

        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Called as a helper to ValidatePropertyValue to validate that an object
        ///    is of a given type.
        /// </summary>
        protected void ValidatePropertyType(string propertyName, object value, Type expectedType, bool allowNull)
        {
            if (value == null)
            {
                if (!allowNull)
                {
                    throw new ArgumentNullException(nameof(value));
                }
            }
            else
            {
                if (!expectedType.IsInstanceOfType(value))
                {
                    throw new ArgumentException(SR.Format(SR.ToolboxItemInvalidPropertyType, propertyName, expectedType.FullName), nameof(value));
                }
            }
        }

        /// <summary>
        /// This is called whenever a value is set in the property dictionary.  It gives you a chance
        /// to change the value of an object before comitting it, our reject it by throwing an 
        /// exception.
        /// </summary>
        protected virtual object ValidatePropertyValue(string propertyName, object value)
        {
            switch (propertyName)
            {
                case nameof(AssemblyName):
                    ValidatePropertyType(propertyName, value, typeof(AssemblyName), true);
                    break;

                case nameof(Bitmap):
                    ValidatePropertyType(propertyName, value, typeof(Bitmap), true);
                    break;

                case nameof(OriginalBitmap):
                    ValidatePropertyType(propertyName, value, typeof(Bitmap), true);
                    break;

                case nameof(Company):
                case nameof(Description):
                case nameof(DisplayName):
                case nameof(TypeName):
                    ValidatePropertyType(propertyName, value, typeof(string), true);
                    if (value == null) value = string.Empty;

                    break;

                case nameof(Filter):
                    ValidatePropertyType(propertyName, value, typeof(ICollection), true);

                    int filterCount = 0;
                    ICollection col = (ICollection)value;

                    if (col != null)
                    {
                        foreach (object f in col)
                        {
                            if (f is ToolboxItemFilterAttribute)
                            {
                                filterCount++;
                            }
                        }
                    }

                    ToolboxItemFilterAttribute[] filter = new ToolboxItemFilterAttribute[filterCount];

                    if (col != null)
                    {
                        filterCount = 0;
                        foreach (object f in col)
                        {
                            ToolboxItemFilterAttribute tfa = f as ToolboxItemFilterAttribute;
                            if (tfa != null)
                            {
                                filter[filterCount++] = tfa;
                            }
                        }
                    }

                    value = filter;
                    break;

                case nameof(IsTransient):
                    ValidatePropertyType(propertyName, value, typeof(bool), false);
                    break;
            }
            return value;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException(SR.SerializationOfThisObjectIsNotSupported);
        }
    }
}

