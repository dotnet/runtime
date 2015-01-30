//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
