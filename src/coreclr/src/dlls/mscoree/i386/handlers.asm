;
; Copyright (c) Microsoft. All rights reserved.
; Licensed under the MIT license. See LICENSE file in the project root for full license information. 
;

; ==++==
; 

; 
; ==--==
;
;  This file contains definitions for each CLR exception handler.  
;  The assembler marks all functions in this file as safe-exception
;  handler.
;

.686
.model  flat

COMPlusFrameHandler proto c
.safeseh COMPlusFrameHandler

COMPlusNestedExceptionHandler proto c
.safeseh COMPlusNestedExceptionHandler

FastNExportExceptHandler proto c
.safeseh FastNExportExceptHandler

UMThunkPrestubHandler proto c
.safeseh UMThunkPrestubHandler

ifdef FEATURE_COMINTEROP
COMPlusFrameHandlerRevCom proto c
.safeseh COMPlusFrameHandlerRevCom
endif

end

