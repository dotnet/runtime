unsigned __int32 Ret_Int(unsigned __int32 argVal){
	unsigned __int32 retVal = (unsigned __int32)argVal;
	return retVal;
}
unsigned __int32 Ret_Ptr(void *argVal){
	unsigned __int32 retVal = (unsigned __int32)argVal;
	return retVal;
}

void Set_Val(__int32 *argVal, __int32 val){
	*argVal = val;;
	
}

void Mul_Val(__int32 *arg1,__int32 *arg2,__int32 *arg3){
	*arg3 = (*arg1)*(*arg2);
	
}

