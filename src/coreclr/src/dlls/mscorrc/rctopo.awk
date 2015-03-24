# Convert string resources from Windows native resource file to the 
# input format of the gettext toolchain.
BEGIN {
    inStringTable = 0;
    inBeginEnd = 0;
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
        # sprintf can only handle signed ints, so we need to convert
        # values >= 0x80000000 to negative values
        if (var >= 2147483648)
        {
            var = var - 4294967296;
        }
        var = sprintf("%X", var);
        print "msgid \""var"\"";
        # remove the L prefix from strings
        gsub(/L"/, "\"", $0);
        # join strings "..." "..." into one
        gsub(/" +"/, "", $0);
        # remove all terminating newlines from the string - the msgfmt fails on those
        # since it expects them to be at the end of the msgid as well
        while(gsub(/\\n"/, "\"", $0)) {}
        print "msgstr" $0;
        print "";
    }
}
