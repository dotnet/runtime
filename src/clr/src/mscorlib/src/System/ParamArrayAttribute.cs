// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Container for assemblies.
**
**
=============================================================================*/
namespace System
{
//This class contains only static members and does not need to be serializable 
   [AttributeUsage (AttributeTargets.Parameter, Inherited=true, AllowMultiple=false)]
[System.Runtime.InteropServices.ComVisible(true)]
   public sealed class ParamArrayAttribute : Attribute
   {
      public ParamArrayAttribute () {}  
   }
}
