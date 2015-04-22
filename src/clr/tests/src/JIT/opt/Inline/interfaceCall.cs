// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
namespace JitInliningTest
{

    // Interface methods
    interface IDimensions
    {
        float Length();
        float Width();
    }

    public class Box : IDimensions
    {
        float lengthInches;
        float widthInches;

        public Box(float length, float width)
        {
            lengthInches = length;
            widthInches = width;
        }
        // Interface member implementation: 
        public float Length()
        {
            return lengthInches;
        }
        // Interface member implementation:
        public float Width()
        {
            return widthInches;
        }
        // Explicit interface member implementation: 
        float IDimensions.Length()
        {
            return lengthInches;
        }
        // Explicit interface member implementation:
        float IDimensions.Width()
        {
            return widthInches;
        }
    }

    class InterfaceCall
    {
        public static int Main()
        {
            // Declare a class instance "myBox":
            Box myBox = new Box(30.0f, 20.0f);
            // Declare an interface instance "myDimensions":
            IDimensions myDimensions = (IDimensions)myBox;
            // Interface call through class instance
            float a = myBox.Length();
            a += myBox.Width();
            // Explicit interface method call
            a += myDimensions.Length();
            a += myDimensions.Width();
            return (int)a;
        }
    }
}

