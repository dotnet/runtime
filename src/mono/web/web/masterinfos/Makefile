# Note that you will need Microsoft.NET to generate those masterinfos

# Customize to indicate your own installation dir.
DLL_PATH=c:/WINDOWS/Microsoft.NET/Framework/v1.1.4322
# DLL_PATH=c:/WINDOWS/Microsoft.NET/Framework/v1.2.30703

all: \
	mscorlib.xml \
	System.xml \
	System.Xml.xml \
	System.Configuration.Install.xml \
	System.Data.xml \
	System.Data.OracleClient.xml \
	System.Design.xml \
	System.Management.xml \
	System.Messaging.xml \
	System.Web.xml \
	System.Web.Services.xml \
	System.Runtime.Serialization.Formatters.Soap.xml \
	System.Drawing.xml \
	System.Drawing.Design.xml \
	System.Security.xml \
	System.DirectoryServices.xml \
	System.EnterpriseServices.xml \
	System.Runtime.Remoting.xml \
	System.Windows.Forms.xml \
	Microsoft.VisualBasic.xml \
	Cscompmgd.xml

mscorlib.xml :
	mono-api-info $(DLL_PATH)/mscorlib.dll > mscorlib.xml

System.xml :
	mono-api-info $(DLL_PATH)/System.dll > System.xml

System.Xml.xml :
	mono-api-info $(DLL_PATH)/System.Xml.dll > System.Xml.xml

System.Configuration.Install.xml :
	mono-api-info $(DLL_PATH)/System.Configuration.Install.dll > System.Configuration.Install.xml

System.Data.xml :
	mono-api-info $(DLL_PATH)/System.Data.dll > System.Data.xml

System.Data.OracleClient.xml :
	mono-api-info $(DLL_PATH)/System.Data.OracleClient.dll > System.Data.OracleClient.xml

System.Design.xml :
	mono-api-info $(DLL_PATH)/System.Design.dll > System.Design.xml

System.Management.xml :
	mono-api-info $(DLL_PATH)/System.Management.dll > System.Management.xml

System.Messaging.xml :
	mono-api-info $(DLL_PATH)/System.Messaging.dll > System.Messaging.xml

System.Web.xml :
	mono-api-info $(DLL_PATH)/System.Web.dll > System.Web.xml

System.Web.Services.xml :
	mono-api-info $(DLL_PATH)/System.Web.Services.dll > System.Web.Services.xml

System.Runtime.Remoting.xml :
	mono-api-info $(DLL_PATH)/System.Runtime.Remoting.dll > System.Runtime.Remoting.xml

System.Runtime.Serialization.Formatters.Soap.xml :
	mono-api-info $(DLL_PATH)/System.Runtime.Serialization.Formatters.Soap.dll > System.Runtime.Serialization.Formatters.Soap.xml

System.Drawing.xml :
	mono-api-info $(DLL_PATH)/System.Drawing.dll > System.Drawing.xml

System.Drawing.Design.xml :
	mono-api-info $(DLL_PATH)/System.Drawing.Design.dll > System.Drawing.Design.xml

System.Security.xml :
	mono-api-info $(DLL_PATH)/System.Security.dll > System.Security.xml

System.DirectoryServices.xml :
	mono-api-info $(DLL_PATH)/System.DirectoryServices.dll > System.DirectoryServices.xml

System.EnterpriseServices.xml :
	mono-api-info $(DLL_PATH)/System.EnterpriseServices.dll > System.EnterpriseServices.xml

System.Windows.Forms.xml :
	mono-api-info $(DLL_PATH)/System.Windows.Forms.dll > System.Windows.Forms.xml

Cscompmgd.xml :
	mono-api-info $(DLL_PATH)/Cscompmgd.dll > Cscompmgd.xml

Microsoft.VisualBasic.xml :
	mono-api-info $(DLL_PATH)/Microsoft.VisualBasic.dll > Microsoft.VisualBasic.xml

clean:
	rm mscorlib.xml System.xml System.Xml.xml System.Configuration.Install.xml System.Data.xml System.Data.OracleClient.xml System.Design.xml System.Drawing.Design.xml System.Management.xml System.Messaging.xml System.Web.xml System.Web.Services.xml System.Runtime.Serialization.Formatters.Soap.xml System.Drawing.xml System.Security.xml System.DirectoryServices.xml System.EnterpriseServices.xml System.Runtime.Remoting.xml System.Windows.Forms.xml Cscompmgd.xml Microsoft.VisualBasic.xml


