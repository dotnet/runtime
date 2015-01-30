// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// MemberFilter is a delegate used to filter Members.  This delegate is used
// 
//    as a callback from Type.FindMembers.
//
//
namespace System.Reflection {
    
    // Define the delegate
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public delegate bool MemberFilter(MemberInfo m, Object filterCriteria);
}
