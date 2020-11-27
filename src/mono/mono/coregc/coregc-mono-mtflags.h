#ifndef __COERGC_MONO_MTFLAGS_H__
#define __COERGC_MONO_MTFLAGS_H__

#define MTFlag_RequireAlign8           0x0001u
#define MTFlag_IsString                0x0004u
#define MTFlag_IsArray                 0x0008u
#define MTFlag_HasFinalizer            0x0010u
#define MTFlag_Category_ValueType      0x0040u
#define MTFlag_ContainsPointers        0x0100u
#define MTFlag_Category_ValueType_Mask 0x0200u
#define MTFlag_HasCriticalFinalizer    0x0800u
#define MTFlag_Collectible             0x1000u
#define MTFlag_HasComponentSize        0x8000u



#endif //__COERGC_MONO_MTFLAGS_H__