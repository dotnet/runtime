// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

