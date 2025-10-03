// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}

.assembly extern ResourceAssembly
{
  .publickeytoken = (00 00 00 00 00 00 00 00)
}

.assembly ManifestResourceAssemblyRef { }

.mresource public 'MyResource'
{
  .assembly extern 'ResourceAssembly'
}
