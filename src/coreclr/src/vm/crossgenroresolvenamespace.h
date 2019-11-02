// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+----------------------------------------------------------------------------
//

//

//
//-----------------------------------------------------------------------------

#ifndef __CROSSGENRORESOLVENAMESPACE_H
#define __CROSSGENRORESOLVENAMESPACE_H

namespace Crossgen
{
	HRESULT WINAPI CrossgenRoResolveNamespace(
		const LPCWSTR   wszNamespace,
		DWORD *         pcMetadataFiles,
		SString **      ppMetadataFiles);

	void SetFirstPartyWinMDPaths(StringArrayList* saAppPaths);
	void SetAppPaths(StringArrayList* saAppPaths);
}

#endif
