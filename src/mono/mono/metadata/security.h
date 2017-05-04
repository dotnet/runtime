/**
 * \file
 * Security internal calls
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */


#ifndef _MONO_METADATA_SECURITY_H_
#define _MONO_METADATA_SECURITY_H_

#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/metadata/object-internals.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/mono-publib.h>

G_BEGIN_DECLS

/* System.Environment */
extern MonoStringHandle ves_icall_System_Environment_get_UserName (MonoError *error);


/* System.Security.Principal.WindowsIdentity */
gpointer mono_security_principal_windows_identity_get_current_token (void);
extern MonoArray* ves_icall_System_Security_Principal_WindowsIdentity_GetRoles (gpointer token);
extern gpointer ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken (MonoError *error);
extern MonoStringHandle ves_icall_System_Security_Principal_WindowsIdentity_GetTokenName (gpointer token, MonoError *error);
extern gpointer ves_icall_System_Security_Principal_WindowsIdentity_GetUserToken (MonoStringHandle username, MonoError *error);


/* System.Security.Principal.WindowsImpersonationContext */
extern gboolean ves_icall_System_Security_Principal_WindowsImpersonationContext_CloseToken (gpointer token);
extern gpointer ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken (gpointer token);
extern gboolean ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken (gpointer token);
extern gboolean ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf (void);


/* System.Security.Principal.WindowsPrincipal */
extern gboolean ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupId (gpointer user, gpointer group);
extern gboolean ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupName (gpointer user, MonoString *group);


/* Mono.Security.Cryptography.KeyPairPersistance */
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_CanSecure (MonoString *root);
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsMachineProtected (MonoString *path);
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsUserProtected (MonoString *path);
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectMachine (MonoString *path);
extern MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectUser (MonoString *path);


/* System.Security.Policy.Evidence */
MonoBoolean ves_icall_System_Security_Policy_Evidence_IsAuthenticodePresent (MonoReflectionAssemblyHandle refass, MonoError *error);

/* System.Security.SecureString */
extern void ves_icall_System_Security_SecureString_DecryptInternal (MonoArray *data, MonoObject *scope);
extern void ves_icall_System_Security_SecureString_EncryptInternal (MonoArray *data, MonoObject *scope);
void invoke_protected_memory_method (MonoArray *data, MonoObject *scope, gboolean encrypt, MonoError *error);

G_END_DECLS

#endif /* _MONO_METADATA_SECURITY_H_ */
