#if defined(_MSC_VER)
#define EXPORT_API extern "C" __declspec(dllexport)
#else
#define EXPORT_API extern "C" __attribute__((visibility("default")))
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

