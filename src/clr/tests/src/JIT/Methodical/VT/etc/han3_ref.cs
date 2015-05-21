// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitTest
{
    internal struct Ring
    {
        public int size;
    }

    internal struct Column
    {
        public Ring[] rings;
        private int[] _heightPtr;

        public int height
        {
            get { return _heightPtr[0]; }
            set { _heightPtr[0] = value; }
        }

        public Column(int maxHeight, int curHeight)
        {
            rings = new Ring[maxHeight];
            for (int i = 0; i < curHeight; i++)
            {
                rings[i].size = maxHeight - i;
            }
            _heightPtr = new int[] { curHeight };
        }

        public void Validate()
        {
            for (int i = 1; i < _heightPtr[0]; i++)
            {
                if (rings[i - 1].size <= rings[i].size)
                    throw new Exception();
            }
        }

        private static void move1(ref Column from, ref Column to)
        {
            to.rings[to.height] = from.rings[from.height - 1];
            to.height = to.height + 1;
            from.height = from.height - 1;
            to.Validate();
            from.Validate();
        }

        private static int move(ref Column from, ref Column to, ref Column temp, int num)
        {
            to.Validate();
            from.Validate();
            temp.Validate();
            int C = 1;
            if (num == 1)
            {
                move1(ref from, ref to);
            }
            else
            {
                C += move(ref from, ref temp, ref to, num - 1);
                move1(ref from, ref to);
                C += move(ref temp, ref to, ref from, num - 1);
            }
            return C;
        }

        private static int Main()
        {
            Column c1 = new Column(17, 17);
            Column c2 = new Column(17, 0);
            Column c3 = new Column(17, 0);
            int ec = move(ref c1, ref c2, ref c3, 17) - 130971;
            c1.Validate();
            c2.Validate();
            c3.Validate();
            return ec;
        }
    }
}
