// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// ReflectEventDescriptor defines an event. Events are the main way that a user can get
    /// run-time notifications from a component.
    /// The ReflectEventDescriptor class takes a component class that the event lives on,
    /// the event name, the type of the event handling delegate, and various
    /// attributes for the event.
    /// Every event has a structure through which it passes it's information. The base
    /// structure, Event, is empty, and there is a default instance, Event.EMPTY, which
    /// is usually passed. When addOnXXX is invoked, it needs a pointer to a method
    /// that takes a source object (the object that fired the event) and a structure
    /// particular to that event. It also needs a pointer to the instance of the
    /// object the method should be invoked on. These two things are what composes a
    /// delegate. An event handler is
    /// a delegate, and the compiler recognizes a special delegate syntax that makes
    /// using delegates easy.
    /// For example, to listen to the click event on a button in class Foo, the
    /// following code will suffice:
    /// 
    /// <code>
    /// class Foo {
    /// Button button1 = new Button();
    /// void button1_click(Object sender, Event e) {
    /// // do something on button1 click.
    /// }
    /// public Foo() {
    /// button1.addOnClick(button1_click);
    /// }
    /// }
    /// </code>
    /// For an event named XXX, a YYYEvent structure, and a YYYEventHandler delegate,
    /// a component writer is required to implement two methods of the following
    /// form:
    /// <code>
    /// public void addOnXXX(YYYEventHandler handler);
    /// public void removeOnXXX(YYYEventHandler handler);
    /// </code>
    /// YYYEventHandler should be an event handler declared as
    /// <code>
    /// public multicast delegate void YYYEventHandler(Object sender, YYYEvent e);
    /// </code>
    /// Note that this event was declared as a multicast delegate. This allows multiple
    /// listeners on an event. This is not a requirement.
    /// Various attributes can be passed to the ReflectEventDescriptor, as are described in
    /// Attribute.
    /// ReflectEventDescriptors can be obtained by a user programmatically through the
    /// ComponentManager.
    /// </summary>
    internal sealed class ReflectEventDescriptor : EventDescriptor
    {
        private Type _type;           // the delegate type for the event
        private readonly Type _componentClass; // the class of the component this info is for

        private MethodInfo _addMethod;     // the method to use when adding an event
        private MethodInfo _removeMethod;  // the method to use when removing an event
        private EventInfo _realEvent;      // actual event info... may be null
        private bool _filledMethods;   // did we already call FillMethods() once?

        /// <summary>
        /// This is the main constructor for an ReflectEventDescriptor.
        /// </summary>
        public ReflectEventDescriptor(Type componentClass, string name, Type type, Attribute[] attributes)
            : base(name, attributes)
        {
            if (componentClass == null)
            {
                throw new ArgumentException(SR.Format(SR.InvalidNullArgument, nameof(componentClass)));
            }
            if (type == null || !(typeof(Delegate)).IsAssignableFrom(type))
            {
                throw new ArgumentException(SR.Format(SR.ErrorInvalidEventType, name));
            }
            Debug.Assert(type.IsSubclassOf(typeof(Delegate)), $"Not a valid ReflectEvent: {componentClass.FullName}. {name} {type.FullName}");
            _componentClass = componentClass;
            _type = type;
        }

        public ReflectEventDescriptor(Type componentClass, EventInfo eventInfo)
            : base(eventInfo.Name, Array.Empty<Attribute>())
        {
            _componentClass = componentClass ?? throw new ArgumentException(SR.Format(SR.InvalidNullArgument, nameof(componentClass)));
            _realEvent = eventInfo;
        }

        /// <summary>
        /// This constructor takes an existing ReflectEventDescriptor and modifies it by merging in the
        /// passed-in attributes.
        /// </summary>
        public ReflectEventDescriptor(Type componentType, EventDescriptor oldReflectEventDescriptor, Attribute[] attributes)
            : base(oldReflectEventDescriptor, attributes)
        {
            _componentClass = componentType;
            _type = oldReflectEventDescriptor.EventType;

            if (oldReflectEventDescriptor is ReflectEventDescriptor desc)
            {
                _addMethod = desc._addMethod;
                _removeMethod = desc._removeMethod;
                _filledMethods = true;
            }
        }

        /// <summary>
        /// Retrieves the type of the component this EventDescriptor is bound to.
        /// </summary>
        public override Type ComponentType => _componentClass;

        /// <summary>
        /// Retrieves the type of the delegate for this event.
        /// </summary>
        public override Type EventType
        {
            get
            {
                FillMethods();
                return _type;
            }
        }

        /// <summary>
        /// Indicates whether the delegate type for this event is a multicast delegate.
        /// </summary>
        public override bool IsMulticast => (typeof(MulticastDelegate)).IsAssignableFrom(EventType);

        /// <summary>
        /// This adds the delegate value as a listener to when this event is fired
        /// by the component, invoking the addOnXXX method.
        /// </summary>
        public override void AddEventHandler(object component, Delegate value)
        {
            FillMethods();

            if (component != null)
            {
                ISite site = GetSite(component);
                IComponentChangeService changeService = null;

                // Announce that we are about to change this component
                if (site != null)
                {
                    changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                }

                if (changeService != null)
                {
                    try {
                        changeService.OnComponentChanging(component, this);
                    }
                    catch (CheckoutException coEx) {
                        if (coEx == CheckoutException.Canceled) {
                            return;
                        }
                        throw coEx;
                    }
                    changeService.OnComponentChanging(component, this);
                }

                bool shadowed = false;

                if (site != null && site.DesignMode)
                {
                    // Events are final, so just check the class
                    if (EventType != value.GetType())
                    {
                        throw new ArgumentException(SR.Format(SR.ErrorInvalidEventHandler, Name));
                    }
                    IDictionaryService dict = (IDictionaryService)site.GetService(typeof(IDictionaryService));
                    if (dict != null)
                    {
                        Delegate eventdesc = (Delegate)dict.GetValue(this);
                        eventdesc = Delegate.Combine(eventdesc, value);
                        dict.SetValue(this, eventdesc);
                        shadowed = true;
                    }
                }

                if (!shadowed)
                {
                    _addMethod.Invoke(component, new[] { value });
                }

                // Now notify the change service that the change was successful.
                changeService?.OnComponentChanged(component, this, null, value);
            }
        }

        /// <summary>
        /// Adds in custom attributes found on either the AddOn or RemoveOn method...
        /// </summary>
        protected override void FillAttributes(IList attributes)
        {
            // The order that we fill in attributes is critical. The list of attributes will be
            // filtered so that matching attributes at the end of the list replace earlier matches
            // (last one in wins). Therefore, the two categories of attributes we add must be
            // added as follows:
            //
            // 1. Attributes of the event, from base class to most derived. This way
            //     derived class attributes replace base class attributes.
            //
            // 2. Attributes from our base MemberDescriptor. While this seems opposite of what
            //     we want, MemberDescriptor only has attributes if someone passed in a new
            //     set in the constructor. Therefore, these attributes always
            //     supercede existing values.

            FillMethods();
            Debug.Assert(_componentClass != null, "Must have a component class for FilterAttributes");
            if (_realEvent != null)
            {
                FillEventInfoAttribute(_realEvent, attributes);
            }
            else
            {
                Debug.Assert(_removeMethod != null, $"Null remove method for {Name}");
                FillSingleMethodAttribute(_removeMethod, attributes);

                Debug.Assert(_addMethod != null, $"Null remove method for {Name}");
                FillSingleMethodAttribute(_addMethod, attributes);
            }

            // Include the base attributes. These override all attributes on the actual
            // property, so we want to add them last.
            base.FillAttributes(attributes);
        }

        private void FillEventInfoAttribute(EventInfo realEventInfo, IList attributes)
        {
            string eventName = realEventInfo.Name;
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            Type currentReflectType = realEventInfo.ReflectedType;
            Debug.Assert(currentReflectType != null, "currentReflectType cannot be null");
            int depth = 0;

            // First, calculate the depth of the object hierarchy. We do this so we can do a single
            // object create for an array of attributes.
            while (currentReflectType != typeof(object))
            {
                depth++;
                currentReflectType = currentReflectType.BaseType;
            }

            if (depth > 0)
            {
                // Now build up an array in reverse order
                currentReflectType = realEventInfo.ReflectedType;
                Attribute[][] attributeStack = new Attribute[depth][];

                while (currentReflectType != typeof(object))
                {
                    // Fill in our member info so we can get at the custom attributes.
                    MemberInfo memberInfo = currentReflectType.GetEvent(eventName, bindingFlags);

                    // Get custom attributes for the member info.
                    if (memberInfo != null)
                    {
                        attributeStack[--depth] = ReflectTypeDescriptionProvider.ReflectGetAttributes(memberInfo);
                    }

                    // Ready for the next loop iteration.
                    currentReflectType = currentReflectType.BaseType;
                }

                // Now trawl the attribute stack so that we add attributes
                // from base class to most derived.
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
        }

        /// <summary>
        /// This fills the get and set method fields of the event info. It is shared
        /// by the various constructors.
        /// </summary>
        private void FillMethods()
        {
            if (_filledMethods) return;

            if (_realEvent != null)
            {
                _addMethod = _realEvent.GetAddMethod();
                _removeMethod = _realEvent.GetRemoveMethod();

                EventInfo defined = null;

                if (_addMethod == null || _removeMethod == null)
                {
                    Type start = _componentClass.BaseType;
                    while (start != null && start != typeof(object))
                    {
                        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                        EventInfo test = start.GetEvent(_realEvent.Name, bindingFlags);
                        if (test.GetAddMethod() != null)
                        {
                            defined = test;
                            break;
                        }
                    }
                }

                if (defined != null)
                {
                    _addMethod = defined.GetAddMethod();
                    _removeMethod = defined.GetRemoveMethod();
                    _type = defined.EventHandlerType;
                }
                else
                {
                    _type = _realEvent.EventHandlerType;
                }
            }
            else
            {
                // first, try to get the eventInfo...
                _realEvent = _componentClass.GetEvent(Name);
                if (_realEvent != null)
                {
                    // if we got one, just recurse and return.
                    FillMethods();
                    return;
                }

                Type[] argsType = new Type[] { _type };
                _addMethod = FindMethod(_componentClass, "AddOn" + Name, argsType, typeof(void));
                _removeMethod = FindMethod(_componentClass, "RemoveOn" + Name, argsType, typeof(void));
                if (_addMethod == null || _removeMethod == null)
                {
                    Debug.Fail($"Missing event accessors for {_componentClass.FullName}. {Name}");
                    throw new ArgumentException(SR.Format(SR.ErrorMissingEventAccessors, Name));
                }
            }

            _filledMethods = true;
        }

        private void FillSingleMethodAttribute(MethodInfo realMethodInfo, IList attributes)
        {
            string methodName = realMethodInfo.Name;
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            Type currentReflectType = realMethodInfo.ReflectedType;
            Debug.Assert(currentReflectType != null, "currentReflectType cannot be null");

            // First, calculate the depth of the object hierarchy. We do this so we can do a single
            // object create for an array of attributes.
            //
            int depth = 0;
            while (currentReflectType != null && currentReflectType != typeof(object))
            {
                depth++;
                currentReflectType = currentReflectType.BaseType;
            }

            if (depth > 0)
            {
                // Now build up an array in reverse order
                //
                currentReflectType = realMethodInfo.ReflectedType;
                Attribute[][] attributeStack = new Attribute[depth][];

                while (currentReflectType != null && currentReflectType != typeof(object))
                {
                    // Fill in our member info so we can get at the custom attributes.
                    MemberInfo memberInfo = currentReflectType.GetMethod(methodName, bindingFlags);

                    // Get custom attributes for the member info.
                    if (memberInfo != null)
                    {
                        attributeStack[--depth] = ReflectTypeDescriptionProvider.ReflectGetAttributes(memberInfo);
                    }

                    // Ready for the next loop iteration.
                    currentReflectType = currentReflectType.BaseType;
                }

                // Now trawl the attribute stack so that we add attributes
                // from base class to most derived.
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
        }

        /// <summary>
        /// This will remove the delegate value from the event chain so that 
        /// it no longer gets events from this component.
        /// </summary>
        public override void RemoveEventHandler(object component, Delegate value)
        {
            FillMethods();

            if (component != null)
            {
                ISite site = GetSite(component);
                IComponentChangeService changeService = null;

                // Announce that we are about to change this component
                if (site != null)
                {
                    changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                }

                if (changeService != null)
                {
                    try {
                        changeService.OnComponentChanging(component, this);
                    }
                    catch (CheckoutException coEx) {
                        if (coEx == CheckoutException.Canceled) {
                            return;
                        }
                        throw coEx;
                    }
                    changeService.OnComponentChanging(component, this);
                }

                bool shadowed = false;

                if (site != null && site.DesignMode)
                {
                    IDictionaryService dict = (IDictionaryService)site.GetService(typeof(IDictionaryService));
                    if (dict != null)
                    {
                        Delegate del = (Delegate)dict.GetValue(this);
                        del = Delegate.Remove(del, value);
                        dict.SetValue(this, del);
                        shadowed = true;
                    }
                }

                if (!shadowed)
                {
                    _removeMethod.Invoke(component, new[] { value });
                }

                // Now notify the change service that the change was successful.
                changeService?.OnComponentChanged(component, this, null, value);
            }
        }
    }
}
