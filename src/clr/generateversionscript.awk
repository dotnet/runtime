BEGIN {
	print "V1.0 {";
	print "    global:";
} 
{ 
	# Remove the CR character in case the sources are mapped from
	# a Windows share and contain CRLF line endings
	gsub(/\r/,"", $0);
	print "        "  $0 ";";
} 
END {
	print "    local: *;"
	print "};";
}
