#include <config.h>
#include <glib.h>
#include <errno.h>
#include <sys/ipc.h>
#include <sys/sem.h>

#include <mono/io-layer/io-layer.h>

/* We're digging into handle internals here... */
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>

int main (int argc, char **argv)
{
	int sem_id, ret;
	
	_wapi_shared_layout = _wapi_shm_attach(WAPI_SHM_DATA);
	if (_wapi_shared_layout == FALSE) {
		g_error ("Failed to attach shared memory!");
		exit (-1);
	}

	sem_id = semget (_wapi_shared_layout->sem_key, _WAPI_SHARED_SEM_COUNT, 0600);
	if (sem_id == -1) {
		g_message ("Can't open semaphore: %s", g_strerror (errno));
	} else {
		ret = semctl (sem_id, IPC_RMID, 0);
		if (ret == -1) {
			g_message ("Error deleting semaphore: %s",
				   g_strerror (errno));
		}
	}
	
	exit (0);
}
