// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
//#define VerifyIndex
#define VerifyPath
#define VerifySort
#endif

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal enum RBTreeError
    {
        InvalidPageSize = 1,
        //      InvalidCompareDelegate                      =  2,
        PagePositionInSlotInUse = 3,
        NoFreeSlots = 4,
        InvalidStateinInsert = 5,
        //      InvalidStateinEndInsert                     =  6,
        InvalidNextSizeInDelete = 7,
        InvalidStateinDelete = 8,
        InvalidNodeSizeinDelete = 9,
        InvalidStateinEndDelete = 10,
        CannotRotateInvalidsuccessorNodeinDelete = 11,
        //      IndexOutOfRange                             = 12,
        IndexOutOFRangeinGetNodeByIndex = 13,
        RBDeleteFixup = 14,
        UnsupportedAccessMethod1 = 15,
        UnsupportedAccessMethod2 = 16,
        UnsupportedAccessMethodInNonNillRootSubtree = 17,
        AttachedNodeWithZerorbTreeNodeId = 18, // DataRowCollection
        CompareNodeInDataRowTree = 19, // DataRowCollection
        CompareSateliteTreeNodeInDataRowTree = 20, // DataRowCollection
        NestedSatelliteTreeEnumerator = 21,
    }

    internal enum TreeAccessMethod
    {
        KEY_SEARCH_AND_INDEX = 1,
        INDEX_ONLY = 2,
    }

    // an index represents location the tree
    // a tree has an array of pages (max 2^16) (top 16 bits)
    // a page has an array of nodes (max 2^16) (bottom 16 bits)
    // nodes are indexed by RBTree.PageTable[index>>16].Slots[index&0xFFFF]

    // a tree has an PageTableBitmap to indicate which allocated pages have free nodes
    // a page has a SlotBitmap to indicate which slots are free

    // initial page allocation (assuming no deletes)
    //          #page  * #slot =     #total, #cumulative
    // (            4) *    32 =        128,           127 (subtract 1 for NIL node)
    // (   32 -     4) *   256 =       7168,         7,295
    // (  128 -    32) *  1024 =      98304,       105,599
    // ( 4096 -   128) *  4096 =   16252928,    16,358,527
    // (32768 -  4096) *  8192 =  234881024,   251,239,551
    // (65535 - 32768) * 65536 = 2147418112, 2,398,657,663 (excess nodes 251,174,016 > Int32.MaxValue)

    // tree page size is GetIntValueFromBitMap(inUsePageCount) // return highest bit in array
    //private static readonly int[] PageSize = new int[17] { // nobit + 16 bits == 17 position
    //       32,   32,   32,              // inUsePageCount <      4           0,    1,     2,
    //      256,  256,  256,              // inUsePageCount <     32           4,    8,    16,
    //     1024, 1024,                    // inUsePageCount <    128          32,   64,
    //     4096, 4096, 4096, 4096, 4096,  // inUsePageCount <   4096         128,  256,   512, 1024, 2048,
    //     8192, 8192, 8192,              // inUsePageCount <  32768        4096, 8192, 16384
    //    65535                           // inUsePageCount <= 65535
    //};

    // the in-ordering of nodes in the tree  (the second graph has duplicate nodes)
    // for the satellite tree, the main tree node is the clone, GetNodeByIndex always returns the satelliteRootid
    //      4       |           4
    //    /   \     |     /          \
    //   2     6    |    3  -   3     7
    //  / \   / \   |   / \    / \   / \
    // 1   3 5   7  |  1   5  2   4 8   9

    // PageTable (starting at 32) doubles in size on demand (^16 - ^5 = 11 grows to reach max PageTable size)

    // if a page has no allocated slots, it will be dropped
    // worst case scenario is to repeatedly add/remove on a boundary condition

    // the primary change to support Index using Predicate<DataRow> or Comparison<DataRow> was to eliminate all
    // unnecessary searching for the node in the main tree when operating on a node in the satellite branch
    // in all cases except GetNodeByKey(K)& GetIndexByNode(int), we know what that mainTreeNodeID is and can avoid searching

    internal abstract class RBTree<K> : IEnumerable
    {
        // 2^16 #pages * 2^n == total number of nodes.  512 = 32 million, 1024 = 64 million, 2048 = 128m, 4096=256m, 8192=512m, 16284=1 billion
        // 32K=2 billion.
        internal const int DefaultPageSize = 32; /* 512 = 2^9 32 million nodes*/
        internal const int NIL = 0;                  // 0th page, 0th slot for each tree till CLR static & generics issue is fixed

        private TreePage?[] _pageTable; // initial size 4, then doubles (grows) - it never shrinks.
        private int[] _pageTableMap;
        private int _inUsePageCount;    // contains count of allocated pages per tree, its <= the capacity of  pageTable
        private int _nextFreePageLine;   // used for keeping track of position of last used free page in pageTable
        public int root;
        private int _version;

        private int _inUseNodeCount; // total number of nodes currently in use by this tree.
        private int _inUseSatelliteTreeCount; // total number of satellite associated with this tree.
        private readonly TreeAccessMethod _accessMethod;

        protected abstract int CompareNode(K? record1, K? record2);
        protected abstract int CompareSateliteTreeNode(K? record1, K? record2);

        protected RBTree(TreeAccessMethod accessMethod)
        {
            _accessMethod = accessMethod;
            InitTree();
        }

        [MemberNotNull(nameof(_pageTable))]
        [MemberNotNull(nameof(_pageTableMap))]
        private void InitTree()
        {
            root = NIL;
            _pageTable = new TreePage[1 * TreePage.slotLineSize];
            _pageTableMap = new int[(_pageTable.Length + TreePage.slotLineSize - 1) / TreePage.slotLineSize]; // Ceiling(size)
            _inUsePageCount = 0;
            _nextFreePageLine = 0;
            AllocPage(DefaultPageSize);

            // alloc storage for reserved NIL node. segment 0, slot 0; Initialize NIL
            _pageTable[0]!._slots[0]._nodeColor = NodeColor.black;
            _pageTable[0]!._slotMap[0] = 0x1;
            _pageTable[0]!.InUseCount = 1;

            _inUseNodeCount = 1;
            _inUseSatelliteTreeCount = 0; // total number of satellite associated with this tree.
        }

        private void FreePage(TreePage page)
        {
            MarkPageFree(page);
            _pageTable[page.PageId] = null;
            _inUsePageCount--;
        }

        /* AllocPage()
         *  size : Allocates a page of the specified size.
         *
         * Look for an unallocated page entry.
         *   (1) If entry for an unallocated page exists in current pageTable - use it
         *   (2) else extend pageTable
         */
        private TreePage AllocPage(int size)
        {
            int freePageIndex = GetIndexOfPageWithFreeSlot(false);

            if (freePageIndex != -1)
            {
                _pageTable[freePageIndex] = new TreePage(size);
                _nextFreePageLine = freePageIndex / TreePage.slotLineSize;
            }
            else
            {
                // no free position found, increase pageTable size
                TreePage[] newPageTable = new TreePage[_pageTable.Length * 2];
                Array.Copy(_pageTable, newPageTable, _pageTable.Length);
                int[] newPageTableMap = new int[(newPageTable.Length + TreePage.slotLineSize - 1) / TreePage.slotLineSize];
                Array.Copy(_pageTableMap, newPageTableMap, _pageTableMap.Length);

                _nextFreePageLine = _pageTableMap.Length;
                freePageIndex = _pageTable.Length;
                _pageTable = newPageTable;
                _pageTableMap = newPageTableMap;
                _pageTable[freePageIndex] = new TreePage(size);
            }
            _pageTable[freePageIndex]!.PageId = freePageIndex;
            _inUsePageCount++;
            return _pageTable[freePageIndex]!;
        }

        /* MarkPageFull()
         * Mark the specified page "Full" as all its slots aer in use
         */
        private void MarkPageFull(TreePage page)
        {
            // set bit associated with page to mark it as full
            /*
            int pageTableMapIndex = (page.PageId / TreePage.slotLineSize);
            int pageTableMapOffset = (page.PageId % TreePage.slotLineSize);
            Int32 pageBitMask = ((Int32)1) << pageTableMapOffset;
            _pageTableMap[pageTableMapIndex] |= (pageBitMask);
            */
            _pageTableMap[page.PageId / TreePage.slotLineSize] |= (1 << (page.PageId % TreePage.slotLineSize));
        }

        /* MarkPageFree()
         * Mark the specified page as "Free". It has atleast 1 available slot.
         */
        private void MarkPageFree(TreePage page)
        {
            // set bit associated with page to mark it as free
            /*
            int pageTableMapIndex = (page.PageId / TreePage.slotLineSize);
            int pageTableMapOffset = (page.PageId % TreePage.slotLineSize);
            Int32 pageBitMask = ((Int32)1) << pageTableMapOffset;
            _pageTableMap[pageTableMapIndex] &= ~(pageBitMask);
            */
            _pageTableMap[page.PageId / TreePage.slotLineSize] &= ~(1 << (page.PageId % TreePage.slotLineSize));
        }

        private static int GetIntValueFromBitMap(uint bitMap)
        {
            int value = 0; // 0 based slot position

            /*
             * Assumption: bitMap can have max, exactly 1 bit set.
             * convert bitMap to int value giving number of 0's to its right
             * return value between 0 and 31
             */
            if ((bitMap & 0xFFFF0000) != 0)
            {
                value += 16;
                bitMap >>= 16;
            }
            if ((bitMap & 0x0000FF00) != 0)
            {
                value += 8;
                bitMap >>= 8;
            }
            if ((bitMap & 0x000000F0) != 0)
            {
                value += 4;
                bitMap >>= 4;
            }
            if ((bitMap & 0x0000000C) != 0)
            {
                value += 2;
                bitMap >>= 2;
            }
            if ((bitMap & 0x00000002) != 0)
                value += 1;
            return value;
        }

        /*
         * FreeNode()
         * nodeId: The nodeId of the node to be freed
         */
        private void FreeNode(int nodeId)
        {
            TreePage page = _pageTable[nodeId >> 16]!;
            int slotIndex = nodeId & 0xFFFF;

            page._slots[slotIndex] = default(Node);

            // clear slotMap entry associated with nodeId
            page._slotMap[slotIndex / TreePage.slotLineSize] &= ~(1 << slotIndex % TreePage.slotLineSize);
            page.InUseCount--;
            _inUseNodeCount--;
            if (page.InUseCount == 0)
                FreePage(page);
            else if (page.InUseCount == page._slots.Length - 1)
                MarkPageFree(page); // With freeing of a node, a previous full page has a free slot.
        }

        /*
         * GetIndexOfPageWithFreeSlot()
         * allocatedPage: If true, look for an allocatedPage with free slot else look for an unallocated page entry in pageTable
         * return: if allocatedPage is true, return index of a page with at least 1 free slot
         *            else return index of an unallocated page, pageTable[index] is empty.
         */
        private int GetIndexOfPageWithFreeSlot(bool allocatedPage)
        {
            int pageTableMapPos = _nextFreePageLine;
            int pageIndex = -1;

            while (pageTableMapPos < _pageTableMap.Length)
            {
                if (((uint)_pageTableMap[pageTableMapPos]) < 0xFFFFFFFF)
                {
                    uint pageSegmentMap = (uint)_pageTableMap[pageTableMapPos];
                    while ((pageSegmentMap ^ (0xFFFFFFFF)) != 0)         //atleast one "0" is there (same as <0xFFFFFFFF)
                    {
                        uint pageWithFreeSlot = (~(pageSegmentMap)) & (pageSegmentMap + 1);

                        if ((_pageTableMap[pageTableMapPos] & pageWithFreeSlot) != 0) //paranoia check
                            throw ExceptionBuilder.InternalRBTreeError(RBTreeError.PagePositionInSlotInUse);

                        pageIndex = (pageTableMapPos * TreePage.slotLineSize) + GetIntValueFromBitMap(pageWithFreeSlot); // segment + offset
                        if (allocatedPage)
                        {
                            if (_pageTable[pageIndex] != null)
                                return pageIndex;
                        }
                        else
                        {
                            if (_pageTable[pageIndex] == null)
                                return pageIndex;           // pageIndex points to an unallocated Page
                        }
                        pageIndex = -1;
                        pageSegmentMap |= pageWithFreeSlot; // found "reset bit", but unallocated page, mark it as unavaiable and continue search
                    }
                }

                pageTableMapPos++;
            }

            if (_nextFreePageLine != 0)
            {
                //Try one more time, starting from 0th page segment position to locate a page with free slots
                _nextFreePageLine = 0;
                pageIndex = GetIndexOfPageWithFreeSlot(allocatedPage);
            }
            return pageIndex;
        }

        public int Count
        {
            get
            {
                Debug.Assert(_inUseNodeCount - 1 == SubTreeSize(root), "count mismatch");
                return (_inUseNodeCount - 1);
            }
        }

        public bool HasDuplicates
        {
            get
            {
                return (0 != _inUseSatelliteTreeCount);
            }
        }

        /*
         * GetNewNode()
         * Allocate storage for a new node and assign in the specified key.
         *
         * Find a page with free slots or allocate a new page.
         * Use bitmap associated with page to allocate a slot.
         * mark the slot as used and return its index.
         */
        private int GetNewNode(K? key)
        {
            // find page with free slots, if none, allocate a new page
            TreePage? page = null;

            int freePageIndex = GetIndexOfPageWithFreeSlot(true);
            if (freePageIndex != -1)
                page = _pageTable[freePageIndex];
            else if (_inUsePageCount < (4))
                page = AllocPage(DefaultPageSize);  // First 128 slots
            else if (_inUsePageCount < (32))
                page = AllocPage(256);
            else if (_inUsePageCount < (128))
                page = AllocPage(1024);
            else if (_inUsePageCount < (4096))
                page = AllocPage(4096);
            else if (_inUsePageCount < (32 * 1024))
                page = AllocPage(8192);              // approximately First 16 million slots (2^24)
            else
                page = AllocPage(64 * 1024);          // Page size to accomodate more than 16 million slots (Max 2 Billion and 16 million slots)

            // page contains atleast 1 free slot.
            int slotId = page!.AllocSlot(this);

            if (slotId == -1)
                throw ExceptionBuilder.InternalRBTreeError(RBTreeError.NoFreeSlots);

            // NodeId: Upper 16 bits pageId, lower bits slotId
            page._slots[slotId]._selfId = (int)(((uint)page.PageId) << 16) | slotId;
            Debug.Assert(page._slots[slotId]._leftId == NIL, "node not cleared");
            Debug.Assert(page._slots[slotId]._rightId == NIL, "node not cleared");
            Debug.Assert(page._slots[slotId]._parentId == NIL, "node not cleared");
            Debug.Assert(page._slots[slotId]._nextId == NIL, "node not cleared");
            page._slots[slotId]._subTreeSize = 1;     // new Nodes have size 1.
            page._slots[slotId]._keyOfNode = key;
            Debug.Assert(page._slots[slotId]._nodeColor == NodeColor.red, "node not cleared");
            return page._slots[slotId]._selfId;
        }

        private int Successor(int x_id)
        {
            if (Right(x_id) != NIL)
                return Minimum(Right(x_id)); //return left most node in right sub-tree.
            int y_id = Parent(x_id);

            while (y_id != NIL && x_id == Right(y_id))
            {
                x_id = y_id;
                y_id = Parent(y_id);
            }
            return y_id;
        }

        private bool Successor(ref int nodeId, ref int mainTreeNodeId)
        {
            if (NIL == nodeId)
            {   // find first node, using branchNodeId as the root
                nodeId = Minimum(mainTreeNodeId);
                mainTreeNodeId = NIL;
            }
            else
            {   // find next node
                nodeId = Successor(nodeId);

                if ((NIL == nodeId) && (NIL != mainTreeNodeId))
                {   // done with satellite branch, move back to main tree
                    nodeId = Successor(mainTreeNodeId);
                    mainTreeNodeId = NIL;
                }
            }
            if (NIL != nodeId)
            {   // test for satellite branch
                if (NIL != Next(nodeId))
                {   // find first node of satellite branch
                    if (NIL != mainTreeNodeId)
                    {   // satellite branch has satellite branch - very bad
                        throw ExceptionBuilder.InternalRBTreeError(RBTreeError.NestedSatelliteTreeEnumerator);
                    }
                    mainTreeNodeId = nodeId;
                    nodeId = Minimum(Next(nodeId));
                }
                // has value
                return true;
            }
            // else no value, done with main tree
            return false;
        }

        private int Minimum(int x_id)
        {
            while (Left(x_id) != NIL)
            {
                x_id = Left(x_id);
            }
            return x_id;
        }

        /*
         * LeftRotate()
         *
         * It returns the node id for the root of the rotated tree
         */
        private int LeftRotate(int root_id, int x_id, int mainTreeNode)
        {
            int y_id = Right(x_id);

            // Turn y's left subtree into x's right subtree
            SetRight(x_id, Left(y_id));
            if (Left(y_id) != NIL)
            {
                SetParent(Left(y_id), x_id);
            }

            SetParent(y_id, Parent(x_id));
            if (Parent(x_id) == NIL)
            {
                if (root_id == NIL)
                {
                    root = y_id;
                }
                else
                {
                    SetNext(mainTreeNode, y_id);
                    SetKey(mainTreeNode, Key(y_id));
                    root_id = y_id;
                }
            }
            else if (x_id == Left(Parent(x_id)))
            {  // x is left child of its parent
                SetLeft(Parent(x_id), y_id);
            }
            else
            {
                SetRight(Parent(x_id), y_id);
            }

            SetLeft(y_id, x_id);
            SetParent(x_id, y_id);

            //maintain size:  y_id = parent & x_id == child
            if (x_id != NIL)
            {
                SetSubTreeSize(x_id, (SubTreeSize(Left(x_id)) + SubTreeSize(Right(x_id)) + (Next(x_id) == NIL ? 1 : SubTreeSize(Next(x_id)))));
            }

            if (y_id != NIL)
            {
                SetSubTreeSize(y_id, (SubTreeSize(Left(y_id)) + SubTreeSize(Right(y_id)) + (Next(y_id) == NIL ? 1 : SubTreeSize(Next(y_id)))));
            }
            return root_id;
        }


        /*
         * RightRotate()
         *
         * It returns the node id for the root of the rotated tree
         */
        private int RightRotate(int root_id, int x_id, int mainTreeNode)
        {
            int y_id = Left(x_id);

            SetLeft(x_id, Right(y_id));       // Turn y's right subtree into x's left subtree
            if (Right(y_id) != NIL)
            {
                SetParent(Right(y_id), x_id);
            }

            SetParent(y_id, Parent(x_id));
            if (Parent(x_id) == NIL)
            {
                if (root_id == NIL)
                {
                    root = y_id;
                }
                else
                {
                    SetNext(mainTreeNode, y_id);
                    SetKey(mainTreeNode, Key(y_id));
                    root_id = y_id;
                }
            }
            else if (x_id == Left(Parent(x_id))) // x is left child of its parent
                SetLeft(Parent(x_id), y_id);
            else
                SetRight(Parent(x_id), y_id);

            SetRight(y_id, x_id);
            SetParent(x_id, y_id);

            //maintain size: y_id == parent && x_id == child.
            if (x_id != NIL)
            {
                SetSubTreeSize(x_id, (SubTreeSize(Left(x_id)) + SubTreeSize(Right(x_id)) + (Next(x_id) == NIL ? 1 : SubTreeSize(Next(x_id)))));
            }

            if (y_id != NIL)
            {
                SetSubTreeSize(y_id, (SubTreeSize(Left(y_id)) + SubTreeSize(Right(y_id)) + (Next(y_id) == NIL ? 1 : SubTreeSize(Next(y_id)))));
            }
            return root_id;
        }

#if VerifySort
        // This helps validate the sorting of the tree to help catch instances of corruption much sooner.
        // corruption happens when the data changes without telling the tree or when multi-threads do simultanous write operations
        private int Compare(int root_id, int x_id, int z_id)
        {
            Debug.Assert(NIL != x_id, "nil left");
            Debug.Assert(NIL != z_id, "nil right");
            return (root_id == NIL) ? CompareNode(Key(x_id), Key(z_id)) : CompareSateliteTreeNode(Key(x_id), Key(z_id));
        }
#endif

        /*
         * RBInsert()
         * root_id: root_id of the tree to which a node has to be inserted. it is NIL for inserting to Main tree.
         * x_id    : node_id of node to be inserted
         *
         * returns: The root of the tree to which the specified node was added. its NIL if the node was added to Main RBTree.
         *
         * if root_id is NIL -> use CompareNode else use CompareSateliteTreeNode
         *
         * Satelite tree creation:
         * First Duplicate value encountered. Create a *new* tree whose root will have the same key value as the current node.
         * The Duplicate tree nodes have same key when used with CompareRecords but distinct record ids.
         * The current record at all times will have the same *key* as the duplicate tree root.
         */
        private int RBInsert(int root_id, int x_id, int mainTreeNodeID, int position, bool append)
        {
            unchecked { _version++; }

            // Insert Node x at the appropriate position
            int y_id = NIL;
            int z_id = (root_id == NIL) ? root : root_id;  //if non NIL, then use the specifid root_id as tree's root.

            if (_accessMethod == TreeAccessMethod.KEY_SEARCH_AND_INDEX && !append)
            {
                Debug.Assert(-1 == position, "KEY_SEARCH_AND_INDEX with bad position");
                while (z_id != NIL)  // in-order traverse and find node with a NILL left or right child
                {
                    IncreaseSize(z_id);
                    y_id = z_id;            // y_id set to the proposed parent of x_id

                    int c = (root_id == NIL) ? CompareNode(Key(x_id), Key(z_id)) : CompareSateliteTreeNode(Key(x_id), Key(z_id));

                    if (c < 0)
                    {
#if VerifySort
                        Debug.Assert((NIL == Left(z_id)) || (0 > Compare(root_id, Left(z_id), z_id)), "Left is not left");
#endif
                        z_id = Left(z_id);
                    }
                    else if (c > 0)
                    {
#if VerifySort
                        Debug.Assert((NIL == Right(z_id)) || (0 < Compare(root_id, Right(z_id), z_id)), "Right is not right");
#endif
                        z_id = Right(z_id);
                    }
                    else
                    {
                        // Multiple records with same key - insert it to the duplicate record tree associated with current node
                        if (root_id != NIL)
                        {
                            throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidStateinInsert);
                        }
                        if (Next(z_id) != NIL)
                        {
                            root_id = RBInsert(Next(z_id), x_id, z_id, -1, false); // z_id is existing mainTreeNodeID
                            SetKey(z_id, Key(Next(z_id)));
#if VerifyPath
                            (new NodePath(x_id, z_id)).VerifyPath(this); // verify x_id after its been added
#endif
                        }
                        else
                        {
                            int newMainTreeNodeId = NIL;
                            // The existing node is pushed into the Satellite Tree and a new Node
                            // is created in the main tree, whose's next points to satellite root.
                            newMainTreeNodeId = GetNewNode(Key(z_id));
                            _inUseSatelliteTreeCount++;

                            // copy contents of z_id to dupRootId (main tree node).
                            SetNext(newMainTreeNodeId, z_id);
                            SetColor(newMainTreeNodeId, color(z_id));
                            SetParent(newMainTreeNodeId, Parent(z_id));
                            SetLeft(newMainTreeNodeId, Left(z_id));
                            SetRight(newMainTreeNodeId, Right(z_id));

                            // Update z_id's non-nil parent
                            if (Left(Parent(z_id)) == z_id)
                                SetLeft(Parent(z_id), newMainTreeNodeId);
                            else if (Right(Parent(z_id)) == z_id)
                                SetRight(Parent(z_id), newMainTreeNodeId);

                            // update children.
                            if (Left(z_id) != NIL)
                                SetParent(Left(z_id), newMainTreeNodeId);
                            if (Right(z_id) != NIL)
                                SetParent(Right(z_id), newMainTreeNodeId);

                            if (root == z_id)
                                root = newMainTreeNodeId;

                            // Reset z_id's pointers to NIL. It will start as the satellite tree's root.
                            SetColor(z_id, NodeColor.black);
                            SetParent(z_id, NIL);
                            SetLeft(z_id, NIL);
                            SetRight(z_id, NIL);

                            int savedSize = SubTreeSize(z_id);
                            SetSubTreeSize(z_id, 1);
                            // With z_id as satellite root, insert x_id
                            root_id = RBInsert(z_id, x_id, newMainTreeNodeId, -1, false);

                            SetSubTreeSize(newMainTreeNodeId, savedSize);
#if VerifyPath
                            (new NodePath(x_id, newMainTreeNodeId)).VerifyPath(this); // verify x_id after its been added
#endif
                        }
                        return root_id;
                    }
                }
            }
            else if (_accessMethod == TreeAccessMethod.INDEX_ONLY || append)
            {
                if (position == -1)
                {
                    position = SubTreeSize(root);   // append
                }

                while (z_id != NIL)    // in-order traverse and find node with a NILL left or right child
                {
                    IncreaseSize(z_id);
                    y_id = z_id;            // y_id set to the proposed parent of x_id

                    //int c = (SubTreeSize(y_id)-(position)); // Actually it should be: SubTreeSize(y_id)+1 - (position + 1)
                    int c = (position) - (SubTreeSize(Left(y_id)));

                    if (c <= 0)
                    {
                        z_id = Left(z_id);
                    }
                    else
                    {
                        //position = position - SubTreeSize(z_id);
                        z_id = Right(z_id);
                        if (z_id != NIL)
                        {
                            position = c - 1;    //skip computation of position for leaf node
                        }
                    }
                }
            }
            else
            {
                throw ExceptionBuilder.InternalRBTreeError(RBTreeError.UnsupportedAccessMethod1);
            }

            SetParent(x_id, y_id);
            if (y_id == NIL)
            {
                if (root_id == NIL)
                {
                    root = x_id;
                }
                else
                {
                    // technically we should never come here. Satellite tree always has a root and atleast 1 child.
                    // if it has only root as it's node, then the satellite tree gets collapsed into the main tree.
#if VerifyPath
                    (new NodePath(x_id, mainTreeNodeID)).VerifyPath(this); // verify x_id after its been added
#endif
                    SetNext(mainTreeNodeID, x_id);
                    SetKey(mainTreeNodeID, Key(x_id));
                    root_id = x_id;
                }
            }
            else
            {
                int c = 0;
                if (_accessMethod == TreeAccessMethod.KEY_SEARCH_AND_INDEX)
                    c = (root_id == NIL) ? CompareNode(Key(x_id), Key(y_id)) : CompareSateliteTreeNode(Key(x_id), Key(y_id));
                else if (_accessMethod == TreeAccessMethod.INDEX_ONLY)
                    c = (position <= 0) ? -1 : 1;
                else
                {
                    throw ExceptionBuilder.InternalRBTreeError(RBTreeError.UnsupportedAccessMethod2);
                }

                if (c < 0)
                    SetLeft(y_id, x_id);
                else
                    SetRight(y_id, x_id);
            }

            SetLeft(x_id, NIL);
            SetRight(x_id, NIL);
            SetColor(x_id, NodeColor.red);
            z_id = x_id; // for verification later

            // fix the tree
            while (color(Parent(x_id)) == NodeColor.red)
            {
                if (Parent(x_id) == Left(Parent(Parent(x_id))))     // if x.parent is a left child
                {
                    y_id = Right(Parent(Parent(x_id)));              // x.parent.parent.right;
                    if (color(y_id) == NodeColor.red)              // my right uncle is red
                    {
                        SetColor(Parent(x_id), NodeColor.black);      // x.parent.color = Color.black;
                        SetColor(y_id, NodeColor.black);
                        SetColor(Parent(Parent(x_id)), NodeColor.red);   // x.parent.parent.color = Color.red;
                        x_id = Parent(Parent(x_id));                     // x = x.parent.parent;
                    }
                    else
                    {     // my right uncle is black
                        if (x_id == Right(Parent(x_id)))
                        {
                            x_id = Parent(x_id);
                            root_id = LeftRotate(root_id, x_id, mainTreeNodeID);
                        }

                        SetColor(Parent(x_id), NodeColor.black);                           // x.parent.color = Color.black;
                        SetColor(Parent(Parent(x_id)), NodeColor.red);                 //    x.parent.parent.color = Color.red;
                        root_id = RightRotate(root_id, Parent(Parent(x_id)), mainTreeNodeID);   //    RightRotate (x.parent.parent);
                    }
                }
                else
                {     // x.parent is a right child
                    y_id = Left(Parent(Parent(x_id)));          // y = x.parent.parent.left;
                    if (color(y_id) == NodeColor.red)      // if (y.color == Color.red)    // my right uncle is red
                    {
                        SetColor(Parent(x_id), NodeColor.black);
                        SetColor(y_id, NodeColor.black);
                        SetColor(Parent(Parent(x_id)), NodeColor.red);   // x.parent.parent.color = Color.red;
                        x_id = Parent(Parent(x_id));
                    }
                    else
                    {// my right uncle is black
                        if (x_id == Left(Parent(x_id)))
                        {
                            x_id = Parent(x_id);
                            root_id = RightRotate(root_id, x_id, mainTreeNodeID);
                        }

                        SetColor(Parent(x_id), NodeColor.black);             // x.parent.color = Color.black;
                        SetColor(Parent(Parent(x_id)), NodeColor.red);   // x.parent.parent.color = Color.red;
                        root_id = LeftRotate(root_id, Parent(Parent(x_id)), mainTreeNodeID);
                    }
                }
            }

            if (root_id == NIL)
                SetColor(root, NodeColor.black);
            else
                SetColor(root_id, NodeColor.black);

#if VerifyPath
            (new NodePath(z_id, mainTreeNodeID)).VerifyPath(this); // verify x_id after its been added
#endif
            return root_id;
        } //Insert

        public void UpdateNodeKey(K currentKey, K newKey)
        {
            // swap oldRecord with NewRecord in nodeId associated with oldRecord
            // if the matched node is a satellite root then also change the key in the associated main tree node.
            NodePath x_id = GetNodeByKey(currentKey);
            if (Parent(x_id._nodeID) == NIL && x_id._nodeID != root) //determine if x_id is a satellite root.
            {
#if VerifyPath
                x_id.VerifyPath(this);
#endif
                SetKey(x_id._mainTreeNodeID, newKey);
            }
            SetKey(x_id._nodeID, newKey);
        }

        public K DeleteByIndex(int i)
        {
            // This check was not correct, it should have been ((uint)this.Count <= (uint)i)
            // Even then, the index will be checked by GetNodebyIndex which will throw either
            // using RowOutOfRange or InternalRBTreeError depending on _accessMethod
            //
            //if (i >= (_inUseNodeCount - 1)) {
            //    throw ExceptionBuilder.InternalRBTreeError(RBTreeError.IndexOutOfRange);
            //}

            K key;
            NodePath x_id = GetNodeByIndex(i); // it'l throw if corresponding node does not exist
            key = Key(x_id._nodeID);
            RBDeleteX(NIL, x_id._nodeID, x_id._mainTreeNodeID);
            return key;
        }

        public int RBDelete(int z_id)
        {
            // always perform delete operation on the main tree
            Debug.Assert(_accessMethod == TreeAccessMethod.INDEX_ONLY, "not expecting anything else");
            return RBDeleteX(NIL, z_id, NIL);
        }


        /*
         * RBDelete()
         *  root_id: root_id of the tree. it is NIL for Main tree.
         *  z_id    : node_id of node to be deleted
         *
         * returns: The id of the spliced node
         *
         * Case 1: Node is in main tree only        (decrease size in main tree)
         * Case 2: Node's key is shared with a main tree node whose next is non-NIL
         *                                       (decrease size in both trees)
         * Case 3: special case of case 2: After deletion, node leaves satelite tree with only 1 node (only root),
         *             it should collapse the satelite tree - go to case 4. (decrease size in both trees)
         * Case 4: (1) Node is in Main tree and is a satelite tree root AND
         *             (2) It is the only node in Satelite tree
         *                   (Do not decrease size in any tree, as its a collpase operation)
         *
         */

        private int RBDeleteX(int root_id, int z_id, int mainTreeNodeID)
        {
            int x_id = NIL; // used for holding spliced node (y_id's) child
            int y_id;                // the spliced node
            int py_id;           // for holding spliced node (y_id's) parent

#if VerifyPath
            // by knowing the NodePath, when z_id is in a satellite branch we don't have to Search for mainTreeNodeID
            (new NodePath(z_id, mainTreeNodeID)).VerifyPath(this);
#endif
            if (Next(z_id) != NIL)
                return RBDeleteX(Next(z_id), Next(z_id), z_id); // delete root of satelite tree.

            // if we reach here, we are guaranteed z_id.next is NIL.
            bool isCase3 = false;
            int mNode = ((_accessMethod == TreeAccessMethod.KEY_SEARCH_AND_INDEX) ? mainTreeNodeID : z_id);

            if (Next(mNode) != NIL)
                root_id = Next(mNode);

            if (SubTreeSize(Next(mNode)) == 2) // Next(mNode) == root_id
                isCase3 = true;
            else if (SubTreeSize(Next(mNode)) == 1)
            {
                throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidNextSizeInDelete);
            }

            if (Left(z_id) == NIL || Right(z_id) == NIL)
                y_id = z_id;
            else
                y_id = Successor(z_id);

            if (Left(y_id) != NIL)
                x_id = Left(y_id);
            else
                x_id = Right(y_id);

            py_id = Parent(y_id);
            if (x_id != NIL)
                SetParent(x_id, py_id);

            if (py_id == NIL) // if the spliced node is the root.
            {
                // check for main tree or Satellite tree root
                if (root_id == NIL)
                    root = x_id;
                else
                {
                    // spliced node is root of satellite tree
                    root_id = x_id;
                }
            }
            else if (y_id == Left(py_id))    // update y's parent to point to X as its child
                SetLeft(py_id, x_id);
            else
                SetRight(py_id, x_id);

            if (y_id != z_id)
            {
                // assign all values from y (spliced node) to z (node containing key to be deleted)
                // -----------

                SetKey(z_id, Key(y_id));      // assign all values from y to z
                SetNext(z_id, Next(y_id));    //z.value = y.value;
            }

            if (Next(mNode) != NIL)
            {
                // update mNode to point to satellite tree root and have the same key value.
                // mNode will have to be patched again after RBDeleteFixup as root_id can again change
                if (root_id == NIL && z_id != mNode)
                {
                    throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidStateinDelete);
                }
                // -- it's possible for Next(mNode) to be != NIL and root_id == NIL when, the spliced node is a mNode of some
                // -- satellite tree and its "next" gets assigned to mNode
                if (root_id != NIL)
                {
                    SetNext(mNode, root_id);
                    SetKey(mNode, Key(root_id));
                }
            }

            // traverse from y_id's parent to root and decrement size by 1
            int tmp_py_id = py_id;
            // case: 1, 2, 3
            while (tmp_py_id != NIL)
            {
                //DecreaseSize (py_id, (Next(y_id)==NIL)?1:Size(Next(y_id)));
                RecomputeSize(tmp_py_id);
                tmp_py_id = Parent(tmp_py_id);
            }

            //if satelite tree node deleted, decrease size in main tree as well.
            if (root_id != NIL)
            {
                // case 2, 3
                int nodeId = mNode;
                while (nodeId != NIL)
                {
                    DecreaseSize(nodeId);
                    nodeId = Parent(nodeId);
                }
            }

            if (color(y_id) == NodeColor.black)
                root_id = RBDeleteFixup(root_id, x_id, py_id, mainTreeNodeID); // passing x.parent as y.parent, to handle x=Node.NIL case.

            if (isCase3)
            {
                // Collpase satelite tree, by swapping it with the main tree counterpart and freeing the main tree node
                if (mNode == NIL || SubTreeSize(Next(mNode)) != 1)
                {
                    throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidNodeSizeinDelete);
                }
                _inUseSatelliteTreeCount--;
                int satelliteRootId = Next(mNode);
                SetLeft(satelliteRootId, Left(mNode));
                SetRight(satelliteRootId, Right(mNode));
                SetSubTreeSize(satelliteRootId, SubTreeSize(mNode));
                SetColor(satelliteRootId, color(mNode));  // Next of satelliteRootId is already NIL
                if (Parent(mNode) != NIL)
                {
                    SetParent(satelliteRootId, Parent(mNode));
                    if (Left(Parent(mNode)) == mNode)
                    {
                        SetLeft(Parent(mNode), satelliteRootId);
                    }
                    else
                    {
                        SetRight(Parent(mNode), satelliteRootId);
                    }
                }

                // update mNode's children.
                if (Left(mNode) != NIL)
                {
                    SetParent(Left(mNode), satelliteRootId);
                }
                if (Right(mNode) != NIL)
                {
                    SetParent(Right(mNode), satelliteRootId);
                }
                if (root == mNode)
                {
                    root = satelliteRootId;
                }

                FreeNode(mNode);
                mNode = NIL;
            }
            else if (Next(mNode) != NIL)
            {
                // update mNode to point to satellite tree root and have the same key value
                if (root_id == NIL && z_id != mNode)
                { //if mNode being deleted, its OK for root_id (it should be) NIL.
                    throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidStateinEndDelete);
                }

                if (root_id != NIL)
                {
                    SetNext(mNode, root_id);
                    SetKey(mNode, Key(root_id));
                }
            }

            // In order to pin a key to it's node, free deleted z_id instead of the spliced y_id
            if (y_id != z_id)
            {
                // we know that key, next and value are same for z_id and y_id
                SetLeft(y_id, Left(z_id));
                SetRight(y_id, Right(z_id));
                SetColor(y_id, color(z_id));
                SetSubTreeSize(y_id, SubTreeSize(z_id));
                if (Parent(z_id) != NIL)
                {
                    SetParent(y_id, Parent(z_id));
                    if (Left(Parent(z_id)) == z_id)
                    {
                        SetLeft(Parent(z_id), y_id);
                    }
                    else
                    {
                        SetRight(Parent(z_id), y_id);
                    }
                }
                else
                {
                    SetParent(y_id, NIL);
                }

                // update children.
                if (Left(z_id) != NIL)
                {
                    SetParent(Left(z_id), y_id);
                }
                if (Right(z_id) != NIL)
                {
                    SetParent(Right(z_id), y_id);
                }

                if (root == z_id)
                {
                    root = y_id;
                }
                else if (root_id == z_id)
                {
                    root_id = y_id;
                }
                // update a next reference to z_id (if any)
                if (mNode != NIL && Next(mNode) == z_id)
                {
                    SetNext(mNode, y_id);
                }
            }
            FreeNode(z_id);
            unchecked { _version++; }
            return z_id;
        }

        /*
         * RBDeleteFixup()
         * Fix the specified tree for RedBlack properties
         *
         * returns: The id of the root
         */
        private int RBDeleteFixup(int root_id, int x_id, int px_id /* px is parent of x */, int mainTreeNodeID)
        {    //x is successor's non nil child or nil if both children are nil
            int w_id;

#if VerifyPath
            // by knowing the NodePath, when z_id is in a satellite branch we don't have to Search for mainTreeNodeID
            (new NodePath(root_id, mainTreeNodeID)).VerifyPath(this);
#endif

            if (x_id == NIL && px_id == NIL)
            {
                return NIL; //case of satelite tree root being deleted.
            }

            while (((root_id == NIL ? root : root_id) != x_id) && color(x_id) == NodeColor.black)
            {
                // (1) x's parent should have aleast 1 non-NIL child.
                // (2) check if x is a NIL left child or a non NIL left child
                if ((x_id != NIL && x_id == Left(Parent(x_id))) || (x_id == NIL && Left(px_id) == NIL))
                {
                    // we have from DELETE, then x cannot be NIL and be a right child of its parent
                    // also from DELETE, if x is non nil, it will be a left child.
                    w_id = (x_id == NIL) ? Right(px_id) : Right(Parent(x_id));     // w is x's right sibling and it cannot be NIL

                    if (w_id == NIL)
                    {
                        throw ExceptionBuilder.InternalRBTreeError(RBTreeError.RBDeleteFixup);
                    }

                    if (color(w_id) == NodeColor.red)
                    {
                        SetColor(w_id, NodeColor.black);
                        SetColor(px_id, NodeColor.red);
                        root_id = LeftRotate(root_id, px_id, mainTreeNodeID);
                        w_id = (x_id == NIL) ? Right(px_id) : Right(Parent(x_id));
                    }

                    if (color(Left(w_id)) == NodeColor.black && color(Right(w_id)) == NodeColor.black)
                    {
                        SetColor(w_id, NodeColor.red);
                        x_id = px_id;
                        px_id = Parent(px_id); //maintain px_id
                    }
                    else
                    {
                        if (color(Right(w_id)) == NodeColor.black)
                        {
                            SetColor(Left(w_id), NodeColor.black);
                            SetColor(w_id, NodeColor.red);
                            root_id = RightRotate(root_id, w_id, mainTreeNodeID);
                            w_id = (x_id == NIL) ? Right(px_id) : Right(Parent(x_id));
                        }

                        SetColor(w_id, color(px_id));
                        SetColor(px_id, NodeColor.black);
                        SetColor(Right(w_id), NodeColor.black);
                        root_id = LeftRotate(root_id, px_id, mainTreeNodeID);

                        x_id = (root_id == NIL) ? root : root_id;
                        px_id = Parent(x_id);
                    }
                }
                else
                {  //x is a right child or it is NIL
                    w_id = Left(px_id);
                    if (color(w_id) == NodeColor.red)
                    {   // x_id is y's (the spliced node) sole non-NIL child or NIL if y had no children
                        SetColor(w_id, NodeColor.black);
                        if (x_id != NIL)
                        {
                            SetColor(px_id, NodeColor.red);
                            root_id = RightRotate(root_id, px_id, mainTreeNodeID);
                            w_id = (x_id == NIL) ? Left(px_id) : Left(Parent(x_id));
                        }
                        else
                        {
                            //we have from DELETE, then x cannot be NIL and be a right child of its parent
                            // w_id cannot be nil.
                            SetColor(px_id, NodeColor.red);
                            root_id = RightRotate(root_id, px_id, mainTreeNodeID);
                            w_id = (x_id == NIL) ? Left(px_id) : Left(Parent(x_id));

                            if (w_id == NIL)
                            {
                                throw ExceptionBuilder.InternalRBTreeError(RBTreeError.CannotRotateInvalidsuccessorNodeinDelete);
                            }
                        }
                    }

                    if (color(Right(w_id)) == NodeColor.black && color(Left(w_id)) == NodeColor.black)
                    {
                        SetColor(w_id, NodeColor.red);
                        x_id = px_id;
                        px_id = Parent(px_id);
                    }
                    else
                    {
                        if (color(Left(w_id)) == NodeColor.black)
                        {
                            SetColor(Right(w_id), NodeColor.black);
                            SetColor(w_id, NodeColor.red);
                            root_id = LeftRotate(root_id, w_id, mainTreeNodeID);
                            w_id = (x_id == NIL) ? Left(px_id) : Left(Parent(x_id));
                        }

                        SetColor(w_id, color(px_id));
                        SetColor(px_id, NodeColor.black);
                        SetColor(Left(w_id), NodeColor.black);
                        root_id = RightRotate(root_id, px_id, mainTreeNodeID);

                        x_id = (root_id == NIL) ? root : root_id;
                        px_id = Parent(x_id);
                    }
                }
            }

            SetColor(x_id, NodeColor.black);
            return root_id;
        }

        private int SearchSubTree(int root_id, K? key)
        {
            if (root_id != NIL && _accessMethod != TreeAccessMethod.KEY_SEARCH_AND_INDEX)
            {
                throw ExceptionBuilder.InternalRBTreeError(RBTreeError.UnsupportedAccessMethodInNonNillRootSubtree);
            }

            int x_id = (root_id == NIL) ? root : root_id;
            int c;
            while (x_id != NIL)
            {
                c = (root_id == NIL) ? CompareNode(key, Key(x_id)) : CompareSateliteTreeNode(key, Key(x_id));
                if (c == 0)
                {
                    break;
                }
                if (c < 0)
                {
#if VerifySort
                    Debug.Assert((NIL == Left(x_id)) || (0 > Compare(root_id, Left(x_id), x_id)), "Search duplicate Left is not left");
#endif
                    x_id = Left(x_id);
                }
                else
                {
#if VerifySort
                    Debug.Assert((NIL == Right(x_id)) || (0 < Compare(root_id, Right(x_id), x_id)), "Search duplicate Right is not right");
#endif
                    x_id = Right(x_id);
                }
            }
            return x_id;
        }

        // only works on the main tree - does not work with satelite tree
        public int Search(K key)
        {   // for performance reasons, written as a while loop instead of a recursive method
            int x_id = root;
            int c;
            while (x_id != NIL)
            {
                c = CompareNode(key, Key(x_id));
                if (c == 0)
                {
                    break;
                }
                if (c < 0)
                {
#if VerifySort
                    Debug.Assert((NIL == Left(x_id)) || (0 > Compare(NIL, Left(x_id), x_id)), "Search Left is not left");
#endif
                    x_id = Left(x_id);
                }
                else
                {
#if VerifySort
                    Debug.Assert((NIL == Right(x_id)) || (0 < Compare(NIL, Right(x_id), x_id)), "Search Right is not right");
#endif
                    x_id = Right(x_id);
                }
            }
            return x_id;
        }

        // To simulate direct access for records[index]= record
        /// <summary>
        ///  return key associated with the specified value. Specifically, return record for specified index/value
        ///  indexer
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        // return record i.e key at specified index
        public K this[int index]
        {
            get
            {
                return Key(GetNodeByIndex(index)._nodeID);
            }
        }

        // Get Record(s) having same key value as that of specified record. Then scan the matched nodes
        // and return the node with the matching record
        /// <returns>Determine node and the branch it took to get there.</returns>
        private NodePath GetNodeByKey(K key) //i.e. GetNodeByKey
        {
            int nodeId = SearchSubTree(NIL, key);
            if (Next(nodeId) != NIL)
            {
                return new NodePath(SearchSubTree(Next(nodeId), key), nodeId);
            }
            else if (!Key(nodeId)!.Equals(key))
            {
                nodeId = NIL;
            }
            return new NodePath(nodeId, NIL);
        }

        /*
         * GetIndexByRecord()
         * Gets index of the specified record. returns (-1) if specified record is not found.
         */
        public int GetIndexByKey(K key)
        {
            int nodeIndex = -1;
            NodePath nodeId = GetNodeByKey(key);
            if (nodeId._nodeID != NIL)
            {
                nodeIndex = GetIndexByNodePath(nodeId);
            }
            return nodeIndex;
        }


        /*

         * GetIndexByNode()
         *
         * If I am right child then size=my size + size of left child of my parent + 1
         * go up till root, if right child keep adding to the size.
         * (1) compute rank in main tree.
         * (2) if node member of a satelite tree, add to rank its relative rank in that tree.
         *
         * Rank:
         * Case 1: Node is in Main RBTree only
         *         Its rank/index is its main tree index
         * Case 2: Node is in a Satelite tree only
         *         Its rank/index is its satelite tree index
         * Case 3: Nodes is in both Main and Satelite RBTree (a main tree node can be a satelite tree root)
         *         Its rank/index is its main tree index + its satelite tree index - 1
         * Returns the index of the specified node.
         * returns -1, if the specified Node is tree.NIL.
         *
         * Assumption: The specified node always exist in the tree.
         */

        // this improves performance when used heavily, like with the default view (creating before rows added)
        public int GetIndexByNode(int node)
        {
            Debug.Assert(NIL != node, "GetIndexByNode(NIL)");

            if (0 == _inUseSatelliteTreeCount)
            {   // compute from the main tree when no satellite branches exist
                return ComputeIndexByNode(node);
            }
            else if (NIL != Next(node))
            {   // node is a main tree node
#if VerifyIndex && VerifyPath
                (new NodePath(Next(node), node)).VerifyPath(this);
#endif
                return ComputeIndexWithSatelliteByNode(node);
            }
            else
            {
                int mainTreeNodeId = SearchSubTree(NIL, Key(node));
                if (mainTreeNodeId == node)
                {   // node is a main tree node
#if VerifyIndex && VerifyPath
                    (new NodePath(node, NIL)).VerifyPath(this);
#endif
                    return ComputeIndexWithSatelliteByNode(node);
                }
                else
                {   //compute the main tree rank + satellite branch rank
#if VerifyIndex && VerifyPath
                    (new NodePath(node, mainTreeNodeId)).VerifyPath(this);
#endif
                    return ComputeIndexWithSatelliteByNode(mainTreeNodeId) +
                           ComputeIndexByNode(node);
                }
            }
        }

        /// <summary>Determine tree index position from node path.</summary>
        /// <remarks>This differs from GetIndexByNode which would search for the main tree node instead of just knowing it</remarks>
        private int GetIndexByNodePath(NodePath path)
        {
#if VerifyIndex && VerifyPath
            path.VerifyPath(this);
#endif
            if (0 == _inUseSatelliteTreeCount)
            {   // compute from the main tree when no satellite branches exist
                return ComputeIndexByNode(path._nodeID);
            }
            else if (NIL == path._mainTreeNodeID)
            {   // compute from the main tree accounting for satellite branches
                return ComputeIndexWithSatelliteByNode(path._nodeID);
            }
            else
            {   //compute the main tree rank + satellite branch rank
                return ComputeIndexWithSatelliteByNode(path._mainTreeNodeID) +
                       ComputeIndexByNode(path._nodeID);
            }
        }

        private int ComputeIndexByNode(int nodeId)
        {
#if VerifyIndex
            Debug.Assert(NIL != nodeId, "ComputeIndexByNode(NIL)");
#endif
            int myRank = SubTreeSize(Left(nodeId));
            while (nodeId != NIL)
            {
#if VerifyIndex && VerifyPath
                Debug.Assert(NIL == Next(nodeId), "Next not NIL");
#endif
                int parent = Parent(nodeId);
                if (nodeId == Right(parent))
                {
                    myRank += (SubTreeSize(Left(parent)) + 1);
                }
                nodeId = parent;
            }
            return myRank;
        }

        private int ComputeIndexWithSatelliteByNode(int nodeId)
        {
#if VerifyIndex
            Debug.Assert(NIL != nodeId, "ComputeIndexWithSatelliteByNode(NIL)");
#endif
            int myRank = SubTreeSize(Left(nodeId));
            while (nodeId != NIL)
            {
                int parent = Parent(nodeId);
                if (nodeId == Right(parent))
                {
                    myRank += (SubTreeSize(Left(parent)) + ((Next(parent) == NIL) ? 1 : SubTreeSize(Next(parent))));
                }
                nodeId = parent;
            }
            return myRank;
        }

        /// <returns>Determine node and the branch it took to get there.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        private NodePath GetNodeByIndex(int userIndex)
        {
            int x_id, satelliteRootId;
            if (0 == _inUseSatelliteTreeCount)
            {
                // if rows were only contigously append, then using (userIndex -= _pageTable[i].InUseCount) would
                // be faster for the first 12 pages (about 5248) nodes before (log2 of Count) becomes faster again.
                // the additional complexity was deemed not worthy for the possible perf gain

                // computation cost is (log2 of Count)
                x_id = ComputeNodeByIndex(root, unchecked(userIndex + 1));
                satelliteRootId = NIL;
            }
            else
            {
                // computation cost is ((log2 of Distinct Count) + (log2 of Duplicate Count))
                x_id = ComputeNodeByIndex(userIndex, out satelliteRootId);
            }
            if (x_id == NIL)
            {
                if (TreeAccessMethod.INDEX_ONLY == _accessMethod)
                {
                    throw ExceptionBuilder.RowOutOfRange(userIndex);
                }
                else
                {
                    throw ExceptionBuilder.InternalRBTreeError(RBTreeError.IndexOutOFRangeinGetNodeByIndex);
                }
            }
            return new NodePath(x_id, satelliteRootId);
        }

        private int ComputeNodeByIndex(int index, out int satelliteRootId)
        {
            index = unchecked(index + 1); // index is 0 based, while size is 1 based.
            satelliteRootId = NIL;
            int x_id = root;

            int rank = -1;
            while (x_id != NIL && !(((rank = SubTreeSize(Left(x_id)) + 1) == index) && Next(x_id) == NIL))
            {
                if (index < rank)
                {
                    x_id = Left(x_id);
                }
                else if (Next(x_id) != NIL && index >= rank && index <= rank + SubTreeSize(Next(x_id)) - 1)
                {
                    // node with matching index is in the associated satellite tree. continue searching for index in satellite tree.
                    satelliteRootId = x_id;
                    index = index - rank + 1; // rank is SubTreeSize(Node.left)+1, we do +1 here to offset +1 done in rank. index -= rank;
                    return ComputeNodeByIndex(Next(x_id), index); //satellite tree root
                }
                else
                {
                    if (Next(x_id) == NIL)
                        index -= rank;
                    else
                        index -= rank + SubTreeSize(Next(x_id)) - 1;

                    x_id = Right(x_id);
                }
            }
            return x_id;
        }

        private int ComputeNodeByIndex(int x_id, int index)
        {
            while (x_id != NIL)
            {
                Debug.Assert(NIL == Next(x_id), "has unexpected satellite tree");

                int y_id = Left(x_id);
                int rank = SubTreeSize(y_id) + 1;
                if (index < rank)
                {
                    x_id = y_id;
                }
                else if (rank < index)
                {
                    x_id = Right(x_id);
                    index -= rank;
                }
                else
                {
                    break;
                }
            }
            return x_id;
        }

#if DEBUG
        // return true if all nodes are unique; i.e. no satelite trees.
        public bool CheckUnique(int curNodeId)
        {
            if (curNodeId != NIL)
            {
                if (Next(curNodeId) != NIL)
                    return false;    // atleast 1 duplicate found

                if (!CheckUnique(Left(curNodeId)) || !CheckUnique(Right(curNodeId)))
                    return false;
            }

            return true;
        }
#endif

        public int Insert(K item)
        {
            int nodeId = GetNewNode(item);

            RBInsert(NIL, nodeId, NIL, -1, false);
            return nodeId;
        }

        // Begin: List of methods for making it easy to work with ArrayList

        public int Add(K? item) //Insert (int record)
        {
            int nodeId = GetNewNode(item);
            RBInsert(NIL, nodeId, NIL, -1, false);
            return nodeId;
        }

        public IEnumerator GetEnumerator()
        {
            return new RBTreeEnumerator(this);
        }

        // *****BruteForceImplementation*****
        //
        // iterate over all nodes, InOrder and return index of node with the specified Item
        // For the short term use a recursive method, later re-write it based on a stack data structure (if needed)
        public int IndexOf(int nodeId, K item)
        {
            int index = -1;
            // BIG ASSUMPTION: There is not satellite tree, this is INDEX_ONLY.
            if (nodeId != NIL)
            {
                if ((object)Key(nodeId)! == (object)item!)
                {
                    return GetIndexByNode(nodeId);
                }
                if ((index = IndexOf(Left(nodeId), item)) != -1)
                {
                    return index;
                }
                if ((index = IndexOf(Right(nodeId), item)) != -1)
                {
                    return index;
                }
            }

            return index;
        }

        public int Insert(int position, K item) //Insert (int record)
        {
            return InsertAt(position, item, false);
        }


        public int InsertAt(int position, K item, bool append)
        {
            int nodeId = GetNewNode(item);
            RBInsert(NIL, nodeId, NIL, position, append);
            return nodeId;
        }

        public void RemoveAt(int position)
        {
            DeleteByIndex(position);
        }

        public void Clear()
        {
            InitTree();
            unchecked { _version++; }
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(array));
            }
            if (index < 0)
            {
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(index));
            }

            int count = Count;
            if (array.Length - index < Count)
            {
                throw ExceptionBuilder.InvalidOffsetLength();
            }

            int x_id = Minimum(root);
            for (int i = 0; i < count; ++i)
            {
                array.SetValue(Key(x_id), index + i);
                x_id = Successor(x_id);
            }
        }

        public void CopyTo(K[] array, int index)
        {
            if (array == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(array));
            }
            if (index < 0)
            {
                throw ExceptionBuilder.ArgumentOutOfRange(nameof(index));
            }
            int count = Count;
            if (array.Length - index < Count)
            {
                throw ExceptionBuilder.InvalidOffsetLength();
            }

            int x_id = Minimum(root);
            for (int i = 0; i < count; ++i)
            {
                // Can't annotate generic array for element nullability
                array[index + i] = Key(x_id)!;
                x_id = Successor(x_id);
            }
        }

        // End: List of methods for making it easy to work with ArrayList

        private void SetRight(int nodeId, int rightNodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            page.Slots[slotIndex].rightId = rightNodeId;
            */
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._rightId = rightNodeId;
        }

        private void SetLeft(int nodeId, int leftNodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            page.Slots[slotIndex].leftId = leftNodeId;
            */
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._leftId = leftNodeId;
        }

        private void SetParent(int nodeId, int parentNodeId)
        {
            Debug.Assert(nodeId != NIL, " in SetParent  nodeId == NIL");
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            page.Slots[slotIndex].parentId = parentNodeId;
            */
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._parentId = parentNodeId;
        }

        private void SetColor(int nodeId, NodeColor color)
        {
            Debug.Assert(nodeId != NIL, " in SetColor  nodeId == NIL");
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            page.Slots[slotIndex].nodeColor = color;
            */
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._nodeColor = color;
        }

        private void SetKey(int nodeId, K key)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            page.Slots[slotIndex].keyOfNode = key;
            */
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._keyOfNode = key;
        }

        private void SetNext(int nodeId, int nextNodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            page.Slots[slotIndex].nextId = nextNodeId;
            */
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._nextId = nextNodeId;
        }

        private void SetSubTreeSize(int nodeId, int size)
        {
            Debug.Assert(nodeId != NIL &&
                         (size != 0 || _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._selfId == NIL) &&
                         (size != 1 || _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._nextId == NIL), "SetSize");

            // this improves performance by reducing the impact of this heavily used method
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._subTreeSize = size;
            VerifySize(nodeId, size);
        }

        private void IncreaseSize(int nodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            page.Slots[slotIndex].subTreeSize += 1;
            */
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._subTreeSize += 1;
        }


        private void RecomputeSize(int nodeId)
        {
            int myCorrectSize = SubTreeSize(Left(nodeId)) + SubTreeSize(Right(nodeId)) + (Next(nodeId) == NIL ? 1 : SubTreeSize(Next(nodeId)));
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            page.Slots[slotIndex].subTreeSize = myCorrectSize;
            */
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._subTreeSize = myCorrectSize;
        }

        private void DecreaseSize(int nodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            page.Slots[slotIndex].subTreeSize -= 1;
            */
            _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._subTreeSize -= 1;
            VerifySize(nodeId, _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._subTreeSize);
        }

        [ConditionalAttribute("DEBUG")]
        private void VerifySize(int nodeId, int size)
        {
            int myCorrectSize = SubTreeSize(Left(nodeId)) + SubTreeSize(Right(nodeId)) + (Next(nodeId) == NIL ? 1 : SubTreeSize(Next(nodeId)));
            Debug.Assert(myCorrectSize == size, "VerifySize");
        }

        public int Right(int nodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            int rightId = page.Slots[slotIndex].rightId;
            return rightId;
            */
            return (_pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._rightId);
        }

        public int Left(int nodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            int leftId = page.Slots[slotIndex].leftId;
            return leftId;
            */
            return (_pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._leftId);
        }

        public int Parent(int nodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            int parentId = page.Slots[slotIndex].parentId;
            return parentId;
            */
            return (_pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._parentId);
        }

        private NodeColor color(int nodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            NodeColor col = page.Slots[slotIndex].nodeColor;
            return col;
            */
            return (_pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._nodeColor);
        }

        public int Next(int nodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            int nextId = page.Slots[slotIndex].nextId;
            return nextId;
            */
            return (_pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._nextId);
        }

        public int SubTreeSize(int nodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            int size = page.Slots[slotIndex].subTreeSize;
            return size;
            */
            return (_pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._subTreeSize);
        }

        public K Key(int nodeId)
        {
            /*
            TreePage page = _pageTable[nodeId >> 16];
            int slotIndex = nodeId & 0xFFFF;
            K key = page.Slots[slotIndex].keyOfNode;
            return key;
            */
            return _pageTable[nodeId >> 16]!._slots[nodeId & 0xFFFF]._keyOfNode!;
        }

        private enum NodeColor
        {
            red = 0,
            black = 1,
        };

        private struct Node
        {
            internal int _selfId;
            internal int _leftId;
            internal int _rightId;
            internal int _parentId;
            internal int _nextId;      // multiple records associated with same key
            internal int _subTreeSize;     // number of nodes in subtree rooted at the current node
            internal K? _keyOfNode;
            internal NodeColor _nodeColor;
        }


        /// <summary>Represents the node in the tree and the satellite branch it took to get there.</summary>
        private readonly struct NodePath
        {
            /// <summary>Represents the node in the tree</summary>
            internal readonly int _nodeID;

            /// <summary>
            /// When not NIL, it represents the fact NodeID is has duplicate values in the tree.
            /// This is the 'fake' node in the main tree that redirects to the root of the satellite tree.
            /// By tracking this value, we don't have to repeatedly search for this node.
            /// </summary>
            internal readonly int _mainTreeNodeID;

            internal NodePath(int nodeID, int mainTreeNodeID)
            {
                _nodeID = nodeID;
                _mainTreeNodeID = mainTreeNodeID;
            }

#if VerifyPath
            internal void VerifyPath(RBTree<K> tree)
            {
                Debug.Assert(null != tree, "null tree");
                Debug.Assert((NIL == _nodeID && NIL == _mainTreeNodeID) || (NIL != _nodeID), "MainTreeNodeID is not NIL");

                if (NIL != _mainTreeNodeID)
                {
                    Debug.Assert(NIL != tree.Next(_mainTreeNodeID), "MainTreeNodeID should have a Next");
                    int node = _mainTreeNodeID;
                    while (NIL != tree.Parent(node))
                    {
                        node = tree.Parent(node);
                    }
                    Debug.Assert(tree.root == node, "MainTreeNodeID parent change doesn't align");
                }
                if (NIL != _nodeID)
                {
                    Debug.Assert(NIL == tree.Next(_nodeID), "NodeID should not have a Next");
                    int node = _nodeID;
                    if (NIL == _mainTreeNodeID)
                    {
                        while (NIL != tree.Parent(node))
                        {
                            node = tree.Parent(node);
                        }
                    }
                    else
                    {
                        while (NIL != tree.Parent(node))
                        {
                            Debug.Assert(NIL == tree.Next(node), "duplicate node should not have a next");
                            node = tree.Parent(node);
                        }
                    }
                    Debug.Assert((NIL == _mainTreeNodeID && tree.root == node) ||
                                 (tree.Next(_mainTreeNodeID) == node), "NodeID parent change doesn't align");
                }
            }
#endif
        }

        private sealed class TreePage
        {
            public const int slotLineSize = 32;

            internal readonly Node[] _slots;             // List of slots
            internal readonly int[] _slotMap;          // CEILING(slots.size/slotLineSize)
            private int _inUseCount;          // 0 to _slots.size
            private int _pageId;              // Page's Id
            private int _nextFreeSlotLine;    // o based position of next free slot line

            /*
             * size: number of slots per page. Maximum allowed is 64K
             */
            internal TreePage(int size)
            {
                if (size > 64 * 1024)
                {
                    throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidPageSize);
                }
                _slots = new Node[size];
                _slotMap = new int[(size + slotLineSize - 1) / slotLineSize];
            }

            /*
             * Allocate a free slot from the current page belonging to the specified tree.
             * return the Id of the allocated slot, or -1 if the current page does not have any free slots.
             */
            internal int AllocSlot(RBTree<K> tree)
            {
                int segmentPos = 0;  // index into _SlotMap
                int freeSlot = 0;  // Uint, slot offset within the segment
                int freeSlotId = -1; // 0 based slot position

                if (_inUseCount < _slots.Length)
                {
                    segmentPos = _nextFreeSlotLine;
                    while (segmentPos < _slotMap.Length)
                    {
                        if (unchecked((uint)_slotMap[segmentPos]) < 0xFFFFFFFF)
                        {
                            freeSlotId = 0;
                            freeSlot = (~(_slotMap[segmentPos])) & unchecked(_slotMap[segmentPos] + 1);

                            // avoid string concat to allow debug code to run faster
                            Debug.Assert((_slotMap[segmentPos] & freeSlot) == 0, "Slot position segment[segmentPos ]: [freeSlot] is in use. Expected to be empty");

                            _slotMap[segmentPos] |= freeSlot; //mark free slot as used.
                            _inUseCount++;
                            if (_inUseCount == _slots.Length) // mark page as full
                                tree.MarkPageFull(this);
                            tree._inUseNodeCount++;

                            // convert freeSlotPos to int value giving number of 0's to its right i.e. freeSlotId
                            freeSlotId = GetIntValueFromBitMap(unchecked((uint)freeSlot));

                            _nextFreeSlotLine = segmentPos;
                            freeSlotId = (segmentPos * TreePage.slotLineSize) + freeSlotId;
                            break;
                        }
                        else
                        {
                            segmentPos++;
                        }
                    }

                    if (freeSlotId == -1 && _nextFreeSlotLine != 0)
                    {
                        //Try one more time, starting from 0th segment position to locate a free slot.
                        _nextFreeSlotLine = 0;
                        freeSlotId = AllocSlot(tree);
                    }
                }

                return freeSlotId; // 0 based slot position
            }

            internal int InUseCount
            {
                get { return _inUseCount; }
                set { _inUseCount = value; }
            }

            internal int PageId
            {
                get { return _pageId; }
                set { _pageId = value; }
            }
        }


        // this improves performance allowing to iterating of the index instead of computing record by index
        // changes are required to handle satellite nodes which do not exist in DataRowCollection
        // enumerator over index will not be handed to the user, only used internally

        // instance of this enumerator will be handed to the user via DataRowCollection.GetEnumerator()
        internal struct RBTreeEnumerator : IEnumerator<K>, IEnumerator
        {
            private readonly RBTree<K> _tree;
            private readonly int _version;
            private int _index, _mainTreeNodeId;
            private K? _current;

            internal RBTreeEnumerator(RBTree<K> tree)
            {
                _tree = tree;
                _version = tree._version;
                _index = NIL;
                _mainTreeNodeId = tree.root;
                _current = default(K);
            }

            internal RBTreeEnumerator(RBTree<K> tree, int position)
            {
                _tree = tree;
                _version = tree._version;
                if (0 == position)
                {
                    _index = NIL;
                    _mainTreeNodeId = tree.root;
                }
                else
                {
                    _index = tree.ComputeNodeByIndex(position - 1, out _mainTreeNodeId);
                    if (NIL == _index)
                    {
                        throw ExceptionBuilder.InternalRBTreeError(RBTreeError.IndexOutOFRangeinGetNodeByIndex);
                    }
                }
                _current = default(K);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_version != _tree._version)
                {
                    throw ExceptionBuilder.EnumeratorModified();
                }

                bool hasCurrent = _tree.Successor(ref _index, ref _mainTreeNodeId);
                _current = _tree.Key(_index);
                return hasCurrent;
            }

            public K Current
            {
                get
                {
                    // TODO: Should throw if MoveNext hasn't been called
                    return _current!;
                }
            }

            object? IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _tree._version)
                {
                    throw ExceptionBuilder.EnumeratorModified();
                }

                _index = NIL;
                _mainTreeNodeId = _tree.root;
                _current = default(K);
            }
        }
    }
}
