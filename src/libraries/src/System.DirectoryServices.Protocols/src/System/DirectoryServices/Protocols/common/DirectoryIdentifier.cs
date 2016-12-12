//------------------------------------------------------------------------------
// <copyright file="DirectoryIdentifier.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.Protocols {
     using System;

     public abstract class DirectoryIdentifier {
        protected DirectoryIdentifier() 
        {
            Utility.CheckOSVersion();
        }
     }
 }
