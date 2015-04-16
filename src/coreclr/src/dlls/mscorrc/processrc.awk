# Parse string resources from Windows native resource file 
# and pass them to the writestringentry function that 
# is responsible for writing the resource id and string
# to a platform specific resource file.
# A script containing this function needs to be specified
# using the -f command line parameter before this script.

BEGIN {
    inStringTable = 0;
    inBeginEnd = 0;
    writeheader();
}
{
    if ($1 == "STRINGTABLE" && $2 == "DISCARDABLE")
    {
        inStringTable = 1;
    }
    else if ($1 == "BEGIN")
    {
        inBeginEnd = inStringTable;
    }
    else if (inBeginEnd && $1 == "END")
    {
        inBeginEnd = 0;
        inStringTable = 0;
    }
    else if (inBeginEnd && $1 != "")
    {
        # combine all items until the first string and remove them 
        # from the line
        i = 1
        expression = ""
        # string starts with either a quote or L followed by a quote
        while (substr($i, 1, 1) != "\"" && substr($i, 1, 2) != "L\"")
        {
            # some of the resource ids contain cast to HRESULT
            gsub(/\(HRESULT\)/, "", $i);
            # some of the resource ids have trailing L
            gsub(/L/, "", $i);
            expression = expression $i;
            $i="";
            i++;
        }
        # evaluate the resource id expression and format it as hex number
        cmd = "echo $(("expression"))";
        cmd | getline var;
        close(cmd);
        # in case shell returned the result as a string, ensure the var has numeric type
        var = var + 0;
        # sprintf can only handle signed ints, so we need to convert
        # values >= 0x80000000 to negative values
        if (var >= 2147483648)
        {
            var = var - 4294967296;
        }
        var = sprintf("%X", var);
        # remove the L prefix from strings
        gsub(/L"/, "\"", $0);
        # join strings "..." "..." into one
        gsub(/" +"/, "", $0);
        # remove all terminating newlines from the string - the msgfmt fails on those
        # since it expects them to be at the end of the msgid as well
        while(gsub(/\\n"/, "\"", $0)) {}

        # write the resource entry to the target file
        writestringentry(var, $0);
    }
}
END {
    writefooter();
}
