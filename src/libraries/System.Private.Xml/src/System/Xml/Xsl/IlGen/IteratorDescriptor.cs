// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen
{
    /// <summary>
    /// Type of location in which iterator items are stored.
    /// </summary>
    internal enum ItemLocation
    {
        None = 0,
        Stack,                              // Each value is stored as the top value on the IL stack
        Parameter,                          // Each value is stored as a parameter to the current method
        Local,                              // Each value is stored as a local variable in the current method
        Current,                            // Each value is stored as an iterator's Current property
        Global,                             // Each value is stored as a global variable
    };


    /// <summary>
    /// None--Not in a branching context
    /// True--Branch if boolean expression evaluates to true
    /// False--Branch if boolean expression evaluates to false
    /// </summary>
    internal enum BranchingContext
    {
        None,
        OnTrue,
        OnFalse
    };

    /// <summary>
    /// Describes the Clr type and location of items returned by an iterator.
    /// This struct is meant to be immutable.
    /// </summary>
    internal struct StorageDescriptor
    {
        private ItemLocation _location;
        private object _locationObject;
        private Type _itemStorageType;
        private bool _isCached;


        //-----------------------------------------------
        // Create Methods
        //-----------------------------------------------

        /// <summary>
        /// Create default, empty StorageDescriptor.
        /// </summary>
        public static StorageDescriptor None()
        {
            return default;
        }

        /// <summary>
        /// Create a StorageDescriptor for an item located on the stack.
        /// </summary>
        public static StorageDescriptor Stack(Type itemStorageType, bool isCached)
        {
            StorageDescriptor storage = default;
            storage._location = ItemLocation.Stack;
            storage._itemStorageType = itemStorageType;
            storage._isCached = isCached;
            return storage;
        }

        /// <summary>
        /// Create a StorageDescriptor for an item which is a parameter to the current method.
        /// </summary>
        public static StorageDescriptor Parameter(int paramIndex, Type itemStorageType, bool isCached)
        {
            StorageDescriptor storage = default;
            storage._location = ItemLocation.Parameter;
            storage._locationObject = paramIndex;
            storage._itemStorageType = itemStorageType;
            storage._isCached = isCached;
            return storage;
        }

        /// <summary>
        /// Create a StorageDescriptor for an item located in a local variable.
        /// </summary>
        public static StorageDescriptor Local(LocalBuilder loc, Type itemStorageType, bool isCached)
        {
            Debug.Assert(loc.LocalType == itemStorageType ||
                         typeof(IList<>).MakeGenericType(itemStorageType).IsAssignableFrom(loc.LocalType),
                $"Type {itemStorageType} does not match the local variable's type");

            StorageDescriptor storage = default;
            storage._location = ItemLocation.Local;
            storage._locationObject = loc;
            storage._itemStorageType = itemStorageType;
            storage._isCached = isCached;
            return storage;
        }

        /// <summary>
        /// Create a StorageDescriptor for an item which is the Current item in an iterator.
        /// </summary>
        public static StorageDescriptor Current(LocalBuilder locIter, MethodInfo currentMethod, Type itemStorageType)
        {
            Debug.Assert(currentMethod.ReturnType == itemStorageType,
                         $"Type {itemStorageType} does not match type of Current property.");

            StorageDescriptor storage = default;
            storage._location = ItemLocation.Current;
            storage._locationObject = new CurrentContext(locIter, currentMethod);
            storage._itemStorageType = itemStorageType;
            return storage;
        }

        /// <summary>
        /// Create a StorageDescriptor for an item located in a global variable.
        /// </summary>
        public static StorageDescriptor Global(MethodInfo methGlobal, Type itemStorageType, bool isCached)
        {
            Debug.Assert(methGlobal.ReturnType == itemStorageType ||
                         typeof(IList<>).MakeGenericType(itemStorageType).IsAssignableFrom(methGlobal.ReturnType),
                $"Type {itemStorageType} does not match the global method's return type");

            StorageDescriptor storage = default;
            storage._location = ItemLocation.Global;
            storage._locationObject = methGlobal;
            storage._itemStorageType = itemStorageType;
            storage._isCached = isCached;
            return storage;
        }


        //-----------------------------------------------
        // Accessor Methods
        //-----------------------------------------------

        /// <summary>
        /// Return copy of current descriptor, but change item's location to the stack.
        /// </summary>
        public StorageDescriptor ToStack()
        {
            return Stack(_itemStorageType, _isCached);
        }

        /// <summary>
        /// Create a StorageDescriptor for an item located in a local variable.
        /// </summary>
        public StorageDescriptor ToLocal(LocalBuilder loc)
        {
            return Local(loc, _itemStorageType, _isCached);
        }

        /// <summary>
        /// Create a StorageDescriptor which is the same as this one, except for the item storage type.
        /// </summary>
        public StorageDescriptor ToStorageType(Type itemStorageType)
        {
            StorageDescriptor storage = this;
            storage._itemStorageType = itemStorageType;
            return storage;
        }

        /// <summary>
        /// Return an enumeration specifying where the value is located.
        /// </summary>
        public ItemLocation Location
        {
            get { return _location; }
        }

        /// <summary>
        /// Return the index of the parameter that stores this iterator's values.
        /// </summary>
        public int ParameterLocation
        {
            get { return (int)_locationObject; }
        }

        /// <summary>
        /// Return the LocalBuilder that stores this iterator's values.
        /// </summary>
        public LocalBuilder? LocalLocation
        {
            get { return _locationObject as LocalBuilder; }
        }

        /// <summary>
        /// Return the "Current" location information (LocalBuilder and Current MethodInfo) that will store
        /// this iterator's helper class. The Current property on this iterator can be accessed to get the CurrentMethod.
        /// </summary>
        public CurrentContext? CurrentLocation
        {
            get { return _locationObject as CurrentContext; }
        }

        /// <summary>
        /// Return the MethodInfo for the method that computes this global value.
        /// </summary>
        public MethodInfo? GlobalLocation
        {
            get { return _locationObject as MethodInfo; }
        }

        /// <summary>
        /// Return true if this iterator's values are cached.
        /// </summary>
        public bool IsCached
        {
            get { return _isCached; }
        }

        /// <summary>
        /// Return the Clr type of an individual item in the storage location (never an IList{T} type).
        /// </summary>
        public Type ItemStorageType
        {
            get { return _itemStorageType; }
        }
    }

    /// <summary>
    /// A data class to hold information for a "Current" StorageLocation.
    /// </summary>
    internal sealed class CurrentContext
    {
        public CurrentContext(LocalBuilder local, MethodInfo currentMethod)
        {
            Local = local;
            CurrentMethod = currentMethod;
        }

        public readonly LocalBuilder Local;
        public readonly MethodInfo CurrentMethod;
    }

    /// <summary>
    /// Iterators are joined together, are nested within each other, and reference each other.  This internal class
    /// contains detailed information about iteration next labels, caching, iterator item location, etc.
    /// </summary>
    [RequiresDynamicCode("Creates DynamicMethods")]
    internal sealed class IteratorDescriptor
    {
        private GenerateHelper _helper;

        // Related iterators
        private IteratorDescriptor? _iterParent;

        // Iteration
        private Label _lblNext;
        private bool _hasNext;
        private LocalBuilder? _locPos;

        // Branching
        private BranchingContext _brctxt;
        private Label _lblBranch;

        // Storage
        private StorageDescriptor _storage;


        //-----------------------------------------------
        // Initialize
        //-----------------------------------------------

        /// <summary>
        /// Create a "root" IteratorDescriptor which has no parent iterator.
        /// </summary>
        public IteratorDescriptor(GenerateHelper helper)
        {
            Init(null, helper);
        }

        /// <summary>
        /// Create an IteratorDescriptor that is nested in a parent iterator.
        /// </summary>
        public IteratorDescriptor(IteratorDescriptor iterParent)
        {
            Init(iterParent, iterParent._helper);
        }

        /// <summary>
        /// Internal helper initializor.
        /// </summary>
        [MemberNotNull(nameof(_helper))]
        private void Init(IteratorDescriptor? iterParent, GenerateHelper helper)
        {
            _helper = helper;
            _iterParent = iterParent;
        }


        //-----------------------------------------------
        // Related Iterators
        //-----------------------------------------------

        /// <summary>
        /// Return the iterator in which this iterator is nested.
        /// </summary>
        public IteratorDescriptor? ParentIterator
        {
            get { return _iterParent; }
        }


        //-----------------------------------------------
        // Iteration
        //-----------------------------------------------

        /// <summary>
        /// Returns true if LabelNext is currently defined.  If not, then this iterator will return
        /// exactly one result (iterator is cardinality one).
        /// </summary>
        public bool HasLabelNext
        {
            get { return _hasNext; }
        }

        /// <summary>
        /// Return the label that is anchored to this code iterator's MoveNext code.
        /// </summary>
        public Label GetLabelNext()
        {
            Debug.Assert(_hasNext);
            return _lblNext;
        }

        /// <summary>
        /// Set this iterator's next label and storage.  This iterator will range over a set of values located in
        /// "storage".  To get the next value, jump to "lblNext".
        /// </summary>
        public void SetIterator(Label lblNext, StorageDescriptor storage)
        {
            _lblNext = lblNext;
            _hasNext = true;
            _storage = storage;
        }

        /// <summary>
        /// Set this iterator to be the same as the specified iterator.
        /// </summary>
        public void SetIterator(IteratorDescriptor iterInfo)
        {
            if (iterInfo.HasLabelNext)
            {
                _lblNext = iterInfo.GetLabelNext();
                _hasNext = true;
            }

            _storage = iterInfo.Storage;
        }

        /// <summary>
        /// Continue iteration until it is complete.  Branch to "lblOnEnd" when iteration is complete.
        /// </summary>
        /// <remarks>
        /// goto LabelNextCtxt;
        /// LabelOnEnd:
        /// </remarks>
        public void LoopToEnd(Label lblOnEnd)
        {
            if (_hasNext)
            {
                _helper.BranchAndMark(_lblNext, lblOnEnd);
                _hasNext = false;
            }

            // After looping is finished, storage is N/A
            _storage = StorageDescriptor.None();
        }

        /// <summary>
        /// Storage location containing the position of the current item as an integer.
        /// This location is only defined on iterators, and then only if they might be
        /// referenced by a PositionOf operator.
        /// </summary>
        public LocalBuilder? LocalPosition
        {
            get { return _locPos; }
            set { _locPos = value; }
        }


        //-----------------------------------------------
        // Caching
        //-----------------------------------------------

        /// <summary>
        /// Push the count of items in the cache onto the stack.
        /// </summary>
        public void CacheCount()
        {
            Debug.Assert(_storage.IsCached);
            PushValue();
            _helper.CallCacheCount(_storage.ItemStorageType);
        }

        /// <summary>
        /// If the iterator has been fully cached, then iterate the values one-by-one.
        /// </summary>
        public void EnsureNoCache()
        {
            if (_storage.IsCached)
            {
                if (!HasLabelNext)
                {
                    // If no Next label, this must be a singleton cache
                    EnsureStack();
                    _helper.LoadInteger(0);
                    _helper.CallCacheItem(_storage.ItemStorageType);

                    _storage = StorageDescriptor.Stack(_storage.ItemStorageType, false);
                }
                else
                {
                    // int idx;
                    LocalBuilder locIdx = _helper.DeclareLocal("$$$idx", typeof(int));
                    Label lblNext;

                    // Make sure cache is not on the stack
                    EnsureNoStack("$$$cache");

                    // idx = -1;
                    _helper.LoadInteger(-1);
                    _helper.Emit(OpCodes.Stloc, locIdx);

                    // LabelNext:
                    lblNext = _helper.DefineLabel();
                    _helper.MarkLabel(lblNext);

                    // idx++;
                    _helper.Emit(OpCodes.Ldloc, locIdx);
                    _helper.LoadInteger(1);
                    _helper.Emit(OpCodes.Add);
                    _helper.Emit(OpCodes.Stloc, locIdx);

                    // if (idx >= cache.Count) goto LabelNextCtxt;
                    _helper.Emit(OpCodes.Ldloc, locIdx);
                    CacheCount();
                    _helper.Emit(OpCodes.Bge, GetLabelNext());

                    // item = cache[idx];
                    PushValue();
                    _helper.Emit(OpCodes.Ldloc, locIdx);
                    _helper.CallCacheItem(_storage.ItemStorageType);

                    SetIterator(lblNext, StorageDescriptor.Stack(_storage.ItemStorageType, false));
                }
            }
        }


        //-----------------------------------------------
        // If-then-else branching
        //-----------------------------------------------

        /// <summary>
        /// Setup a branching context.  All nested iterators compiled in this context must evaluate
        /// to a single boolean value.  However, these expressions must not push the result as a boolean
        /// onto the stack.  Instead, if brctxt is BranchType.True, then the expression should
        /// jump to lblBranch if it evaluates to true.  If brctxt is BranchType.False, then the
        /// branch should happen if the evaluation result is false.
        /// </summary>
        public void SetBranching(BranchingContext brctxt, Label lblBranch)
        {
            Debug.Assert(brctxt != BranchingContext.None);
            _brctxt = brctxt;
            _lblBranch = lblBranch;
        }

        /// <summary>
        /// True if within a branching context.
        /// </summary>
        public bool IsBranching
        {
            get { return _brctxt != BranchingContext.None; }
        }

        /// <summary>
        /// Returns the label to which conditionals should branch.
        /// </summary>
        public Label LabelBranch
        {
            get { return _lblBranch; }
        }

        /// <summary>
        /// If BranchingContext.OnTrue, branch on true.  Otherwise, branch on false.
        /// </summary>
        public BranchingContext CurrentBranchingContext
        {
            get { return _brctxt; }
        }


        //-----------------------------------------------
        // Storage
        //-----------------------------------------------

        /// <summary>
        /// Returns information about how and where iterator values are stored.
        /// </summary>
        public StorageDescriptor Storage
        {
            get { return _storage; }
            set { _storage = value; }
        }

        /// <summary>
        /// Push current item onto the stack without affecting Location.
        /// </summary>
        public void PushValue()
        {
            switch (_storage.Location)
            {
                case ItemLocation.Stack:
                    _helper.Emit(OpCodes.Dup);
                    break;

                case ItemLocation.Parameter:
                    _helper.LoadParameter(_storage.ParameterLocation);
                    break;

                case ItemLocation.Local:
                    _helper.Emit(OpCodes.Ldloc, _storage.LocalLocation!);
                    break;

                case ItemLocation.Current:
                    CurrentContext currentContext = _storage.CurrentLocation!;
                    _helper.Emit(OpCodes.Ldloca, currentContext.Local);
                    _helper.Call(currentContext.CurrentMethod);
                    break;

                default:
                    Debug.Fail($"Invalid location: {_storage.Location}");
                    break;
            }
        }

        /// <summary>
        /// Ensure that the current item is pushed onto the stack.
        /// </summary>
        public void EnsureStack()
        {
            switch (_storage.Location)
            {
                case ItemLocation.Stack:
                    // Already on the stack
                    return;

                case ItemLocation.Parameter:
                case ItemLocation.Local:
                case ItemLocation.Current:
                    PushValue();
                    break;

                case ItemLocation.Global:
                    // Call method that computes the value of this global value
                    _helper.LoadQueryRuntime();
                    _helper.Call(_storage.GlobalLocation!);
                    break;

                default:
                    Debug.Fail($"Invalid location: {_storage.Location}");
                    break;
            }

            _storage = _storage.ToStack();
        }

        /// <summary>
        /// If the current item is on the stack, move it to a local variable.
        /// </summary>
        public void EnsureNoStack(string locName)
        {
            if (_storage.Location == ItemLocation.Stack)
                EnsureLocal(locName);
        }

        /// <summary>
        /// If current item is not already in a local variable, then move it to a local variable of the specified name.
        /// </summary>
        public void EnsureLocal(string locName)
        {
            if (_storage.Location != ItemLocation.Local)
            {
                if (_storage.IsCached)
                    EnsureLocal(_helper.DeclareLocal(locName, typeof(IList<>).MakeGenericType(_storage.ItemStorageType)));
                else
                    EnsureLocal(_helper.DeclareLocal(locName, _storage.ItemStorageType));
            }
        }

        /// <summary>
        /// Ensure that current item is saved to the specified local variable.
        /// </summary>
        public void EnsureLocal(LocalBuilder bldr)
        {
            if (_storage.LocalLocation != bldr)
            {
                // Push value onto stack and then save to bldr
                EnsureStack();
                _helper.Emit(OpCodes.Stloc, bldr);
                _storage = _storage.ToLocal(bldr);
            }
        }

        /// <summary>
        /// Discard the current item if it is pushed onto the stack.
        /// </summary>
        public void DiscardStack()
        {
            if (_storage.Location == ItemLocation.Stack)
            {
                _helper.Emit(OpCodes.Pop);
                _storage = StorageDescriptor.None();
            }
        }

        /// <summary>
        /// Ensure that the iterator's items are not cached, and that the current item is pushed onto the stack.
        /// </summary>
        public void EnsureStackNoCache()
        {
            EnsureNoCache();
            EnsureStack();
        }

        /// <summary>
        /// Ensure that the iterator's items are not cached, and that if the current item is pushed onto the stack,
        /// that it is moved to a local variable.
        /// </summary>
        public void EnsureNoStackNoCache(string locName)
        {
            EnsureNoCache();
            EnsureNoStack(locName);
        }

        /// <summary>
        /// Ensure that the iterator's items are not cached, and that if the current item is not already in a local,
        /// variable, that it is moved to a local variable of the specified name.
        /// </summary>
        public void EnsureLocalNoCache(string locName)
        {
            EnsureNoCache();
            EnsureLocal(locName);
        }

        /// <summary>
        /// Ensure that the iterator's items are not cached and that the current item is saved to the specified local variable.
        /// </summary>
        public void EnsureLocalNoCache(LocalBuilder bldr)
        {
            EnsureNoCache();
            EnsureLocal(bldr);
        }

        /// <summary>
        /// Each XmlQueryType has multiple legal CLR representations.  Ensure that all items returned by this iterator are in
        /// the Clr representation specified by "storageTypeDest".
        /// </summary>
        public void EnsureItemStorageType(XmlQueryType xmlType, Type storageTypeDest)
        {
            // If source type = destination type, then done
            if (_storage.ItemStorageType == storageTypeDest)
                goto SetStorageType;

            Debug.Assert(_storage.ItemStorageType == typeof(XPathItem) || storageTypeDest == typeof(XPathItem),
                         "EnsureItemStorageType must convert to or from Item");

            // If items are cached,
            if (_storage.IsCached)
            {
                // Check for special case of IList<XPathNavigator> -> IList<XPathItem>
                if (_storage.ItemStorageType == typeof(XPathNavigator))
                {
                    EnsureStack();
                    _helper.Call(XmlILMethods.NavsToItems);
                    goto SetStorageType;
                }

                // Check for special case of IList<XPathItem> -> IList<XPathNavigator>
                if (storageTypeDest == typeof(XPathNavigator))
                {
                    EnsureStack();
                    _helper.Call(XmlILMethods.ItemsToNavs);
                    goto SetStorageType;
                }
            }

            // Iterate over each item, and convert each to the destination type
            EnsureStackNoCache();

            // If source type is Item,
            if (_storage.ItemStorageType == typeof(XPathItem))
            {
                // Then downcast to Navigator
                if (storageTypeDest == typeof(XPathNavigator))
                {
                    _helper.Emit(OpCodes.Castclass, typeof(XPathNavigator));
                }
                else
                {
                    // Call ValueAs methods for atomic types
                    _helper.CallValueAs(storageTypeDest);
                }
                goto SetStorageType;
            }
            else if (_storage.ItemStorageType == typeof(XPathNavigator))
            {
                // No-op if converting from XPathNavigator to XPathItem
                Debug.Assert(storageTypeDest == typeof(XPathItem), "Must be converting from XPathNavigator to XPathItem");
                goto SetStorageType;
            }

            // Destination type must be item, so generate code to create an XmlAtomicValue
            _helper.LoadInteger(_helper.StaticData.DeclareXmlType(xmlType));
            _helper.LoadQueryRuntime();
            _helper.Call(XmlILMethods.StorageMethods[_storage.ItemStorageType].ToAtomicValue!);

        SetStorageType:
            _storage = _storage.ToStorageType(storageTypeDest);
        }
    }
}
