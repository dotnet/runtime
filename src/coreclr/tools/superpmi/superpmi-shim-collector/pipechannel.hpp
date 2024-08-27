#ifndef _PIPECHANNEL_HPP
#define _PIPECHANNEL_HPP

#include <utility>

#include <string.h>
#include <sys/errno.h>
#include <unistd.h>

class PipeChannel final
{
private:
	int m_readFd;
	int m_writeFd;

	PipeChannel (int readFd, int writeFd) : m_readFd{readFd}, m_writeFd{writeFd} {}
public:
	class Reader;
	class Writer;
	
	static PipeChannel Create();
	~PipeChannel()
	{
		if (m_writeFd >= 0)
			close(m_writeFd);
	}
	PipeChannel (const PipeChannel&) = delete;
	PipeChannel& operator=(PipeChannel& other) = delete;

	PipeChannel (PipeChannel &&other) : m_readFd{other.m_readFd}, m_writeFd{other.m_writeFd}
	{
		other.m_readFd = -1;
		other.m_writeFd = -1;
	}
	PipeChannel& operator=(PipeChannel &&other) {
		if (&other == this) {
			return *this;
		}
		std::swap (m_readFd, other.m_readFd);
		std::swap (m_writeFd, other.m_writeFd);
		return *this;
	}

	Reader ExtractReader() {
		int readFd = m_readFd;
		m_readFd = -1;
		return Reader(readFd);
	}
	Writer ExtractWriter() {
		int writeFd = m_writeFd;
		m_writeFd = -1;
		return Writer(writeFd);
	}

	class Reader final {
	private:
		int m_readFd;
	public:
		explicit Reader(int readFd) : m_readFd{readFd} {}
		Reader(const Reader&) = delete;
		Reader& operator=(const Reader&) = delete;
		~Reader() {
			if (m_readFd != -1)
				close (m_readFd);
		}
		Reader (Reader&& other) : m_readFd{other.m_readFd} {
			other.m_readFd = -1;
		}
		Reader& operator=(Reader&& other) {
			if (&other == this)
				return *this;
			std::swap(m_readFd, other.m_readFd);
			return *this;
		}

		int ReadOne(char &dest) const
		{
			return ReadFull(&dest, 1);
		}

		int ReadSome(char *dest, int destSize) const
		{
			int toRead = destSize;
			int haveRead = 0;
			do {
				int res = read (m_readFd, dest, toRead);
				if (res < 0) {
					if (errno == EINTR || errno == EAGAIN) {
						continue;
					} else {
						return res;
					}
				} else if (res == 0) {
					break;
				}
				toRead -= res;
				dest += res;
				haveRead += res;
			} while (toRead > 0);
			return haveRead;
		}

		int ReadFull(char *dest, int destSize) const
		{
			int res = ReadSome(dest, destSize);
			if (res < 0) {
				return res;
			} else if (res != destSize) {
				return 0; // partial reads are not ok
			} else {
				return res;
			}
		}
	};

	class Writer final {
	private:
		int m_writeFd;
	public:
		explicit Writer(int writeFd) : m_writeFd{writeFd} {}
		Writer(const Writer&) = delete;
		Writer& operator=(const Writer&) = delete;
		~Writer() {
			if (m_writeFd != -1)
				close (m_writeFd);
		}
		Writer (Writer&& other) : m_writeFd{other.m_writeFd} {
			other.m_writeFd = -1;
		}
		Writer& operator=(Writer&& other) {
			if (&other == this)
				return *this;
			std::swap(m_writeFd, other.m_writeFd);
			return *this;
		}

		int SendOne(char c) const
		{
			return SendFull(&c, sizeof(c));
		}

		int SendFull(const char *buf, int bufSize) const
		{
			const char *cur = buf;
			int toWrite = bufSize;
			int written = 0;
			do {
				int res = write(m_writeFd, cur, toWrite);
				if (res < 0) {
					if (errno == EINTR || errno == EAGAIN) {
						continue;
					} else {
						return res;
					}
				}
				written += res;
				cur += res;
				toWrite -= res;
			} while (toWrite > 0);
			return written;
		}
	};
};

#endif /*_PIPECHANNEL_HPP*/
