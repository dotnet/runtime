/**
 * \file
 * ObjectiveC hacks to improve our changes with thread shutdown
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2014 Xamarin Inc
 */

#include "config.h"

#if defined(__MACH__)

#include <stdio.h>
#include <objc/runtime.h>
#include <objc/message.h>
#include <mono/utils/mono-compiler.h>

/*
 * We cannot include mono-threads.h as this includes io-layer internal types
 * which conflicts with objc.
 * Hence the hack here.
*/
void mono_threads_init_dead_letter (void);
void mono_threads_install_dead_letter (void);
void mono_thread_info_detach (void);

static Class nsobject, nsthread, mono_dead_letter_class;
static SEL dealloc, release, currentThread, threadDictionary, init, alloc, objectForKey, setObjectForKey;
static id mono_dead_letter_key;

/*
 * Our Mach bindings have a problem in that they might need to attach
 * the runtime after the the user tls keys have been destroyed.
 *
 * This happens when a bound object is retained by NSThread, which is
 * released very late in the TLS cleanup process.
 *
 * At that point, attaching the runtime leaves us in no position to
 * detach it later using TLS destructors as pthread is done with user
 * keys. This leaves us with a dead thread registered, which can cause
 * all sorts of terrible problems.
 *
 * The tipical crash is when another thread is created at the exact
 * same address of the previous one, cause thread registration to abort
 * due to duplicate entries.
 *
 * So what do we do here?
 * 
 * Experimentation showns that threadDictionary is destroied after the
 * problematic keys, so we add our dead letter object as an aditional
 * way to be notified of thread death.
 */
static void
mono_dead_letter_dealloc (id self, SEL _cmd)
{
	struct objc_super super;
	super.receiver = self;
	super.class = nsobject;
	objc_msgSendSuper (&super, dealloc);

	mono_thread_info_detach ();
}

void
mono_threads_install_dead_letter (void)
{
	id cur, dict;

	/*
	 * See the 'Dispatch Objective-C Messages Using the Method Function’s Prototype' section in
	 * the '64-Bit Transition Guide for Cocoa Touch' as to why this is required.
	 *
	 * It doesn't hurt on other architectures either, so no need to #ifdef it only for ARM64.
	 */

	id (*id_objc_msgSend_id)(id, SEL, id) = (id (*)(id, SEL, id)) objc_msgSend;
	void (*objc_msgSend_id_id)(id, SEL, id, id) = (void (*)(id, SEL, id, id)) objc_msgSend;

	cur = objc_msgSend ((id)nsthread, currentThread);
	if (!cur)
		return;
	dict = objc_msgSend (cur, threadDictionary);
	if (dict && id_objc_msgSend_id (dict, objectForKey, mono_dead_letter_key) == nil) {
		id value = objc_msgSend (objc_msgSend ((id)mono_dead_letter_class, alloc), init);

		objc_msgSend_id_id (dict, setObjectForKey, value, mono_dead_letter_key);

		objc_msgSend (value, release);
	}
}

void
mono_threads_init_dead_letter (void)
{
	id nsstring = (id) objc_getClass ("NSString");
	id nsautoreleasepool = (id) objc_getClass ("NSAutoreleasePool");
	SEL stringWithUTF8String = sel_registerName ("stringWithUTF8String:");
	SEL retain = sel_registerName ("retain");
	id pool;

	nsthread = (Class)objc_getClass ("NSThread");
	nsobject = (Class)objc_getClass ("NSObject");

	init = sel_registerName ("init");
	alloc = sel_registerName ("alloc");
	release = sel_registerName ("release");
	dealloc = sel_registerName ("dealloc");
	

	currentThread = sel_registerName ("currentThread");
	threadDictionary = sel_registerName ("threadDictionary");
	setObjectForKey = sel_registerName ("setObject:forKey:");
	objectForKey = sel_registerName ("objectForKey:");

	// define the dead letter class
	mono_dead_letter_class = objc_allocateClassPair (nsobject, "MonoDeadLetter", 0);
	class_addMethod (mono_dead_letter_class, dealloc, (IMP)mono_dead_letter_dealloc, "v@:");
	objc_registerClassPair (mono_dead_letter_class);

	// create the dict key
	pool = objc_msgSend (objc_msgSend (nsautoreleasepool, alloc), init);

	id (*objc_msgSend_char)(id, SEL, const char*) = (id (*)(id, SEL, const char*)) objc_msgSend;
	mono_dead_letter_key = objc_msgSend_char (nsstring, stringWithUTF8String, "mono-dead-letter");

	objc_msgSend (mono_dead_letter_key, retain);
	objc_msgSend (pool, release);
}
#endif
