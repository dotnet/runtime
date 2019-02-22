#if defined(_MSC_VER)
#define EXPORT_API extern "C" __declspec(dllexport)
#else
#define EXPORT_API extern "C" __attribute__((visibility("default")))

#ifdef BIT64
#define __int64     long
#else // BIT64
#define __int64     long long
#endif // BIT64

#define __int32     int
#define __int16     short int
#define __int8      char        // assumes char is signed

#endif 

#include <cstddef>

EXPORT_API unsigned __int32 Ret_Int(unsigned __int32 argVal){
	unsigned __int32 retVal = (unsigned __int32)argVal;
	return retVal;
}
EXPORT_API unsigned __int32 Ret_Ptr(void *argVal){
	unsigned __int32 retVal = (unsigned __int32)(size_t)argVal;
	return retVal;
}

EXPORT_API void Set_Val(__int32 *argVal, __int32 val){
	*argVal = val;;
	
}

EXPORT_API void Mul_Val(__int32 *arg1,__int32 *arg2,__int32 *arg3){
	*arg3 = (*arg1)*(*arg2);
	
}

