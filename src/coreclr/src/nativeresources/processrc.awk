# Parse string resources from Windows native resource file
# and pass them to the writestringentry function that
# is responsible for writing the resource id and string
# to a platform specific resource file.
# A script containing this function needs to be specified
# using the -f command line parameter before this script.

BEGIN {
    inStringTable = 0;
    inBeginEnd = 0;
    arrayName = "nativeStringResourceArray_" name;
    tableName = "nativeStringResourceTable_" name;
    writeheader(arrayName, tableName);
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
            # some of the resource IDs contain cast to HRESULT
            gsub(/\(HRESULT\)/, "", $i);
            # some of the resource IDs have trailing L
            gsub(/L/, "", $i);
            expression = expression $i;
            i++;
        }

        # evaluate the resource ID expression
        cmd = "echo $(("expression"))";
        cmd | getline var;
        close(cmd);
        # in case shell returned the result as a string, ensure the var has numeric type
        var = var + 0;

        # Extract string content starting with either " or L"
        idx = match($0, /L?\"/);
        content = substr($0, idx);

        # remove the L prefix from strings
        gsub(/L"/, "\"", content);
        # join strings "..." "..." into one
        gsub(/" +"/, "", content);

        # write the resource entry to the target file
        writestringentry(var, content);
    }
}
END {
    writefooter(arrayName, tableName);
}
