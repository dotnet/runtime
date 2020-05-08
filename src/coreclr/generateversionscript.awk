BEGIN {
    print "V1.0 {";
    print "    global:";
} 
{ 
    # Remove the CR character in case the sources are mapped from
    # a Windows share and contain CRLF line endings
    gsub(/\r/,"", $0);
	
    # Skip empty lines and comment lines starting with semicolon
    if (NF && !match($0, /^[ \t]*;/))
    {
        # Only prefix the entries that start with "#"
	if (match($0, /^#.*/))
	{
	    gsub(/^#/,"", $0);
	    print "        "prefix $0 ";";
	}
        else
        {
	    print "        "$0 ";";
	}
    }
} 
END {
    print "    local: *;"
    print "};";
}
