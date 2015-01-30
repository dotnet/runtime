// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// The enumeration constants used in NumberFormatInfo.DigitSubstitution.
//
namespace System.Globalization {    


    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum DigitShapes : int {

        Context         = 0x0000,   // The shape depends on the previous text in the same output.

        None            = 0x0001,   // Gives full Unicode compatibility.

        NativeNational  = 0x0002,   // National shapes determined by LOCALE_SNATIVEDIGITS
    }
}

