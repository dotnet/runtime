// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyCopyright("\x00a9 Microsoft Corporation.  All rights reserved.")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyFileVersion("999.999.999.0")]
[assembly: AssemblyInformationalVersion("999.999.999.0")]
[assembly: AssemblyVersion("999.999.999.0")]

[assembly: NeutralResourcesLanguageAttribute("en-US")]

#if SIGNED
[assembly: InternalsVisibleTo("System.Reflection.Metadata.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293")]
#else
[assembly: InternalsVisibleTo("System.Reflection.Metadata.Tests")]
#endif
