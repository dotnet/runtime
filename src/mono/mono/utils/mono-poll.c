#include "mono-poll.h"

#ifdef HAVE_POLL
int
mono_poll (mono_pollfd *ufds, unsigned int nfds, int timeout)
{
	return poll (ufds, nfds, timeout);
}
#else

int
mono_poll (mono_pollfd *ufds, unsigned int nfds, int timeout)
{
	struct timeval tv, *tvptr;
	int i, fd, events, affected, count;
	fd_set rfds, wfds, efds;
	int nexc = 0;
	int maxfd = 0;

	if (timeout < 0) {
		tvptr = NULL;
	} else {
		tv.tv_sec = timeout / 1000;
		tv.tv_usec = (timeout % 1000) * 1000;
		tvptr = &tv;
	}

	FD_ZERO (&rfds);
	FD_ZERO (&wfds);
	FD_ZERO (&efds);

	for (i = 0; i < nfds; i++) {
		ufds [i].revents = 0;
		fd = ufds [i].fd;
		if (fd < 0)
			continue;

#ifdef PLATFORM_WIN32
		if (nexc >= FD_SETSIZE) {
			ufds [i].revents = MONO_POLLNVAL;
			return 1;
		}
#else
		if (fd > FD_SETSIZE) {
			ufds [i].revents = MONO_POLLNVAL;
			return 1;
		}
#endif

		events = ufds [i].events;
		if ((events & MONO_POLLIN) != 0)
			FD_SET (fd, &rfds);

		if ((events & MONO_POLLOUT) != 0)
			FD_SET (fd, &wfds);

		FD_SET (fd, &efds);
		nexc++;
		if (fd > maxfd)
			maxfd = fd;
			
	}

	affected = select (maxfd + 1, &rfds, &wfds, &efds, tvptr);
	if (affected == -1) /* EBADF should be translated to POLLNVAL */
		return -1;

	count = 0;
	for (i = 0; i < nfds && affected > 0; i++) {
		fd = ufds [i].fd;
		if (fd < 0)
			continue;

		events = ufds [i].events;
		if ((events & MONO_POLLIN) != 0 && FD_ISSET (fd, &rfds)) {
			ufds [i].revents |= MONO_POLLIN;
			affected--;
		}

		if ((events & MONO_POLLOUT) != 0 && FD_ISSET (fd, &wfds)) {
			ufds [i].revents |= MONO_POLLOUT;
			affected--;
		}

		if (FD_ISSET (fd, &efds)) {
			ufds [i].revents |= MONO_POLLERR;
			affected--;
		}

		if (ufds [i].revents != 0)
			count++;
	}

	return count;
}

#endif

