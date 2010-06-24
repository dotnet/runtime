/*
 * security.c:  Security internal calls
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */


#ifndef _MONO_METADATA_SECURITY_H_
#define _MONO_METADATA_SECURITY_H_

#include <mono/metadata/object.h>

G_BEGIN_DECLS

/* System.Environment */
extern MonoString* ves_icall_System_Environment_get_UserName (void) MONO_INTERNAL;


/* System.Security.Principal.WindowsIdentity */
extern MonoArray* ves_icall_System_Security_Principal_WindowsIdentity_GetRoles (gpointer token) MONO_INTERNAL;
extern gpointer ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken (void) MONO_INTERNAL;
extern MonoString* ves_icall_System_Security_Principal_WindowsIdentity_GetTokenName (gpointer token) MONO_INTERNAL;
extern gpointer ves_icall_System_Security_Principal_WindowsIdentity_GetUserToken (MonoString *username) MONO_INTERNAL;


/* System.Security.Principal.WindowsImpersonationContext */
extern gboolean ves_icall_System_Security_Principal_WindowsImpersonationContext_CloseToken (gpointer token) MONO_INTERNAL;
extern gpointer ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken (gpointer token) MONO_INTERNAL;
extern gboolean ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken (gpointer token) MONO_INTERNAL;
extern gboolean ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf (void) MONO_INTERNAL;


/* System.Security.Principal.WindowsPrincipal */
extern gboolean ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupId (gpointer user, gpointer group) MONO_INTERNAL;
extern gboolean ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupName (gpointer user, MonoString *group) MONO_INTERNAL;


/* Mono.Security.Cryptography.KeyPairPersistance */
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_CanSecure (MonoString *root) MONO_INTERNAL;
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsMachineProtected (MonoString *path) MONO_INTERNAL;
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsUserProtected (MonoString *path) MONO_INTERNAL;
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectMachine (MonoString *path) MONO_INTERNAL;
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectUser (MonoString *path) MONO_INTERNAL;


/* System.Security.Policy.Evidence */
MonoBoolean ves_icall_System_Security_Policy_Evidence_IsAuthenticodePresent (MonoReflectionAssembly *refass) MONO_INTERNAL;

/* System.Security.SecureString */
extern void ves_icall_System_Security_SecureString_DecryptInternal (MonoArray *data, MonoObject *scope) MONO_INTERNAL;
extern void ves_icall_System_Security_SecureString_EncryptInternal (MonoArray *data, MonoObject *scope) MONO_INTERNAL;
void invoke_protected_memory_method (MonoArray *data, MonoObject *scope, gboolean encrypt) MONO_INTERNAL;

G_END_DECLS

#endif /* _MONO_METADATA_SECURITY_H_ */
