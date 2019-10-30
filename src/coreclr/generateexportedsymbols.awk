{ 
    # Remove the CR character in case the sources are mapped from
    # a Windows share and contain CRLF line endings
    gsub(/\r/,"", $0);

    # Skip empty lines and comment lines starting with semicolon
    if (NF && !match($0, /^[[:space:]]*;/))
    {
        gsub(/^#/,"", $0);
        print "_"  $0;
    }
} 
