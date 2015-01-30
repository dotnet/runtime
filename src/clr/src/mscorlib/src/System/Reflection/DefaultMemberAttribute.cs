// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// DefaultMemberAttribute is defines the Member of a Type that is the "default"
// 
//    member used by Type.InvokeMember.  The default member is simply a name given
//    to a type.
//
// 
// 
//
namespace System.Reflection {
    
    using System;

[Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class DefaultMemberAttribute : Attribute
    {
        // The name of the member
        private String m_memberName;

        // You must provide the name of the member, this is required
        public DefaultMemberAttribute(String memberName) {
            m_memberName = memberName;
        }

        // A get accessor to return the name from the attribute.
        // NOTE: There is no setter because the name must be provided
        //    to the constructor.  The name is not optional.
        public String MemberName {
            get {return m_memberName;}
        }
    }
}
