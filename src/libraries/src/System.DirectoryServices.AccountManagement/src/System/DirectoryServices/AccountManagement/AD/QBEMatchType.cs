/*++

Copyright (c) 2007  Microsoft Corporation

Module Name:

    QBEMatchType.cs

Abstract:


History:

    5-3-2007    TQuerec     Created

--*/

using System;

namespace System.DirectoryServices.AccountManagement
{
    internal class QbeMatchType
    {
        private object value;
        private MatchType matchType;
        
        internal QbeMatchType(object value, MatchType matchType) 
        {  
            this.value = value;
            this.matchType = matchType;
        }

        internal object Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
            }
        }
            
        internal MatchType Match        {
            get
            {
                return this.matchType;
            }   
            set
            {
                this.matchType = value;
            }
        }
            


    }
}
