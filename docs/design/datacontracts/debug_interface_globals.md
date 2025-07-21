# Debug Interface Globals

The following document lists the global variables that are used directly in the debug interface managed code (SOSDacImpl.cs, etc.)

Global variables used
| Global Name | Type | Purpose |
| --- | --- | --- |
| StringMethodTable | TargetPointer | Identify where the string MethodTable exists |
| ObjectMethodTable | TargetPointer | Identify where the object MethodTable exists |
| SystemDomain | TargetPointer | Identify where the SystemDomain exists |
| DirectorySeparator | TargetPointer | Identify where the directory separator exists |
| FeatureCOMInterop | TargetPointer | Identify where the flag for FeatureCOMInterop exists |
| StressLog | TargetPointer | Identify where the StressLog exists |
| AppDomain | TargetPointer | Identify where the AppDomain exists |
| ObjectArrayMethodTable | TargetPointer | Identify where the ObjectArrayMethodTable exists |
| ExceptionMethodTable | TargetPointer | Identify where the ExceptionMethodTable exists |
| FreeObjectMethodTable | TargetPointer | Identify where the FreeObjectMethodTable exists |
| SOSBreakingChangeVersion | TargetPointer | Identify where the SOSBreakingChangeVersion exists |
| DacNotificationFlags | TargetPointer | Identify where the DacNotificationFlags exists |
| MaxClrNotificationArgs | uint32 | Identify the maximum number of CLR notification arguments |
| ClrNotificationArguments | TargetPointer | Identify where the ClrNotificationArguments exists |
| DefaultADID | uint | Identify the default AppDomain ID |
