// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.Speech.Internal
{
    /// <summary>
    /// Sorted List using the Red-Black algorithm
    /// </summary>
    internal abstract class RBList : IEnumerable
    {
        #region Constructors

        internal RBList()
        {
        }

        #endregion

        #region Internal Methods

        internal void Add(object key)
        {
#if DEBUG
            if (_root != null && _root._inEnumaration)
            {
                throw new InvalidOperationException();
            }
#endif
            TreeNode node = new(key);
            node.IsRed = true;
            InsertNode(_root, node);
            FixUpInsertion(node);

            _root = FindRoot(node);
        }

        internal void Remove(object key)
        {
#if DEBUG
            if (_root != null && _root._inEnumaration)
            {
                throw new InvalidOperationException();
            }
#endif
            TreeNode node = FindItem(_root, key);
            if (node == null)
            {
                throw new KeyNotFoundException();
            }
            TreeNode nodeRemoved = DeleteNode(node);
            FixUpRemoval(nodeRemoved);

            if (nodeRemoved == _root)
            {
                if (_root.Left != null)
                {
                    _root = FindRoot(_root.Left);
                }
                else if (_root.Right != null)
                {
                    _root = FindRoot(_root.Right);
                }
                else
                {
                    _root = null;
                }
            }
            else
            {
                _root = FindRoot(_root);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new MyEnumerator(_root);
        }

        #endregion

        #region Internal Properties

        internal bool IsEmpty
        {
            get
            {
                return _root == null;
            }
        }

        internal bool CountIsOne
        {
            get
            {
                return _root != null && _root.Left == null && _root.Right == null;
            }
        }

        internal bool ContainsMoreThanOneItem
        {
            get
            {
                return _root != null && (_root.Right != null || _root.Left != null);
            }
        }

        internal object First
        {
            get
            {
                if (_root == null)
                {
                    // We don't expect First to be called on empty graphs
                    System.Diagnostics.Debug.Assert(false);
                    return null;
                }
                // Set the current pointer to the last element
                return FindMinSubTree(_root).Key;
            }
        }

        #endregion

        #region Protected Methods

        protected abstract int CompareTo(object object1, object object2);

        #endregion

        #region Private Methods

        #region Implement utility operations on Tree

        private static TreeNode GetUncle(TreeNode node)
        {
            if (node.Parent == node.Parent.Parent.Left)
            {
                return node.Parent.Parent.Right;
            }
            else
            {
                return node.Parent.Parent.Left;
            }
        }

        private static TreeNode GetSibling(TreeNode node, TreeNode parent)
        {
            if (node == parent.Left)
            {
                return parent.Right;
            }
            else
            {
                return parent.Left;
            }
        }

        private static NodeColor GetColor(TreeNode node)
        {
            return node == null ? NodeColor.BLACK : (node.IsRed ? NodeColor.RED : NodeColor.BLACK);
        }

        private static void SetColor(TreeNode node, NodeColor color)
        {
            if (node != null)
            {
                node.IsRed = (color == NodeColor.RED);
            }
            else
            {
                Debug.Assert(color == NodeColor.BLACK);
            }
        }

        private static void TakeParent(TreeNode node, TreeNode newNode)
        {
            if (node.Parent == null)
            {
                if (newNode != null)
                {
                    newNode.Parent = null;
                }
            }
            else if (node.Parent.Left == node)
            {
                node.Parent.Left = newNode;
            }
            else if (node.Parent.Right == node)
            {
                node.Parent.Right = newNode;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static TreeNode RotateLeft(TreeNode node)
        {
            TreeNode newNode = node.Right;
            node.Right = newNode.Left;
            TakeParent(node, newNode);
            newNode.Left = node;
            return newNode;
        }

        private static TreeNode RotateRight(TreeNode node)
        {
            TreeNode newNode = node.Left;
            node.Left = newNode.Right;
            TakeParent(node, newNode);
            newNode.Right = node;

            return newNode;
        }

        private static TreeNode FindMinSubTree(TreeNode node)
        {
            while (node.Left != null)
            {
                node = node.Left;
            }
            return node;
        }

        private static TreeNode FindSuccessor(TreeNode node)
        {
            if (node.Right == null)
            {
                while (node.Parent != null && node.Parent.Left != node)
                {
                    node = node.Parent;
                }

                return node.Parent ?? null;
            }
            else
            {
                return FindMinSubTree(node.Right);
            }
        }

        // Return the actual node that is deleted
        private static TreeNode DeleteNode(TreeNode node)
        {
            if (node.Right == null)
            {
                TakeParent(node, node.Left);

                return node;
            }
            else if (node.Left == null)
            {
                TakeParent(node, node.Right);

                return node;
            }
            else
            {
                TreeNode successor = FindSuccessor(node);
                Debug.Assert(successor != null && successor.Left == null);
                node.CopyNode(successor);
                TakeParent(successor, successor.Right);
                return successor;
            }
        }

        #endregion Implement utility operations on Tree

        // Return the root of the new subtree
        private TreeNode InsertNode(TreeNode node, TreeNode newNode)
        {
            if (node == null)
            {
                return newNode;
            }

            int diff = CompareTo(newNode.Key, node.Key);

            if (diff < 0)
            {
                node.Left = InsertNode(node.Left, newNode);
            }
            else
            {
                node.Right = InsertNode(node.Right, newNode);
            }

            return node;
        }

        private TreeNode FindItem(TreeNode node, object key)
        {
            if (node == null)
            {
                return null;
            }
            int diff = CompareTo(key, node.Key);
            if (diff == 0)
            {
                return node;
            }
            else if (diff < 0)
            {
                return FindItem(node.Left, key);
            }
            else
            {
                return FindItem(node.Right, key);
            }
        }

        private TreeNode FindRoot(TreeNode node)
        {
            while (node.Parent != null)
            {
                node = node.Parent;
            }
            return node;
        }

        private void FixUpInsertion(TreeNode node)
        {
            FixInsertCase1(node);
        }

        private void FixInsertCase1(TreeNode node)
        {
            Debug.Assert(node.IsRed);

            if (node.Parent == null)
            {
                node.IsRed = false;
            }
            else
            {
                FixInsertCase2(node);
            }
        }
        private void FixInsertCase2(TreeNode node)
        {
            if (GetColor(node.Parent) == NodeColor.BLACK)
            {
                return; // Tree is still valid.
            }

            // Now, its parent is RED, so it must have an uncle since its parent is not root.
            // Also, its grandparent must be BLACK.
            Debug.Assert(GetColor(node.Parent.Parent) == NodeColor.BLACK);
            TreeNode uncle = GetUncle(node);

            if (GetColor(uncle) == NodeColor.RED)
            {
                SetColor(node.Parent, NodeColor.BLACK);
                SetColor(uncle, NodeColor.BLACK);
                SetColor(node.Parent.Parent, NodeColor.RED);
                FixInsertCase1(node.Parent.Parent); // Move recursively up
            }
            else
            {
                FixInsertCase3(node);
            }
        }

        private void FixInsertCase3(TreeNode node)
        {
            //
            // Now it's RED, parent is RED, uncle is BLACK,
            // We want to rotate so that its uncle is on the opposite side
            if (node == node.Parent.Right && node.Parent == node.Parent.Parent.Left)
            {
                RotateLeft(node.Parent);
                node = node.Left;
            }
            else if (node == node.Parent.Left && node.Parent == node.Parent.Parent.Right)
            {
                RotateRight(node.Parent);
                node = node.Right;
            }
            FixInsertCase4(node);
        }

        private void FixInsertCase4(TreeNode node)
        {
            //
            // Must follow case 3, here we are finally done!
            //

            SetColor(node.Parent, NodeColor.BLACK);
            SetColor(node.Parent.Parent, NodeColor.RED);
            if (node == node.Parent.Left)
            {
                Debug.Assert(node.Parent == node.Parent.Parent.Left); // From case 3
                RotateRight(node.Parent.Parent);
            }
            else
            {
                Debug.Assert(node.Parent == node.Parent.Parent.Right); // From case 3
                RotateLeft(node.Parent.Parent);
            }
        }

        private static void FixUpRemoval(TreeNode node)
        {
            // This node must have at most 1 child
            Debug.Assert(node.Left == null || node.Right == null);

            TreeNode onlyChild = node.Left ?? node.Right;

            // This node should have been deleted already, and the child has replaced the this node.
            Debug.Assert(node.Parent == null || node.Parent.Left == onlyChild || node.Parent.Right == onlyChild);
            Debug.Assert(onlyChild == null || onlyChild.Parent == node.Parent);

            //
            // If the node removed was red, all properties still hold.
            // Otherwise, we need fix up.
            //

            if (GetColor(node) == NodeColor.BLACK)
            {
                if (GetColor(onlyChild) == NodeColor.RED)
                {
                    SetColor(onlyChild, NodeColor.BLACK);
                }
                else if (node.Parent == null)  // if we remove a root node, nothing has changed.
                {
                    return;
                }
                else
                {
                    //
                    // Note that onlyChild could be null.
                    // The deleted node and its only child are BLACK, and there is a real parent, therefore,
                    // the total black height was at least 2 (excluding the real parent), thus the sibling subtree also has a black height of at least 2
                    //
                    FixRemovalCase2(GetSibling(onlyChild, node.Parent));
                }
            }
        }

        private static void FixRemovalCase1(TreeNode node)
        {
            Debug.Assert(GetColor(node) == NodeColor.BLACK);
            if (node.Parent == null)
            {
                return;
            }
            else
            {
                FixRemovalCase2(GetSibling(node, node.Parent));
            }
        }

        private static void FixRemovalCase2(TreeNode sibling)
        {
            Debug.Assert(sibling != null);
            if (GetColor(sibling) == NodeColor.RED)
            {
                Debug.Assert(sibling.Left != null && sibling.Right != null);
                TreeNode parent = sibling.Parent;
                // the parent must be black
                SetColor(parent, NodeColor.RED);
                SetColor(sibling, NodeColor.BLACK);

                if (sibling == parent.Right)
                {
                    RotateLeft(sibling.Parent);
                    // new sibling was the old sibling left child, and must be non-leaf black
                    sibling = parent.Right;
                }
                else
                {
                    RotateRight(sibling.Parent);
                    // new sibling was the old sibling right child, and must be non-leaf black
                    sibling = parent.Left;
                }
            }

            // Now the sibling will be a BLACK non-leaf.
            FixRemovalCase3(sibling);
        }

        private static void FixRemovalCase3(TreeNode sibling)
        {
            if (GetColor(sibling.Parent) == NodeColor.BLACK &&
                GetColor(sibling) == NodeColor.BLACK &&
                GetColor(sibling.Left) == NodeColor.BLACK &&
                GetColor(sibling.Right) == NodeColor.BLACK)
            {
                SetColor(sibling, NodeColor.RED);
                FixRemovalCase1(sibling.Parent);
            }
            else
            {
                FixRemovalCase4(sibling);
            }
        }

        private static void FixRemovalCase4(TreeNode sibling)
        {
            if (GetColor(sibling.Parent) == NodeColor.RED &&
                GetColor(sibling) == NodeColor.BLACK &&
                GetColor(sibling.Left) == NodeColor.BLACK &&
                GetColor(sibling.Right) == NodeColor.BLACK)
            {
                SetColor(sibling, NodeColor.RED);
                SetColor(sibling.Parent, NodeColor.BLACK);
            }
            else
            {
                FixRemovalCase5(sibling);
            }
        }

        private static void FixRemovalCase5(TreeNode sibling)
        {
            if (sibling == sibling.Parent.Right &&
                GetColor(sibling) == NodeColor.BLACK &&
                GetColor(sibling.Left) == NodeColor.RED &&
                GetColor(sibling.Right) == NodeColor.BLACK)
            {
                SetColor(sibling, NodeColor.RED);
                SetColor(sibling.Left, NodeColor.BLACK);
                RotateRight(sibling);
                sibling = sibling.Parent;
            }
            else if (sibling == sibling.Parent.Left &&
                GetColor(sibling) == NodeColor.BLACK &&
                GetColor(sibling.Right) == NodeColor.RED &&
                GetColor(sibling.Left) == NodeColor.BLACK)
            {
                SetColor(sibling, NodeColor.RED);
                SetColor(sibling.Right, NodeColor.BLACK);
                RotateLeft(sibling);
                sibling = sibling.Parent;
            }
            FixRemovalCase6(sibling);
        }

        private static void FixRemovalCase6(TreeNode sibling)
        {
            Debug.Assert(GetColor(sibling) == NodeColor.BLACK);

            SetColor(sibling, GetColor(sibling.Parent));
            SetColor(sibling.Parent, NodeColor.BLACK);
            if (sibling == sibling.Parent.Right)
            {
                Debug.Assert(GetColor(sibling.Right) == NodeColor.RED);
                SetColor(sibling.Right, NodeColor.BLACK);
                RotateLeft(sibling.Parent);
            }
            else
            {
                Debug.Assert(GetColor(sibling.Left) == NodeColor.RED);
                SetColor(sibling.Left, NodeColor.BLACK);
                RotateRight(sibling.Parent);
            }
        }

        #endregion

        #region Private Fields

        private TreeNode _root;

        #endregion

        #region Private Types

        private sealed class MyEnumerator : IEnumerator
        {
            internal MyEnumerator(TreeNode node)
            {
                _root = node;
            }

            public object Current
            {
                get
                {
                    if (_node == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return _node.Key;
                }
            }

            public bool MoveNext()
            {
                if (!_moved)
                {
                    _node = _root != null ? FindMinSubTree(_root) : null;
                    _moved = true;
#if DEBUG
                    if (_root != null)
                    {
                        _root._inEnumaration = true;
                    }
#endif
                }
                else
                {
                    _node = _node != null ? FindSuccessor(_node) : null;
                }
#if DEBUG
                if (_root != null)
                {
                    _root._inEnumaration = _node != null;
                }
#endif
                return _node != null;
            }

            public void Reset()
            {
                _moved = false;
                _node = null;
            }

            private TreeNode _node;
            private TreeNode _root;
            private bool _moved;
        }

#if DEBUG
        [DebuggerDisplay("{((System.Speech.Internal.SrgsCompiler.Arc)Key).ToString ()}")]
#endif
        private sealed class TreeNode
        {
            internal TreeNode(object key)
            {
                _key = key;
            }

            internal TreeNode Left
            {
                get
                {
                    return _leftChild;
                }
                set
                {
                    _leftChild = value;
                    if (_leftChild != null)
                    {
                        _leftChild._parent = this;
                    }
                }
            }

            internal TreeNode Right
            {
                get
                {
                    return _rightChild;
                }
                set
                {
                    _rightChild = value;
                    if (_rightChild != null)
                    {
                        _rightChild._parent = this;
                    }
                }
            }

            internal TreeNode Parent
            {
                get
                {
                    return _parent;
                }
                set
                {
                    _parent = value;
                }
            }

            internal bool IsRed
            {
                get
                {
                    return _isRed;
                }
                set
                {
                    _isRed = value;
                }
            }

            internal object Key
            {
                get
                {
                    return _key;
                }
            }

            internal void CopyNode(TreeNode from)
            {
                _key = from._key;
            }

#if DEBUG
            internal bool _inEnumaration;
#endif
            private object _key;
            private bool _isRed;

            private TreeNode _leftChild, _rightChild, _parent;
        }

        private enum NodeColor
        {
            BLACK = 0,
            RED = 1
        }

        #endregion
    }
}
