#include <config.h>
#include <glib.h>

/* We're digging into handle internals here... */
#include <mono/io-layer/shared.h>

int main (int argc, char **argv)
{
	_wapi_shm_destroy ();
	
	exit (0);
}
