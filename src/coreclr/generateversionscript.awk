BEGIN {
	print "V1.0 {";
	print "    global:";
} 
{ 
	# Remove the CR character in case the sources are mapped from
	# a Windows share and contain CRLF line endings
	gsub(/\r/,"", $0);
	
	# Skip empty lines and comment lines starting with semicolon
	if (NF && !match($0, /^[:space:]*;/))
	{
		print "        "  $0 ";";
	}
} 
END {
	print "    local: *;"
	print "};";
}
