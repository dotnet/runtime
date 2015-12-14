// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JitInliningTest
{
    internal interface IDimensions
    {
        float Length();
        float Width();
    }

    public class Box : IDimensions
    {
        private float _lengthInches;
        private float _widthInches;

        public Box(float length, float width)
        {
            _lengthInches = length;
            _widthInches = width;
        }
        public float Length()
        {
            return _lengthInches;
        }
        public float Width()
        {
            return _widthInches;
        }
        float IDimensions.Length()
        {
            return _lengthInches;
        }
        float IDimensions.Width()
        {
            return _widthInches;
        }
    }

    internal class InterfaceCall
    {
        public static int Main()
        {
            Box myBox = new Box(30.0f, 20.0f);
            IDimensions myDimensions = (IDimensions)myBox;
            float a = myBox.Length();
            a += myBox.Width();
            a += myDimensions.Length();
            a += myDimensions.Width();
            return (int)a;
        }
    }
}

