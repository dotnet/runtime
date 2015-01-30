// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// TypeFilter defines a delegate that is as a callback function for filtering
// 
//    a list of Types.
//
//
namespace System.Reflection {
    
    // Define the delegate
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public delegate bool TypeFilter(Type m, Object filterCriteria);
}
