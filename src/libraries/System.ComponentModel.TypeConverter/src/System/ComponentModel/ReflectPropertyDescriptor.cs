// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace System.ComponentModel
{
    /// <internalonly/>
    /// <summary>
    ///    <para>
    ///       ReflectPropertyDescriptor defines a property. Properties are the main way that a user can
    ///       set up the state of a component.
    ///       The ReflectPropertyDescriptor class takes a component class that the property lives on,
    ///       a property name, the type of the property, and various attributes for the
    ///       property.
    ///       For a property named XXX of type YYY, the associated component class is
    ///       required to implement two methods of the following
    ///       form:
    ///    </para>
    ///    <code>
    /// public YYY GetXXX();
    ///     public void SetXXX(YYY value);
    ///    </code>
    ///    The component class can optionally implement two additional methods of
    ///    the following form:
    ///    <code>
    /// public boolean ShouldSerializeXXX();
    ///     public void ResetXXX();
    ///    </code>
    ///    These methods deal with a property's default value. The ShouldSerializeXXX()
    ///    method returns true if the current value of the XXX property is different
    ///    than it's default value, so that it should be persisted out. The ResetXXX()
    ///    method resets the XXX property to its default value. If the ReflectPropertyDescriptor
    ///    includes the default value of the property (using the DefaultValueAttribute),
    ///    the ShouldSerializeXXX() and ResetXXX() methods are ignored.
    ///    If the ReflectPropertyDescriptor includes a reference to an editor
    ///    then that value editor will be used to
    ///    edit the property. Otherwise, a system-provided editor will be used.
    ///    Various attributes can be passed to the ReflectPropertyDescriptor, as are described in
    ///    Attribute.
    ///    ReflectPropertyDescriptors can be obtained by a user programmatically through the
    ///    ComponentManager.
    /// </summary>
    internal sealed class ReflectPropertyDescriptor : PropertyDescriptor
    {
        private static readonly object s_noValue = new object();

        private static readonly int s_bitDefaultValueQueried = InterlockedBitVector32.CreateMask();
        private static readonly int s_bitGetQueried = InterlockedBitVector32.CreateMask(s_bitDefaultValueQueried);
        private static readonly int s_bitSetQueried = InterlockedBitVector32.CreateMask(s_bitGetQueried);
        private static readonly int s_bitShouldSerializeQueried = InterlockedBitVector32.CreateMask(s_bitSetQueried);
        private static readonly int s_bitResetQueried = InterlockedBitVector32.CreateMask(s_bitShouldSerializeQueried);
        private static readonly int s_bitChangedQueried = InterlockedBitVector32.CreateMask(s_bitResetQueried);
        private static readonly int s_bitIPropChangedQueried = InterlockedBitVector32.CreateMask(s_bitChangedQueried);
        private static readonly int s_bitReadOnlyChecked = InterlockedBitVector32.CreateMask(s_bitIPropChangedQueried);
        private static readonly int s_bitAmbientValueQueried = InterlockedBitVector32.CreateMask(s_bitReadOnlyChecked);
        private static readonly int s_bitSetOnDemand = InterlockedBitVector32.CreateMask(s_bitAmbientValueQueried);

        private InterlockedBitVector32 _state;             // Contains the state bits for this proeprty descriptor.
        private readonly Type _componentClass;             // used to determine if we should all on us or on the designer
        private readonly Type _type;                       // the data type of the property
        private object _defaultValue;               // the default value of the property (or noValue)
        private object _ambientValue;               // the ambient value of the property (or noValue)
        private PropertyInfo _propInfo;                   // the property info
        private MethodInfo _getMethod;                  // the property get method
        private MethodInfo _setMethod;                  // the property set method
        private MethodInfo _shouldSerializeMethod;      // the should serialize method
        private MethodInfo _resetMethod;                // the reset property method
        private EventDescriptor _realChangedEvent;           // <propertyname>Changed event handler on object
        private EventDescriptor _realIPropChangedEvent;      // INotifyPropertyChanged.PropertyChanged event handler on object
        private readonly Type _receiverType;               // Only set if we are an extender

        /// <summary>
        ///     The main constructor for ReflectPropertyDescriptors.
        /// </summary>
        public ReflectPropertyDescriptor(Type componentClass, string name, Type type,
                                         Attribute[] attributes)
        : base(name, attributes)
        {
            Debug.WriteLine($"Creating ReflectPropertyDescriptor for {componentClass?.FullName}.{name}");

            try
            {
                if (type == null)
                {
                    Debug.WriteLine($"type == null, name == {name}");
                    throw new ArgumentException(string.Format(SR.ErrorInvalidPropertyType, name));
                }
                if (componentClass == null)
                {
                    Debug.WriteLine($"componentClass == null, name == {name}");
                    throw new ArgumentException(string.Format(SR.InvalidNullArgument, nameof(componentClass)));
                }
                _type = type;
                _componentClass = componentClass;
            }
            catch (Exception t)
            {
                Debug.Fail($"Property '{name}' on component {componentClass.FullName} failed to init.");
                Debug.Fail(t.ToString());
                throw;
            }
        }

        /// <summary>
        ///     A constructor for ReflectPropertyDescriptors that have no attributes.
        /// </summary>
        public ReflectPropertyDescriptor(Type componentClass, string name, Type type, PropertyInfo propInfo, MethodInfo getMethod, MethodInfo setMethod, Attribute[] attrs) : this(componentClass, name, type, attrs)
        {
            _propInfo = propInfo;
            _getMethod = getMethod;
            _setMethod = setMethod;
            if (getMethod != null && propInfo != null && setMethod == null)
                _state.DangerousSet(s_bitGetQueried | s_bitSetOnDemand, true);
            else
                _state.DangerousSet(s_bitGetQueried | s_bitSetQueried, true);
        }

        /// <summary>
        ///     A constructor for ReflectPropertyDescriptors that creates an extender property.
        /// </summary>
        public ReflectPropertyDescriptor(Type componentClass, string name, Type type, Type receiverType, MethodInfo getMethod, MethodInfo setMethod, Attribute[] attrs) : this(componentClass, name, type, attrs)
        {
            _receiverType = receiverType;
            _getMethod = getMethod;
            _setMethod = setMethod;
            _state.DangerousSet(s_bitGetQueried | s_bitSetQueried, true);
        }

        /// <summary>
        ///     This constructor takes an existing ReflectPropertyDescriptor and modifies it by merging in the
        ///     passed-in attributes.
        /// </summary>
        public ReflectPropertyDescriptor(Type componentClass, PropertyDescriptor oldReflectPropertyDescriptor, Attribute[] attributes)
        : base(oldReflectPropertyDescriptor, attributes)
        {
            _componentClass = componentClass;
            _type = oldReflectPropertyDescriptor.PropertyType;

            if (componentClass == null)
            {
                throw new ArgumentException(string.Format(SR.InvalidNullArgument, nameof(componentClass)));
            }

            // If the classes are the same, we can potentially optimize the method fetch because
            // the old property descriptor may already have it.
            //
            ReflectPropertyDescriptor oldProp = oldReflectPropertyDescriptor as ReflectPropertyDescriptor;
            if (oldProp != null)
            {
                if (oldProp.ComponentType == componentClass)
                {
                    _propInfo = oldProp._propInfo;
                    _getMethod = oldProp._getMethod;
                    _setMethod = oldProp._setMethod;
                    _shouldSerializeMethod = oldProp._shouldSerializeMethod;
                    _resetMethod = oldProp._resetMethod;
                    _defaultValue = oldProp._defaultValue;
                    _ambientValue = oldProp._ambientValue;
                    _state = oldProp._state;
                }

                // Now we must figure out what to do with our default value.  First, check to see
                // if the caller has provided an new default value attribute.  If so, use it.  Otherwise,
                // just let it be and it will be picked up on demand.
                //
                if (attributes != null)
                {
                    foreach (Attribute a in attributes)
                    {
                        DefaultValueAttribute dva = a as DefaultValueAttribute;

                        if (dva != null)
                        {
                            _defaultValue = dva.Value;
                            // Default values for enums are often stored as their underlying integer type:
                            if (_defaultValue != null && PropertyType.IsEnum && PropertyType.GetEnumUnderlyingType() == _defaultValue.GetType())
                            {
                                _defaultValue = Enum.ToObject(PropertyType, _defaultValue);
                            }

                            _state.DangerousSet(s_bitDefaultValueQueried, true);
                        }
                        else
                        {
                            AmbientValueAttribute ava = a as AmbientValueAttribute;
                            if (ava != null)
                            {
                                _ambientValue = ava.Value;
                                _state.DangerousSet(s_bitAmbientValueQueried, true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///      Retrieves the ambient value for this property.
        /// </summary>
        private object AmbientValue
        {
            get
            {
                if (!_state[s_bitAmbientValueQueried])
                {
                    Attribute a = Attributes[typeof(AmbientValueAttribute)];
                    if (a != null)
                    {
                        _ambientValue = ((AmbientValueAttribute)a).Value;
                    }
                    else
                    {
                        _ambientValue = s_noValue;
                    }
                    _state[s_bitAmbientValueQueried] = true;
                }
                return _ambientValue;
            }
        }

        /// <summary>
        ///     The EventDescriptor for the "{propertyname}Changed" event on the component, or null if there isn't one for this property.
        /// </summary>
        private EventDescriptor ChangedEventValue
        {
            get
            {
                if (!_state[s_bitChangedQueried])
                {
                    _realChangedEvent = TypeDescriptor.GetEvents(ComponentType)[string.Format(CultureInfo.InvariantCulture, "{0}Changed", Name)];
                    _state[s_bitChangedQueried] = true;
                }

                return _realChangedEvent;
            }
        }

        /// <summary>
        ///     The EventDescriptor for the INotifyPropertyChanged.PropertyChanged event on the component, or null if there isn't one for this property.
        /// </summary>
        private EventDescriptor IPropChangedEventValue
        {
            get
            {
                if (!_state[s_bitIPropChangedQueried])
                {
#if FEATURE_INOTIFYPROPERTYCHANGED
                    if (typeof(INotifyPropertyChanged).IsAssignableFrom(ComponentType))
                    {
                        _realIPropChangedEvent = TypeDescriptor.GetEvents(typeof(INotifyPropertyChanged))["PropertyChanged"];
                    }
#endif
                    _state[s_bitIPropChangedQueried] = true;
                }

                return _realIPropChangedEvent;
            }
            set
            {
                _realIPropChangedEvent = value;
                _state[s_bitIPropChangedQueried] = true;
            }
        }

        /// <summary>
        ///     Retrieves the type of the component this PropertyDescriptor is bound to.
        /// </summary>
        public override Type ComponentType => _componentClass;

        /// <summary>
        ///      Retrieves the default value for this property.
        /// </summary>
        private object DefaultValue
        {
            get
            {
                if (!_state[s_bitDefaultValueQueried])
                {
                    Attribute a = Attributes[typeof(DefaultValueAttribute)];
                    if (a != null)
                    {
                        // Default values for enums are often stored as their underlying integer type:
                        object defaultValue = ((DefaultValueAttribute)a).Value;
                        bool storedAsUnderlyingType = defaultValue != null && PropertyType.IsEnum && PropertyType.GetEnumUnderlyingType() == defaultValue.GetType();
                        _defaultValue = storedAsUnderlyingType ?
                            Enum.ToObject(PropertyType, _defaultValue) :
                            defaultValue;
                    }
                    else
                    {
                        _defaultValue = s_noValue;
                    }
                    _state[s_bitDefaultValueQueried] = true;
                }
                return _defaultValue;
            }
        }

        /// <summary>
        ///     The GetMethod for this property
        /// </summary>
        private MethodInfo GetMethodValue
        {
            get
            {
                if (!_state[s_bitGetQueried])
                {
                    if (_receiverType == null)
                    {
                        if (_propInfo == null)
                        {
#if VERIFY_REFLECTION_CHANGE
                            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty;
                            _propInfo = _componentClass.GetProperty(Name, bindingFlags, null, PropertyType, Array.Empty<Type>(), Array.Empty<ParameterModifier>());
#else 
                            _propInfo = _componentClass.GetProperty(Name, PropertyType, Array.Empty<Type>(), Array.Empty<ParameterModifier>());
#endif
                        }
                        if (_propInfo != null)
                        {
                            _getMethod = _propInfo.GetMethod;
                        }
                        if (_getMethod == null)
                        {
                            throw new InvalidOperationException(string.Format(SR.ErrorMissingPropertyAccessors, _componentClass.FullName + "." + Name));
                        }
                    }
                    else
                    {
                        _getMethod = FindMethod(_componentClass, "Get" + Name, new Type[] { _receiverType }, _type);
                        if (_getMethod == null)
                        {
                            throw new ArgumentException(string.Format(SR.ErrorMissingPropertyAccessors, Name));
                        }
                    }
                    _state[s_bitGetQueried] = true;
                }
                return _getMethod;
            }
        }

        /// <summary>
        ///     Determines if this property is an extender property.
        /// </summary>
        private bool IsExtender => (_receiverType != null);

        /// <summary>
        ///     Indicates whether this property is read only.
        /// </summary>
        public override bool IsReadOnly => SetMethodValue == null || ((ReadOnlyAttribute)Attributes[typeof(ReadOnlyAttribute)]).IsReadOnly;


        /// <summary>
        ///     Retrieves the type of the property.
        /// </summary>
        public override Type PropertyType => _type;

        /// <summary>
        ///     Access to the reset method, if one exists for this property.
        /// </summary>
        private MethodInfo ResetMethodValue
        {
            get
            {
                if (!_state[s_bitResetQueried])
                {
                    Type[] args;

                    if (_receiverType == null)
                    {
                        args = Array.Empty<Type>();
                    }
                    else
                    {
                        args = new Type[] { _receiverType };
                    }

                    _resetMethod = FindMethod(_componentClass, "Reset" + Name, args, typeof(void), /* publicOnly= */ false);

                    _state[s_bitResetQueried] = true;
                }
                return _resetMethod;
            }
        }

        /// <summary>
        ///     Accessor for the set method
        /// </summary>
        private MethodInfo SetMethodValue
        {
            get
            {
                if (!_state[s_bitSetQueried] && _state[s_bitSetOnDemand])
                {
                    string name = _propInfo.Name;

                    if (_setMethod == null)
                    {
                        for (Type t = ComponentType.BaseType; t != null && t != typeof(object); t = t.BaseType)
                        {
#if VERIFY_REFLECTION_CHANGE
                            BindingFlags bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
                            PropertyInfo p = t.GetProperty(name, bindingFlags, null, PropertyType, Array.Empty<Type>(), null);
#endif
                            PropertyInfo p = t.GetProperty(name, PropertyType, Array.Empty<Type>(), null);
                            if (p != null)
                            {
                                _setMethod = p.SetMethod;
                                if (_setMethod != null)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    _state[s_bitSetQueried] = true;
                }
                if (!_state[s_bitSetQueried])
                {
                    if (_receiverType == null)
                    {
                        if (_propInfo == null)
                        {
#if VERIFY_REFLECTION_CHANGE
                            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty;
                            _propInfo = _componentClass.GetProperty(Name, bindingFlags, null, PropertyType, Array.Empty<Type>(), Array.Empty<ParameterModifier>());
#else
                            _propInfo = _componentClass.GetProperty(Name, PropertyType, Array.Empty<Type>(), Array.Empty<ParameterModifier>());
#endif
                        }
                        if (_propInfo != null)
                        {
                            _setMethod = _propInfo.SetMethod;
                        }
                    }
                    else
                    {
                        _setMethod = FindMethod(_componentClass, "Set" + Name,
                                               new Type[] { _receiverType, _type }, typeof(void));
                    }

                    _state[s_bitSetQueried] = true;
                }
                return _setMethod;
            }
        }

        /// <summary>
        ///     Accessor for the ShouldSerialize method.
        /// </summary>
        private MethodInfo ShouldSerializeMethodValue
        {
            get
            {
                if (!_state[s_bitShouldSerializeQueried])
                {
                    Type[] args;

                    if (_receiverType == null)
                    {
                        args = Array.Empty<Type>();
                    }
                    else
                    {
                        args = new Type[] { _receiverType };
                    }

                    _shouldSerializeMethod = FindMethod(_componentClass, "ShouldSerialize" + Name, args, typeof(Boolean), /* publicOnly= */ false);
                    _state[s_bitShouldSerializeQueried] = true;
                }
                return _shouldSerializeMethod;
            }
        }

        /// <summary>
        ///     Allows interested objects to be notified when this property changes.
        /// </summary>
        public override void AddValueChanged(object component, EventHandler handler)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // If there's an event called <propertyname>Changed, hook the caller's handler directly up to that on the component
            EventDescriptor changedEvent = ChangedEventValue;
            if (changedEvent != null && changedEvent.EventType.IsInstanceOfType(handler))
            {
                changedEvent.AddEventHandler(component, handler);
            }

            // Otherwise let the base class add the handler to its ValueChanged event for this component
            else
            {
                // Special case: If this will be the FIRST handler added for this component, and the component implements
                // INotifyPropertyChanged, the property descriptor must START listening to the generic PropertyChanged event
                if (GetValueChangedHandler(component) == null)
                {
                    EventDescriptor iPropChangedEvent = IPropChangedEventValue;
                    iPropChangedEvent?.AddEventHandler(component, new PropertyChangedEventHandler(OnINotifyPropertyChanged));
                }

                base.AddValueChanged(component, handler);
            }
        }

        internal bool ExtenderCanResetValue(IExtenderProvider provider, object component)
        {
            if (DefaultValue != s_noValue)
            {
                return !object.Equals(ExtenderGetValue(provider, component), _defaultValue);
            }

            MethodInfo reset = ResetMethodValue;
            if (reset != null)
            {
                MethodInfo shouldSerialize = ShouldSerializeMethodValue;
                if (shouldSerialize != null)
                {
                    try
                    {
                        provider = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);
                        return (bool)shouldSerialize.Invoke(provider, new object[] { component });
                    }
                    catch { }
                }
                return true;
            }

            return false;
        }

        internal Type ExtenderGetReceiverType()
        {
            return _receiverType;
        }

        internal Type ExtenderGetType(IExtenderProvider provider)
        {
            return PropertyType;
        }

        internal object ExtenderGetValue(IExtenderProvider provider, object component)
        {
            if (provider != null)
            {
                provider = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);
                return GetMethodValue.Invoke(provider, new object[] { component });
            }
            return null;
        }

        internal void ExtenderResetValue(IExtenderProvider provider, object component, PropertyDescriptor notifyDesc)
        {
            if (DefaultValue != s_noValue)
            {
                ExtenderSetValue(provider, component, DefaultValue, notifyDesc);
            }
            else if (AmbientValue != s_noValue)
            {
                ExtenderSetValue(provider, component, AmbientValue, notifyDesc);
            }
            else if (ResetMethodValue != null)
            {
                ISite site = GetSite(component);
                IComponentChangeService changeService = null;
                object oldValue = null;
                object newValue;

                // Announce that we are about to change this component
                //
                if (site != null)
                {
                    changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                    Debug.Assert(!CompModSwitches.CommonDesignerServices.Enabled || changeService != null, "IComponentChangeService not found");
                }

                // Make sure that it is ok to send the onchange events
                //
                if (changeService != null)
                {
                    oldValue = ExtenderGetValue(provider, component);
                    try
                    {
                        changeService.OnComponentChanging(component, notifyDesc);
                    }
                    catch (CheckoutException coEx)
                    {
                        if (coEx == CheckoutException.Canceled)
                        {
                            return;
                        }
                        throw coEx;
                    }
                }

                provider = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);
                if (ResetMethodValue != null)
                {
                    ResetMethodValue.Invoke(provider, new object[] { component });

                    // Now notify the change service that the change was successful.
                    //
                    if (changeService != null)
                    {
                        newValue = ExtenderGetValue(provider, component);
                        changeService.OnComponentChanged(component, notifyDesc, oldValue, newValue);
                    }
                }
            }
        }

        internal void ExtenderSetValue(IExtenderProvider provider, object component, object value, PropertyDescriptor notifyDesc)
        {
            if (provider != null)
            {
                ISite site = GetSite(component);
                IComponentChangeService changeService = null;
                object oldValue = null;

                // Announce that we are about to change this component
                //
                if (site != null)
                {
                    changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                    Debug.Assert(!CompModSwitches.CommonDesignerServices.Enabled || changeService != null, "IComponentChangeService not found");
                }

                // Make sure that it is ok to send the onchange events
                //
                if (changeService != null)
                {
                    oldValue = ExtenderGetValue(provider, component);
                    try
                    {
                        changeService.OnComponentChanging(component, notifyDesc);
                    }
                    catch (CheckoutException coEx)
                    {
                        if (coEx == CheckoutException.Canceled)
                        {
                            return;
                        }
                        throw coEx;
                    }
                }

                provider = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);

                if (SetMethodValue != null)
                {
                    SetMethodValue.Invoke(provider, new object[] { component, value });

                    // Now notify the change service that the change was successful.
                    //
                    changeService?.OnComponentChanged(component, notifyDesc, oldValue, value);
                }
            }
        }

        internal bool ExtenderShouldSerializeValue(IExtenderProvider provider, object component)
        {
            provider = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);

            if (IsReadOnly)
            {
                if (ShouldSerializeMethodValue != null)
                {
                    try
                    {
                        return (bool)ShouldSerializeMethodValue.Invoke(provider, new object[] { component });
                    }
                    catch { }
                }
                return Attributes.Contains(DesignerSerializationVisibilityAttribute.Content);
            }
            else if (DefaultValue == s_noValue)
            {
                if (ShouldSerializeMethodValue != null)
                {
                    try
                    {
                        return (bool)ShouldSerializeMethodValue.Invoke(provider, new object[] { component });
                    }
                    catch { }
                }
                return true;
            }
            return !object.Equals(DefaultValue, ExtenderGetValue(provider, component));
        }

        /// <summary>
        ///     Indicates whether reset will change the value of the component.  If there
        ///     is a DefaultValueAttribute, then this will return true if getValue returns
        ///     something different than the default value.  If there is a reset method and
        ///     a ShouldSerialize method, this will return what ShouldSerialize returns.
        ///     If there is just a reset method, this always returns true.  If none of these
        ///     cases apply, this returns false.
        /// </summary>
        public override bool CanResetValue(object component)
        {
            if (IsExtender || IsReadOnly)
            {
                return false;
            }

            if (DefaultValue != s_noValue)
            {
                return !object.Equals(GetValue(component), DefaultValue);
            }

            if (ResetMethodValue != null)
            {
                if (ShouldSerializeMethodValue != null)
                {
                    component = GetInvocationTarget(_componentClass, component);
                    try
                    {
                        return (bool)ShouldSerializeMethodValue.Invoke(component, null);
                    }
                    catch { }
                }
                return true;
            }

            if (AmbientValue != s_noValue)
            {
                return ShouldSerializeValue(component);
            }

            return false;
        }

        protected override void FillAttributes(IList attributes)
        {
            Debug.Assert(_componentClass != null, "Must have a component class for FillAttributes");

            //
            // The order that we fill in attributes is critical.  The list of attributes will be
            // filtered so that matching attributes at the end of the list replace earlier matches
            // (last one in wins).  Therefore, the four categories of attributes we add must be
            // added as follows:
            //
            // 1.  Attributes of the property type.  These are the lowest level and should be
            //     overwritten by any newer attributes.
            //
            // 2.  Attributes obtained from any SpecificTypeAttribute.  These supercede attributes
            //     for the property type.
            //
            // 3.  Attributes of the property itself, from base class to most derived.  This way
            //     derived class attributes replace base class attributes.
            //
            // 4.  Attributes from our base MemberDescriptor.  While this seems opposite of what
            //     we want, MemberDescriptor only has attributes if someone passed in a new
            //     set in the constructor.  Therefore, these attributes always
            //     supercede existing values.
            //


            // We need to include attributes from the type of the property.
            //
            foreach (Attribute typeAttr in TypeDescriptor.GetAttributes(PropertyType))
            {
                attributes.Add(typeAttr);
            }

            // NOTE : Must look at method OR property, to handle the case of Extender properties...
            //
            // Note : Because we are using BindingFlags.DeclaredOnly it is more effcient to re-acquire
            //      : the property info, rather than use the one we have cached.  The one we have cached
            //      : may ave come from a base class, meaning we will request custom metadata for this
            //      : class twice.

            Type currentReflectType = _componentClass;
            int depth = 0;

            // First, calculate the depth of the object hierarchy.  We do this so we can do a single
            // object create for an array of attributes.
            //
            while (currentReflectType != null && currentReflectType != typeof(object))
            {
                depth++;
                currentReflectType = currentReflectType.BaseType;
            }

            // Now build up an array in reverse order
            //
            if (depth > 0)
            {
                currentReflectType = _componentClass;
                Attribute[][] attributeStack = new Attribute[depth][];

                while (currentReflectType != null && currentReflectType != typeof(object))
                {
                    MemberInfo memberInfo = null;

#if VERIFY_REFLECTION_CHANGE
                    BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
                    // Fill in our member info so we can get at the custom attributes.
                    //
                    if (IsExtender)
                    {
                        //receiverType is used to avoid ambitiousness when there are overloads for the get method.
                        memberInfo = currentReflectType.GetMethod("Get" + Name, bindingFlags, null, new Type[] { _receiverType }, null);
                    }
                    else
                    {
                        memberInfo = currentReflectType.GetProperty(Name, bindingFlags, null, PropertyType, Array.Empty<Type>(), Array.Empty<ParameterModifier>());
                    }
#else
                    // Fill in our member info so we can get at the custom attributes.
                    //
                    if (IsExtender)
                    {
                        //receiverType is used to avoid ambitiousness when there are overloads for the get method.
                        memberInfo = currentReflectType.GetMethod("Get" + Name, new Type[] { _receiverType }, null);
                    }
                    else
                    {
                        memberInfo = currentReflectType.GetProperty(Name, PropertyType, Array.Empty<Type>(), Array.Empty<ParameterModifier>());
                    }
#endif
                    // Get custom attributes for the member info.
                    //
                    if (memberInfo != null)
                    {
                        attributeStack[--depth] = ReflectTypeDescriptionProvider.ReflectGetAttributes(memberInfo);
                    }

                    // Ready for the next loop iteration.
                    //
                    currentReflectType = currentReflectType.BaseType;
                }

                // Look in the attribute stack for AttributeProviders
                // 
                foreach (Attribute[] attributeArray in attributeStack)
                {
                    if (attributeArray != null)
                    {
                        foreach (Attribute attr in attributeArray)
                        {
                            AttributeProviderAttribute sta = attr as AttributeProviderAttribute;
                            if (sta != null)
                            {
                                Type specificType = Type.GetType(sta.TypeName);

                                if (specificType != null)
                                {
                                    Attribute[] stAttrs = null;

                                    if (!String.IsNullOrEmpty(sta.PropertyName))
                                    {
                                        MemberInfo[] milist = specificType.GetMember(sta.PropertyName);
                                        if (milist.Length > 0 && milist[0] != null)
                                        {
                                            stAttrs = ReflectTypeDescriptionProvider.ReflectGetAttributes(milist[0]);
                                        }
                                    }
                                    else
                                    {
                                        stAttrs = ReflectTypeDescriptionProvider.ReflectGetAttributes(specificType);
                                    }
                                    if (stAttrs != null)
                                    {
                                        foreach (Attribute stAttr in stAttrs)
                                        {
                                            attributes.Add(stAttr);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Now trawl the attribute stack so that we add attributes
                // from base class to most derived.
                //
                foreach (Attribute[] attributeArray in attributeStack)
                {
                    if (attributeArray != null)
                    {
                        foreach (Attribute attr in attributeArray)
                        {
                            attributes.Add(attr);
                        }
                    }
                }
            }

            // Include the base attributes.  These override all attributes on the actual
            // property, so we want to add them last.
            //
            base.FillAttributes(attributes);

            // Finally, override any form of ReadOnlyAttribute.  
            //
            if (SetMethodValue == null)
            {
                attributes.Add(ReadOnlyAttribute.Yes);
            }
        }

        /// <summary>
        ///     Retrieves the current value of the property on component,
        ///     invoking the getXXX method.  An exception in the getXXX
        ///     method will pass through.
        /// </summary>
        public override object GetValue(object component)
        {
            Debug.WriteLine($"[{Name}]: GetValue({component?.GetType().Name ?? "(null)"})");

            if (IsExtender)
            {
                Debug.WriteLine($"[{Name}]:   ---> returning: null");
                return null;
            }

            Debug.Assert(component != null, "GetValue must be given a component");

            if (component != null)
            {
                component = GetInvocationTarget(_componentClass, component);


                try
                {
                    return GetMethodValue.Invoke(component, null);
                }
                catch (Exception t)
                {
                    string name = null;
                    IComponent comp = component as IComponent;
                    ISite site = comp?.Site;
                    if (site?.Name != null)
                    {
                        name = site.Name;
                    }

                    if (name == null)
                    {
                        name = component.GetType().FullName;
                    }

                    if (t is TargetInvocationException)
                    {
                        t = t.InnerException;
                    }

                    string message = t.Message ?? t.GetType().Name;

                    throw new TargetInvocationException(string.Format(SR.ErrorPropertyAccessorException, Name, name, message), t);
                }
            }
            Debug.WriteLine("[" + Name + "]:   ---> returning: null");
            return null;
        }

        /// <summary>
        ///     Handles INotifyPropertyChanged.PropertyChange events from components.
        ///     If event pertains to this property, issue a ValueChanged event.
        /// </summary>
        /// </internalonly>
        internal void OnINotifyPropertyChanged(object component, PropertyChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(e.PropertyName) ||
                String.Compare(e.PropertyName, Name, true, System.Globalization.CultureInfo.InvariantCulture) == 0)
            {
                OnValueChanged(component, e);
            }
        }

        /// <summary>
        ///     This should be called by your property descriptor implementation
        ///     when the property value has changed.
        /// </summary>
        protected override void OnValueChanged(object component, EventArgs e)
        {
            if (_state[s_bitChangedQueried] && _realChangedEvent == null)
            {
                base.OnValueChanged(component, e);
            }
        }

        /// <summary>
        ///     Allows interested objects to be notified when this property changes.
        /// </summary>
        public override void RemoveValueChanged(object component, EventHandler handler)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // If there's an event called <propertyname>Changed, we hooked the caller's
            // handler directly up to that on the component, so remove it now.
            EventDescriptor changedEvent = ChangedEventValue;
            if (changedEvent != null && changedEvent.EventType.IsInstanceOfType(handler))
            {
                changedEvent.RemoveEventHandler(component, handler);
            }

            // Otherwise the base class added the handler to its ValueChanged
            // event for this component, so let the base class remove it now.
            else
            {
                base.RemoveValueChanged(component, handler);

                // Special case: If that was the LAST handler removed for this component, and the component implements
                // INotifyPropertyChanged, the property descriptor must STOP listening to the generic PropertyChanged event
                if (GetValueChangedHandler(component) == null)
                {
                    EventDescriptor iPropChangedEvent = IPropChangedEventValue;
                    iPropChangedEvent?.RemoveEventHandler(component, new PropertyChangedEventHandler(OnINotifyPropertyChanged));
                }
            }
        }

        /// <summary>
        ///     Will reset the default value for this property on the component.  If
        ///     there was a default value passed in as a DefaultValueAttribute, that
        ///     value will be set as the value of the property on the component.  If
        ///     there was no default value passed in, a ResetXXX method will be looked
        ///     for.  If one is found, it will be invoked.  If one is not found, this
        ///     is a nop.
        /// </summary>
        public override void ResetValue(object component)
        {
            object invokee = GetInvocationTarget(_componentClass, component);

            if (DefaultValue != s_noValue)
            {
                SetValue(component, DefaultValue);
            }
            else if (AmbientValue != s_noValue)
            {
                SetValue(component, AmbientValue);
            }
            else if (ResetMethodValue != null)
            {
                ISite site = GetSite(component);
                IComponentChangeService changeService = null;
                object oldValue = null;
                object newValue;

                // Announce that we are about to change this component
                //
                if (site != null)
                {
                    changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                    Debug.Assert(!CompModSwitches.CommonDesignerServices.Enabled || changeService != null, "IComponentChangeService not found");
                }

                // Make sure that it is ok to send the onchange events
                //
                if (changeService != null)
                {
                    // invokee might be a type from mscorlib or system, GetMethodValue might return a NonPublic method
                    oldValue = SecurityUtils.MethodInfoInvoke(GetMethodValue, invokee, (object[])null);
                    try
                    {
                        changeService.OnComponentChanging(component, this);
                    }
                    catch (CheckoutException coEx)
                    {
                        if (coEx == CheckoutException.Canceled)
                        {
                            return;
                        }
                        throw coEx;
                    }
                }

                if (ResetMethodValue != null)
                {
                    SecurityUtils.MethodInfoInvoke(ResetMethodValue, invokee, (object[])null);

                    // Now notify the change service that the change was successful.
                    //
                    if (changeService != null)
                    {
                        newValue = SecurityUtils.MethodInfoInvoke(GetMethodValue, invokee, (object[])null);
                        changeService.OnComponentChanged(component, this, oldValue, newValue);
                    }
                }
            }
        }

        /// <summary>
        ///     This will set value to be the new value of this property on the
        ///     component by invoking the setXXX method on the component.  If the
        ///     value specified is invalid, the component should throw an exception
        ///     which will be passed up.  The component designer should design the
        ///     property so that getXXX following a setXXX should return the value
        ///     passed in if no exception was thrown in the setXXX call.
        /// </summary>
        public override void SetValue(object component, object value)
        {
            Debug.WriteLine($"[{Name}]: SetValue({component?.GetType().Name ?? "(null)"}, {value?.GetType().Name ?? "(null)"})");

            if (component != null)
            {
                ISite site = GetSite(component);
                object oldValue = null;

                object invokee = GetInvocationTarget(_componentClass, component);

                if (!IsReadOnly)
                {
                    IComponentChangeService changeService = null;

                    // Announce that we are about to change this component
                    //
                    if (site != null)
                    {
                        changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                        Debug.Assert(!CompModSwitches.CommonDesignerServices.Enabled || changeService != null, "IComponentChangeService not found");
                    }


                    // Make sure that it is ok to send the onchange events
                    //
                    if (changeService != null)
                    {
                        oldValue = SecurityUtils.MethodInfoInvoke(GetMethodValue, invokee, (object[])null);
                        try
                        {
                            changeService.OnComponentChanging(component, this);
                        }
                        catch (CheckoutException coEx)
                        {
                            if (coEx == CheckoutException.Canceled)
                            {
                                return;
                            }
                            throw coEx;
                        }
                    }

                    try
                    {
                        try
                        {
                            SetMethodValue.Invoke(invokee, new object[] { value });
                            OnValueChanged(invokee, EventArgs.Empty);
                        }
                        catch (Exception t)
                        {
                            // Give ourselves a chance to unwind properly before rethrowing the exception.
                            //
                            value = oldValue;

                            // If there was a problem setting the controls property then we get:
                            // ArgumentException (from properties set method)
                            // ==> Becomes inner exception of TargetInvocationException
                            // ==> caught here

                            if (t is TargetInvocationException && t.InnerException != null)
                            {
                                // Propagate the original exception up
                                throw t.InnerException;
                            }
                            else
                            {
                                throw t;
                            }
                        }
                    }
                    finally
                    {
                        // Now notify the change service that the change was successful.
                        //
                        changeService?.OnComponentChanged(component, this, oldValue, value);
                    }
                }
            }
        }

        /// <summary>
        ///     Indicates whether the value of this property needs to be persisted. In
        ///     other words, it indicates whether the state of the property is distinct
        ///     from when the component is first instantiated. If there is a default
        ///     value specified in this ReflectPropertyDescriptor, it will be compared against the
        ///     property's current value to determine this. If there isn't, the
        ///     ShouldSerializeXXX method is looked for and invoked if found.  If both
        ///     these routes fail, true will be returned.
        ///
        ///     If this returns false, a tool should not persist this property's value.
        /// </summary>
        public override bool ShouldSerializeValue(object component)
        {
            component = GetInvocationTarget(_componentClass, component);

            if (IsReadOnly)
            {
                if (ShouldSerializeMethodValue != null)
                {
                    try
                    {
                        return (bool)ShouldSerializeMethodValue.Invoke(component, null);
                    }
                    catch { }
                }
                return Attributes.Contains(DesignerSerializationVisibilityAttribute.Content);
            }
            else if (DefaultValue == s_noValue)
            {
                if (ShouldSerializeMethodValue != null)
                {
                    try
                    {
                        return (bool)ShouldSerializeMethodValue.Invoke(component, null);
                    }
                    catch { }
                }
                return true;
            }
            return !object.Equals(DefaultValue, GetValue(component));
        }

        /// <summary>
        ///     Indicates whether value change notifications for this property may originate from outside the property
        ///     descriptor, such as from the component itself (value=true), or whether notifications will only originate
        ///     from direct calls made to PropertyDescriptor.SetValue (value=false). For example, the component may
        ///     implement the INotifyPropertyChanged interface, or may have an explicit '{name}Changed' event for this property.
        /// </summary>
        public override bool SupportsChangeEvents => IPropChangedEventValue != null || ChangedEventValue != null;
    }
}
