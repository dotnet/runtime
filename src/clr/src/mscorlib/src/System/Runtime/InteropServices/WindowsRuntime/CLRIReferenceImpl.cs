// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class CLRIReferenceImpl<T> : CLRIPropertyValueImpl, IReference<T>, IGetProxyTarget 
    {
        private T _value;

        public CLRIReferenceImpl(PropertyType type, T obj)
            : base(type, obj)
        {
            BCLDebug.Assert(obj != null, "Must not be null");
            _value = obj;
        }

        public T Value {
            get { return _value; }
        }

        public override string ToString()
        {
            if (_value != null)
            {
                return _value.ToString();
            }
            else
            {
                return base.ToString();
            }
        }

        object IGetProxyTarget.GetTarget()
        {
            return (object)_value;
        }

        // We have T in an IReference<T>.  Need to QI for IReference<T> with the appropriate GUID, call
        // the get_Value property, allocate an appropriately-sized managed object, marshal the native object
        // to the managed object, and free the native method.  Also we want the return value boxed (aka normal value type boxing).
        //
        // This method is called by VM. Mark the method with FriendAccessAllowed attribute to ensure that the unreferenced method
        // optimization skips it and the code will be saved into NGen image.
        [System.Runtime.CompilerServices.FriendAccessAllowed]
        internal static Object UnboxHelper(Object wrapper)
        {
            Contract.Requires(wrapper != null);
            IReference<T> reference = (IReference<T>) wrapper;
            Contract.Assert(reference != null, "CLRIReferenceImpl::UnboxHelper - QI'ed for IReference<"+typeof(T)+">, but that failed.");
            return reference.Value;
        }
    }

    // T can be any WinRT-compatible type
    internal sealed class CLRIReferenceArrayImpl<T> : CLRIPropertyValueImpl,
                                                      IGetProxyTarget, 
                                                      IReferenceArray<T>, 
                                                      IList                     // Jupiter data binding needs IList/IEnumerable
    {
        private T[] _value;
        private IList _list;

        public CLRIReferenceArrayImpl(PropertyType type, T[] obj)
            : base(type, obj)
        {
            BCLDebug.Assert(obj != null, "Must not be null");

            _value = obj;

            _list = (IList) _value;
        }

        public T[] Value {
            get { return _value; }
        }

        public override string ToString()
        {
            if (_value != null)
            {
                return _value.ToString();
            }
            else
            {
                return base.ToString();
            }
        }

        //
        // IEnumerable methods. Used by data-binding in Jupiter when you try to data bind
        // against a managed array
        //
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_value).GetEnumerator();
        }

        //
        // IList & ICollection methods. 
        // This enables two-way data binding and index access in Jupiter
        //
        Object IList.this[int index] {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
            }
        }
    
        int IList.Add(Object value)
        {
            return _list.Add(value);
        }
    
        bool IList.Contains(Object value)
        {
            return _list.Contains(value);
        }
    
        void IList.Clear()
        {
            _list.Clear();
        }

        bool IList.IsReadOnly 
        { 
            get
            {
                return _list.IsReadOnly;
            }
        }
    
        bool IList.IsFixedSize
        {
            get
            {
                return _list.IsFixedSize;
            }
        }

        int IList.IndexOf(Object value)
        {
            return _list.IndexOf(value);
        }
    
        void IList.Insert(int index, Object value)
        {
            _list.Insert(index, value);
        }
    
        void IList.Remove(Object value)
        {
            _list.Remove(value);
        }
    
        void IList.RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            _list.CopyTo(array, index);
        }
        
        int ICollection.Count
        { 
            get
            {
                return _list.Count;
            }
        }

        Object ICollection.SyncRoot
        { 
            get
            {
                return _list.SyncRoot;
            }
        }
            
        bool ICollection.IsSynchronized
        { 
            get
            {
                return _list.IsSynchronized;
            }
        }

        object IGetProxyTarget.GetTarget()
        {
            return (object)_value;
        }
        
        // We have T in an IReferenceArray<T>.  Need to QI for IReferenceArray<T> with the appropriate GUID, call
        // the get_Value property, allocate an appropriately-sized managed object, marshal the native object
        // to the managed object, and free the native method.
        //
        // This method is called by VM. Mark the method with FriendAccessAllowed attribute to ensure that the unreferenced method
        // optimization skips it and the code will be saved into NGen image.
        [System.Runtime.CompilerServices.FriendAccessAllowed]
        internal static Object UnboxHelper(Object wrapper)
        {
            Contract.Requires(wrapper != null);
            IReferenceArray<T> reference = (IReferenceArray<T>)wrapper;
            Contract.Assert(reference != null, "CLRIReferenceArrayImpl::UnboxHelper - QI'ed for IReferenceArray<" + typeof(T) + ">, but that failed.");
            T[] marshaled = reference.Value;
            return marshaled;
        }
    }

    // For creating instances of Windows Runtime's IReference<T> and IReferenceArray<T>.
    internal static class IReferenceFactory
    {
        internal static readonly Type s_pointType = Type.GetType("Windows.Foundation.Point, " + AssemblyRef.SystemRuntimeWindowsRuntime);
        internal static readonly Type s_rectType = Type.GetType("Windows.Foundation.Rect, " + AssemblyRef.SystemRuntimeWindowsRuntime);
        internal static readonly Type s_sizeType = Type.GetType("Windows.Foundation.Size, " + AssemblyRef.SystemRuntimeWindowsRuntime);

        [SecuritySafeCritical]
        internal static Object CreateIReference(Object obj)
        {
            Contract.Requires(obj != null, "Null should not be boxed.");
            Contract.Ensures(Contract.Result<Object>() != null);

            Type type = obj.GetType();

            if (type.IsArray)
                return CreateIReferenceArray((Array) obj);

            if (type == typeof(int))
                return new CLRIReferenceImpl<int>(PropertyType.Int32, (int)obj);
            if (type == typeof(String))
                return new CLRIReferenceImpl<String>(PropertyType.String, (String)obj);
            if (type == typeof(byte))
                return new CLRIReferenceImpl<byte>(PropertyType.UInt8, (byte)obj);
            if (type == typeof(short))
                return new CLRIReferenceImpl<short>(PropertyType.Int16, (short)obj);
            if (type == typeof(ushort))
                return new CLRIReferenceImpl<ushort>(PropertyType.UInt16, (ushort)obj);
            if (type == typeof(uint))
                return new CLRIReferenceImpl<uint>(PropertyType.UInt32, (uint)obj);
            if (type == typeof(long))
                return new CLRIReferenceImpl<long>(PropertyType.Int64, (long)obj);
            if (type == typeof(ulong))
                return new CLRIReferenceImpl<ulong>(PropertyType.UInt64, (ulong)obj);
            if (type == typeof(float))
                return new CLRIReferenceImpl<float>(PropertyType.Single, (float)obj);
            if (type == typeof(double))
                return new CLRIReferenceImpl<double>(PropertyType.Double, (double)obj);
            if (type == typeof(char))
                return new CLRIReferenceImpl<char>(PropertyType.Char16, (char)obj);
            if (type == typeof(bool))
                return new CLRIReferenceImpl<bool>(PropertyType.Boolean, (bool)obj);
            if (type == typeof(Guid))
                return new CLRIReferenceImpl<Guid>(PropertyType.Guid, (Guid)obj);
            if (type == typeof(DateTimeOffset))
                return new CLRIReferenceImpl<DateTimeOffset>(PropertyType.DateTime, (DateTimeOffset)obj);
            if (type == typeof(TimeSpan))
                return new CLRIReferenceImpl<TimeSpan>(PropertyType.TimeSpan, (TimeSpan)obj);
            if (type == typeof(Object))
                return new CLRIReferenceImpl<Object>(PropertyType.Inspectable, (Object)obj);
            if (type == typeof(RuntimeType))
            {   // If the type is System.RuntimeType, we want to use System.Type marshaler (it's parent of the type)
                return new CLRIReferenceImpl<Type>(PropertyType.Other, (Type)obj);
            }

            // Handle arbitrary WinRT-compatible value types, and recognize a few special types.
            PropertyType? propType = null;
            if (type == s_pointType)
            {
                propType = PropertyType.Point;
            }
            else if (type == s_rectType)
            {
                propType = PropertyType.Rect;
            }
            else if (type == s_sizeType)
            {
                propType = PropertyType.Size;
            }
            else if (type.IsValueType || obj is Delegate)
            {
                propType = PropertyType.Other;
            }

            if (propType.HasValue)
            {
                Type specificType = typeof(CLRIReferenceImpl<>).MakeGenericType(type);
                return Activator.CreateInstance(specificType, new Object[] { propType.Value, obj });
            }

            Contract.Assert(false, "We should not see non-WinRT type here");
            return null;
        }

        [SecuritySafeCritical]
        internal static Object CreateIReferenceArray(Array obj)
        {
            Contract.Requires(obj != null);
            Contract.Requires(obj.GetType().IsArray);
            Contract.Ensures(Contract.Result<Object>() != null);

            Type type = obj.GetType().GetElementType();
            
            Contract.Assert(obj.Rank == 1 && obj.GetLowerBound(0) == 0 && !type.IsArray);

            if (type == typeof(int))
                return new CLRIReferenceArrayImpl<int>(PropertyType.Int32Array, (int[])obj);
            if (type == typeof(String))
                return new CLRIReferenceArrayImpl<String>(PropertyType.StringArray, (String[])obj);
            if (type == typeof(byte))
                return new CLRIReferenceArrayImpl<byte>(PropertyType.UInt8Array, (byte[])obj);
            if (type == typeof(short))
                return new CLRIReferenceArrayImpl<short>(PropertyType.Int16Array, (short[])obj);
            if (type == typeof(ushort))
                return new CLRIReferenceArrayImpl<ushort>(PropertyType.UInt16Array, (ushort[])obj);
            if (type == typeof(uint))
                return new CLRIReferenceArrayImpl<uint>(PropertyType.UInt32Array, (uint[])obj);
            if (type == typeof(long))
                return new CLRIReferenceArrayImpl<long>(PropertyType.Int64Array, (long[])obj);
            if (type == typeof(ulong))
                return new CLRIReferenceArrayImpl<ulong>(PropertyType.UInt64Array, (ulong[])obj);
            if (type == typeof(float))
                return new CLRIReferenceArrayImpl<float>(PropertyType.SingleArray, (float[])obj);
            if (type == typeof(double))
                return new CLRIReferenceArrayImpl<double>(PropertyType.DoubleArray, (double[])obj);
            if (type == typeof(char))
                return new CLRIReferenceArrayImpl<char>(PropertyType.Char16Array, (char[])obj);
            if (type == typeof(bool))
                return new CLRIReferenceArrayImpl<bool>(PropertyType.BooleanArray, (bool[])obj);
            if (type == typeof(Guid))
                return new CLRIReferenceArrayImpl<Guid>(PropertyType.GuidArray, (Guid[])obj);
            if (type == typeof(DateTimeOffset))
                return new CLRIReferenceArrayImpl<DateTimeOffset>(PropertyType.DateTimeArray, (DateTimeOffset[])obj);
            if (type == typeof(TimeSpan))
                return new CLRIReferenceArrayImpl<TimeSpan>(PropertyType.TimeSpanArray, (TimeSpan[])obj);
            if (type == typeof(Type))
            {   // Note: The array type will be System.Type, not System.RuntimeType
                return new CLRIReferenceArrayImpl<Type>(PropertyType.OtherArray, (Type[])obj);
            }

            PropertyType? propType = null;
            if (type == s_pointType)
            {
                propType = PropertyType.PointArray;
            }
            else if (type == s_rectType)
            {
                propType = PropertyType.RectArray;
            }
            else if (type == s_sizeType)
            {
                propType = PropertyType.SizeArray;
            }
            else if (type.IsValueType)
            {
                // note that KeyValuePair`2 is a reference type on the WinRT side so the array
                // must be wrapped with CLRIReferenceArrayImpl<Object>
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
                {
                    Object[] objArray = new Object[obj.Length];
                    for (int i = 0; i < objArray.Length; i++)
                    {
                        objArray[i] = obj.GetValue(i);
                    }
                    obj = objArray;
                }
                else
                {
                    propType = PropertyType.OtherArray;
                }
            }
            else if (typeof(Delegate).IsAssignableFrom(type))
            {
                propType = PropertyType.OtherArray;
            }


            if (propType.HasValue)
            {
                // All WinRT value type will be Property.Other
                Type specificType = typeof(CLRIReferenceArrayImpl<>).MakeGenericType(type);
                return Activator.CreateInstance(specificType, new Object[] { propType.Value, obj });
            }
            else
            {
                // All WinRT reference type (including arbitary managed type) will be PropertyType.ObjectArray
                return new CLRIReferenceArrayImpl<Object>(PropertyType.InspectableArray, (Object[])obj);
            }
        }
    }
}
