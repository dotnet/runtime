// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Diagnostics;

namespace System.Xml
{
    // Array index to indicate the meaning of the each byte.
    internal enum TernaryTreeByte { characterByte = 0, leftTree = 1, rightTree = 2, data = 3 };

    //
    // XSL HTML output method properties
    //
    // Keep the first four bits in sync, so that the element and attribute mask operation can be combined.
    internal enum ElementProperties : uint { DEFAULT = 0, URI_PARENT = 1, BOOL_PARENT = 2, NAME_PARENT = 4, EMPTY = 8, NO_ENTITIES = 16, HEAD = 32, BLOCK_WS = 64, HAS_NS = 128 }
    internal enum AttributeProperties : uint { DEFAULT = 0, URI = 1, BOOLEAN = 2, NAME = 4 }


    /**
     * TernaryTreeRO
     * -------------
     *
     * Ternary tree implementation used to make fast dictionary lookups in pre-generated
     * ternary trees.
     *
     * Note: Only strings composed of ASCII characters can exist in the tree.
     */
    /// <include file='doc\ReadOnlyTernaryTree.uex' path='docs/doc[@for="TernaryTreeReadOnly"]/*' />
    internal class TernaryTreeReadOnly
    {
        private byte[] _nodeBuffer;

        //define the array positions


        /// <include file='doc\ReadOnlyTernaryTree.uex' path='docs/doc[@for="TernaryTreeReadOnly.TernaryTreeReadOnly"]/*' />
        public TernaryTreeReadOnly(byte[] nodeBuffer)
        {
            _nodeBuffer = nodeBuffer;
        }

        /*  ----------------------------------------------------------------------------
            findStringI()

            Find a Unicode string in the ternary tree and return the data byte it's
            mapped to.  Find is case-insensitive.
        */
        /// <include file='doc\ReadOnlyTernaryTree.uex' path='docs/doc[@for="TernaryTreeReadOnly.FindCaseInsensitiveString"]/*' />
        public byte FindCaseInsensitiveString(String stringToFind)
        {
            //Debug.Assert(wszFind != null && wszFind.Length != 0);

            int stringPos = 0, nodePos = 0;
            int charToFind, charInTheTree;
            byte[] node = _nodeBuffer;

            charToFind = stringToFind[stringPos];

            if (charToFind > 'z') return 0;                    // Ternary tree only stores ASCII strings
            if (charToFind >= 'a') charToFind -= ('a' - 'A');     // Normalize to upper case

            while (true)
            {
                int pos = nodePos * 4;

                charInTheTree = node[pos + (int)TernaryTreeByte.characterByte];
                //Console.WriteLine("charToFind: {0},charInTheTree: {1}, nodePos: {2}", charToFind, charInTheTree, nodePos);

                if (charToFind < charInTheTree)
                {
                    // If input character is less than the tree character, take the left branch
                    if (node[pos + (int)TernaryTreeByte.leftTree] == 0x0)
                    {
                        break;
                    }
                    nodePos = nodePos + node[pos + (int)TernaryTreeByte.leftTree];
                }
                else if (charToFind > charInTheTree)
                {
                    // If input character is greater than the tree character, take the right branch
                    if (node[pos + (int)TernaryTreeByte.rightTree] == 0x0)
                        break;
                    nodePos = nodePos + node[pos + (int)TernaryTreeByte.rightTree];
                }
                else
                {
                    // If input character is equal to the tree character, take the equal branch
                    if (charToFind == 0)
                        return node[pos + (int)TernaryTreeByte.data];

                    // The offset for the equal branch is always one
                    ++nodePos;

                    // Move to the next input character
                    ++stringPos;
                    if (stringPos == stringToFind.Length)
                    {
                        charToFind = 0;
                    }
                    else
                    {
                        charToFind = stringToFind[stringPos];
                        if (charToFind > 'z') return 0;                     // Ternary tree only stores ASCII strings
                        if (charToFind >= 'a') charToFind -= ('a' - 'A');   // Normalize to upper case
                    }
                }
            }

            // Return default
            return 0;
        }
    }
}
