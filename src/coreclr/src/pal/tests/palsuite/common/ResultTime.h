// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _RESULT_TIME_H_
#define _RESULT_TIME_H_

#include <palsuite.h>

#define DWORD_MAX            ((DWORD) 0xFFFFFFFF)
const char *szDotNetInstallEnvVar = "DOTNET_INSTALL";
const char *szQASupportDirEnvVar = "QA_SUPPORT_DIR";

#ifdef PLATFORM_UNIX
#define SEPERATOR "/"
#else
#define SEPERATOR "\\"
#endif
char *getBuildNumber()
{  
        char *szBuildFileName = "buildinfo.txt";
        char *pDirectoryName = NULL; 
        char szBuildFileLoc[256];

        char szTemp[100];
        // buildinfo.txt contains information in key/value pair
        char szTempKey[100];
        char *szTempValue;
        FILE *fp;
        
        szTempValue = (char *) malloc (sizeof(char) *100);
        if (szTempValue == NULL)
        {        
            Fail("ERROR: Couldn't allocate enough memory to potentially store build number\n");    
        }

#ifndef PLATFORM_UNIX
        pDirectoryName = getenv(szDotNetInstallEnvVar);    
        if (pDirectoryName == NULL)    
        {        
            /* This condition may exist if the test is being run in say the Dev environment.*/        
            Trace("WARNING: Coriolis Test Environment may not be setup correctly. Variable DOTNET_INSTALL not set\n");        
            _snprintf(szTempValue, 99, "0000.00");        
            return szTempValue;  
        }    
#else        
        pDirectoryName = getenv(szQASupportDirEnvVar);    
        if (pDirectoryName == NULL)    
        {        
            Trace("WARNING: Coriolis Test Environment may not be setup correctly. Variable QA_SUPPORT_DIR not set\n");        
            _snprintf(szTempValue, 99, "0000.00");        
            return szTempValue;  
        }

#endif //PLATFORM_UNIX

#ifndef PLATFORM_UNIX
        _snprintf(szBuildFileLoc, MAX_PATH, "%s%s%s", pDirectoryName, SEPERATOR, szBuildFileName);
#else
        // To avoid buffer overruns for pDirectoryName
        _snprintf(szBuildFileLoc, MAX_PATH, "%s/../1.0%s%s", pDirectoryName, SEPERATOR, szBuildFileName);
#endif  //PLATFORM_UNIX
        fp = fopen( szBuildFileLoc, "r");    
        if( fp == NULL)    
        {        
            Trace("WARNING: Couldn't open szBuildFileLoc [%s]\n", szBuildFileLoc);    
            _snprintf(szTempValue, 99, "0000.00");        
            return szTempValue;    
        }    

        while( fgets( szTemp, 100, fp ) != NULL)    
        {          
            sscanf(szTemp, "%s %s\n", szTempKey, szTempValue);        
            if(strcmp(szTempKey, "Build-Number:") == 0)        
            {            
                fclose(fp);            
                return szTempValue;        
            }    
        }    

        fclose(fp);    
        return szTempValue;

}

DWORD GetTimeDiff( DWORD dwStartTime)
{
	DWORD dwDiffTime = 0;
	DWORD dwEndTime = GetTickCount();

	if( dwEndTime < dwStartTime)
	{
	    // To account for overflow, we add one
	    dwDiffTime = dwEndTime + (DWORD_MAX  -  dwStartTime) + 1;
	}
	else
	{
	    dwDiffTime = dwEndTime -  dwStartTime;
	}

	return dwDiffTime;
}
#endif // _RESULT_TIME_H_
