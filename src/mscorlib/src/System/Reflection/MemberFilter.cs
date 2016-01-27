// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
