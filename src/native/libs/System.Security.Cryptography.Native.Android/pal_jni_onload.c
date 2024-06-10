#include "pal_jni.h"

JNIEXPORT jint JNICALL
JNI_OnLoad(JavaVM *vm, void *reserved)
{
	return init_library_on_load (vm, reserved);
}
