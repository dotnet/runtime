// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
// Logger for the CoreCLR host ccrun.
// Relies on the SYSCRT and therefore cannot use C++ libraries.
//


class Logger {
    bool m_isEnabled;
    bool m_prefixRequired;
    bool m_formatHRESULT;

public:
    Logger() :
        m_isEnabled(true),
        m_prefixRequired(true),
        m_formatHRESULT(false) { }

    ~Logger() { }

    // Enables output from the logger
    void Enable();

    // Disables output from the logger
    void Disable();


    Logger& operator<< (bool val);
    Logger& operator<< (short val);
    Logger& operator<< (unsigned short val);
    Logger& operator<< (int val);
    Logger& operator<< (unsigned int val);
#ifdef _MSC_VER
    Logger& operator<< (long val);
    Logger& operator<< (unsigned long val);
#endif
    Logger& operator<< (float val);
    Logger& operator<< (double val);
    Logger& operator<< (long double val);
    Logger& operator<< (const wchar_t* val);
    Logger& operator<< (const char* val);
    Logger& operator<< (Logger& ( *pf )(Logger&));
    static Logger& endl ( Logger& log );
    static Logger& hresult ( Logger& log);

private:
    void EnsurePrefixIsPrinted();
};





