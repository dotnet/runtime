# Note that you will need Microsoft.NET to generate those masterinfos

# Customize to indicate your own installation dir.
DLL_PATH=c:/WINDOWS/Microsoft.NET/Framework/v1.1.4322

all: \
	mscorlib.xml \
	System.xml \
	System.Xml.xml \
	System.Data.xml \
	System.Web.xml \
	System.Web.Services.xml \
	System.Runtime.Serialization.Formatters.Soap.xml \
	System.Drawing.xml \
	System.Security.xml \
	System.DirectoryServices.xml \
	System.Windows.Forms.xml \
	Cscompmgd.xml

mscorlib.xml :
	mono-api-info $(DLL_PATH)/mscorlib.dll > mscorlib.xml

System.xml :
	mono-api-info $(DLL_PATH)/System.dll > System.xml

System.Xml.xml :
	mono-api-info $(DLL_PATH)/System.Xml.dll > System.Xml.xml

System.Data.xml :
	mono-api-info $(DLL_PATH)/System.Data.dll > System.Data.xml

System.Web.xml :
	mono-api-info $(DLL_PATH)/System.Web.dll > System.Web.xml

System.Web.Services.xml :
	mono-api-info $(DLL_PATH)/System.Web.Services.dll > System.Web.Services.xml

System.Runtime.Serialization.Formatters.Soap.xml :
	mono-api-info $(DLL_PATH)/System.Runtime.Serialization.Formatters.Soap.dll > System.Runtime.Serialization.Formatters.Soap.xml

System.Drawing.xml :
	mono-api-info $(DLL_PATH)/System.Drawing.dll > System.Drawing.xml

System.Security.xml :
	mono-api-info $(DLL_PATH)/System.Security.dll > System.Security.xml

System.DirectoryServices.xml :
	mono-api-info $(DLL_PATH)/System.DirectoryServices.dll > System.DirectoryServices.xml

System.Windows.Forms.xml :
	mono-api-info $(DLL_PATH)/System.Windows.Forms.dll > System.Windows.Forms.xml

Cscompmgd.xml :
	mono-api-info $(DLL_PATH)/Cscompmgd.dll > Cscompmgd.xml


clean:
	rm mscorlib.xml System.xml System.Xml.xml System.Data.xml System.Web.xml System.Web.Services.xml System.Runtime.Serialization.Formatters.Soap.xml System.Drawing.xml System.Security.xml System.DirectoryServices.xml System.Windows.Forms.xml Cscompmgd.xml 


