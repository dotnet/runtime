//------------------------------------------------------------------------------
// <copyright file="DsmlDirectoryIdentifier.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
 namespace System.DirectoryServices.Protocols {
     using System;
     using System.Globalization;

     public class DsmlDirectoryIdentifier :DirectoryIdentifier
     {
         // private members
         Uri uri = null;

         
         public DsmlDirectoryIdentifier(Uri serverUri)
         {
             if (serverUri == null)
             {
                 throw new ArgumentNullException("serverUri");
             }

             //   Is it a http or https Uri?
             if ( (String.Compare(serverUri.Scheme, "http", StringComparison.OrdinalIgnoreCase) != 0) &&
                (String.Compare(serverUri.Scheme, "https", StringComparison.OrdinalIgnoreCase) != 0) )
             {                 
                 throw new ArgumentException(Res.GetString(Res.DsmlNonHttpUri));
             }

             this.uri = serverUri;
         }

         
         public Uri ServerUri {
            get {
                return uri;
            }
         }
     }
 }
