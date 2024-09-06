#ifndef _PIPECHANNEL_HPP
#define _PIPECHANNEL_HPP

#include <utility>

#ifndef HOST_WINDOWS
class PipeChannel final
{
public:
    static PipeChannel Create();
    ~PipeChannel() = default;
    PipeChannel (const PipeChannel&) = delete;
    PipeChannel& operator=(PipeChannel& other) = delete;

    class Reader final {
    private:
        int m_readFd;
    public:
        explicit Reader(int readFd) : m_readFd{readFd} {}
        Reader(const Reader&) = delete;
        Reader& operator=(const Reader&) = delete;
        ~Reader();
        Reader (Reader&& other) : m_readFd{other.m_readFd} {
            other.m_readFd = -1;
        }
        Reader& operator=(Reader&& other) {
            if (&other == this)
                return *this;
            std::swap(m_readFd, other.m_readFd);
            return *this;
        }

        friend void swap(Reader& first, Reader& second) {
            using std::swap;
            swap(first.m_readFd, second.m_readFd);
        }

        int ReadOne(char &dest) const
        {
            return ReadAll(&dest, 1);
        }

    private:
        int ReadSome(char *dest, int destSize) const;
        int ReadAll(char *dest, int destSize) const;
    };

    class Writer final {
    private:
        int m_writeFd;
    public:
        explicit Writer(int writeFd) : m_writeFd{writeFd} {}
        Writer(const Writer&) = delete;
        Writer& operator=(const Writer&) = delete;
        ~Writer();
        Writer (Writer&& other) : m_writeFd{other.m_writeFd} {
            other.m_writeFd = -1;
        }
        Writer& operator=(Writer&& other) {
            if (&other == this)
                return *this;
            std::swap(m_writeFd, other.m_writeFd);
            return *this;
        }

        friend void swap (Writer& first, Writer& second) {
            using std::swap;
            swap(first.m_writeFd, second.m_writeFd);
        }

        int SendOne(char c) const
        {
            return SendAll(&c, sizeof(c));
        }

    private:
        int SendAll(const char *buf, int bufSize) const;
    };

private:
    Reader m_reader;
    Writer m_writer;

    PipeChannel (int readFd, int writeFd) : m_reader{readFd}, m_writer{writeFd} {}

public:
    PipeChannel (PipeChannel &&other) : m_reader{std::move(other.m_reader)}, m_writer{std::move(other.m_writer)}
    {
    }

    PipeChannel& operator=(PipeChannel &&other) {
        if (&other == this) {
            return *this;
        }
        using std::swap;
        swap (m_reader, other.m_reader);
        swap (m_writer, other.m_writer);
        return *this;
    }

    Reader ExtractReader() {
        return std::move(m_reader);
    }
    Writer ExtractWriter() {
        return std::move(m_writer);
    }

};
#endif

#endif /*_PIPECHANNEL_HPP*/
