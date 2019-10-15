# "jump" is the jump instruction for the platform
# "prefix1" is the prefix of what is being mapped from
# "prefix2" is the prefix of what is being mapped to
{ 
    # Remove the CR character in case the sources are mapped from
    # a Windows share and contain CRLF line endings
    gsub(/\r/,"", $0);
    
    # Skip empty lines and comment lines starting with semicolon
    if (NF && !match($0, /^[[:space:]]*;/))
    {
        # Only process the entries that begin with "#"
        if (match($0, /^#.*/))
        {
            gsub(/^#/,"", $0);
            print "LEAF_ENTRY " prefix1 $0 ", _TEXT"
            print "    " jump " EXTERNAL_C_FUNC(" prefix2 $0 ")"
            print "LEAF_END " prefix1 $0 ", _TEXT"
            print ""
        }
    }
} 
