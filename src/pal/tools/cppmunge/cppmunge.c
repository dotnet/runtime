//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

//
// ============================================================================
// File: cppmunge.c
//
// ============================================================================
// cppmunge reads in preprocessed C and C++ source files, munges their
// contents, and writes out a new version of the preprocessed file with
// assorted changes.
//
// cppmunge currently does these tasks:AddEscapeCharToString
//   -Converts wide strings (L"...") from native wchar_t characters to
//    two-byte characters, casting the result to a PAL-defined
//    wchar_t *.
//
// As input, it takes a relative or absolute path to the preprocessed
// file. It is intended to be called from a makefile.
//
// To test, create a file containing the following line:
//  WCHAR* p = L"te\x5544\033\x22\n";
// and run cppmunge on it.  The file should be replaced by this line on
// little-endian machines:
//  WCHAR* p = ((WCHAR *) ("t\000e\000\x55\x44\033\000\x22\000\n\000\0\000"));
// and on big-endian, with this:
//  WCHAR* p = ((WCHAR *) ("\000t\000e\x44\x55\000\033\000\x22\000\n\000\0"));

#ifdef __APPLE__
#define _DARWIN_BETTER_REALPATH
#endif

#include <assert.h>
#include <ctype.h>
#if !defined(__APPLE__)
#include <linux/limits.h>
#endif
#include <limits.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>

typedef enum {
    eStateNormal,
    eStateEscape,
    eStateOctal,
    eStateHex,
    eStateNotInString,
    eStateNewStringPending,
    eStateDone
} CharacterState;

typedef enum {
    eModeLiteral,       // L"foo"
    eModeSizeOf,        // sizeof(L"foo")
    eModeInitializer    // WCHAR bar[] = L"foo"
} ConvertMode;

typedef enum {
    eMatchStateNoMatch,
    eMatchStateFoundHash,
    eMatchStateFoundSpace,
    eMatchStateFoundLineDirective
} DirectiveMatchState;

static int ProcessFile(const char *filename);
static int ConvertFile(const char *filename, FILE *file);
static int ConvertCharacters(FILE *original, FILE *destination);
static int ConvertOneString(FILE *original, FILE *destination, ConvertMode mode);
static int AddCharToString(char *string, int stringLength, char ch, ConvertMode mode);
static int AddEscapeCharToString(char *string, int stringLength,
                                 const char escapedChar[],
                                 int charLength,
                                 CharacterState state,
                                 ConvertMode mode);
static int IsBaseDigit(char ch, CharacterState type);
static int MaxSizeForBase(CharacterState base);
static char *CheckStringBufferSize(char *buffer, int *allocatedSize,
                                    int bytesUsed);

int main(int argc, char **argv) {

    
    if (argc == 2 && !strcmp(argv[1], "--help")) {
        fprintf(stderr, "Usage: cppmunge [file]\n");
        exit(1);
    }
    
    return ProcessFile(argc == 2 ? argv[1] : NULL);
}

/* ProcessFile
 * -----------
 * Reads the specified file, converts it, and writes it out. Returns 0 if
 * successful and an error code otherwise. If there's an error, the output
 * file is removed.
 *
 * If filename == NULL, act as a filter from stdin to stdout. If there's an
 * error, since there's no output file to remove, spit out a #error instead.
 */
static int ProcessFile(const char *filename) {
    int err = 0;
    if (filename) {
        FILE *file;
        
        file = fopen(filename, "r");
        if (file == NULL) {
            err = errno;
            goto done;
        }
        err = ConvertFile(filename, file);
        fclose(file);
    }
    else {
        err = ConvertCharacters(stdin, stdout);
        if (err) {
            printf ("#error \"cppmunge failed when converting wide chars\"\n");
            goto done;
        }
    }
  done:
    return err;
}

/* ConvertFile
 * -----------
 * Takes a filename and a FILE * to that file that is already opened for
 * reading. Creates a temporary file to write output to, and converts the
 * given file, character by character, writing the result into the
 * temporary file. The temporary file is renamed to the specified filename
 * on successful completion. If renaming fails or the function itself fails, 
 * the temporary file is deleted.
 *
 * Returns 0 on success and an error code otherwise.
 */
static int ConvertFile(const char *filename, FILE *file) {
    char tempFileName[PATH_MAX + 1];
    FILE *tempFile;
    int err;
    int removeErr;

    snprintf(tempFileName, sizeof(tempFileName) - 1, "%s.cppmunge", filename);
    
    tempFile = fopen(tempFileName, "w");
    if (tempFile == NULL) {
        return errno;
    }

    err = ConvertCharacters(file, tempFile);
    if (err != 0) {
        fclose(tempFile);
        remove(tempFileName);
        return err;
    }

    // Close the files.
    fclose(tempFile);

    // Move the temporary file over the original.
    err = rename(tempFileName, filename);
    if (err != 0) {
        removeErr = remove(tempFileName);
        if (removeErr != 0) {
            fprintf(stderr, "cppmunge: error: delete of %s failed\n", tempFileName);
            err = errno;
        }
    }
    return err;
}

/* ConvertCharacters
 * -----------------
 * Takes two FILE *s, one to the original file that is opened for reading
 * and one to the destination that is open for writing. Copies the contents
 * of the original to the destination, converting wide strings in the
 * process. Returns 0 on success and an error code on failure.
 */

static int ConvertCharacters(FILE *original, FILE *destination) {
    int ch;
    int err;
    int escape;
    int previousCh;
    int matched;
    int terminator;
    int upper;
    int badslash;
    DirectiveMatchState directive_match_state;
    char* endoflastline;
    char lastlinebuf[256];
    ConvertMode mode;

    previousCh = EOF;
    err = 0;

    while ((ch = fgetc(original)) != EOF) {

    Restart:
        switch (ch) {
        case '\"':
        case '\'':
            // regular string or char - pass it through
            if (fputc(ch, destination) == EOF) {
                return ferror(destination);
            }
            
            terminator = ch;

            escape = 0;
            while ((ch = fgetc(original)) != EOF) {
                if (fputc(ch, destination) == EOF) {
                    return ferror(destination);
                }

                switch (ch) {
                case '\"':
                case '\'':
                    if (!escape) {
                        if (ch == terminator) 
                            break;
                    }
                    escape = 0;
                    continue;
                case '\\':
                    escape = !escape;
                    continue;
                default:
                    escape = 0;
                    continue;
                }
                break;
            }
            previousCh = ch;
            break;

        case 'L':
        case 'l':
            mode = eModeLiteral;

        UnicodeString:
            previousCh = ch;

            if ((ch = fgetc(original)) == EOF)
                break;

            if (ch != '\"') {
                if (fputc(previousCh, destination) == EOF) {
                    return ferror(destination);
                }
                goto Restart;
            }

            // The start of a string
            err = ConvertOneString(original, destination, mode);
            previousCh = EOF;
            break;

#define trymatch(expected, ignorewhitespace) \
    matched = 0; \
    for (;;) { \
        if (ch == EOF) { \
            if ((ch = fgetc(original)) == EOF) \
                break; \
        } \
        if (ignorewhitespace && isspace(ch)) { \
            if (fputc(ch, destination) == EOF) { \
                return ferror(destination); \
            } \
            ch = EOF; \
            continue; \
        } \
        if (ch == expected) { \
            if (fputc(ch, destination) == EOF) { \
                return ferror(destination); \
            } \
            matched = 1; \
            ch = EOF; \
        } \
        break; \
    }

#define match(c, ignorewhitespace) \
    trymatch(c, ignorewhitespace); \
    if (!matched) goto Restart;

        case 's':
            // look for "sizeof ( L"

            if (isalnum(previousCh))
                goto FallBack;

            match('s', 0);
            match('i', 0);
            match('z', 0);
            match('e', 0);
            match('o', 0);
            match('f', 0);
            do {
                trymatch('(', 1);
            } while (matched);

            if (ch != 'l' && ch != 'L')
                goto Restart;

            mode = eModeSizeOf;
            goto UnicodeString;

        case '[':
            // look for "[ ] = ( L"

            match('[', 1);
            match(']', 1);
            match('=', 1);

            do {
                trymatch('(', 1);
            } while (matched);

            if (ch != 'l' && ch != 'L')
                goto Restart;

            mode = eModeInitializer;
            goto UnicodeString;

        case '#':
            if (previousCh != '\r' && previousCh != '\n' && previousCh != EOF)
                goto FallBack;

            // line directive
            if (fputc(ch, destination) == EOF) {
                return ferror(destination);
            }
       
            directive_match_state = eMatchStateFoundHash;
            // directive match currently detects "#\w+[0-9]" 
            // which is indicative of a line directive 
            // Currently this is used to avoid signaling an error condition 
            // when a "#pragma" line contains a capital letter (see below)
                
            upper = 0;
            badslash = 0;
            endoflastline = lastlinebuf;
 
            while((ch = fgetc(original)) != EOF) {
                switch (ch) {
                case '\r': 
                case '\n':
                    *endoflastline = '\0';

                    // only signal error if our "#..." line is actually a 
                    // line directive.  "#pragma" "#error", etc.
                    // can legitimately have capital letters in that line
#if 0
                    if (upper && directive_match_state == eMatchStateFoundLineDirective) {
                        fprintf(stderr, "cppmunge:0: use lowercased filenames in #include directives for portability\n");
                        fprintf(stderr, "cppmunge:0: %s\n", lastlinebuf);
                        return 1;
                    }
#endif
                    if (badslash) {
                        fprintf(stderr, "cppmunge:0: use '/' as path separator in #include directives for portability\n");
                        fprintf(stderr, "cppmunge:0: %s\n", lastlinebuf);
                        return 1;
                    }
                    goto FallBack;

#ifdef PLATFORM_UNIX
                case '\\':
                    badslash = 1;
                    break;
#endif

                case '/':
                    upper = 0;
                    break;

                default:
                    if (directive_match_state == eMatchStateFoundHash)
                    {
                        if (isspace(ch)) {
                                directive_match_state = eMatchStateFoundSpace;
                        } else {
                                directive_match_state = eMatchStateNoMatch;
                        }
                    } else if (directive_match_state == eMatchStateFoundSpace) {
                        if (isdigit(ch)) {
                                directive_match_state = eMatchStateFoundLineDirective;                
                        } else if (!isspace(ch)) {
                                directive_match_state = eMatchStateNoMatch;
                        }
                        // otherwise remain in eMatchStateFoundSpace
                    } 

                    if (isupper(ch)) 
                    {
                        upper = 1;
                    }
                    break;
                }

                if (endoflastline < &lastlinebuf[sizeof(lastlinebuf)-1]) {
                    *endoflastline++ = ch;
                }

                if (fputc(ch, destination) == EOF) {
                    return ferror(destination);
                }
            } 
            break;
            
        default:
        FallBack:
            if (fputc(ch, destination) == EOF) {
                return ferror(destination);
            }
            previousCh = ch;
            break;
        }
    }

    return err;
}

/* ConvertOneString
 * ----------------
 * Takes in a FILE * to the original file and another FILE * to the
 * destination. Assumes that the previous two characters read from
 * the original file were L", so the mark is at the start of a wide
 * string. This function reads the rest of the string, including any
 * immediately following strings that should be concatenated with
 * it, and writes out a CLR-compatible version of the string. Whitespace
 * between concatenated strings will be skipped, but an equivalent number
 * of newlines will be written out if any are part of that whitespace.
 * Whitespace that follows a wide string will be decreased to a single
 * space.
 * Returns 0 on success and an error code on failure.
 */
static int ConvertOneString(FILE *original, FILE *destination, ConvertMode mode) {
    char *newString;
    int newStringLength;
    int ch = '\0';
    int bytesCopied;
    int err;
    char numberBuffer[8]; // A pending octal or hex number
    int numberOffset;
    CharacterState state;
    int newlines;
    int previousCh;
    int hasTrailingWhitespace;
    int i;

    err = 0;
    newStringLength = 1024;
    bytesCopied = 0;
    state = eStateNormal;
    numberOffset = 0;
    newlines = 0;
    previousCh = EOF;
    hasTrailingWhitespace = 0;

    newString = (char*)malloc(newStringLength);
    if (newString == NULL) {
        return errno;
    }

    while (err == 0 && state != eStateDone && (ch = fgetc(original)) != EOF) {
        switch (state) {
            case eStateNormal:
                if (ch == '\\') {
                    state = eStateEscape;
                } else if (ch == '"') {
                    // Done with the string. Read on till we
                    // know we don't have a concatenated string.
                    state = eStateNotInString;
                } else {
                    bytesCopied += AddCharToString(newString, bytesCopied, ch, mode);
                }
                break;
            case eStateEscape:
                if (IsBaseDigit(ch, eStateOctal)) {
                    // Octal number. Put it back so it gets handled
                    // in the octal case.
                    if (ungetc(ch, original) == EOF) {
                        err = EIO;  // As good as anything else
                    }
                    state = eStateOctal;
                    memset(numberBuffer, 0, sizeof(numberBuffer));
                } else if (ch == 'x') {
                    // Hex number.
                    state = eStateHex;
                    memset(numberBuffer, 0, sizeof(numberBuffer));
                } else {
                    // Escape sequences that aren't numbers are
                    // two bytes. Write those out as a single
                    // character.
                    char curCh = ch;
                    bytesCopied += AddEscapeCharToString(newString,
                                            bytesCopied, &curCh, 1,
                                            eStateNormal,
                                            mode);
                    state = eStateNormal;
                }
                break;
            case eStateOctal:
            case eStateHex:
                if (IsBaseDigit(ch, state) &&
                                numberOffset < MaxSizeForBase(state)) {
                    // Add the number to our buffer.
                       numberBuffer[numberOffset++] = ch;
                } else if (numberOffset == 0) {
                    // Weird.
                    err = EINVAL;
                } else {
                    // The number's done.
                    // Write it out and put the character back.
                    bytesCopied += AddEscapeCharToString(newString,
                                            bytesCopied,
                                            numberBuffer, numberOffset,
                                            state,
                                            mode);
                    numberOffset = 0;
                    state = eStateNormal;
                    if (ungetc(ch, original) == EOF) {
                        err = EIO;  // As good as anything else
                    }
                }
                break;
            case eStateNotInString:
                if (toupper(ch) == 'L') {
                    previousCh = ch;
                    state = eStateNewStringPending;
                } else if (ch == '\n') {
                    newlines++;
                } else if (!isspace(ch)) {
                    // Put the character back
                    if (ungetc(ch, original) == EOF) {
                        err = EIO;  // As good as anything else
                    }
                    // We're done!
                    state = eStateDone;
                } else {
                    // Bypass whitespace, but mark it for later.
                    hasTrailingWhitespace = 1;
                }
                break;
            case eStateNewStringPending:
                if (ch == '"') {
                    // New string. Add it to the previous one.
                    state = eStateNormal;
                } else {
                    // Write out the previous character.
                    if (fputc(previousCh, destination) != EOF) {
                        // Put the current one back.
                        if (ungetc(ch, original) == EOF) {
                            err = EIO;  // As good as anything else
                        }
                    } else {
                        err = ferror(destination);
                    }
                    // We're done!
                    state = eStateDone;
                }
                break;
            default:
                break;
        }
        // Reallocate our string if necessary
        newString = CheckStringBufferSize(newString, &newStringLength,
                                            bytesCopied);
        if (newString == NULL) {
            err = errno;
        }
    }

    if (ch == EOF) {
        // Hmm...that shouldn't have happened. We're only supposed
        // to run on preprocessor output, and the preprocessor will
        // catch an unterminated string constant.
        err = EIO;  // As good a value as any
    }

    if (err == 0) {
        // Write out the string. First, add the trailing null character.
        char zeroCh = '0';
        bytesCopied += AddEscapeCharToString(newString, bytesCopied,
                                                &zeroCh, 1, eStateNormal, mode);

        // Reallocate our string if necessary
        newString = CheckStringBufferSize(newString, &newStringLength,
                                         bytesCopied);
        if (newString == NULL) {
            err = errno;
        }
    }

    if (err == 0) {

        if (mode == eModeInitializer) {
            // eat the last comma
            bytesCopied -= 1;
        }
        else {
            // eat the last \000
            bytesCopied -= 4;
        }

        // Terminate the buffer so we can use it as a real string.
        newString[bytesCopied] = '\0';

        // Now, write it out.
        switch (mode) {
        case eModeLiteral:
            fprintf(destination, "((WCHAR *) (\"%s\"))", newString);
            break;

        case eModeSizeOf:
            fprintf(destination, "(\"%s\")", newString);
            break;

        case eModeInitializer:
            fprintf(destination, "{%s}", newString);
            break;

        default:
            break;
        }

        // If there's trailing whitespace, print that.
        if (hasTrailingWhitespace) {
            fprintf(destination, " ");
        }

        // And print all of the newlines.
        for(i = 0; i < newlines; i++) {
            fprintf(destination, "\n");
        }
    }

    return err;
}

/* AddCharToString
 * ---------------
 * Writes a two-byte version of the given character to the specified
 * location in the string buffer. This should not fail, so there is
 * no return value.
 *
 */
static int AddCharToString(char *string, int stringLength, char ch, ConvertMode mode) {
    int initialStringLength = stringLength;
    if (mode != eModeInitializer) {
#if BIGENDIAN
        // Octal zero
        string[stringLength++] = '\\';
        string[stringLength++] = '0';
        string[stringLength++] = '0';
        string[stringLength++] = '0';
        // The character
        string[stringLength++] = ch;
#else
        string[stringLength++] = ch;
        // Octal zero
        string[stringLength++] = '\\';
        string[stringLength++] = '0';
        string[stringLength++] = '0';
        string[stringLength++] = '0';
#endif
    }
    else {
        string[stringLength++] = '\'';
        if (ch == '\'') {
            string[stringLength++] = '\\';
        }
        string[stringLength++] = ch;
        string[stringLength++] = '\'';
        string[stringLength++] = ',';
    }
    return stringLength - initialStringLength;
}

/* AddEscapeCharToString
 * ---------------------
 * Writes each character in the given buffer to the specified location
 * in the string buffer, along with an appropriately-placed null
 * character. Appends a backslash to the string prior to writing any
 * characters in the buffer to produce an escape sequence in the
 * resulting string. If the state is eStateHex, the character buffer
 * is divided into two-character chunks, and each of those is written
 * as a separate hex character.
 *
 */

int AddEscapeCharToString(char *string, int stringLength,
                          const char escapedChar[],
                          int charLength,
                          CharacterState state,
                          ConvertMode mode) {
    int i;
    int initialStringLength = stringLength;

    if (mode != eModeInitializer) {
        // copy escapedChar into tmp buffer so we can 0-extend if necessary
        char esc[4];
        int  esc_len = 0;
        if (charLength % 2) {
            esc[esc_len++] = '0';
        }
        memcpy (esc + esc_len, escapedChar, charLength);
        esc_len += charLength;
        
        assert(esc_len % 2 == 0);
        assert(esc_len <= 4);

#define WRITE_NULL_CHAR                                                 \
        if (esc[esc_len - 1] != '\0' &&                                 \
            (state != eStateHex || (esc_len % 4 != 0))) {               \
            string[stringLength++] = '\\';                              \
            string[stringLength++] = '0';                               \
            string[stringLength++] = '0';                               \
            string[stringLength++] = '0';                               \
        }
        
#if BIGENDIAN
        // Only write a zero if the last character to write isn't zero
        // and, if we're writing hex characters, we aren't about to write
        // an even block of four.
        WRITE_NULL_CHAR;
#endif
        if (state == eStateHex) {
            for (i = 0; i < esc_len; i += 2) {
                string[stringLength++] = '\\';
                string[stringLength++] = 'x';
#if BIGENDIAN
                string[stringLength++] = esc[esc_len - 2 - i];
                string[stringLength++] = esc[esc_len - 1 - i];
#else
                string[stringLength++] = esc[i];
                string[stringLength++] = esc[i + 1];
#endif // BIGENDIAN
            }
        }
        else {
            string[stringLength++] = '\\';
            for(i = 0; i < charLength; i++) {
                string[stringLength++] = escapedChar[i];
            }
        }
#if !BIGENDIAN
        // Only append a zero if our last character was not zero
        // and, if we're writing hex characters, we didn't write
        // an even block of four.
        WRITE_NULL_CHAR;
#endif // BIGENDIAN

#undef WRITE_NULL_CHAR
    }
    else {
        if (state == eStateHex) {
            string[stringLength++] = '0';
            string[stringLength++] = 'x';
            for(i = 0; i < charLength; i++) {
                string[stringLength++] = escapedChar[i];
            }
        } 
        else
            if (state == eStateOctal) {
                string[stringLength++] = '0';
                for(i = 0; i < charLength; i++) {
                    string[stringLength++] = escapedChar[i];
                }
            }
            else {
                string[stringLength++] = '\'';
                string[stringLength++] = '\\';
                for(i = 0; i < charLength; i++) {
                    string[stringLength++] = escapedChar[i];
                }
                string[stringLength++] = '\'';
            }

        string[stringLength++] = ',';
    }

    return stringLength - initialStringLength;
}

/* IsBaseDigit
 * -----------
 * Returns whether the character is valid for a number of the specified
 * type.
 */
static int IsBaseDigit(char ch, CharacterState type) {
    if (type == eStateOctal) {
        return (ch >= '0' && ch <= '7');
    } else if (type == eStateHex) {
        return isxdigit((unsigned char) ch);
    } else {
        return 0;
    }
}

/* MaxSizeForBase
 * --------------
 * Returns the maximum number of bytes after the escape sequence that
 * can be used to represent a character in the given base.
 */
static int MaxSizeForBase(CharacterState base) {
    if (base == eStateOctal) {
        return 3;
    } else if (base == eStateHex) {
        return 8;   /* OK, so it's technically infinite, but there's
                     * a limit on just how big of an infinity is
                     * likely. */
    } else {
        return 0;
    }
}

/* CheckStringBufferSize
 * ---------------------
 * Compares the size of the buffer to the size of the buffer's contents
 * and realloc's to twice the original size if the contents are within
 * six of the buffer size. Returns NULL and sets errno if reallocation
 * fails.
 */
static char *CheckStringBufferSize(char *buffer, int *allocatedSize,
                                    int bytesUsed) {
    char *newBuffer;

    // The actual max size of the margin should be 12 (an eight-digit hex 
    // character, such as * \x1234abcd, followed by a trailing null character 
    // to make a wide char)

    // We will be generous about the margin. 32 should cover 
    // any border case that we have missed.

    if (bytesUsed >= *allocatedSize - 32) {
        newBuffer = (char*)realloc(buffer, 2 * (*allocatedSize));
        if (newBuffer != NULL) {
            *allocatedSize *= 2;
        }
    } else {
        newBuffer = buffer;
    }
    return newBuffer;
}
