// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#:include ECallManifest.cs
#:include ECallManifestGenerator.cs
#:include ../../tools/illink/src/ILLink.Tasks/RuntimeHeaderParser.cs

return CoreCLR.Tools.ECallManifestGenerator.Run(args);
