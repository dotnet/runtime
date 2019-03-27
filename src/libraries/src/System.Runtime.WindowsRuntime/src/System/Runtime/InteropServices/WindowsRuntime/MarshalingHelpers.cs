// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Windows.Input;

using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // Local definition of Windows.UI.Xaml.Interop.INotifyCollectionChangedEventArgs
    [ComImport]
    [Guid("4cf68d33-e3f2-4964-b85e-945b4f7e2f21")]
    [WindowsRuntimeImport]
    internal interface INotifyCollectionChangedEventArgs
    {
        NotifyCollectionChangedAction Action { get; }
        IList NewItems { get; }
        IList OldItems { get; }
        int NewStartingIndex { get; }
        int OldStartingIndex { get; }
    }

    // Local definition of Windows.UI.Xaml.Data.IPropertyChangedEventArgs
    [ComImport]
    [Guid("4f33a9a0-5cf4-47a4-b16f-d7faaf17457e")]
    [WindowsRuntimeImport]
    internal interface IPropertyChangedEventArgs
    {
        string PropertyName { get; }
    }

    // Local definition of Windows.UI.Xaml.Interop.INotifyCollectionChanged
    [ComImport]
    [Guid("28b167d5-1a31-465b-9b25-d5c3ae686c40")]
    [WindowsRuntimeImport]
    internal interface INotifyCollectionChanged_WinRT
    {
        EventRegistrationToken add_CollectionChanged(NotifyCollectionChangedEventHandler value);
        void remove_CollectionChanged(EventRegistrationToken token);
    }

    // Local definition of Windows.UI.Xaml.Data.INotifyPropertyChanged
    [ComImport]
    [Guid("cf75d69c-f2f4-486b-b302-bb4c09baebfa")]
    [WindowsRuntimeImport]
    internal interface INotifyPropertyChanged_WinRT
    {
        EventRegistrationToken add_PropertyChanged(PropertyChangedEventHandler value);
        void remove_PropertyChanged(EventRegistrationToken token);
    }

    // Local definition of Windows.UI.Xaml.Input.ICommand
    [ComImport]
    [Guid("e5af3542-ca67-4081-995b-709dd13792df")]
    [WindowsRuntimeImport]
    internal interface ICommand_WinRT
    {
        EventRegistrationToken add_CanExecuteChanged(EventHandler<object> value);
        void remove_CanExecuteChanged(EventRegistrationToken token);
        bool CanExecute(object parameter);
        void Execute(object parameter);
    }

    // Local definition of Windows.UI.Xaml.Interop.NotifyCollectionChangedEventHandler
    [Guid("ca10b37c-f382-4591-8557-5e24965279b0")]
    [WindowsRuntimeImport]
    internal delegate void NotifyCollectionChangedEventHandler_WinRT(object sender, NotifyCollectionChangedEventArgs e);

    // Local definition of Windows.UI.Xaml.Data.PropertyChangedEventHandler
    [Guid("50f19c16-0a22-4d8e-a089-1ea9951657d2")]
    [WindowsRuntimeImport]
    internal delegate void PropertyChangedEventHandler_WinRT(object sender, PropertyChangedEventArgs e);

    // Local definition of Windows.UI.Xaml.Interop.INotifyCollectionChangedEventArgsFactory
    [ComImport]
    [Guid("b30c3e3a-df8d-44a5-9a38-7ac0d08ce63d")]
    [WindowsRuntimeImport]
    internal interface INotifyCollectionChangedEventArgsFactory
    {
        // The return type for this function is actually an INotifyCollectionChangedEventArgs*
        // but we need to make sure we don't accidentally project our native object back to managed
        // when marshalling it to native (which happens when correctly typing the return type as an INotifyCollectionChangedEventArgs).
        IntPtr CreateInstanceWithAllParameters(int action, IList newItems, IList oldItems, int newIndex, int oldIndex, object outer, ref object inner);
    }

    // Local definition of Windows.UI.Xaml.Data.INotifyCollectionChangedEventArgsFactory
    [ComImport]
    [Guid("6dcc9c03-e0c7-4eee-8ea9-37e3406eeb1c")]
    [WindowsRuntimeImport]
    internal interface IPropertyChangedEventArgsFactory
    {
        // The return type for this function is actually an IPropertyChangedEventArgs*
        // but we need to make sure we don't accidentally project our native object back to managed
        // when marshalling it to native (which happens when correctly typing the return type as an IPropertyChangedEventArgs).
        IntPtr CreateInstance(string name, object outer, ref object inner);
    }

    internal static class NotifyCollectionChangedEventArgsMarshaler
    {
        const string WinRTNotifyCollectionChangedEventArgsName = "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs";

        // IBindableVector Guid
        static Guid IID_IBindableVector = new Guid("393de7de-6fd0-4c0d-bb71-47244a113e93");

        static INotifyCollectionChangedEventArgsFactory s_EventArgsFactory;

        // Extracts properties from a managed NotifyCollectionChangedEventArgs and passes them to
        // a VM-implemented helper that creates a WinRT NotifyCollectionChangedEventArgs instance.
        // This method is called from IL stubs and needs to have its token stabilized.
        internal static IntPtr ConvertToNative(NotifyCollectionChangedEventArgs managedArgs)
        {
            if (managedArgs == null)
                return IntPtr.Zero;

            if (s_EventArgsFactory == null)
            {
                object factory = null;
                Guid guid = typeof(INotifyCollectionChangedEventArgsFactory).GUID;
                int hr = Interop.mincore.RoGetActivationFactory(WinRTNotifyCollectionChangedEventArgsName, ref guid, out factory);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                s_EventArgsFactory = (INotifyCollectionChangedEventArgsFactory)factory;
            }

            object inner = null;
            return s_EventArgsFactory.CreateInstanceWithAllParameters((int)managedArgs.Action, managedArgs.NewItems, managedArgs.OldItems, managedArgs.NewStartingIndex, managedArgs.OldStartingIndex, null, ref inner);
        }

        // Extracts properties from a WinRT NotifyCollectionChangedEventArgs and creates a new
        // managed NotifyCollectionChangedEventArgs instance.
        // This method is called from IL stubs and needs to have its token stabilized.
        internal static NotifyCollectionChangedEventArgs ConvertToManaged(IntPtr nativeArgsIP)
        {
            if (nativeArgsIP == IntPtr.Zero)
                return null;

            object obj = WindowsRuntimeMarshal.GetUniqueObjectForIUnknownWithoutUnboxing(nativeArgsIP);
            INotifyCollectionChangedEventArgs nativeArgs = (INotifyCollectionChangedEventArgs)obj;

            return CreateNotifyCollectionChangedEventArgs(
                        nativeArgs.Action,
                        nativeArgs.NewItems,
                        nativeArgs.OldItems,
                        nativeArgs.NewStartingIndex,
                        nativeArgs.OldStartingIndex);
        }

        internal static NotifyCollectionChangedEventArgs CreateNotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction action, IList newItems, IList oldItems, int newStartingIndex, int oldStartingIndex)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    return new NotifyCollectionChangedEventArgs(action, newItems, newStartingIndex);

                case NotifyCollectionChangedAction.Remove:
                    return new NotifyCollectionChangedEventArgs(action, oldItems, oldStartingIndex);

                case NotifyCollectionChangedAction.Replace:
                    return new NotifyCollectionChangedEventArgs(action, newItems, oldItems, newStartingIndex);

                case NotifyCollectionChangedAction.Move:
                    return new NotifyCollectionChangedEventArgs(action, newItems, newStartingIndex, oldStartingIndex);

                case NotifyCollectionChangedAction.Reset:
                    return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

                default: throw new ArgumentException("Invalid action value: " + action);
            }
        }
    }

    internal static class PropertyChangedEventArgsMarshaler
    {
        const string WinRTPropertyChangedEventArgsName = "Windows.UI.Xaml.Data.PropertyChangedEventArgs";

        static IPropertyChangedEventArgsFactory s_pPCEventArgsFactory;

        // Extracts PropertyName from a managed PropertyChangedEventArgs and passes them to
        // a VM-implemented helper that creates a WinRT PropertyChangedEventArgs instance.
        // This method is called from IL stubs and needs to have its token stabilized.
        internal static IntPtr ConvertToNative(PropertyChangedEventArgs managedArgs)
        {
            if (managedArgs == null)
                return IntPtr.Zero;

            if(s_pPCEventArgsFactory == null)
            {
                object factory = null;
                Guid guid = typeof(IPropertyChangedEventArgsFactory).GUID;
                int hr = Interop.mincore.RoGetActivationFactory(WinRTPropertyChangedEventArgsName, ref guid, out factory);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                s_pPCEventArgsFactory = (IPropertyChangedEventArgsFactory)factory;
            }

            object inner = null;
            return s_pPCEventArgsFactory.CreateInstance(managedArgs.PropertyName, null, ref inner);
        }

        // Extracts properties from a WinRT PropertyChangedEventArgs and creates a new
        // managed PropertyChangedEventArgs instance.
        // This method is called from IL stubs and needs to have its token stabilized.
        internal static PropertyChangedEventArgs ConvertToManaged(IntPtr nativeArgsIP)
        {
            if (nativeArgsIP == IntPtr.Zero)
                return null;

            object obj = WindowsRuntimeMarshal.GetUniqueObjectForIUnknownWithoutUnboxing(nativeArgsIP);
            IPropertyChangedEventArgs nativeArgs = (IPropertyChangedEventArgs)obj;

            return new PropertyChangedEventArgs(nativeArgs.PropertyName);
        }
    }

    // This is a set of stub methods implementing the support for the managed INotifyCollectionChanged
    // interface on WinRT objects that support the WinRT INotifyCollectionChanged. Used by the interop
    // mashaling infrastructure.
    internal sealed class NotifyCollectionChangedToManagedAdapter
    {
        private NotifyCollectionChangedToManagedAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        internal event NotifyCollectionChangedEventHandler CollectionChanged
        {
            // void CollectionChanged.add(NotifyCollectionChangedEventHandler)
            add
            {
                INotifyCollectionChanged_WinRT _this = Unsafe.As<INotifyCollectionChanged_WinRT>(this);

                // call the WinRT eventing support in mscorlib to subscribe the event
                Func<NotifyCollectionChangedEventHandler, EventRegistrationToken> addMethod =
                    new Func<NotifyCollectionChangedEventHandler, EventRegistrationToken>(_this.add_CollectionChanged);
                Action<EventRegistrationToken> removeMethod =
                    new Action<EventRegistrationToken>(_this.remove_CollectionChanged);

                WindowsRuntimeMarshal.AddEventHandler<NotifyCollectionChangedEventHandler>(addMethod, removeMethod, value);
            }

            // void CollectionChanged.remove(NotifyCollectionChangedEventHandler)
            remove
            {
                INotifyCollectionChanged_WinRT _this = Unsafe.As<INotifyCollectionChanged_WinRT>(this);

                // call the WinRT eventing support in mscorlib to unsubscribe the event
                Action<EventRegistrationToken> removeMethod =
                    new Action<EventRegistrationToken>(_this.remove_CollectionChanged);

                WindowsRuntimeMarshal.RemoveEventHandler<NotifyCollectionChangedEventHandler>(removeMethod, value);
            }
        }
    }

    // This is a set of stub methods implementing the support for the WinRT INotifyCollectionChanged
    // interface on managed objects that support the managed INotifyCollectionChanged. Used by the interop
    // mashaling infrastructure.
    internal sealed class NotifyCollectionChangedToWinRTAdapter
    {
        private NotifyCollectionChangedToWinRTAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // An instance field typed as EventRegistrationTokenTable is injected into managed classed by the compiler when compiling for /t:winmdobj.
        // Since here the class can be an arbitrary implementation of INotifyCollectionChanged, we have to keep the EventRegistrationTokenTable's
        // separately, associated with the implementations using ConditionalWeakTable.
        private static ConditionalWeakTable<INotifyCollectionChanged, EventRegistrationTokenTable<NotifyCollectionChangedEventHandler>> s_weakTable =
            new ConditionalWeakTable<INotifyCollectionChanged, EventRegistrationTokenTable<NotifyCollectionChangedEventHandler>>();

        // EventRegistrationToken CollectionChanged.add(NotifyCollectionChangedEventHandler value)
        internal EventRegistrationToken add_CollectionChanged(NotifyCollectionChangedEventHandler value)
        {
            INotifyCollectionChanged _this = Unsafe.As<INotifyCollectionChanged>(this);
            EventRegistrationTokenTable<NotifyCollectionChangedEventHandler> table = s_weakTable.GetOrCreateValue(_this);

            EventRegistrationToken token = table.AddEventHandler(value);
            _this.CollectionChanged += value;

            return token;
        }

        // void CollectionChanged.remove(EventRegistrationToken token)
        internal void remove_CollectionChanged(EventRegistrationToken token)
        {
            INotifyCollectionChanged _this = Unsafe.As<INotifyCollectionChanged>(this);
            EventRegistrationTokenTable<NotifyCollectionChangedEventHandler> table = s_weakTable.GetOrCreateValue(_this);

            if (table.RemoveEventHandler(token, out NotifyCollectionChangedEventHandler handler))
            {
                _this.CollectionChanged -= handler;
            }
        }
    }

    // This is a set of stub methods implementing the support for the managed INotifyPropertyChanged
    // interface on WinRT objects that support the WinRT INotifyPropertyChanged. Used by the interop
    // mashaling infrastructure.
    internal sealed class NotifyPropertyChangedToManagedAdapter
    {
        private NotifyPropertyChangedToManagedAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        internal event PropertyChangedEventHandler PropertyChanged
        {
            // void PropertyChanged.add(PropertyChangedEventHandler)
            add
            {
                INotifyPropertyChanged_WinRT _this = Unsafe.As<INotifyPropertyChanged_WinRT>(this);

                // call the WinRT eventing support in mscorlib to subscribe the event
                Func<PropertyChangedEventHandler, EventRegistrationToken> addMethod =
                    new Func<PropertyChangedEventHandler, EventRegistrationToken>(_this.add_PropertyChanged);
                Action<EventRegistrationToken> removeMethod =
                    new Action<EventRegistrationToken>(_this.remove_PropertyChanged);

                WindowsRuntimeMarshal.AddEventHandler<PropertyChangedEventHandler>(addMethod, removeMethod, value);
            }

            // void PropertyChanged.remove(PropertyChangedEventHandler)
            remove
            {
                INotifyPropertyChanged_WinRT _this = Unsafe.As<INotifyPropertyChanged_WinRT>(this);

                // call the WinRT eventing support in mscorlib to unsubscribe the event
                Action<EventRegistrationToken> removeMethod =
                    new Action<EventRegistrationToken>(_this.remove_PropertyChanged);

                WindowsRuntimeMarshal.RemoveEventHandler<PropertyChangedEventHandler>(removeMethod, value);
            }
        }
    }

    // This is a set of stub methods implementing the support for the WinRT INotifyPropertyChanged
    // interface on managed objects that support the managed INotifyPropertyChanged. Used by the interop
    // mashaling infrastructure.
    internal sealed class NotifyPropertyChangedToWinRTAdapter
    {
        private NotifyPropertyChangedToWinRTAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // An instance field typed as EventRegistrationTokenTable is injected into managed classed by the compiler when compiling for /t:winmdobj.
        // Since here the class can be an arbitrary implementation of INotifyCollectionChanged, we have to keep the EventRegistrationTokenTable's
        // separately, associated with the implementations using ConditionalWeakTable.
        private static ConditionalWeakTable<INotifyPropertyChanged, EventRegistrationTokenTable<PropertyChangedEventHandler>> s_weakTable =
            new ConditionalWeakTable<INotifyPropertyChanged, EventRegistrationTokenTable<PropertyChangedEventHandler>>();

        // EventRegistrationToken PropertyChanged.add(PropertyChangedEventHandler value)
        internal EventRegistrationToken add_PropertyChanged(PropertyChangedEventHandler value)
        {
            INotifyPropertyChanged _this = Unsafe.As<INotifyPropertyChanged>(this);
            EventRegistrationTokenTable<PropertyChangedEventHandler> table = s_weakTable.GetOrCreateValue(_this);

            EventRegistrationToken token = table.AddEventHandler(value);
            _this.PropertyChanged += value;

            return token;
        }

        // void PropertyChanged.remove(EventRegistrationToken token)
        internal void remove_PropertyChanged(EventRegistrationToken token)
        {
            INotifyPropertyChanged _this = Unsafe.As<INotifyPropertyChanged>(this);
            EventRegistrationTokenTable<PropertyChangedEventHandler> table = s_weakTable.GetOrCreateValue(_this);

            if (table.RemoveEventHandler(token, out PropertyChangedEventHandler handler))
            {
                _this.PropertyChanged -= handler;
            }
        }
    }

    // This is a set of stub methods implementing the support for the managed ICommand
    // interface on WinRT objects that support the WinRT ICommand_WinRT.
    // Used by the interop mashaling infrastructure.
    // Instances of this are really RCWs of ICommand_WinRT (not ICommandToManagedAdapter or any ICommand).
    internal sealed class ICommandToManagedAdapter /*: System.Windows.Input.ICommand*/
    {
        private static ConditionalWeakTable<EventHandler, EventHandler<object>> s_weakTable =
            new ConditionalWeakTable<EventHandler, EventHandler<object>>();

        private ICommandToManagedAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        private event EventHandler CanExecuteChanged
        {
            // void CanExecuteChanged.add(EventHandler)
            add
            {
                ICommand_WinRT _this = Unsafe.As<ICommand_WinRT>(this);

                // call the WinRT eventing support in mscorlib to subscribe the event
                Func<EventHandler<object>, EventRegistrationToken> addMethod =
                    new Func<EventHandler<object>, EventRegistrationToken>(_this.add_CanExecuteChanged);
                Action<EventRegistrationToken> removeMethod =
                    new Action<EventRegistrationToken>(_this.remove_CanExecuteChanged);

                // value is of type System.EventHandler, but ICommand_WinRT (and thus WindowsRuntimeMarshal.AddEventHandler)
                // expects an instance of EventHandler<object>. So we get/create a wrapper of value here.
                EventHandler<object> handler_WinRT = s_weakTable.GetValue(value, ICommandAdapterHelpers.CreateWrapperHandler);
                WindowsRuntimeMarshal.AddEventHandler<EventHandler<object>>(addMethod, removeMethod, handler_WinRT);
            }

            // void CanExecuteChanged.remove(EventHandler)
            remove
            {
                ICommand_WinRT _this = Unsafe.As<ICommand_WinRT>(this);

                // call the WinRT eventing support in mscorlib to unsubscribe the event
                Action<EventRegistrationToken> removeMethod =
                    new Action<EventRegistrationToken>(_this.remove_CanExecuteChanged);

                // value is of type System.EventHandler, but ICommand_WinRT (and thus WindowsRuntimeMarshal.RemoveEventHandler)
                // expects an instance of EventHandler<object>. So we get/create a wrapper of value here.

                // Also we do a value check rather than an instance check to ensure that different instances of the same delegates are treated equal.
                EventHandler<object> handler_WinRT = ICommandAdapterHelpers.GetValueFromEquivalentKey(s_weakTable, value, ICommandAdapterHelpers.CreateWrapperHandler);
                WindowsRuntimeMarshal.RemoveEventHandler<EventHandler<object>>(removeMethod, handler_WinRT);
            }
        }

        private bool CanExecute(object parameter)
        {
            ICommand_WinRT _this = Unsafe.As<ICommand_WinRT>(this);
            return _this.CanExecute(parameter);
        }

        private void Execute(object parameter)
        {
            ICommand_WinRT _this = Unsafe.As<ICommand_WinRT>(this);
            _this.Execute(parameter);
        }
    }

    // This is a set of stub methods implementing the support for the WinRT ICommand_WinRT
    // interface on managed objects that support the managed ICommand interface.
    // Used by the interop mashaling infrastructure.
    // Instances of this are really CCWs of ICommand (not ICommandToWinRTAdapter or any ICommand_WinRT).
    internal sealed class ICommandToWinRTAdapter /*: ICommand_WinRT*/
    {
        private ICommandToWinRTAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // An instance field typed as EventRegistrationTokenTable is injected into managed classed by the compiler when compiling for /t:winmdobj.
        // Since here the class can be an arbitrary implementation of ICommand, we have to keep the EventRegistrationTokenTable's
        // separately, associated with the implementations using ConditionalWeakTable.
        private static ConditionalWeakTable<ICommand, EventRegistrationTokenTable<EventHandler>> s_weakTable =
            new ConditionalWeakTable<ICommand, EventRegistrationTokenTable<EventHandler>>();

        // EventRegistrationToken PropertyChanged.add(EventHandler<object> value)
        private EventRegistrationToken add_CanExecuteChanged(EventHandler<object> value)
        {
            ICommand _this = Unsafe.As<ICommand>(this);
            EventRegistrationTokenTable<EventHandler> table = s_weakTable.GetOrCreateValue(_this);

            EventHandler handler = ICommandAdapterHelpers.CreateWrapperHandler(value);
            EventRegistrationToken token = table.AddEventHandler(handler);
            _this.CanExecuteChanged += handler;

            return token;
        }

        // void PropertyChanged.remove(EventRegistrationToken token)
        private void remove_CanExecuteChanged(EventRegistrationToken token)
        {
            ICommand _this = Unsafe.As<ICommand>(this);
            EventRegistrationTokenTable<EventHandler> table = s_weakTable.GetOrCreateValue(_this);

            if (table.RemoveEventHandler(token, out EventHandler handler))
            {
                _this.CanExecuteChanged -= handler;
            }
        }

        private bool CanExecute(object parameter)
        {
            ICommand _this = Unsafe.As<ICommand>(this);
            return _this.CanExecute(parameter);
        }

        private void Execute(object parameter)
        {
            ICommand _this = Unsafe.As<ICommand>(this);
            _this.Execute(parameter);
        }
    }

    // A couple of ICommand adapter helpers need to be transparent, and so are in their own type
    internal static class ICommandAdapterHelpers
    {
        internal static EventHandler<object> CreateWrapperHandler(EventHandler handler)
        {
            // Check whether it is a round-tripping case i.e. the sender is of the type eventArgs,
            // If so we use it else we pass EventArgs.Empty
            return (object sender, object e) =>
                {
                    EventArgs eventArgs = e as EventArgs;
                    handler(sender, (eventArgs == null ? System.EventArgs.Empty : eventArgs));
                };
        }

        internal static EventHandler CreateWrapperHandler(EventHandler<object> handler)
        {
            return (object sender, EventArgs e) => handler(sender, e);
        }

        internal static EventHandler<object> GetValueFromEquivalentKey(
            ConditionalWeakTable<EventHandler, EventHandler<object>> table,
            EventHandler key,
            ConditionalWeakTable<EventHandler, EventHandler<object>>.CreateValueCallback callback)
        {
            foreach (KeyValuePair<EventHandler, EventHandler<object>> item in table)
            {
                if (object.Equals(item.Key, key))
                    return item.Value;
            }

            EventHandler<object> value = callback(key);
            table.Add(key, value);
            return value;
        }
    }
}
