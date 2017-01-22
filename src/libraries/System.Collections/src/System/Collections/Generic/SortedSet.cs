// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    // A binary search tree is a red-black tree if it satisfies the following red-black properties:
    // 1. Every node is either red or black
    // 2. Every leaf (nil node) is black
    // 3. If a node is red, the both its children are black
    // 4. Every simple path from a node to a descendant leaf contains the same number of black nodes
    //
    // The basic idea of red-black tree is to represent 2-3-4 trees as standard BSTs but to add one extra bit of information
    // per node to encode 3-nodes and 4-nodes.
    // 4-nodes will be represented as:          B
    //                                                              R            R
    // 3 -node will be represented as:           B             or         B
    //                                                              R          B               B       R
    //
    // For a detailed description of the algorithm, take a look at "Algorithm" by Rebert Sedgewick.

    internal delegate bool TreeWalkPredicate<T>(SortedSet<T>.Node node);

    internal enum TreeRotation
    {
        Left = 1,
        Right = 2,
        RightLeft = 3,
        LeftRight = 4,
    }

    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "by design name choice")]
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public partial class SortedSet<T> : ISet<T>, ICollection<T>, ICollection, IReadOnlyCollection<T>, ISerializable, IDeserializationCallback
    {
        #region Local variables/constants

        private Node _root;
        private IComparer<T> _comparer;
        private int _count;
        private int _version;
        [NonSerialized]
        private object _syncRoot;
        private SerializationInfo _siInfo; // A temporary variable which we need during deserialization

        private const string ComparerName = "Comparer";
        private const string CountName = "Count";
        private const string ItemsName = "Items";
        private const string VersionName = "Version";
        // Needed for enumerator
        private const string TreeName = "Tree";
        private const string NodeValueName = "Item";
        private const string EnumStartName = "EnumStarted";
        private const string ReverseName = "Reverse";
        private const string EnumVersionName = "EnumVersion";
        // Needed for TreeSubset
        private const string MinName = "Min";
        private const string MaxName = "Max";
        private const string LowerBoundActiveName = "lBoundActive";
        private const string UpperBoundActiveName = "uBoundActive";

        internal const int StackAllocThreshold = 100;

        #endregion

        #region Constructors

        public SortedSet()
        {
            _comparer = Comparer<T>.Default;
        }

        public SortedSet(IComparer<T> comparer)
        {
            _comparer = comparer ?? Comparer<T>.Default;
        }


        public SortedSet(IEnumerable<T> collection) : this(collection, Comparer<T>.Default) { }

        public SortedSet(IEnumerable<T> collection, IComparer<T> comparer)
            : this(comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            // these are explicit type checks in the mould of HashSet. It would have worked better
            // with something like an ISorted<T> (we could make this work for SortedList.Keys etc)
            SortedSet<T> baseSortedSet = collection as SortedSet<T>;
            SortedSet<T> baseTreeSubSet = collection as TreeSubSet;
            if (baseSortedSet != null && baseTreeSubSet == null && AreComparersEqual(this, baseSortedSet))
            {
                // breadth first traversal to recreate nodes
                if (baseSortedSet.Count == 0)
                {
                    return;
                }

                // pre order way to replicate nodes
                Stack<Node> theirStack = new Stack<SortedSet<T>.Node>(2 * Log2(baseSortedSet.Count) + 2);
                Stack<Node> myStack = new Stack<SortedSet<T>.Node>(2 * Log2(baseSortedSet.Count) + 2);
                Node theirCurrent = baseSortedSet._root;
                Node myCurrent = (theirCurrent != null ? new SortedSet<T>.Node(theirCurrent.Item, theirCurrent.IsRed) : null);
                _root = myCurrent;
                while (theirCurrent != null)
                {
                    theirStack.Push(theirCurrent);
                    myStack.Push(myCurrent);
                    myCurrent.Left = (theirCurrent.Left != null ? new SortedSet<T>.Node(theirCurrent.Left.Item, theirCurrent.Left.IsRed) : null);
                    theirCurrent = theirCurrent.Left;
                    myCurrent = myCurrent.Left;
                }
                while (theirStack.Count != 0)
                {
                    theirCurrent = theirStack.Pop();
                    myCurrent = myStack.Pop();
                    Node theirRight = theirCurrent.Right;
                    Node myRight = null;
                    if (theirRight != null)
                    {
                        myRight = new SortedSet<T>.Node(theirRight.Item, theirRight.IsRed);
                    }
                    myCurrent.Right = myRight;

                    while (theirRight != null)
                    {
                        theirStack.Push(theirRight);
                        myStack.Push(myRight);
                        myRight.Left = (theirRight.Left != null ? new SortedSet<T>.Node(theirRight.Left.Item, theirRight.Left.IsRed) : null);
                        theirRight = theirRight.Left;
                        myRight = myRight.Left;
                    }
                }
                _count = baseSortedSet._count;
            }
            else
            {
                int count;
                T[] els = EnumerableHelpers.ToArray(collection, out count);
                if (count > 0)
                {
                    comparer = _comparer; // If comparer is null, sets it to Comparer<T>.Default
                    Array.Sort(els, 0, count, comparer);
                    int index = 1;
                    for (int i = 1; i < count; i++)
                    {
                        if (comparer.Compare(els[i], els[i - 1]) != 0)
                        {
                            els[index++] = els[i];
                        }
                    }
                    count = index;

                    _root = ConstructRootFromSortedArray(els, 0, count - 1, null);
                    _count = count;
                }
            }
        }

        protected SortedSet(SerializationInfo info, StreamingContext context)
        {
            _siInfo = info;
        }

        #endregion

        #region Bulk operation helpers

        private void AddAllElements(IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                if (!Contains(item))
                    Add(item);
            }
        }

        private void RemoveAllElements(IEnumerable<T> collection)
        {
            T min = Min;
            T max = Max;
            foreach (T item in collection)
            {
                if (!(_comparer.Compare(item, min) < 0 || _comparer.Compare(item, max) > 0) && Contains(item))
                    Remove(item);
            }
        }

        private bool ContainsAllElements(IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                if (!Contains(item))
                {
                    return false;
                }
            }
            return true;
        }

        // Do a in order walk on tree and calls the delegate for each node.
        // If the action delegate returns false, stop the walk.
        // Return true if the entire tree has been walked.
        // Otherwise returns false.

        internal bool InOrderTreeWalk(TreeWalkPredicate<T> action) => InOrderTreeWalk(action, reverse: false);

        // Allows for the change in traversal direction. Reverse visits nodes in descending order
        internal virtual bool InOrderTreeWalk(TreeWalkPredicate<T> action, bool reverse)
        {
            if (_root == null)
            {
                return true;
            }

            // The maximum height of a red-black tree is 2*lg(n+1).
            // See page 264 of "Introduction to algorithms" by Thomas H. Cormen
            // note: this should be logbase2, but since the stack grows itself, we
            // don't want the extra cost
            Stack<Node> stack = new Stack<Node>(2 * (int)(SortedSet<T>.Log2(Count + 1)));
            Node current = _root;
            while (current != null)
            {
                stack.Push(current);
                current = (reverse ? current.Right : current.Left);
            }
            while (stack.Count != 0)
            {
                current = stack.Pop();
                if (!action(current))
                {
                    return false;
                }

                Node node = (reverse ? current.Left : current.Right);
                while (node != null)
                {
                    stack.Push(node);
                    node = (reverse ? node.Right : node.Left);
                }
            }
            return true;
        }

        // Do a left to right breadth first walk on tree and
        // calls the delegate for each node.
        // If the action delegate returns false, stop the walk.
        // Return true if the entire tree has been walked.
        // Otherwise returns false.
        internal virtual bool BreadthFirstTreeWalk(TreeWalkPredicate<T> action)
        {
            if (_root == null)
            {
                return true;
            }

            Queue<Node> processQueue = new Queue<Node>();
            processQueue.Enqueue(_root);
            Node current;

            while (processQueue.Count != 0)
            {
                current = processQueue.Dequeue();
                if (!action(current))
                {
                    return false;
                }
                if (current.Left != null)
                {
                    processQueue.Enqueue(current.Left);
                }
                if (current.Right != null)
                {
                    processQueue.Enqueue(current.Right);
                }
            }
            return true;
        }
        #endregion

        #region Properties

        public int Count
        {
            get
            {
                VersionCheck();
                return _count;
            }
        }

        public IComparer<T> Comparer => _comparer;

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Threading.Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }

        #endregion

        #region Subclass helpers

        // virtual function for subclass that needs to update count
        internal virtual void VersionCheck() { }

        // virtual function for subclass that needs to do range checks
        internal virtual bool IsWithinRange(T item) => true;

        #endregion

        #region ICollection<T> members

        public bool Add(T item) => AddIfNotPresent(item);

        void ICollection<T>.Add(T item) => AddIfNotPresent(item);

        internal virtual bool AddIfNotPresent(T item)
        {
            if (_root == null)
            {
                // empty tree
                _root = new Node(item, isRed: false);
                _count = 1;
                _version++;
                return true;
            }

            // Search for a node at bottom to insert the new node.
            // If we can guarantee the node we found is not a 4-node, it would be easy to do insertion.
            // We split 4-nodes along the search path.
            Node current = _root;
            Node parent = null;
            Node grandParent = null;
            Node greatGrandParent = null;

            // even if we don't actually add to the set, we may be altering its structure (by doing rotations
            // and such). so update version to disable any enumerators/subsets working on it
            _version++;

            int order = 0;
            while (current != null)
            {
                order = _comparer.Compare(item, current.Item);
                if (order == 0)
                {
                    // We could have changed root node to red during the search process.
                    // We need to set it to black before we return.
                    _root.IsRed = false;
                    return false;
                }

                // split a 4-node into two 2-nodes
                if (Is4Node(current))
                {
                    Split4Node(current);
                    // We could have introduced two consecutive red nodes after split. Fix that by rotation.
                    if (IsRed(parent))
                    {
                        InsertionBalance(current, ref parent, grandParent, greatGrandParent);
                    }
                }
                greatGrandParent = grandParent;
                grandParent = parent;
                parent = current;
                current = (order < 0) ? current.Left : current.Right;
            }

            Debug.Assert(parent != null);
            // ready to insert the new node
            Node node = new Node(item);
            if (order > 0)
            {
                parent.Right = node;
            }
            else
            {
                parent.Left = node;
            }

            // the new node will be red, so we will need to adjust the colors if parent node is also red
            if (parent.IsRed)
            {
                InsertionBalance(node, ref parent, grandParent, greatGrandParent);
            }

            // Root node is always black
            _root.IsRed = false;
            ++_count;
            return true;
        }

        public bool Remove(T item) => DoRemove(item); // hack so it can be made non-virtual

        internal virtual bool DoRemove(T item)
        {
            if (_root == null)
            {
                return false;
            }

            // Search for a node and then find its successor.
            // Then copy the item from the successor to the matching node and delete the successor.
            // If a node doesn't have a successor, we can replace it with its left child (if not empty.)
            // or delete the matching node.
            //
            // In top-down implementation, it is important to make sure the node to be deleted is not a 2-node.
            // Following code will make sure the node on the path is not a 2-node.

            // even if we don't actually remove from the set, we may be altering its structure (by doing rotations
            // and such). so update version to disable any enumerators/subsets working on it
            _version++;

            Node current = _root;
            Node parent = null;
            Node grandParent = null;
            Node match = null;
            Node parentOfMatch = null;
            bool foundMatch = false;
            while (current != null)
            {
                if (Is2Node(current))
                {
                    // fix up 2-Node
                    if (parent == null)
                    {
                        // current is root. Mark it as red
                        current.IsRed = true;
                    }
                    else
                    {
                        Node sibling = GetSibling(current, parent);
                        if (sibling.IsRed)
                        {
                            // If parent is a 3-node, flip the orientation of the red link.
                            // We can achieve this by a single rotation
                            // This case is converted to one of other cased below.
                            Debug.Assert(!parent.IsRed);
                            if (parent.Right == sibling)
                            {
                                RotateLeft(parent);
                            }
                            else
                            {
                                RotateRight(parent);
                            }

                            parent.IsRed = true;
                            sibling.IsRed = false; // parent's color
                            // sibling becomes child of grandParent or root after rotation. Update link from grandParent or root
                            ReplaceChildOfNodeOrRoot(grandParent, parent, sibling);
                            // sibling will become grandParent of current node
                            grandParent = sibling;
                            if (parent == match)
                            {
                                parentOfMatch = sibling;
                            }

                            // update sibling, this is necessary for following processing
                            sibling = (parent.Left == current) ? parent.Right : parent.Left;
                        }
                        Debug.Assert(sibling != null && !sibling.IsRed);

                        if (Is2Node(sibling))
                        {
                            Merge2Nodes(parent, current, sibling);
                        }
                        else
                        {
                            // current is a 2-node and sibling is either a 3-node or a 4-node.
                            // We can change the color of current to red by some rotation.
                            TreeRotation rotation = RotationNeeded(parent, current, sibling);
                            Node newGrandParent = null;
                            switch (rotation)
                            {
                                case TreeRotation.Right:
                                    Debug.Assert(parent.Left == sibling);
                                    Debug.Assert(sibling.Left.IsRed);
                                    sibling.Left.IsRed = false;
                                    newGrandParent = RotateRight(parent);
                                    break;
                                case TreeRotation.Left:
                                    Debug.Assert(parent.Right == sibling);
                                    Debug.Assert(sibling.Right.IsRed);
                                    sibling.Right.IsRed = false;
                                    newGrandParent = RotateLeft(parent);
                                    break;

                                case TreeRotation.RightLeft:
                                    Debug.Assert(parent.Right == sibling);
                                    Debug.Assert(sibling.Left.IsRed);
                                    newGrandParent = RotateRightLeft(parent);
                                    break;

                                case TreeRotation.LeftRight:
                                    Debug.Assert(parent.Left == sibling);
                                    Debug.Assert(sibling.Right.IsRed);
                                    newGrandParent = RotateLeftRight(parent);
                                    break;
                            }

                            newGrandParent.IsRed = parent.IsRed;
                            parent.IsRed = false;
                            current.IsRed = true;
                            ReplaceChildOfNodeOrRoot(grandParent, parent, newGrandParent);
                            if (parent == match)
                            {
                                parentOfMatch = newGrandParent;
                            }
                            grandParent = newGrandParent;
                        }
                    }
                }

                // we don't need to compare any more once we found the match
                int order = foundMatch ? -1 : _comparer.Compare(item, current.Item);
                if (order == 0)
                {
                    // save the matching node
                    foundMatch = true;
                    match = current;
                    parentOfMatch = parent;
                }

                grandParent = parent;
                parent = current;
                // If we found a match, continue the search in the right sub-tree.
                current = order < 0 ? current.Left : current.Right;
            }

            // move successor to the matching node position and replace links
            if (match != null)
            {
                ReplaceNode(match, parentOfMatch, parent, grandParent);
                --_count;
            }

            if (_root != null)
            {
                _root.IsRed = false;
            }

            return foundMatch;
        }

        public virtual void Clear()
        {
            _root = null;
            _count = 0;
            ++_version;
        }


        public virtual bool Contains(T item) => FindNode(item) != null;

        public void CopyTo(T[] array) => CopyTo(array, 0, Count);

        public void CopyTo(T[] array, int index) => CopyTo(array, index, Count);

        public void CopyTo(T[] array, int index, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            // will array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (index > array.Length || count > array.Length - index)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            // upper bound
            count += index;

            InOrderTreeWalk(node =>
            {
                if (index >= count)
                {
                    return false;
                }

                array[index++] = node.Item;
                return true;
            });
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            T[] tarray = array as T[];
            if (tarray != null)
            {
                CopyTo(tarray, index);
            }
            else
            {
                object[] objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }

                try
                {
                    InOrderTreeWalk(node => { objects[index++] = node.Item; return true; });
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }
        }

        #endregion

        #region IEnumerable<T> members

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Tree-specific operations

        private static Node GetSibling(Node node, Node parent) => parent.Left == node ? parent.Right : parent.Left;

        // After calling InsertionBalance, we need to make sure current and parent up-to-date.
        // It doesn't matter if we keep grandParent and greatGrantParent up-to-date
        // because we won't need to split again in the next node.
        // By the time we need to split again, everything will be correctly set.
        private void InsertionBalance(Node current, ref Node parent, Node grandParent, Node greatGrandParent)
        {
            Debug.Assert(grandParent != null);
            bool parentIsOnRight = (grandParent.Right == parent);
            bool currentIsOnRight = (parent.Right == current);

            Node newChildOfGreatGrandParent;
            if (parentIsOnRight == currentIsOnRight)
            {
                // same orientation, single rotation
                newChildOfGreatGrandParent = currentIsOnRight ? RotateLeft(grandParent) : RotateRight(grandParent);
            }
            else
            {
                // different orientation, double rotation
                newChildOfGreatGrandParent = currentIsOnRight ? RotateLeftRight(grandParent) : RotateRightLeft(grandParent);
                // current node now becomes the child of greatgrandparent
                parent = greatGrandParent;
            }
            // grand parent will become a child of either parent of current.
            grandParent.IsRed = true;
            newChildOfGreatGrandParent.IsRed = false;

            ReplaceChildOfNodeOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);
        }

        private static bool Is2Node(Node node)
        {
            Debug.Assert(node != null);
            return IsBlack(node) && IsNullOrBlack(node.Left) && IsNullOrBlack(node.Right);
        }

        private static bool Is4Node(Node node) => IsRed(node.Left) && IsRed(node.Right);

        private static bool IsBlack(Node node) => node != null && !node.IsRed;

        private static bool IsNullOrBlack(Node node) => node == null || !node.IsRed;

        private static bool IsRed(Node node) => node != null && node.IsRed;

        private static void Merge2Nodes(Node parent, Node child1, Node child2)
        {
            Debug.Assert(IsRed(parent));
            // Combine two 2-nodes into a 4-node
            parent.IsRed = false;
            child1.IsRed = true;
            child2.IsRed = true;
        }

        // Replace the child of a parent node.
        // If the parent node is null, replace the root.
        private void ReplaceChildOfNodeOrRoot(Node parent, Node child, Node newChild)
        {
            if (parent != null)
            {
                if (parent.Left == child)
                {
                    parent.Left = newChild;
                }
                else
                {
                    parent.Right = newChild;
                }
            }
            else
            {
                _root = newChild;
            }
        }

        // Replace the matching node with its successor.
        private void ReplaceNode(Node match, Node parentOfMatch, Node successor, Node parentOfSuccessor)
        {
            if (successor == match)
            {
                // this node has no successor, should only happen if right child of matching node is null.
                Debug.Assert(match.Right == null);
                successor = match.Left;
            }
            else
            {
                Debug.Assert(parentOfSuccessor != null);
                Debug.Assert(successor.Left == null);
                Debug.Assert((successor.Right == null && successor.IsRed) || (successor.Right.IsRed && !successor.IsRed));

                if (successor.Right != null)
                {
                    successor.Right.IsRed = false;
                }

                if (parentOfSuccessor != match)
                {
                    // detach successor from its parent and set its right child
                    parentOfSuccessor.Left = successor.Right;
                    successor.Right = match.Right;
                }

                successor.Left = match.Left;
            }

            if (successor != null)
            {
                successor.IsRed = match.IsRed;
            }

            ReplaceChildOfNodeOrRoot(parentOfMatch, match, successor);
        }

        internal virtual Node FindNode(T item)
        {
            Node current = _root;
            while (current != null)
            {
                int order = _comparer.Compare(item, current.Item);
                if (order == 0)
                {
                    return current;
                }

                current = (order < 0) ? current.Left : current.Right;
            }

            return null;
        }

        // Used for bithelpers. Note that this implementation is completely different
        // from the Subset's. The two should not be mixed. This indexes as if the tree were an array.
        // http://en.wikipedia.org/wiki/Binary_Tree#Methods_for_storing_binary_trees
        internal virtual int InternalIndexOf(T item)
        {
            Node current = _root;
            int count = 0;
            while (current != null)
            {
                int order = _comparer.Compare(item, current.Item);
                if (order == 0)
                {
                    return count;
                }

                current = (order < 0) ? current.Left : current.Right;
                count = (order < 0) ? (2 * count + 1) : (2 * count + 2);
            }

            return -1;
        }

        internal Node FindRange(T from, T to) => FindRange(from, to, lowerBoundActive: true, upperBoundActive: true);

        internal Node FindRange(T from, T to, bool lowerBoundActive, bool upperBoundActive)
        {
            Node current = _root;
            while (current != null)
            {
                if (lowerBoundActive && _comparer.Compare(from, current.Item) > 0)
                {
                    current = current.Right;
                }
                else
                {
                    if (upperBoundActive && _comparer.Compare(to, current.Item) < 0)
                    {
                        current = current.Left;
                    }
                    else
                    {
                        return current;
                    }
                }
            }

            return null;
        }

        internal void UpdateVersion() => ++_version;

        private static Node RotateLeft(Node node)
        {
            Node x = node.Right;
            node.Right = x.Left;
            x.Left = node;
            return x;
        }

        private static Node RotateLeftRight(Node node)
        {
            Node child = node.Left;
            Node grandChild = child.Right;

            node.Left = grandChild.Right;
            grandChild.Right = node;
            child.Right = grandChild.Left;
            grandChild.Left = child;
            return grandChild;
        }

        private static Node RotateRight(Node node)
        {
            Node x = node.Left;
            node.Left = x.Right;
            x.Right = node;
            return x;
        }

        private static Node RotateRightLeft(Node node)
        {
            Node child = node.Right;
            Node grandChild = child.Left;

            node.Right = grandChild.Left;
            grandChild.Left = node;
            child.Left = grandChild.Right;
            grandChild.Right = child;
            return grandChild;
        }

        /// <summary>
        /// Testing counter that can track rotations.
        /// </summary>
        private static TreeRotation RotationNeeded(Node parent, Node current, Node sibling)
        {
            Debug.Assert(IsRed(sibling.Left) || IsRed(sibling.Right));
            bool currentIsLeft = parent.Left == current;
            return IsRed(sibling.Left) ?
                (currentIsLeft ? TreeRotation.RightLeft : TreeRotation.Right) :
                (currentIsLeft ? TreeRotation.Left : TreeRotation.LeftRight);
        }

        /// <summary>
        /// Used for deep equality of SortedSet testing.
        /// </summary>
        public static IEqualityComparer<SortedSet<T>> CreateSetComparer() => CreateSetComparer(memberEqualityComparer: null);

        /// <summary>
        /// Creates a new set comparer for this set, where this set's members' equality is defined by the specified comparer.
        /// </summary>
        public static IEqualityComparer<SortedSet<T>> CreateSetComparer(IEqualityComparer<T> memberEqualityComparer)
        {
            return new SortedSetEqualityComparer<T>(memberEqualityComparer);
        }

        /// <summary>
        /// Decides whether two sets have equal contents, using a fallback comparer if the sets do not have equivalent equality comparers.
        /// </summary>
        internal static bool SortedSetEquals(SortedSet<T> set1, SortedSet<T> set2, IComparer<T> comparer)
        {
            // handle null cases first
            if (set1 == null)
            {
                return (set2 == null);
            }
            else if (set2 == null)
            {
                // set1 != null
                return false;
            }

            if (AreComparersEqual(set1, set2))
            {
                if (set1.Count != set2.Count)
                    return false;

                return set1.SetEquals(set2);
            }
            else
            {
                bool found = false;
                foreach (T item1 in set1)
                {
                    found = false;
                    foreach (T item2 in set2)
                    {
                        if (comparer.Compare(item1, item2) == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        return false;
                }
                return true;
            }

        }

        // This is a little frustrating because we can't support more sorted structures.
        private static bool AreComparersEqual(SortedSet<T> set1, SortedSet<T> set2)
        {
            return set1.Comparer.Equals(set2.Comparer);
        }

        private static void Split4Node(Node node)
        {
            node.IsRed = true;
            node.Left.IsRed = false;
            node.Right.IsRed = false;
        }

        #endregion

        #region ISet members

        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            SortedSet<T> s = other as SortedSet<T>;
            TreeSubSet t = this as TreeSubSet;

            if (t != null)
                VersionCheck();

            if (s != null && t == null && _count == 0)
            {
                SortedSet<T> dummy = new SortedSet<T>(s, _comparer);
                _root = dummy._root;
                _count = dummy._count;
                _version++;
                return;
            }

            if (s != null && t == null && AreComparersEqual(this, s) && (s.Count > this.Count / 2))
            {
                // This actually hurts if N is much greater than M. The /2 is arbitrary.
                // First do a merge sort to an array.
                T[] merged = new T[s.Count + this.Count];
                int c = 0;
                Enumerator mine = this.GetEnumerator();
                Enumerator theirs = s.GetEnumerator();
                bool mineEnded = !mine.MoveNext(), theirsEnded = !theirs.MoveNext();
                while (!mineEnded && !theirsEnded)
                {
                    int comp = Comparer.Compare(mine.Current, theirs.Current);
                    if (comp < 0)
                    {
                        merged[c++] = mine.Current;
                        mineEnded = !mine.MoveNext();
                    }
                    else if (comp == 0)
                    {
                        merged[c++] = theirs.Current;
                        mineEnded = !mine.MoveNext();
                        theirsEnded = !theirs.MoveNext();
                    }
                    else
                    {
                        merged[c++] = theirs.Current;
                        theirsEnded = !theirs.MoveNext();
                    }
                }

                if (!mineEnded || !theirsEnded)
                {
                    Enumerator remaining = (mineEnded ? theirs : mine);
                    do
                    {
                        merged[c++] = remaining.Current;
                    } while (remaining.MoveNext());
                }

                // now merged has all c elements

                // safe to gc the root, we  have all the elements
                _root = null;

                _root = SortedSet<T>.ConstructRootFromSortedArray(merged, 0, c - 1, null);
                _count = c;
                _version++;
            }
            else
            {
                AddAllElements(other);
            }
        }

        private static Node ConstructRootFromSortedArray(T[] arr, int startIndex, int endIndex, Node redNode)
        {
            // You're given a sorted array... say 1 2 3 4 5 6
            // There are 2 cases:
            // -  If there are odd # of elements, pick the middle element (in this case 4), and compute
            //    its left and right branches
            // -  If there are even # of elements, pick the left middle element, save the right middle element
            //    and call the function on the rest
            //    1 2 3 4 5 6 -> pick 3, save 4 and call the fn on 1,2 and 5,6
            //    now add 4 as a red node to the lowest element on the right branch
            //             3                       3
            //         1       5       ->     1        5
            //           2       6             2     4   6
            //    As we're adding to the leftmost of the right branch, nesting will not hurt the red-black properties
            //    Leaf nodes are red if they have no sibling (if there are 2 nodes or if a node trickles
            //    down to the bottom

            // This is done recursively because the iterative way to do this ends up wasting more space than it saves in stack frames
            // Only some base cases are handled below.

            int size = endIndex - startIndex + 1;
            if (size == 0)
            {
                return null;
            }

            Node root = null;
            if (size == 1)
            {
                root = new Node(arr[startIndex], isRed: false);
                if (redNode != null)
                {
                    root.Left = redNode;
                }
            }
            else if (size == 2)
            {
                root = new Node(arr[startIndex], isRed: false);
                root.Right = new Node(arr[endIndex], isRed: false);
                root.Right.IsRed = true;
                if (redNode != null)
                {
                    root.Left = redNode;
                }
            }
            else if (size == 3)
            {
                root = new Node(arr[startIndex + 1], isRed: false);
                root.Left = new Node(arr[startIndex], isRed: false);
                root.Right = new Node(arr[endIndex], isRed: false);
                if (redNode != null)
                {
                    root.Left.Left = redNode;
                }
            }
            else
            {
                int midpt = ((startIndex + endIndex) / 2);
                root = new Node(arr[midpt], isRed: false);
                root.Left = ConstructRootFromSortedArray(arr, startIndex, midpt - 1, redNode);
                root.Right = size % 2 == 0 ?
                    ConstructRootFromSortedArray(arr, midpt + 2, endIndex, new Node(arr[midpt + 1])) :
                    ConstructRootFromSortedArray(arr, midpt + 1, endIndex, null);
            }
            return root;
        }

        public virtual void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
                return;

            // HashSet<T> optimizations can't be done until equality comparers and comparers are related

            // Technically, this would work as well with an ISorted<T>
            SortedSet<T> s = other as SortedSet<T>;
            TreeSubSet t = this as TreeSubSet;
            if (t != null)
                VersionCheck();
            // only let this happen if i am also a SortedSet, not a SubSet
            if (s != null && t == null && AreComparersEqual(this, s))
            {
                // first do a merge sort to an array.
                T[] merged = new T[this.Count];
                int c = 0;
                Enumerator mine = this.GetEnumerator();
                Enumerator theirs = s.GetEnumerator();
                bool mineEnded = !mine.MoveNext(), theirsEnded = !theirs.MoveNext();
                T max = Max;
                T min = Min;

                while (!mineEnded && !theirsEnded && Comparer.Compare(theirs.Current, max) <= 0)
                {
                    int comp = Comparer.Compare(mine.Current, theirs.Current);
                    if (comp < 0)
                    {
                        mineEnded = !mine.MoveNext();
                    }
                    else if (comp == 0)
                    {
                        merged[c++] = theirs.Current;
                        mineEnded = !mine.MoveNext();
                        theirsEnded = !theirs.MoveNext();
                    }
                    else
                    {
                        theirsEnded = !theirs.MoveNext();
                    }
                }

                // now merged has all c elements

                // safe to gc the root, we  have all the elements
                _root = null;

                _root = SortedSet<T>.ConstructRootFromSortedArray(merged, 0, c - 1, null);
                _count = c;
                _version++;
            }
            else
            {
                IntersectWithEnumerable(other);
            }
        }

        internal virtual void IntersectWithEnumerable(IEnumerable<T> other)
        {
            // TODO: Perhaps a more space-conservative way to do this
            List<T> toSave = new List<T>(Count);
            foreach (T item in other)
            {
                if (Contains(item))
                {
                    toSave.Add(item);
                }
            }

            if (toSave.Count < Count)
            {
                Clear();
                AddAllElements(toSave);
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (_count == 0)
                return;

            if (other == this)
            {
                Clear();
                return;
            }

            SortedSet<T> asSorted = other as SortedSet<T>;

            if (asSorted != null && AreComparersEqual(this, asSorted))
            {
                // outside range, no point doing anything
                if (!(_comparer.Compare(asSorted.Max, Min) < 0 || _comparer.Compare(asSorted.Min, Max) > 0))
                {
                    T min = Min;
                    T max = Max;
                    foreach (T item in other)
                    {
                        if (_comparer.Compare(item, min) < 0)
                            continue;
                        if (_comparer.Compare(item, max) > 0)
                            break;
                        Remove(item);
                    }
                }
            }
            else
            {
                RemoveAllElements(other);
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
            {
                UnionWith(other);
                return;
            }

            if (other == this)
            {
                Clear();
                return;
            }

            SortedSet<T> asSorted = other as SortedSet<T>;

            if (asSorted != null && AreComparersEqual(this, asSorted))
            {
                SymmetricExceptWithSameComparer(asSorted);
            }
            else
            {
                int length;
                T[] elements = EnumerableHelpers.ToArray(other, out length);
                Array.Sort(elements, 0, length, Comparer);
                SymmetricExceptWithSameComparer(elements, length);
            }
        }

        private void SymmetricExceptWithSameComparer(SortedSet<T> other)
        {
            Debug.Assert(other != null);
            Debug.Assert(AreComparersEqual(this, other));

            foreach (T item in other)
            {
                bool result = Contains(item) ? Remove(item) : Add(item);
                Debug.Assert(result);
            }
        }

        private void SymmetricExceptWithSameComparer(T[] other, int count)
        {
            Debug.Assert(other != null);
            Debug.Assert(count >= 0 && count <= other.Length);

            if (count == 0)
            {
                return;
            }

            T previous = other[0];
            for (int i = 0; i < count; i++)
            {
                while (i < count && i != 0 && _comparer.Compare(other[i], previous) == 0)
                    i++;
                if (i >= count)
                    break;
                T current = other[i];
                bool result = Contains(current) ? Remove(current) : Add(current);
                Debug.Assert(result);
                previous = current;
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
            {
                return true;
            }

            SortedSet<T> asSorted = other as SortedSet<T>;
            if (asSorted != null && AreComparersEqual(this, asSorted))
            {
                if (Count > asSorted.Count)
                    return false;
                return IsSubsetOfSortedSetWithSameComparer(asSorted);
            }
            else
            {
                // worst case: mark every element in my set and see if I've counted all
                // O(MlogN)

                ElementCount result = CheckUniqueAndUnfoundElements(other, false);
                return (result.UniqueCount == Count && result.UnfoundCount >= 0);
            }
        }

        private bool IsSubsetOfSortedSetWithSameComparer(SortedSet<T> asSorted)
        {
            SortedSet<T> prunedOther = asSorted.GetViewBetween(Min, Max);
            foreach (T item in this)
            {
                if (!prunedOther.Contains(item))
                    return false;
            }
            return true;
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if ((other as ICollection) != null)
            {
                if (Count == 0)
                    return (other as ICollection).Count > 0;
            }

            // another for sorted sets with the same comparer
            SortedSet<T> asSorted = other as SortedSet<T>;
            if (asSorted != null && AreComparersEqual(this, asSorted))
            {
                if (Count >= asSorted.Count)
                    return false;
                return IsSubsetOfSortedSetWithSameComparer(asSorted);
            }

            // worst case: mark every element in my set and see if I've counted all
            // O(MlogN).
            ElementCount result = CheckUniqueAndUnfoundElements(other, false);
            return (result.UniqueCount == Count && result.UnfoundCount > 0);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if ((other as ICollection) != null && (other as ICollection).Count == 0)
                return true;

            // do it one way for HashSets
            // another for sorted sets with the same comparer
            SortedSet<T> asSorted = other as SortedSet<T>;
            if (asSorted != null && AreComparersEqual(this, asSorted))
            {
                if (Count < asSorted.Count)
                    return false;
                SortedSet<T> pruned = GetViewBetween(asSorted.Min, asSorted.Max);
                foreach (T item in asSorted)
                {
                    if (!pruned.Contains(item))
                        return false;
                }
                return true;
            }

            // and a third for everything else
            return ContainsAllElements(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
                return false;

            if ((other as ICollection) != null && (other as ICollection).Count == 0)
                return true;

            // another way for sorted sets
            SortedSet<T> asSorted = other as SortedSet<T>;
            if (asSorted != null && AreComparersEqual(asSorted, this))
            {
                if (asSorted.Count >= Count)
                    return false;
                SortedSet<T> pruned = GetViewBetween(asSorted.Min, asSorted.Max);
                foreach (T item in asSorted)
                {
                    if (!pruned.Contains(item))
                        return false;
                }
                return true;
            }

            // worst case: mark every element in my set and see if I've counted all
            // O(MlogN)
            // slight optimization, put it into a HashSet and then check can do it in O(N+M)
            // but slower in better cases + wastes space
            ElementCount result = CheckUniqueAndUnfoundElements(other, true);
            return (result.UniqueCount < Count && result.UnfoundCount == 0);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            SortedSet<T> asSorted = other as SortedSet<T>;
            if (asSorted != null && AreComparersEqual(this, asSorted))
            {
                Enumerator mine = GetEnumerator();
                Enumerator theirs = asSorted.GetEnumerator();
                bool mineEnded = !mine.MoveNext();
                bool theirsEnded = !theirs.MoveNext();
                while (!mineEnded && !theirsEnded)
                {
                    if (Comparer.Compare(mine.Current, theirs.Current) != 0)
                    {
                        return false;
                    }
                    mineEnded = !mine.MoveNext();
                    theirsEnded = !theirs.MoveNext();
                }
                return mineEnded && theirsEnded;
            }

            // worst case: mark every element in my set and see if I've counted all
            // O(N) by size of other
            ElementCount result = CheckUniqueAndUnfoundElements(other, true);
            return (result.UniqueCount == Count && result.UnfoundCount == 0);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
                return false;

            if ((other as ICollection<T> != null) && (other as ICollection<T>).Count == 0)
                return false;

            SortedSet<T> asSorted = other as SortedSet<T>;
            if (asSorted != null && AreComparersEqual(this, asSorted) && (_comparer.Compare(Min, asSorted.Max) > 0 || _comparer.Compare(Max, asSorted.Min) < 0))
            {
                return false;
            }

            foreach (T item in other)
            {
                if (Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This works similar to HashSet's CheckUniqueAndUnfound (description below), except that the bit
        /// array maps differently than in the HashSet. We can only use this for the bulk boolean checks.
        ///
        /// Determines counts that can be used to determine equality, subset, and superset. This
        /// is only used when other is an IEnumerable and not a HashSet. If other is a HashSet
        /// these properties can be checked faster without use of marking because we can assume
        /// other has no duplicates.
        ///
        /// The following count checks are performed by callers:
        /// 1. Equals: checks if UnfoundCount = 0 and uniqueFoundCount = Count; i.e. everything
        /// in other is in this and everything in this is in other
        /// 2. Subset: checks if UnfoundCount >= 0 and uniqueFoundCount = Count; i.e. other may
        /// have elements not in this and everything in this is in other
        /// 3. Proper subset: checks if UnfoundCount > 0 and uniqueFoundCount = Count; i.e
        /// other must have at least one element not in this and everything in this is in other
        /// 4. Proper superset: checks if unfound count = 0 and uniqueFoundCount strictly less
        /// than Count; i.e. everything in other was in this and this had at least one element
        /// not contained in other.
        ///
        /// An earlier implementation used delegates to perform these checks rather than returning
        /// an ElementCount struct; however this was changed due to the perf overhead of delegates.
        /// </summary>
        private unsafe ElementCount CheckUniqueAndUnfoundElements(IEnumerable<T> other, bool returnIfUnfound)
        {
            ElementCount result;

            // need special case in case this has no elements.
            if (Count == 0)
            {
                int numElementsInOther = 0;
                foreach (T item in other)
                {
                    numElementsInOther++;
                    // break right away, all we want to know is whether other has 0 or 1 elements
                    break;
                }
                result.UniqueCount = 0;
                result.UnfoundCount = numElementsInOther;
                return result;
            }

            int originalLastIndex = Count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalLastIndex);

            BitHelper bitHelper;
            if (intArrayLength <= StackAllocThreshold)
            {
                int* bitArrayPtr = stackalloc int[intArrayLength];
                bitHelper = new BitHelper(bitArrayPtr, intArrayLength);
            }
            else
            {
                int[] bitArray = new int[intArrayLength];
                bitHelper = new BitHelper(bitArray, intArrayLength);
            }

            // count of items in other not found in this
            int UnfoundCount = 0;
            // count of unique items in other found in this
            int uniqueFoundCount = 0;

            foreach (T item in other)
            {
                int index = InternalIndexOf(item);
                if (index >= 0)
                {
                    if (!bitHelper.IsMarked(index))
                    {
                        // item hasn't been seen yet
                        bitHelper.MarkBit(index);
                        uniqueFoundCount++;
                    }
                }
                else
                {
                    UnfoundCount++;
                    if (returnIfUnfound)
                    {
                        break;
                    }
                }
            }

            result.UniqueCount = uniqueFoundCount;
            result.UnfoundCount = UnfoundCount;
            return result;
        }

        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            List<T> matches = new List<T>(this.Count);

            BreadthFirstTreeWalk(n =>
            {
                if (match(n.Item))
                {
                    matches.Add(n.Item);
                }
                return true;
            });

            // reverse breadth first to (try to) incur low cost
            int actuallyRemoved = 0;
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                if (Remove(matches[i]))
                {
                    actuallyRemoved++;
                }
            }

            return actuallyRemoved;
        }

        #endregion

        #region ISorted members

        public T Min
        {
            get
            {
                if (_root == null)
                {
                    return default(T);
                }

                Node current = _root;
                while (current.Left != null)
                {
                    current = current.Left;
                }

                return current.Item;
            }
        }

        public T Max
        {
            get
            {
                if (_root == null)
                {
                    return default(T);
                }

                Node current = _root;
                while (current.Right != null)
                {
                    current = current.Right;
                }

                return current.Item;
            }
        }

        public IEnumerable<T> Reverse()
        {
            Enumerator e = new Enumerator(this, reverse: true);
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }

        public virtual SortedSet<T> GetViewBetween(T lowerValue, T upperValue)
        {
            if (Comparer.Compare(lowerValue, upperValue) > 0)
            {
                throw new ArgumentException(SR.SortedSet_LowerValueGreaterThanUpperValue, nameof(lowerValue));
            }
            return new TreeSubSet(this, lowerValue, upperValue, true, true);
        }

#if DEBUG
        /// <summary>
        /// debug status to be checked whenever any operation is called
        /// </summary>
        /// <returns></returns>
        internal virtual bool versionUpToDate()
        {
            return true;
        }
#endif

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            GetObjectData(info, context);
        }

        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(CountName, _count); // This is the length of the bucket array.
            info.AddValue(ComparerName, _comparer, typeof(IComparer<T>));
            info.AddValue(VersionName, _version);

            if (_root != null)
            {
                T[] items = new T[Count];
                CopyTo(items, 0);
                info.AddValue(ItemsName, items, typeof(T[]));
            }
        }

        void IDeserializationCallback.OnDeserialization(Object sender)
        {
            OnDeserialization(sender);
        }

        protected virtual void OnDeserialization(Object sender)
        {
            if (_comparer != null)
            {
                return; // Somebody had a dependency on this class and fixed us up before the ObjectManager got to it.
            }

            if (_siInfo == null)
            {
                throw new SerializationException(SR.Serialization_InvalidOnDeser);
            }

            _comparer = (IComparer<T>)_siInfo.GetValue(ComparerName, typeof(IComparer<T>));
            int savedCount = _siInfo.GetInt32(CountName);

            if (savedCount != 0)
            {
                T[] items = (T[])_siInfo.GetValue(ItemsName, typeof(T[]));

                if (items == null)
                {
                    throw new SerializationException(SR.Serialization_MissingValues);
                }

                for (int i = 0; i < items.Length; i++)
                {
                    Add(items[i]);
                }
            }

            _version = _siInfo.GetInt32(VersionName);
            if (_count != savedCount)
            {
                throw new SerializationException(SR.Serialization_MismatchedCount);
            }

            _siInfo = null;
        }

        #endregion

        #region Helper classes

        [Serializable]
        internal sealed class Node
        {
            public Node(T item, bool isRed = true)
            {
                // The default color is red since we usually don't need to create a black node directly.
                Item = item;
                IsRed = isRed;
            }

            public T Item { get; set; }

            public Node Left { get; set; }

            public Node Right { get; set; }

            public bool IsRed { get; set; }
        }

        [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "not an expected scenario")]
        [Serializable]
        public struct Enumerator : IEnumerator<T>, IEnumerator, ISerializable, IDeserializationCallback
        {
            private static SortedSet<T>.Node s_dummyNode = new SortedSet<T>.Node(default(T));

            private SortedSet<T> _tree;
            private int _version;

            private Stack<SortedSet<T>.Node> _stack;
            private SortedSet<T>.Node _current;

            private bool _reverse;
            private SerializationInfo _siInfo;

            internal Enumerator(SortedSet<T> set)
            {
                _tree = set;
                _tree.VersionCheck(); // make sure that the underlying subset has not been changed since

                _version = _tree._version;

                // 2lg(n + 1) is the maximum height
                _stack = new Stack<SortedSet<T>.Node>(2 * (int)SortedSet<T>.Log2(set.Count + 1));
                _current = null;
                _reverse = false;

                _siInfo = null;

                Intialize();
            }

            internal Enumerator(SortedSet<T> set, bool reverse)
            {
                _tree = set;
                _tree.VersionCheck(); // make sure that the underlying subset has not been changed since
                _version = _tree._version;

                // 2lg(n + 1) is the maximum height
                _stack = new Stack<SortedSet<T>.Node>(2 * (int)SortedSet<T>.Log2(set.Count + 1));
                _current = null;
                _reverse = reverse;

                _siInfo = null;

                Intialize();
            }

            private Enumerator(SerializationInfo info, StreamingContext context)
            {
                _tree = null;
                _version = -1;
                _current = null;
                _reverse = false;
                _stack = null;
                _siInfo = info;
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                GetObjectData(info, context);
            }

            private void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException(nameof(info));
                }

                info.AddValue(TreeName, _tree, typeof(SortedSet<T>));
                info.AddValue(EnumVersionName, _version);
                info.AddValue(ReverseName, _reverse);
                info.AddValue(EnumStartName, !NotStartedOrEnded);
                info.AddValue(NodeValueName, (_current == null ? s_dummyNode.Item : _current.Item), typeof(T));
            }

            void IDeserializationCallback.OnDeserialization(Object sender)
            {
                OnDeserialization(sender);
            }

            private void OnDeserialization(Object sender)
            {
                if (_siInfo == null)
                {
                    throw new SerializationException(SR.Serialization_InvalidOnDeser);
                }

                _tree = (SortedSet<T>)_siInfo.GetValue(TreeName, typeof(SortedSet<T>));
                _version = _siInfo.GetInt32(EnumVersionName);
                _reverse = _siInfo.GetBoolean(ReverseName);
                bool EnumStarted = _siInfo.GetBoolean(EnumStartName);
                _stack = new Stack<SortedSet<T>.Node>(2 * (int)SortedSet<T>.Log2(_tree.Count + 1));
                _current = null;
                if (EnumStarted)
                {
                    T item = (T)_siInfo.GetValue(NodeValueName, typeof(T));
                    Intialize();

                    // go until it reaches the value we want
                    while (this.MoveNext())
                    {
                        if (_tree.Comparer.Compare(Current, item) == 0)
                            break;
                    }
                }
            }

            private void Intialize()
            {
                _current = null;
                SortedSet<T>.Node node = _tree._root;
                Node next = null, other = null;
                while (node != null)
                {
                    next = (_reverse ? node.Right : node.Left);
                    other = (_reverse ? node.Left : node.Right);
                    if (_tree.IsWithinRange(node.Item))
                    {
                        _stack.Push(node);
                        node = next;
                    }
                    else if (next == null || !_tree.IsWithinRange(next.Item))
                    {
                        node = other;
                    }
                    else
                    {
                        node = next;
                    }
                }
            }

            public bool MoveNext()
            {
                // Make sure that the underlying subset has not been changed since
                _tree.VersionCheck();

                if (_version != _tree._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                if (_stack.Count == 0)
                {
                    _current = null;
                    return false;
                }

                _current = _stack.Pop();
                SortedSet<T>.Node node = (_reverse ? _current.Left : _current.Right);
                Node next = null, other = null;
                while (node != null)
                {
                    next = (_reverse ? node.Right : node.Left);
                    other = (_reverse ? node.Left : node.Right);
                    if (_tree.IsWithinRange(node.Item))
                    {
                        _stack.Push(node);
                        node = next;
                    }
                    else if (other == null || !_tree.IsWithinRange(other.Item))
                    {
                        node = next;
                    }
                    else
                    {
                        node = other;
                    }
                }
                return true;
            }

            public void Dispose() { }

            public T Current
            {
                get
                {
                    if (_current != null)
                    {
                        return _current.Item;
                    }
                    return default(T);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }

                    return _current.Item;
                }
            }

            internal bool NotStartedOrEnded => _current == null;

            internal void Reset()
            {
                if (_version != _tree._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                _stack.Clear();
                Intialize();
            }

            void IEnumerator.Reset() => Reset();
        }

        internal struct ElementCount
        {
            internal int UniqueCount;
            internal int UnfoundCount;
        }

        #endregion

        #region Miscellaneous

        // Used for set checking operations (using enumerables) that rely on counting
        private static int Log2(int value)
        {
            int result = 0;
            while (value > 0)
            {
                result++;
                value >>= 1;
            }
            return result;
        }

        #endregion
    }
}
