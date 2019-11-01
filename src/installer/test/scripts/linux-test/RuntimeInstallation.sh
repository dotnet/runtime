#!/bin/bash

current_user=$(whoami)
if [ $current_user != "root" ]; then
    echo "script requires superuser privileges to run"
    exit 1
fi

source /etc/os-release
distro="$ID"
version="$VERSION_ID"
arch="x64"
result_file="/docker/result.txt"
log_file="/docker/logfile.txt"

exec &>> $log_file

if [ $ID == "ol" ] ; then
        distro="oraclelinux"
fi
if [ "$distro" == "oraclelinux" ] || [ "$distro" == "rhel" ] ||  [ "$distro" == "opensuse" ] ; then
	version=$(echo $version | cut -d . -f 1)
fi      

echo $distro:$version

runtime_version=$1
if [ "$runtime_version" == "latest" ] ; 
then
	BLOB_RUNTIME_DIR="https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master"
else
	BLOB_RUNTIME_DIR="https://dotnetcli.blob.core.windows.net/dotnet/Runtime/$runtime_version"
fi

install_curl(){
	apt-get -y install curl 
	if [ $? -ne 0 ] ; then
		apt-get update
		apt-get -y install curl
	fi
}
download_from_blob_deb(){
	BLOB_PATH=$1
	if curl --output /dev/null --head --fail $BLOB_PATH; then  
		curl -O -s $BLOB_PATH
	else 
		echo "Could not extract file from blob"
		exit 1
	fi
}
download_runtime_packages_deb(){
        download_from_blob_deb "$BLOB_RUNTIME_DIR/dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.deb"
        download_from_blob_deb "$BLOB_RUNTIME_DIR/dotnet-host-$runtime_version-$arch.deb"
        download_from_blob_deb "$BLOB_RUNTIME_DIR/dotnet-hostfxr-$runtime_version-$arch.deb"
        download_from_blob_deb "$BLOB_RUNTIME_DIR/dotnet-runtime-$runtime_version-$arch.deb"
}
install_runtime_packages_deb(){
	dpkg -i dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.deb
        apt-get install -f -y
        dpkg -i *.deb
}
determine_runtime_version_deb(){
	if [ "$runtime_version" == "latest" ] ; then
		runtime_version=$(dpkg-deb -f dotnet-runtime-latest-$arch.deb Package)
		runtime_version=${runtime_version#dotnet-runtime-}
	fi
}
check_if_runtime_is_installed_deb(){
	find_runtime=$(apt list --installed | grep dotnet-runtime-$runtime_version)
	if [ "$find_runtime" == "" ] ; then
		echo "Not able to remove runtime $runtime_version because it is not installed"
		exit 1
	fi
}
uninstall_runtime_deb(){
	apt-get remove -y $(apt list --installed | grep -e dotnet | cut -d "/" -f 1)
	runtime_installed_packages=$(apt list --installed | grep -e dotnet)
}
install_wget_yum(){
	yum install -y wget
}
install_wget_zypper(){
	zypper --non-interactive install wget
}
download_from_blob_rpm(){
	BLOB_PATH=$1
	if wget --spider $BLOB_PATH; then  
		wget -nv $BLOB_PATH 
	else 
		echo "Could not extract file from blob"
		exit 1
	fi
}
download_runtime_packages_rpm(){
	download_from_blob_rpm "$BLOB_RUNTIME_DIR/dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm"
        download_from_blob_rpm "$BLOB_RUNTIME_DIR/dotnet-host-$runtime_version-$arch.rpm"
        download_from_blob_rpm "$BLOB_RUNTIME_DIR/dotnet-hostfxr-$runtime_version-$arch.rpm"
        download_from_blob_rpm "$BLOB_RUNTIME_DIR/dotnet-runtime-$runtime_version-$arch.rpm"
}
install_runtime_packages_yum(){
	yum localinstall -y dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm
        rm dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm
        rpm -Uvh *.rpm
}
install_runtime_packages_zypper(){
	zypper --no-gpg-checks --non-interactive in ./dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm
	rm dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm
        rpm -Uvh *.rpm
}
determine_runtime_version_rpm(){
	if [ "$runtime_version" == "latest" ] ; then
		runtime_version=$(rpm -qip dotnet-runtime-latest-$arch.rpm | grep Version)
		runtime_version=$(echo $runtime_version | cut -d ":" -f 2)
		runtime_version=$(echo $runtime_version | tr _ -)
	fi
}
check_if_runtime_is_installed_rpm(){
	find_runtime=$(rpm -qa | grep dotnet-runtime-$runtime_version)
	if [ "$find_runtime" == "" ] ; then
		echo "Not able to remove runtime $runtime_version because it is not installed"
		exit 1
	fi
}
uninstall_runtime_yum(){
	yum remove -y $(rpm -qa | grep -e dotnet)
	runtime_installed_packages=$(rpm -qa | grep -e dotnet)
}
uninstall_runtime_zypper(){
	zypper -n rm $(rpm -qa | grep -e dotnet)
	runtime_installed_packages=$(rpm -qa | grep -e dotnet)
}
determine_success_install(){
	if [ -e $result_file ] ; then
		installed_runtime=$(dotnet --list-runtimes | grep $runtime_version)
		if [ "$installed_runtime" != "" ] ; then
        	        success_install=1
	        else
        	        success_install=0
		fi
	fi
}
test_result_install(){
        if [ -e $result_file ] ; then
                if [ $success_install -eq 1 ] ; then
                        echo "$distro:$version install  ->  passed" >> $result_file
                else
                        echo "$distro:$version install  ->  failed" >> $result_file
                fi
        fi
}
uninstall_latest_runtime_warning(){
	if [ "$runtime_version" == "latest" ] ; then 
		echo "Specify runtime version to unistall. Type dotnet --list-runtimes to see runtimes versions installed"
		exit 1
	fi 
}
test_result_uninstall(){
        if [ "$runtime_installed_packages" == "" ] ; then
                success_uninstall=1
        else
                success_uninstall=0
        fi
        if [ -e $result_file ] ; then
                if [ $success_uninstall -eq 1 ] ; then
                        echo "$distro:$version uninstall  ->  passed" >> $result_file
                else
                        echo "$distro:$version uninstall  ->  failed" >> $result_file
                fi
        fi
}

if [ "$distro" == "ubuntu" ] || [ "$distro" == "debian" ] ; then
	if [ "$2" == "install" ] ; then
		install_curl 
		
		download_runtime_packages_deb
		install_runtime_packages_deb
		dotnet --list-runtimes

		determine_runtime_version_deb
		determine_success_install
		test_result_install
	
	elif [ "$2" == "uninstall" ] ; then
		uninstall_latest_runtime_warning
	fi		

	if [ "$3" == "uninstall" ] || [ "$2" == "uninstall" ] ; then
		check_if_runtime_is_installed_deb
		uninstall_runtime_deb
		test_result_uninstall
	fi

elif [ "$distro" == "fedora" ] || [ "$distro" == "centos" ] || [ "$distro" == "oraclelinux" ] || [ "$distro" == "rhel" ] ; then
	if [ "$2" == "install" ] ; then
		install_wget_yum

		download_runtime_packages_rpm
		install_runtime_packages_yum

		dotnet --list-runtimes

		determine_runtime_version_rpm
		determine_success_install
		test_result_install

	elif [ "$2" == "uninstall" ] ; then
		uninstall_latest_runtime_warning		
	fi
	if [ "$3" == "uninstall" ] || [ "$2" == "uninstall" ] ; then
		check_if_runtime_is_installed_rpm
		uninstall_runtime_yum
		test_result_uninstall
	fi

elif [ "$distro" == "opensuse" ] || [ "$distro" == "sles" ] ; then
	if [ "$2" == "install" ] ; then
		install_wget_zypper

		download_runtime_packages_rpm
		install_runtime_packages_zypper
		dotnet --list-runtimes
		
		determine_runtime_version_rpm
		determine_success_install
		test_result_install
	
	elif [ "$2" == "uninstall" ] ; then
		uninstall_latest_runtime_warning		
	fi

	if [ "$3" == "uninstall" ] || [ "$2" == "uninstall" ] ; then
		check_if_runtime_is_installed_rpm
		uninstall_runtime_zypper
		test_result_uninstall
	fi	
fi

if [ -e $log_file ] ; then
	ch=$(printf "%-160s" "-")
	echo "${ch// /-} "
fi


