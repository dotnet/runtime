// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml
{
    //
    // XSL HTML output method properties
    //
    // Keep the first four bits in sync, so that the element and attribute mask operation can be combined.
    internal enum ElementProperties : uint
    {
        DEFAULT = 0,
        URI_PARENT = 1,
        BOOL_PARENT = 2,
        NAME_PARENT = 4,
        EMPTY = 8,
        NO_ENTITIES = 16,
        HEAD = 32,
        BLOCK_WS = 64,
        HAS_NS = 128
    }

    internal enum AttributeProperties : uint
    {
        DEFAULT = 0,
        URI = 1,
        BOOLEAN = 2,
        NAME = 4
    }


    /**
     * TernaryTreeRO
     * -------------
     *
     * Ternary tree implementation used to make fast dictionary lookups in pre-generated
     * ternary trees.
     *
     * Note: Only strings composed of ASCII characters can exist in the tree.
     */
    internal static class TernaryTreeReadOnly
    {
        // Array index to indicate the meaning of the each byte.
        private enum TernaryTreeByte
        {
            CharacterByte = 0,
            LeftTree = 1,
            RightTree = 2,
            Data = 3
        }
        private static ReadOnlySpan<byte> HtmlElements =>
        [
            73, 4, 147, 0, 77, 140, 162, 0, 71, 0, 0, 0, 0, 0, 0, 11, 68, 4, 85, 0, 73, 71, 92, 0, 86, 81, 0, 0,
            0, 0, 0, 64, 66, 3, 45, 0, 82, 21, 55, 0, 0, 0, 0, 8, 65, 0, 0, 0, 82, 4, 0, 0, 69, 0, 0, 0,
            65, 0, 0, 0, 0, 0, 0, 75, 68, 7, 8, 0, 68, 0, 0, 0, 82, 0, 0, 0, 69, 0, 0, 0, 83, 0, 0, 0,
            83, 0, 0, 0, 0, 0, 0, 64, 0, 0, 0, 1, 80, 0, 0, 0, 80, 0, 0, 0, 76, 0, 0, 0, 69, 0, 0, 0,
            84, 0, 0, 0, 0, 0, 0, 64, 65, 0, 9, 0, 83, 0, 0, 0, 69, 0, 0, 0, 70, 5, 0, 0, 79, 0, 0, 0,
            78, 0, 0, 0, 84, 0, 0, 0, 0, 0, 0, 72, 0, 0, 0, 73, 76, 0, 10, 0, 79, 0, 0, 0, 67, 0, 0, 0,
            75, 0, 0, 0, 81, 0, 0, 0, 85, 0, 0, 0, 79, 0, 0, 0, 84, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 65,
            79, 0, 0, 0, 68, 0, 0, 0, 89, 0, 0, 0, 0, 0, 0, 64, 67, 0, 0, 0, 79, 3, 0, 0, 76, 0, 0, 0,
            0, 0, 22, 72, 65, 0, 13, 0, 80, 0, 0, 0, 84, 0, 0, 0, 73, 0, 0, 0, 79, 0, 0, 0, 78, 0, 0, 0,
            0, 0, 0, 64, 85, 0, 0, 0, 84, 0, 0, 0, 84, 0, 0, 0, 79, 0, 0, 0, 78, 0, 0, 0, 0, 0, 0, 2,
            69, 0, 0, 0, 78, 0, 0, 0, 84, 0, 0, 0, 69, 0, 0, 0, 82, 0, 0, 0, 0, 0, 0, 64, 68, 0, 8, 0,
            0, 0, 0, 64, 71, 0, 0, 0, 82, 0, 0, 0, 79, 0, 0, 0, 85, 0, 0, 0, 80, 0, 0, 0, 0, 0, 0, 64,
            69, 0, 0, 0, 76, 0, 0, 0, 0, 0, 0, 65, 82, 0, 0, 0, 0, 0, 0, 66, 72, 3, 0, 0, 50, 31, 33, 0,
            0, 0, 0, 64, 70, 0, 0, 0, 79, 8, 16, 0, 78, 0, 20, 0, 84, 0, 0, 0, 0, 0, 0, 64, 84, 2, 0, 0,
            0, 0, 0, 64, 76, 0, 0, 0, 0, 0, 0, 66, 73, 0, 0, 0, 69, 0, 0, 0, 76, 0, 0, 0, 68, 0, 0, 0,
            83, 0, 0, 0, 69, 0, 0, 0, 84, 0, 0, 0, 0, 0, 0, 64, 82, 0, 0, 0, 65, 0, 0, 0, 77, 0, 0, 0,
            69, 0, 0, 0, 0, 0, 4, 74, 82, 0, 0, 0, 77, 0, 0, 0, 0, 0, 0, 65, 83, 0, 0, 0, 69, 0, 0, 0,
            84, 0, 0, 0, 0, 0, 0, 64, 49, 0, 0, 0, 0, 0, 0, 64, 54, 2, 8, 0, 0, 0, 0, 64, 52, 2, 4, 0,
            0, 0, 0, 64, 51, 0, 0, 0, 0, 0, 0, 64, 53, 0, 0, 0, 0, 0, 0, 64, 82, 2, 6, 0, 0, 0, 0, 74,
            69, 0, 0, 0, 65, 0, 0, 0, 68, 0, 0, 0, 0, 0, 0, 97, 84, 0, 0, 0, 77, 0, 0, 0, 76, 0, 0, 0,
            0, 0, 0, 64, 70, 0, 0, 0, 82, 0, 0, 0, 65, 0, 0, 0, 77, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 64,
            80, 4, 84, 0, 82, 77, 0, 0, 69, 0, 0, 0, 0, 0, 0, 64, 77, 5, 37, 0, 69, 30, 0, 0, 84, 32, 0, 0,
            65, 0, 0, 0, 0, 0, 0, 72, 76, 0, 0, 0, 69, 0, 20, 0, 71, 0, 0, 0, 69, 0, 0, 0, 78, 0, 0, 0,
            68, 0, 0, 0, 0, 0, 0, 64, 78, 0, 7, 0, 83, 2, 0, 0, 0, 0, 0, 65, 80, 0, 0, 0, 85, 0, 0, 0,
            84, 0, 0, 0, 0, 0, 0, 11, 83, 0, 0, 0, 73, 0, 0, 0, 78, 0, 0, 0, 68, 0, 0, 0, 69, 0, 0, 0,
            88, 0, 0, 0, 0, 0, 0, 72, 73, 0, 0, 0, 78, 3, 0, 0, 75, 0, 0, 0, 0, 0, 0, 73, 0, 0, 0, 64,
            65, 0, 0, 0, 80, 0, 0, 0, 0, 0, 0, 64, 78, 0, 0, 0, 85, 0, 0, 0, 0, 0, 0, 66, 79, 3, 0, 0,
            76, 18, 24, 0, 0, 0, 0, 66, 78, 0, 0, 0, 79, 0, 0, 0, 83, 7, 0, 0, 67, 0, 0, 0, 82, 0, 0, 0,
            73, 0, 0, 0, 80, 0, 0, 0, 84, 0, 0, 0, 0, 0, 0, 64, 70, 0, 0, 0, 82, 0, 0, 0, 65, 0, 0, 0,
            77, 0, 0, 0, 69, 0, 0, 0, 83, 0, 0, 0, 0, 0, 0, 64, 66, 0, 0, 0, 74, 0, 0, 0, 69, 0, 0, 0,
            67, 0, 0, 0, 84, 0, 0, 0, 0, 0, 0, 3, 80, 0, 0, 0, 84, 0, 0, 0, 73, 4, 0, 0, 79, 0, 0, 0,
            78, 0, 0, 0, 0, 0, 0, 66, 71, 0, 0, 0, 82, 0, 0, 0, 79, 0, 0, 0, 85, 0, 0, 0, 80, 0, 0, 0,
            0, 0, 0, 66, 0, 0, 1, 64, 65, 0, 0, 0, 82, 0, 0, 0, 65, 0, 0, 0, 77, 0, 0, 0, 0, 0, 0, 72,
            84, 3, 65, 0, 68, 28, 38, 0, 0, 0, 0, 66, 83, 8, 0, 0, 69, 6, 15, 0, 76, 0, 0, 0, 69, 0, 0, 0,
            67, 0, 0, 0, 84, 0, 0, 0, 0, 0, 0, 2, 0, 0, 3, 64, 81, 0, 0, 0, 0, 0, 0, 1, 67, 0, 0, 0,
            82, 0, 0, 0, 73, 0, 0, 0, 80, 0, 0, 0, 84, 0, 0, 0, 0, 0, 0, 19, 84, 0, 0, 0, 89, 4, 0, 0,
            76, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 80, 82, 0, 0, 0, 73, 0, 0, 0, 75, 0, 0, 0, 69, 0, 0, 0,
            0, 0, 0, 64, 65, 0, 5, 0, 66, 0, 0, 0, 76, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 65, 66, 0, 0, 0,
            79, 0, 0, 0, 68, 0, 0, 0, 89, 0, 0, 0, 0, 0, 0, 64, 72, 5, 19, 0, 69, 17, 0, 0, 65, 0, 0, 0,
            68, 0, 0, 0, 0, 0, 0, 64, 70, 5, 0, 0, 79, 0, 0, 0, 79, 0, 0, 0, 84, 0, 0, 0, 0, 0, 0, 64,
            69, 0, 0, 0, 88, 0, 0, 0, 84, 0, 0, 0, 65, 0, 0, 0, 82, 0, 0, 0, 69, 0, 0, 0, 65, 0, 0, 0,
            0, 0, 0, 2, 0, 0, 0, 66, 82, 2, 0, 0, 0, 0, 0, 64, 73, 0, 0, 0, 84, 0, 0, 0, 76, 0, 0, 0,
            69, 0, 0, 0, 0, 0, 0, 64, 85, 0, 3, 0, 76, 0, 0, 0, 0, 0, 0, 66, 88, 0, 0, 0, 77, 0, 0, 0,
            80, 0, 0, 0, 0, 0, 0, 64,
        ];
        private static ReadOnlySpan<byte> HtmlAttributes =>
        [
            72, 5, 77, 0, 82, 0, 0, 0, 69, 0, 0, 0, 70, 0, 0, 0, 0, 0, 0, 1, 67, 12, 40, 0, 79, 7, 0, 0,
            77, 31, 0, 0, 80, 0, 0, 0, 65, 0, 0, 0, 67, 0, 0, 0, 84, 0, 0, 0, 0, 0, 0, 2, 73, 11, 18, 0,
            84, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 1, 65, 0, 0, 0, 67, 0, 0, 0, 84, 0, 0, 0, 73, 0, 0, 0,
            79, 0, 0, 0, 78, 0, 0, 0, 0, 0, 0, 1, 72, 0, 0, 0, 69, 0, 0, 0, 67, 0, 0, 0, 75, 0, 0, 0,
            69, 0, 0, 0, 68, 0, 0, 0, 0, 0, 0, 2, 76, 0, 0, 0, 65, 0, 0, 0, 83, 0, 0, 0, 83, 0, 0, 0,
            73, 0, 0, 0, 68, 0, 0, 0, 0, 0, 0, 1, 68, 0, 0, 0, 69, 0, 0, 0, 66, 0, 0, 0, 65, 0, 0, 0,
            83, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 1, 68, 0, 28, 0, 69, 7, 15, 0, 67, 0, 22, 0, 76, 0, 0, 0,
            65, 0, 0, 0, 82, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 2, 65, 0, 0, 0, 84, 0, 0, 0, 65, 0, 0, 0,
            0, 0, 1, 1, 83, 0, 0, 0, 82, 0, 0, 0, 67, 0, 0, 0, 0, 0, 0, 1, 73, 0, 0, 0, 83, 0, 0, 0,
            65, 0, 0, 0, 66, 0, 0, 0, 76, 0, 0, 0, 69, 0, 0, 0, 68, 0, 0, 0, 0, 0, 0, 2, 70, 0, 0, 0,
            69, 0, 0, 0, 82, 0, 0, 0, 0, 0, 0, 2, 70, 0, 0, 0, 79, 0, 0, 0, 82, 0, 0, 0, 0, 0, 0, 1,
            78, 8, 48, 0, 79, 36, 0, 0, 83, 30, 55, 0, 72, 0, 0, 0, 65, 0, 0, 0, 68, 0, 0, 0, 69, 0, 0, 0,
            0, 0, 0, 2, 77, 9, 0, 0, 85, 0, 0, 0, 76, 0, 0, 0, 84, 0, 0, 0, 73, 0, 0, 0, 80, 0, 0, 0,
            76, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 2, 73, 0, 6, 0, 83, 0, 0, 0, 77, 0, 0, 0, 65, 0, 0, 0,
            80, 0, 0, 0, 0, 0, 0, 2, 76, 0, 0, 0, 79, 0, 0, 0, 78, 0, 0, 0, 71, 0, 0, 0, 68, 0, 0, 0,
            69, 0, 0, 0, 83, 0, 0, 0, 67, 0, 0, 0, 0, 0, 0, 1, 72, 0, 9, 0, 82, 0, 0, 0, 69, 0, 0, 0,
            70, 0, 0, 0, 0, 0, 0, 2, 65, 0, 0, 0, 77, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 1, 82, 0, 0, 0,
            69, 0, 0, 0, 83, 0, 0, 0, 73, 0, 0, 0, 90, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 2, 82, 14, 22, 0,
            69, 0, 0, 0, 65, 0, 0, 0, 68, 0, 0, 0, 79, 0, 0, 0, 78, 0, 0, 0, 76, 0, 0, 0, 89, 0, 0, 0,
            0, 0, 0, 2, 87, 0, 0, 0, 82, 0, 0, 0, 65, 0, 0, 0, 80, 0, 0, 0, 0, 0, 0, 2, 80, 0, 0, 0,
            82, 0, 0, 0, 79, 0, 0, 0, 70, 0, 0, 0, 73, 0, 0, 0, 76, 0, 0, 0, 69, 0, 0, 0, 0, 0, 0, 1,
            83, 0, 12, 0, 82, 3, 0, 0, 67, 0, 0, 0, 0, 0, 0, 1, 69, 0, 0, 0, 76, 0, 0, 0, 69, 0, 0, 0,
            67, 0, 0, 0, 84, 0, 0, 0, 69, 0, 0, 0, 68, 0, 0, 0, 0, 0, 0, 2, 85, 0, 0, 0, 83, 0, 0, 0,
            69, 0, 0, 0, 77, 0, 0, 0, 65, 0, 0, 0, 80, 0, 0, 0, 0, 0, 0, 1,
        ];
        public static ElementProperties FindElementProperty(ReadOnlySpan<char> stringToFind)
        {
            return (ElementProperties)FindCaseInsensitiveString(stringToFind, HtmlElements);
        }
        public static AttributeProperties FindAttributeProperty(ReadOnlySpan<char> stringToFind)
        {
            return (AttributeProperties)FindCaseInsensitiveString(stringToFind, HtmlAttributes);
        }

        /*  ----------------------------------------------------------------------------
            findStringI()

            Find a Unicode string in the ternary tree and return the data byte it's
            mapped to.  Find is case-insensitive.
        */
        private static byte FindCaseInsensitiveString(ReadOnlySpan<char> stringToFind, ReadOnlySpan<byte> nodeBuffer)
        {
            int stringPos = 0, nodePos = 0;
            int charToFind = stringToFind[stringPos];

            if (charToFind > 'z')
            {
                return 0; // Ternary tree only stores ASCII strings
            }

            if (charToFind >= 'a')
            {
                charToFind -= 'a' - 'A'; // Normalize to upper case
            }

            while (true)
            {
                int pos = nodePos * 4;
                int charInTheTree = nodeBuffer[pos + (int)TernaryTreeByte.CharacterByte];

                if (charToFind < charInTheTree)
                {
                    // If input character is less than the tree character, take the left branch
                    if (nodeBuffer[pos + (int)TernaryTreeByte.LeftTree] == 0x0)
                    {
                        break;
                    }

                    nodePos += nodeBuffer[pos + (int)TernaryTreeByte.LeftTree];
                }
                else if (charToFind > charInTheTree)
                {
                    // If input character is greater than the tree character, take the right branch
                    if (nodeBuffer[pos + (int)TernaryTreeByte.RightTree] == 0x0)
                    {
                        break;
                    }

                    nodePos += nodeBuffer[pos + (int)TernaryTreeByte.RightTree];
                }
                else
                {
                    // If input character is equal to the tree character, take the equal branch
                    if (charToFind == 0)
                    {
                        return nodeBuffer[pos + (int)TernaryTreeByte.Data];
                    }

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

                        if (charToFind > 'z')
                        {
                            return 0; // Ternary tree only stores ASCII strings
                        }

                        if (charToFind >= 'a')
                        {
                            charToFind -= 'a' - 'A'; // Normalize to upper case
                        }
                    }
                }
            }

            return 0;
        }
    }
}
