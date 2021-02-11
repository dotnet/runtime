// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DoubLink
{
    public class DoubLink
    {
        internal DLinkNode[] Mv_DLink;

        public DoubLink(int Num) : this(Num, false)
        {
        }

        public DoubLink(int Num, bool large)
        {
            Mv_DLink = new DLinkNode[Num];

            if (Num == 0)
            {
                return;
            }

            if (Num == 1)
            {
                // only one element
                Mv_DLink[0] = new DLinkNode((large ? 250 : 1), Mv_DLink[0], Mv_DLink[0]);
                return;
            }

            // first element
            Mv_DLink[0] = new DLinkNode((large ? 250 : 1), Mv_DLink[Num - 1], Mv_DLink[1]);

            // all elements in between
            for (int i = 1; i < Num - 1; i++)
            {
                Mv_DLink[i] = new DLinkNode((large ? 250 : i + 1), Mv_DLink[i - 1], Mv_DLink[i + 1]);
            }

            // last element
            Mv_DLink[Num - 1] = new DLinkNode((large ? 250 : Num), Mv_DLink[Num - 2], Mv_DLink[0]);
        }


        public int NodeNum
        {
            get
            {
                return Mv_DLink.Length;
            }
        }


        public DLinkNode this[int index]
        {
            get
            {
                return Mv_DLink[index];
            }

            set
            {
                Mv_DLink[index] = value;
            }
        }
    }

    public class DLinkNode
    {
        // disabling unused variable warning
#pragma warning disable 0414
        internal DLinkNode Last;
        internal DLinkNode Next;
        internal int[] Size;
#pragma warning restore 0414

        public static int FinalCount = 0;

        public DLinkNode(int SizeNum, DLinkNode LastObject, DLinkNode NextObject)
        {
            Last = LastObject;
            Next = NextObject;
            Size = new int[SizeNum * 1024];
        }

        ~DLinkNode()
        {
            FinalCount++;
        }
    }
}
