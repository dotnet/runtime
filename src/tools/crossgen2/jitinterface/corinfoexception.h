// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <string.h>

#ifdef PLATFORM_UNIX
typedef char16_t WCHAR;
#else
typedef wchar_t WCHAR;
#endif

class CorInfoException
{
public:
    CorInfoException(const WCHAR* message, int messageLength)
    {
        this->message = new WCHAR[messageLength + 1];
        memcpy(this->message, message, messageLength * sizeof(WCHAR));
        this->message[messageLength] = L'\0';
    }

    ~CorInfoException()
    {
        if (message != nullptr)
        {
            delete[] message;
            message = nullptr;
        }
    }

    const WCHAR* GetMessage() const
    {
        return message;
    }

private:
    WCHAR* message;
};
