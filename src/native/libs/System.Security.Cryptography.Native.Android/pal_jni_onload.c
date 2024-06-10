#include "pal_jni.h"

JNIEXPORT jint JNICALL
JNI_OnLoad(JavaVM *vm, void *reserved)
{
	return AndroidCryptoNative_InitLibraryOnLoad (vm, reserved);
}
