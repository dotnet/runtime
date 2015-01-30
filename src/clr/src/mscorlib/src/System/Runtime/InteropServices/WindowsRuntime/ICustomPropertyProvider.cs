// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using System;
using System.StubHelpers;
using System.Reflection;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [Guid("7C925755-3E48-42B4-8677-76372267033F")]
    [WindowsRuntimeImport]
    internal interface ICustomPropertyProvider 
    {    
        [Pure]
        ICustomProperty GetCustomProperty(string name);

        [Pure]
        ICustomProperty GetIndexedProperty(string name, Type indexParameterType);

        [Pure]
        string GetStringRepresentation();

        Type Type 
        { 
            [Pure]        
            get; 
        }
    }

    //
    // Implementation helpers
    //
    internal static class ICustomPropertyProviderImpl
    {
        //
        // Creates a ICustomProperty implementation for Jupiter
        // Called from ICustomPropertyProvider_GetProperty from within runtime
        //
        static internal ICustomProperty CreateProperty(object target, string propertyName)
        {
            Contract.Requires(target != null);
            Contract.Requires(propertyName != null);

            // Only return public instance/static properties
            PropertyInfo propertyInfo = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);

            if (propertyInfo == null)
                return null;
            else
                return new CustomPropertyImpl(propertyInfo);
        }

        //
        // Creates a ICustomProperty implementation for Jupiter
        // Called from ICustomPropertyProvider_GetIndexedProperty from within runtime
        //               
        [System.Security.SecurityCritical]
        static internal unsafe ICustomProperty CreateIndexedProperty(object target, string propertyName, TypeNameNative *pIndexedParamType)
        {
            Contract.Requires(target != null);
            Contract.Requires(propertyName != null);

            Type indexedParamType = null;
            SystemTypeMarshaler.ConvertToManaged(pIndexedParamType, ref indexedParamType);

            return CreateIndexedProperty(target, propertyName, indexedParamType);        
        }

        static internal ICustomProperty CreateIndexedProperty(object target, string propertyName, Type indexedParamType)
        {
            Contract.Requires(target != null);
            Contract.Requires(propertyName != null);

            // Only return public instance/static properties
            PropertyInfo propertyInfo = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
                null,                                                                   // default binder
                null,                                                                   // ignore return type
                new Type[] { indexedParamType },                                        // indexed parameter type
                null                                                                    // ignore type modifier
                );

            if (propertyInfo == null)
                return null;
            else
                return new CustomPropertyImpl(propertyInfo);
        }

        [System.Security.SecurityCritical]
        static internal unsafe void GetType(object target, TypeNameNative *pIndexedParamType)
        {            
            SystemTypeMarshaler.ConvertToNative(target.GetType(), pIndexedParamType);
        }        
    }

    [Flags]
    enum InterfaceForwardingSupport
    {
        None                        = 0,
        IBindableVector             = 0x1,              // IBindableVector -> IBindableVector
        IVector                     = 0x2,              // IBindableVector -> IVector<T>
        IBindableVectorView         = 0x4,              // IBindableVectorView -> IBindableVectorView
        IVectorView                 = 0x8,              // IBindableVectorView -> IVectorView<T>
        IBindableIterableOrIIterable= 0x10              // IBindableIterable -> IBindableIterable/IIterable<T>
    }

    //
    // Interface for data binding code (CustomPropertyImpl) to retreive the target object
    // See CustomPropertyImpl.InvokeInternal for details
    //
    internal interface IGetProxyTarget
    {
        object GetTarget();            
    }
    
    //
    // Proxy that supports data binding on another object
    //
    // This serves two purposes:
    //
    // 1. Delegate data binding interfaces to another object
    // Note that this proxy implements the native interfaces directly to avoid unnecessary overhead 
    // (such as the adapter code that addresses behavior differences between IBindableVector & List
    // as well as simplify forwarding code (except for IEnumerable)
    //
    // 2. ICLRServices.CreateManagedReference will hand out ICCW* of a new instance of this object
    // and will hold the other object alive
    //
    //
    internal class ICustomPropertyProviderProxy<T1, T2> : IGetProxyTarget,
                                                          ICustomPropertyProvider, 
                                                          ICustomQueryInterface,
                                                          IEnumerable,          // IBindableIterable -> IBindableIterable/IIterable<T>
                                                          IBindableVector,      // IBindableVector -> IBindableVector/IVector<T>
                                                          IBindableVectorView   // IBindableVectorView -> IBindableVectorView/IVectorView<T>
    {
        private object _target;
        private InterfaceForwardingSupport _flags;

        internal ICustomPropertyProviderProxy(object target, InterfaceForwardingSupport flags)
        {
            _target = target;
            _flags = flags;
        }

        //
        // Creates a new instance of ICustomPropertyProviderProxy<T1, T2> and assign appropriate
        // flags
        //
        internal static object CreateInstance(object target)
        {
            InterfaceForwardingSupport supportFlags = InterfaceForwardingSupport.None;

            //
            // QI and figure out the right flags
            //
            if (target as IList != null)
                supportFlags |= InterfaceForwardingSupport.IBindableVector;

            // NOTE: We need to use the directed type here
            // If we use IVector_Raw<T1> here, it derives from a different IIterable<T> which the runtime
            // doesn't recognize, and therefore IEnumerable cast won't be able to take advantage of this QI
            if (target as IList<T1> != null)
                supportFlags |= InterfaceForwardingSupport.IVector;
            
            if (target as IBindableVectorView != null)
                supportFlags |= InterfaceForwardingSupport.IBindableVectorView;

            // NOTE: We need to use the redirected type here
            // If we use IVector_Raw<T1> here, it derives from a different IIterable<T> which the runtime
            // doesn't recognize, and therefore IEnumerable cast won't be able to take advantage of this QI
            if (target as IReadOnlyList<T2> != null)
                supportFlags |= InterfaceForwardingSupport.IVectorView;

            // Verify IEnumerable last because the first few QIs might succeed and we need
            // IEnumerable cast to use that cache (instead of having ICustomPropertyProvider to
            // forward it manually)
            // For example, if we try to shoot in the dark by trying IVector<IInspectable> and it 
            // succeeded, IEnumerable needs to know that
            if (target as IEnumerable != null)
                supportFlags |= InterfaceForwardingSupport.IBindableIterableOrIIterable;
            
            return new ICustomPropertyProviderProxy<T1, T2>(target, supportFlags);                
        }

        //
        // ICustomPropertyProvider implementation
        //
        ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name)
        {
            return ICustomPropertyProviderImpl.CreateProperty(_target, name);
        }

        ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type indexParameterType)
        {
            return ICustomPropertyProviderImpl.CreateIndexedProperty(_target, name, indexParameterType);
        }

        string ICustomPropertyProvider.GetStringRepresentation()
        {
            return WindowsRuntime.IStringableHelper.ToString(_target);
        }

        Type ICustomPropertyProvider.Type 
        { 
            get
            {
                return _target.GetType();
            }
        }

        //
        // override ToString() to make sure callers get correct IStringable.ToString() behavior in native code
        //
        public override string ToString()
        {
            return WindowsRuntime.IStringableHelper.ToString(_target);
        }

        //
        // IGetProxyTarget - unwraps the target object and use it for data binding
        // 
        object IGetProxyTarget.GetTarget()
        {
            return _target;
        }

        // 
        // ICustomQueryInterface methods
        //    
        [System.Security.SecurityCritical]
        public CustomQueryInterfaceResult GetInterface([In]ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;

            if (iid == typeof(IBindableIterable).GUID)
            {
                // Reject the QI if target doesn't implement IEnumerable
                if ((_flags & (InterfaceForwardingSupport.IBindableIterableOrIIterable)) == 0)
                    return CustomQueryInterfaceResult.Failed;                    
            }

            if (iid == typeof(IBindableVector).GUID)
            {
                // Reject the QI if target doesn't implement IBindableVector/IVector
                if ((_flags & (InterfaceForwardingSupport.IBindableVector | InterfaceForwardingSupport.IVector)) == 0)
                    return CustomQueryInterfaceResult.Failed;                    
            }

            if (iid == typeof(IBindableVectorView).GUID)
            {
                // Reject the QI if target doesn't implement IBindableVectorView/IVectorView
                if ((_flags & (InterfaceForwardingSupport.IBindableVectorView | InterfaceForwardingSupport.IVectorView)) == 0)
                    return CustomQueryInterfaceResult.Failed;                
            }
            
            return CustomQueryInterfaceResult.NotHandled;
        }
    
        //
        // IEnumerable methods
        //
        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_target).GetEnumerator();
        }

        //
        // IBindableVector implementation (forwards to IBindableVector / IVector<T>)
        //        
        [Pure]
        object IBindableVector.GetAt(uint index)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                // IBindableVector -> IBindableVector
                return bindableVector.GetAt(index);
            }
            else
            {
                // IBindableVector -> IVector<T>
                return GetVectorOfT().GetAt(index);
            }            
        }
        
        [Pure]
        uint IBindableVector.Size 
        { 
            get
            {            
                IBindableVector bindableVector = GetIBindableVectorNoThrow();
                if (bindableVector != null)
                {
                    // IBindableVector -> IBindableVector
                    return bindableVector.Size;
                }
                else
                {
                    // IBindableVector -> IVector<T>
                    return GetVectorOfT().Size;
                }            
            }
        }
        
        [Pure]
        IBindableVectorView IBindableVector.GetView()
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                // IBindableVector -> IBindableVector
                return bindableVector.GetView();
            }
            else
            {
                // IBindableVector -> IVector<T>
                return new IVectorViewToIBindableVectorViewAdapter<T1>(GetVectorOfT().GetView());
            }        
        }

        private sealed class IVectorViewToIBindableVectorViewAdapter<T> : IBindableVectorView
        {
            private IVectorView<T> _vectorView;

            public IVectorViewToIBindableVectorViewAdapter(IVectorView<T> vectorView)
            { 
                this._vectorView = vectorView; 
            }

            [Pure]
            object IBindableVectorView.GetAt(uint index)
            {
                return _vectorView.GetAt(index);
            }
            
            [Pure]
            uint IBindableVectorView.Size
            { 
                get
                {
                    return _vectorView.Size;
                }
            }
            
            [Pure]
            bool IBindableVectorView.IndexOf(object value, out uint index)
            {
                return _vectorView.IndexOf(ConvertTo<T>(value), out index);
            }

            IBindableIterator IBindableIterable.First()
            {
                return new IteratorOfTToIteratorAdapter<T>(_vectorView.First());
            }

        }         
        
        [Pure]
        bool IBindableVector.IndexOf(object value, out uint index)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                // IBindableVector -> IBindableVector
                return bindableVector.IndexOf(value, out index);
            }
            else
            {
                // IBindableVector -> IVector<T>
                return GetVectorOfT().IndexOf(ConvertTo<T1>(value), out index);
            }            
        }
        
        void IBindableVector.SetAt(uint index, object value)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                // IBindableVector -> IBindableVector
                bindableVector.SetAt(index, value);
            }
            else
            {
                // IBindableVector -> IVector<T>
                GetVectorOfT().SetAt(index, ConvertTo<T1>(value));
            }            
        }
        
        void IBindableVector.InsertAt(uint index, object value)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                // IBindableVector -> IBindableVector
                bindableVector.InsertAt(index, value);
            }
            else
            {
                // IBindableVector -> IVector<T>
                GetVectorOfT().InsertAt(index, ConvertTo<T1>(value));
            }            
        }
        
        void IBindableVector.RemoveAt(uint index)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                // IBindableVector -> IBindableVector
                bindableVector.RemoveAt(index);
            }
            else
            {
                // IBindableVector -> IVector<T>
                GetVectorOfT().RemoveAt(index);
            }            
        }
        
        void IBindableVector.Append(object value)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                // IBindableVector -> IBindableVector
                bindableVector.Append(value);
            }
            else
            {
                // IBindableVector -> IVector<T>
                GetVectorOfT().Append(ConvertTo<T1>(value));
            }            
        }        
        
        void IBindableVector.RemoveAtEnd()
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                // IBindableVector -> IBindableVector
                bindableVector.RemoveAtEnd();
            }
            else
            {
                // IBindableVector -> IVector<T>
                GetVectorOfT().RemoveAtEnd();
            }            
        }
        
        void IBindableVector.Clear()
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                // IBindableVector -> IBindableVector
                bindableVector.Clear();
            }
            else
            {
                // IBindableVector -> IVector<T>
                GetVectorOfT().Clear();
            }            
        }

        [SecuritySafeCritical]
        private IBindableVector GetIBindableVectorNoThrow()
        {
            if ((_flags & InterfaceForwardingSupport.IBindableVector) != 0)
                return JitHelpers.UnsafeCast<IBindableVector>(_target);
            else
                return null;
        }

        [SecuritySafeCritical]
        private IVector_Raw<T1> GetVectorOfT()
        {
            if ((_flags & InterfaceForwardingSupport.IVector) != 0)
                return JitHelpers.UnsafeCast<IVector_Raw<T1>>(_target);
            else
                throw new InvalidOperationException();  // We should not go down this path, unless Jupiter pass this out to managed code
                                                        // and managed code use reflection to do the cast
        }
        
        //
        // IBindableVectorView implementation (forwarding to IBindableVectorView or IVectorView<T>)
        //
        [Pure]
        object IBindableVectorView.GetAt(uint index)
        {
            IBindableVectorView bindableVectorView = GetIBindableVectorViewNoThrow();
            if (bindableVectorView != null)
                return bindableVectorView.GetAt(index);
            else
                return GetVectorViewOfT().GetAt(index);
        }
        
        [Pure]
        uint IBindableVectorView.Size
        { 
            get
            {
                IBindableVectorView bindableVectorView = GetIBindableVectorViewNoThrow();
                if (bindableVectorView != null)
                    return bindableVectorView.Size;
                else
                    return GetVectorViewOfT().Size;
            }
        }
        
        [Pure]
        bool IBindableVectorView.IndexOf(object value, out uint index)
        {
            IBindableVectorView bindableVectorView = GetIBindableVectorViewNoThrow();
            if (bindableVectorView != null)
                return bindableVectorView.IndexOf(value, out index);
            else
                return GetVectorViewOfT().IndexOf(ConvertTo<T2>(value), out index);
        }

        IBindableIterator IBindableIterable.First()
        {
            IBindableVectorView bindableVectorView = GetIBindableVectorViewNoThrow();
            if (bindableVectorView != null)
                return bindableVectorView.First();
            else
                return new IteratorOfTToIteratorAdapter<T2>(GetVectorViewOfT().First());
        }

        private sealed class IteratorOfTToIteratorAdapter<T> : IBindableIterator
        {
            private IIterator<T> _iterator;

            public IteratorOfTToIteratorAdapter(IIterator<T> iterator)
            { this._iterator = iterator; }

            public bool HasCurrent { get { return _iterator.HasCurrent; } }
            public object Current  { get { return (object)_iterator.Current; } }
            public bool MoveNext() { return _iterator.MoveNext(); }
        }         

        [SecuritySafeCritical]
        private IBindableVectorView GetIBindableVectorViewNoThrow()
        {
            if ((_flags & InterfaceForwardingSupport.IBindableVectorView) != 0)
                return JitHelpers.UnsafeCast<IBindableVectorView>(_target);
            else
                return null;
        }

        [SecuritySafeCritical]
        private IVectorView<T2> GetVectorViewOfT()
        {
            if ((_flags & InterfaceForwardingSupport.IVectorView) != 0)
                return JitHelpers.UnsafeCast<IVectorView<T2>>(_target);
            else
                throw new InvalidOperationException();  // We should not go down this path, unless Jupiter pass this out to managed code
                                                        // and managed code use reflection to do the cast
        }

        //
        // Convert to type T
        //
        private static T ConvertTo<T>(object value)
        {
            // Throw ArgumentNullException if value is null (otherwise we'll throw NullReferenceException
            // when casting value to T)
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);
        
            // No coersion support needed. If we need coersion later, this is the place
            return (T) value;
        }
    }
}

