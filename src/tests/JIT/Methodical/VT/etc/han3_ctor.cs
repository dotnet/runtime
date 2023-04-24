// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_han3_ctor_cs
{
    public struct Ring
    {
        public int size;
    }

    public struct Column
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

        private static void move1(Column from, Column to)
        {
            to.rings[to.height] = from.rings[from.height - 1];
            to.height = to.height + 1;
            from.height = from.height - 1;
            to.Validate();
            from.Validate();
        }

        private static int move(Column from, Column to, Column temp, int num)
        {
            to.Validate();
            from.Validate();
            temp.Validate();
            int C = 1;
            if (num == 1)
            {
                move1(from, to);
            }
            else
            {
                C += move(from, temp, to, num - 1);
                move1(from, to);
                C += move(temp, to, from, num - 1);
            }
            return C;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            return move(new Column(17, 17),
                        new Column(17, 0),
                        new Column(17, 0),
                        17) - 130971;
        }
    }
}
