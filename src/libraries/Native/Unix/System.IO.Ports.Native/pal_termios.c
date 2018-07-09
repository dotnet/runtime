// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_utilities.h"
#include <termios.h>
#include <sys/ioctl.h>
#include <unistd.h>
#include <errno.h>
#include <pal_termios.h>

/* This is dup of System/IO/Ports/NativeMethods.cs */
enum
{
    ParityNone = 0,
    ParityOdd  = 1,
    ParityEven = 2,
    ParityMark = 3,
    ParitySpace = 4
};

/* System/IO/Ports/Handshake.cs */
enum
{
    HandshakeNone,          // No Flow Control
    HandshakeSoft,          // Software Flow Control -> XOn/Xoff
    HandshakeHard,          // Hardware Flow Control -> RTS/CTS
    HandshakeBoth           // Software & Hardware Flow Control
};

/* Interop/Unix/Interop.Termios.cs */
enum
{
    SignalDtr = 1,
    SignalDsr = 2,
    SignalRts = 3,
    SignalCts = 4,
    SignalDcd = 5,
};

enum
{
    AllQueues = 0,
    ReceiveQueue = 1,
    SendQueue = 2,
};

static int32_t SystemIoPortsNative_TermiosGetStatus(intptr_t handle)
{
    int fd = ToFileDescriptor(handle);
    int status = 0;
    if (ioctl(fd, TIOCMGET, &status) < 0)
    {
        return -1;
    }

    return status;
}

int32_t SystemIoPortsNative_TermiosGetSignal(intptr_t handle, int32_t signal)
{
    int32_t status = SystemIoPortsNative_TermiosGetStatus(handle);
    if (status == -1)
    {
        return -1;
    }

    switch (signal)
    {
    case SignalDtr:
        return (status & TIOCM_DTR) ? 1 : 0;
    case SignalDsr:
        return (status & TIOCM_DSR) ? 1 : 0;
    case SignalRts:
        return (status & TIOCM_RTS) ? 1 : 0;
    case SignalCts:
        return (status & TIOCM_CTS) ? 1 : 0;
    case SignalDcd:
        return (status & TIOCM_CAR) ? 1 : 0;
    default:
        return -1;
   }
}

int32_t SystemIoPortsNative_TermiosSetSignal(intptr_t handle, int32_t signal, int32_t set)
{
    int fd = ToFileDescriptor(handle);
    int bit;

    switch (signal)
    {
    case SignalRts:
        bit = TIOCM_RTS;
        break;
    case SignalDtr:
        bit = TIOCM_DTR;
        break;
    default:
        errno = EINVAL;
        return -1;
    }

    int status = SystemIoPortsNative_TermiosGetStatus(fd);
    if (status >= 0)
    {
        if (set)
        {
            status |= bit;
        }
        else
        {
            status &= ~bit;
        }
        return ioctl(fd, TIOCMSET, &status);
    }

    return -1;
}

static speed_t SystemIoPortsNative_TermiosSpeed2Rate(int speed)
{
    switch (speed)
    {
        /* High speeds are not portable and may or may not be supported */
#ifdef B4000000
    case 4000000:
        return B4000000;
#endif
#ifdef B3500000
    case 3500000:
        return B3500000;
#endif
#ifdef B3000000
    case 3000000:
        return B3000000;
#endif
#ifdef B2500000
    case 2500000:
        return B2500000;
#endif
#ifdef B2000000
    case 2000000:
        return B2000000;
#endif
#ifdef B1500000
    case 1500000:
        return B1500000;
#endif
#ifdef B1152000
    case 1152000:
        return B1152000;
#endif
#ifdef B1000000
    case 1000000:
        return B1000000;
#endif
#ifdef B921600
    case 921600:
        return B921600;
#endif
#ifdef B576000
    case 576000:
        return B576000;
#endif
#ifdef B500000
    case 500000:
        return B500000;
#endif
#ifdef B460800
    case 460800:
        return B460800;
#endif
    case 230400:
        return B230400;
    case 115200:
        return B115200;
    case 57600:
        return B57600;
    case 38400:
        return B38400;
    case 19200:
        return B19200;
    case 9600:
        return B9600;
    case 4800:
        return B4800;
    case 2400:
        return B2400;
    case 1800:
        return B1800;
    case 1200:
        return B1200;
    case 600:
        return B600;
    case 300:
        return B300;
    case 200:
        return B200;
    case 150:
        return B150;
    case 134:
        return B134;
    case 110:
        return B110;
    case 75:
        return B75;
    case 50:
        return B50;
    }
    return B0;
}

static int SystemIoPortsNative_TermiosRate2Speed(speed_t brate)
{
    switch (brate)
    {
#ifdef B4000000
    case B4000000:
        return 4000000;
#endif
#ifdef B3500000
    case B3500000:
        return 3500000;
#endif
#ifdef B3000000
    case B3000000:
        return 3000000;
#endif
#ifdef B2500000
    case B2500000:
        return 2500000;
#endif
#ifdef B2000000
    case B2000000:
        return 2000000;
#endif
#ifdef B1500000
    case B1500000:
        return 1500000;
#endif
#ifdef B1152000
    case B1152000:
        return 1152000;
#endif
#ifdef B1000000
    case B1000000:
        return 1000000;
#endif
#ifdef B921600
    case B921600:
        return 921600;
#endif
#ifdef B576000
    case B576000:
        return 576000;
#endif
#ifdef B500000
    case B500000:
        return 500000;
#endif
#ifdef B460800
    case B460800:
        return 460800;
#endif
    case B230400:
        return 230400;
    case B115200:
        return 115200;
    case B57600:
        return 57600;
    case B38400:
        return 38400;
    case B19200:
        return 19200;
    case B9600:
        return 9600;
    case B4800:
        return 4800;
    case B2400:
        return 2400;
    case B1800:
        return 1800;
    case B1200:
        return 1200;
    case B600:
        return 600;
    case B300:
        return 300;
    case B200:
        return 200;
    case B150:
        return 150;
    case B134:
        return 134;
    case B110:
        return 110;
    case B75:
        return 75;
    case B50:
        return 50;
    }

    return brate;
}

int32_t SystemIoPortsNative_TermiosGetSpeed(intptr_t handle)
{
    int fd = ToFileDescriptor(handle);
    struct termios term;
    if (tcgetattr(fd, &term) < 0)
    {
        return  -1;
    }

    return SystemIoPortsNative_TermiosRate2Speed(cfgetispeed(&term));
}

int32_t SystemIoPortsNative_TermiosSetSpeed(intptr_t handle, int32_t speed)
{
    int fd = ToFileDescriptor(handle);
    struct termios term;
    speed_t brate = SystemIoPortsNative_TermiosSpeed2Rate(speed);

    if (brate == B0)
    {
        errno = EINVAL;
        return -1;
    }

    if (tcgetattr(fd, &term) < 0)
    {
        return  -1;
    }

    cfsetspeed(&term, brate);

    if (tcsetattr(fd, TCSANOW, &term) < 0)
    {
        return -2;
    }

    return speed;
}

int32_t SystemIoPortsNative_TermiosAvailableBytes(intptr_t handle, int32_t readBuffer)
{
    int fd = ToFileDescriptor(handle);
    int32_t bytes;
    if (ioctl (fd, readBuffer ? FIONREAD : TIOCOUTQ, &bytes) == -1)
    {
        return -1;
    }

    return bytes;
}

int32_t SystemIoPortsNative_TermiosDiscard(intptr_t handle, int32_t queue)
{
    int fd = ToFileDescriptor(handle);
    switch (queue)
    {
    case ReceiveQueue:
        return tcflush(fd, TCIFLUSH);
    case SendQueue:
        return tcflush(fd, TCOFLUSH);
    default:
        return tcflush(fd, TCIOFLUSH);
    }
}

int32_t SystemIoPortsNative_TermiosDrain(intptr_t handle)
{
    int fd = ToFileDescriptor(handle);
    return tcdrain(fd);
}

int32_t SystemIoPortsNative_TermiosSendBreak(intptr_t handle, int32_t duration)
{
    int fd = ToFileDescriptor(handle);
    return tcsendbreak(fd, duration);
}

int32_t SystemIoPortsNative_TermiosReset(intptr_t handle, int32_t speed, int32_t dataBits, int32_t stopBits, int32_t parity, int32_t handshake)
{
    int fd = ToFileDescriptor(handle);
    struct termios term;
    int ret = 0;

    if (tcgetattr(fd, &term) < 0)
    {
        return  -1;
    }

    cfmakeraw(&term);
    term.c_cflag |=  (CLOCAL | CREAD);
    term.c_lflag &= ~((tcflag_t)(ICANON | ECHO | ECHOE | ECHOK | ECHONL | ISIG | IEXTEN ));
    term.c_oflag &= ~((tcflag_t)(OPOST));
    term.c_iflag = IGNBRK;

    term.c_cflag &= ~((tcflag_t)(CSIZE));
    switch (dataBits)
    {
    case 5:
        term.c_cflag |= CS5;
        break;
    case 6:
        term.c_cflag |= CS6;
        break;
    case 7:
        term.c_cflag |= CS7;
        break;
    case 8:
    default:
        term.c_cflag |= CS8;
        break;
    }

    // There really is only choice of 1 or 2.
    if (stopBits == 1)
    {
        term.c_cflag &= ~((tcflag_t)(CSTOPB));
    }
    else if (stopBits > 1)
    {
        term.c_cflag |= CSTOPB;
    }

    // Set parity
    term.c_iflag &= ~((tcflag_t)(INPCK | ISTRIP));
    switch (parity)
    {
    case ParityOdd:
        term.c_cflag |= PARENB | PARODD;
        break;
    case ParityEven:
        term.c_cflag |= PARENB;
        term.c_cflag &= ~((tcflag_t)(PARODD));
        break;
    default:
        term.c_cflag &= ~((tcflag_t)(PARENB | PARODD));
    }

    // Flow control - clear first.
    term.c_iflag &= ~((tcflag_t)(IXOFF | IXON));
    term.c_cflag &= ~((tcflag_t)(CRTSCTS));
    switch (handshake)
    {
        case HandshakeNone: /* None */
            /* do nothing. We did the reset above */
            break;
        case HandshakeHard: /* hardware flow control */
            term.c_cflag |= CRTSCTS;
            break;
        case HandshakeBoth: /* software & hardware flow control */
            term.c_cflag |= CRTSCTS;
            // fall through
        case HandshakeSoft: /* XOn/XOff */
            term.c_iflag |= IXOFF | IXON;
            break;
    }

    if (speed)
    {
        speed_t brate = SystemIoPortsNative_TermiosSpeed2Rate(speed);
        if (brate == B0)
        {
            errno = EINVAL;
            return -1;
        }

        ret = cfsetspeed(&term, brate);
    }

    if ((ret != 0) || (tcsetattr(fd, TCSANOW, &term) < 0))
    {
        return -1;
    }

    return 0;
}
