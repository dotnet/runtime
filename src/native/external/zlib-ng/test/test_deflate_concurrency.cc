/* Test deflate() on concurrently modified next_in.
 *
 * Plain zlib does not document that this is supported, but in practice it tolerates this, and QEMU live migration is
 * known to rely on this. Make sure zlib-ng tolerates this as well.
 */

#include "zbuild.h"
#ifdef ZLIB_COMPAT
#include "zlib.h"
#else
#include "zlib-ng.h"
#endif

#include <gtest/gtest.h>

#include <algorithm>
#include <atomic>
#include <cstring>
#include <thread>

static uint8_t buf[8 * 1024];
static uint8_t zbuf[4 * 1024];
static uint8_t tmp[8 * 1024];

/* Thread that increments all bytes in buf by 1. */
class Mutator {
    enum class State {
        PAUSED,
        RUNNING,
        STOPPED,
    };

public:
    Mutator()
        : m_state(State::PAUSED), m_target_state(State::PAUSED),
          m_thread(&Mutator::run, this) {}
    ~Mutator() {
        transition(State::STOPPED);
        m_thread.join();
    }

    void pause() {
        transition(State::PAUSED);
    }

    void resume() {
        transition(State::RUNNING);
    }

private:
    void run() {
        while (true) {
            m_state.store(m_target_state);
            if (m_state == State::PAUSED)
                continue;
            if (m_state == State::STOPPED)
                break;
            for (uint8_t & i: buf)
                i++;
        }
    }

    void transition(State target_state) {
        m_target_state = target_state;
        while (m_state != target_state) {
        }
    }

    std::atomic<State> m_state, m_target_state;
    std::thread m_thread;
};

TEST(deflate, concurrency) {
#ifdef S390_DFLTCC_DEFLATE
    GTEST_SKIP() << "Known to be broken with S390_DFLTCC_DEFLATE";
#endif

    /* Create reusable mutator and streams. */
    Mutator mutator;

    PREFIX3(stream) dstrm;
    memset(&dstrm, 0, sizeof(dstrm));
    int err = PREFIX(deflateInit2)(&dstrm, Z_BEST_SPEED, Z_DEFLATED, -15, 8, Z_DEFAULT_STRATEGY);
    ASSERT_EQ(Z_OK, err) << dstrm.msg;

    PREFIX3(stream) istrm;
    memset(&istrm, 0, sizeof(istrm));
    err = PREFIX(inflateInit2)(&istrm, -15);
    ASSERT_EQ(Z_OK, err) << istrm.msg;

    /* Iterate for a certain amount of time. */
    auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(1);
    while (std::chrono::steady_clock::now() < deadline) {
        /* Start each iteration with a fresh stream state. */
        err = PREFIX(deflateReset)(&dstrm);
        ASSERT_EQ(Z_OK, err) << dstrm.msg;

        err = PREFIX(inflateReset)(&istrm);
        ASSERT_EQ(Z_OK, err) << istrm.msg;

        /* Mutate and compress the first half of buf concurrently.
         * Decompress and throw away the results, which are unpredictable.
         */
        mutator.resume();
        dstrm.next_in = buf;
        dstrm.avail_in = sizeof(buf) / 2;
        while (dstrm.avail_in > 0) {
            dstrm.next_out = zbuf;
            dstrm.avail_out = sizeof(zbuf);
            err = PREFIX(deflate)(&dstrm, Z_NO_FLUSH);
            ASSERT_EQ(Z_OK, err) << dstrm.msg;
            istrm.next_in = zbuf;
            istrm.avail_in = sizeof(zbuf) - dstrm.avail_out;
            while (istrm.avail_in > 0) {
                istrm.next_out = tmp;
                istrm.avail_out = sizeof(tmp);
                err = PREFIX(inflate)(&istrm, Z_NO_FLUSH);
                ASSERT_EQ(Z_OK, err) << istrm.msg;
            }
        }

        /* Stop mutation and compress the second half of buf.
         * Decompress and check that the result matches.
         */
        mutator.pause();
        dstrm.next_in = buf + sizeof(buf) / 2;
        dstrm.avail_in = sizeof(buf) - sizeof(buf) / 2;
        while (dstrm.avail_in > 0) {
            dstrm.next_out = zbuf;
            dstrm.avail_out = sizeof(zbuf);
            err = PREFIX(deflate)(&dstrm, Z_FINISH);
            if (err == Z_STREAM_END)
                ASSERT_EQ(0u, dstrm.avail_in);
            else
                ASSERT_EQ(Z_OK, err) << dstrm.msg;
            istrm.next_in = zbuf;
            istrm.avail_in = sizeof(zbuf) - dstrm.avail_out;
            while (istrm.avail_in > 0) {
                size_t orig_total_out = istrm.total_out;
                istrm.next_out = tmp;
                istrm.avail_out = sizeof(tmp);
                err = PREFIX(inflate)(&istrm, Z_NO_FLUSH);
                if (err == Z_STREAM_END)
                    ASSERT_EQ(0u, istrm.avail_in);
                else
                    ASSERT_EQ(Z_OK, err) << istrm.msg;
                size_t concurrent_size = sizeof(buf) - sizeof(buf) / 2;
                if (istrm.total_out > concurrent_size) {
                    size_t tmp_offset, buf_offset, size;
                    if (orig_total_out >= concurrent_size) {
                        tmp_offset = 0;
                        buf_offset = orig_total_out - concurrent_size;
                        size = istrm.total_out - orig_total_out;
                    } else {
                        tmp_offset = concurrent_size - orig_total_out;
                        buf_offset = 0;
                        size = istrm.total_out - concurrent_size;
                    }
                    ASSERT_EQ(0, memcmp(tmp + tmp_offset, buf + sizeof(buf) / 2 + buf_offset, size));
                }
            }
        }
    }

    err = PREFIX(inflateEnd)(&istrm);
    ASSERT_EQ(Z_OK, err) << istrm.msg;

    err = PREFIX(deflateEnd)(&dstrm);
    ASSERT_EQ(Z_OK, err) << istrm.msg;
}
