#!/usr/bin/env bash

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

if [[ "$ID" == "ol" ]]; then
        distro="oraclelinux"
fi
if [[ "$distro" == "oraclelinux" || "$distro" == "rhel" || "$distro" == "opensuse" ]]; then
	version=$(echo $version | cut -d . -f 1)
fi

echo $distro:$version

sdk_version=$1

BLOB_RUNTIME_DIR="https://dotnetcli.blob.core.windows.net/dotnet/Runtime"
BLOB_SDK_DIR="https://dotnetcli.blob.core.windows.net/dotnet/Sdk"
BLOB_ASPNET_DIR="https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime"

install_curl(){
	apt-get -y install curl
	if [ $? -ne 0 ]; then
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
download_sdk_package_deb(){
	if [[ "$sdk_version" == "latest" ]]; then
                download_from_blob_deb "$BLOB_SDK_DIR/master/dotnet-sdk-latest-$arch.deb"
        else
                download_from_blob_deb "$BLOB_SDK_DIR/$sdk_version/dotnet-sdk-$sdk_version-$arch.deb"
	fi
}
download_aspnet_package_deb(){
        download_from_blob_deb "$BLOB_ASPNET_DIR/$aspnet_version/aspnetcore-runtime-$aspnet_version-$arch.deb"
}
determine_aspnet_version_install_deb(){
	aspnet_version=$(dpkg -I dotnet-sdk-$sdk_version-$arch.deb | grep -o 'aspnetcore-runtime-[^ ]*')
        aspnet_version=${aspnet_version#aspnetcore-runtime-}
        [[ "${aspnet_version: -1}" == "," ]] && aspnet_version=${aspnet_version%,}
}
determine_runtime_sdk_install_deb(){
	runtime_sdk=$(dpkg -I dotnet-sdk-$sdk_version-$arch.deb | grep -o 'dotnet-runtime-[^ ]*')
        runtime_sdk=${runtime_sdk#dotnet-runtime-}
        [[ "${runtime_sdk: -1}" == "," ]] && runtime_sdk=${runtime_sdk%,}
}
determine_runtime_aspnet_install_deb(){
	runtime_aspnet=$(dpkg -I aspnetcore-runtime-$aspnet_version-$arch.deb | grep -o 'dotnet-runtime[^ ]*')
        runtime_aspnet=${runtime_aspnet#dotnet-runtime-}
        [[ "${runtime_aspnet: -1}" == "," ]] && runtime_sdk=${runtime_aspnet%,}
}
download_runtime_packages_deb(){
        download_from_blob_deb "$BLOB_RUNTIME_DIR/$runtime_version/dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.deb"
        download_from_blob_deb "$BLOB_RUNTIME_DIR/$runtime_version/dotnet-host-$runtime_version-$arch.deb"
        download_from_blob_deb "$BLOB_RUNTIME_DIR/$runtime_version/dotnet-hostfxr-$runtime_version-$arch.deb"
        download_from_blob_deb "$BLOB_RUNTIME_DIR/$runtime_version/dotnet-runtime-$runtime_version-$arch.deb"
}
install_runtime_packages_deb(){
	dpkg -i dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.deb
        apt-get install -f -y
        dpkg -i *.deb
}
install_aspnet_and_sdk_deb(){
       	dpkg -i aspnetcore-runtime-$aspnet_version-$arch.deb
        dpkg -i dotnet-sdk-$sdk_version-$arch.deb
}
check_if_sdk_is_installed_deb(){
	find_sdk=$(apt list --installed | grep dotnet-sdk-$sdk_version)
	if [[ -z "$find_sdk" ]]; then
		echo "Not able to remove sdk $sdk_version because it is not installed"
		exit 1
	fi
}
determine_runtime_sdk_uninstall_deb(){
	runtime_sdk=$(apt-cache depends dotnet-sdk-$sdk_version | grep -o 'dotnet-runtime-[^ ]*')
       	runtime_sdk=${runtime_sdk#dotnet-runtime-}
}
determine_aspnet_package_name_uninstall_deb(){
	aspnet_package_name=$(apt-cache depends dotnet-sdk-$sdk_version | grep -o 'aspnetcore-runtime-[^ ]*')
}
determine_runtime_aspnet_uninstall_deb(){
	runtime_aspnet=$(apt-cache depends $aspnet_package_name | grep -o 'dotnet-runtime-[^ ]*')
   	runtime_aspnet=${runtime_aspnet#dotnet-runtime-}
}
uninstall_dotnet_deb(){
	apt-get remove -y $(apt list --installed | grep -e dotnet -e aspnet | cut -d "/" -f 1)
	dotnet_installed_packages=$(apt list --installed | grep -e dotnet -e aspnet)
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
download_sdk_package_rpm(){
	if [[ "$sdk_version" == "latest" ]]; then
                download_from_blob_rpm "$BLOB_SDK_DIR/master/dotnet-sdk-latest-$arch.rpm"
        else
                download_from_blob_rpm "$BLOB_SDK_DIR/$sdk_version/dotnet-sdk-$sdk_version-$arch.rpm"
        fi
}
download_aspnet_package_rpm(){
        download_from_blob_rpm "$BLOB_ASPNET_DIR/$aspnet_version/aspnetcore-runtime-$aspnet_version-$arch.rpm"
}
determine_aspnet_version_install_rpm(){
	aspnet_version=$(rpm -qpR dotnet-sdk-$sdk_version-$arch.rpm | grep -o 'aspnetcore-runtime-[^ ]*')
        aspnet_version=${aspnet_version#aspnetcore-runtime-}
}
determine_runtime_aspnet_install_rpm(){
	runtime_aspnet=$(rpm -qpR aspnetcore-runtime-$aspnet_version-$arch.rpm | grep -o 'dotnet-runtime[^ ]*')
        runtime_aspnet=${runtime_aspnet#dotnet-runtime-}
}
determine_runtime_sdk_install_rpm(){
 	runtime_sdk=$(rpm -qpR dotnet-sdk-$sdk_version-$arch.rpm | grep -o 'dotnet-runtime-[^ ]*')
        runtime_sdk=${runtime_sdk#dotnet-runtime-}

}
download_runtime_packages_rpm(){
	download_from_blob_rpm "$BLOB_RUNTIME_DIR/$runtime_version/dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm"
        download_from_blob_rpm "$BLOB_RUNTIME_DIR/$runtime_version/dotnet-host-$runtime_version-$arch.rpm"
        download_from_blob_rpm "$BLOB_RUNTIME_DIR/$runtime_version/dotnet-hostfxr-$runtime_version-$arch.rpm"
        download_from_blob_rpm "$BLOB_RUNTIME_DIR/$runtime_version/dotnet-runtime-$runtime_version-$arch.rpm"
}
install_runtime_deps_package_yum(){
	yum localinstall -y dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm
	rm dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm
}
install_rpm_from_folder(){
        rpm -Uvh *.rpm
}
install_runtime_deps_package_zypper(){
	zypper --no-gpg-checks --non-interactive in ./dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm
	rm dotnet-runtime-deps-$runtime_version-$distro.$version-$arch.rpm
}
install_aspnet_and_sdk_rpm(){
	rpm -i aspnetcore-runtime-$aspnet_version-$arch.rpm
        rpm -i dotnet-sdk-$sdk_version-$arch.rpm
}
check_if_sdk_is_installed_rpm(){
	find_sdk=$(rpm -qa | grep dotnet-sdk-$sdk_version)
	if [[ -z "$find_sdk" ]]; then
		echo "Not able to remove sdk $sdk_version because it is not installed"
		exit 1
	fi
}
determine_runtime_sdk_uninstall_rpm(){
	runtime_sdk=$(rpm -q --requires dotnet-sdk-$sdk_version | grep -o 'dotnet-runtime-[^ ]*')
        runtime_sdk=${runtime_sdk#dotnet-runtime-}
}
determine_aspnet_package_name_uninstall_rpm(){
        aspnet_package_name=$(rpm -q --requires dotnet-sdk-$sdk_version | grep -o 'aspnetcore-runtime-[^ ]*')
}
determine_runtime_aspnet_uninstall_rpm(){
	runtime_aspnet=$(rpm -q --requires $aspnet_package_name | grep -o 'dotnet-runtime-[^ ]*')
        runtime_aspnet=${runtime_aspnet#dotnet-runtime-}
}
uninstall_dotnet_yum(){
	yum remove -y $(rpm -qa | grep -e dotnet -e aspnet)
	dotnet_installed_packages=$(rpm -qa | grep -e dotnet -e aspnet)
}
uninstall_dotnet_zypper(){
	zypper -n rm $(rpm -qa | grep -e dotnet -e aspnet)
	dotnet_installed_packages=$(rpm -qa | grep -e dotnet -e aspnet)
}
checkout_new_folder(){
	mkdir temp_folder
	cd temp_folder
}
checkout_previous_folder(){
	cd ..
}
run_app(){
	if [ -e $result_file ]; then
		dotnet new console -o dockerApp
		cd dockerApp
		dotnet restore -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json
		project_output=$(dotnet run)
		if [[ "$project_output" == 'Hello World!' ]];
		then
			success_install=1;
		else
			success_install=0;
		fi
	fi
}
test_result_install(){
	if [ -e $result_file ]; then
		if [ $success_install -eq 1 ]; then
			echo "$distro:$version install  ->  passed" >> $result_file
		else
			echo "$distro:$version install  ->  failed" >> $result_file
		fi
	fi
}
test_result_uninstall(){

	if [[ -z "$dotnet_installed_packages" ]]; then
		success_uninstall=1;
	else
		success_uninstall=0;
	fi

	if [ -e $result_file ]; then
		if [ $success_uninstall -eq 1 ]; then
               		echo "$distro:$version uninstall  ->  passed" >> $result_file
		else
	                echo "$distro:$version uninstall  ->  failed" >> $result_file
		fi
	fi
}
uninstall_latest_sdk_warning(){
	if [[ "$sdk_version" == "latest" ]]; then
		echo "Specify sdk version to unistall. Type dotnet --list-sdks to see sdks versions installed"
		exit 1
	fi
}

if [[ "$distro" == "ubuntu" || "$distro" == "debian" ]]; then
	if [[ "$2" == "install" ]]; then
		install_curl

		download_sdk_package_deb

		determine_aspnet_version_install_deb
		download_aspnet_package_deb

		determine_runtime_aspnet_install_deb
		determine_runtime_sdk_install_deb

		runtime_version="$runtime_aspnet"
		download_runtime_packages_deb
		install_runtime_packages_deb

		if [ "$runtime_aspnet" != "$runtime_sdk" ]; then
			runtime_version="$runtime_sdk"
			checkout_new_folder
			download_runtime_packages_deb
			install_runtime_packages_deb
			checkout_previous_folder
		fi

		install_aspnet_and_sdk_deb

		dotnet --list-runtimes
		dotnet --list-sdks

		run_app
		test_result_install

	elif [[ "$2" == "uninstall" ]]; then
		uninstall_latest_sdk_warning
		check_if_sdk_is_installed_deb

		determine_runtime_sdk_uninstall_deb
		determine_aspnet_package_name_uninstall_deb
		determine_runtime_aspnet_uninstall_deb

	fi

	if [[ "$3" == "uninstall" && "$success_install" == 1 || "$2" == "uninstall" ]]; then
		uninstall_dotnet_deb
		test_result_uninstall
	fi

elif [[ "$distro" == "fedora" || "$distro" == "centos" || "$distro" == "oraclelinux" || "$distro" == "rhel" ]]; then
	if [[ "$2" == "install" ]]; then
		install_wget_yum

		download_sdk_package_rpm

		determine_aspnet_version_install_rpm
		download_aspnet_package_rpm

		determine_runtime_aspnet_install_rpm
		determine_runtime_sdk_install_rpm

		checkout_new_folder
		runtime_version="$runtime_aspnet"
		download_runtime_packages_rpm
		install_runtime_deps_package_yum

		if [ "$runtime_aspnet" != "$runtime_sdk" ]; then
			runtime_version="$runtime_sdk"
			download_runtime_packages_rpm
			install_runtime_deps_package_yum
		fi
		install_rpm_from_folder
		checkout_previous_folder

		install_aspnet_and_sdk_rpm

		dotnet --list-runtimes
		dotnet --list-sdks

		run_app
		test_result_install

	elif [[ "$2" == "uninstall" ]]; then
		uninstall_latest_sdk_warning
		check_if_sdk_is_installed_rpm

		determine_runtime_sdk_uninstall_rpm
		determine_aspnet_package_name_uninstall_rpm
		determine_runtime_aspnet_uninstall_rpm

                echo $runtime_sdk
                echo $runtime_aspnet

	fi
	if [[ "$3" == "uninstall" && "$success_install" == 1 || "$2" == "uninstall" ]]; then
		uninstall_dotnet_yum
		test_result_uninstall
	fi


elif [[ "$distro" == "opensuse" || "$distro" == "sles" ]]; then
	if [[ "$2" == "install" ]]; then
		install_wget_zypper

		download_sdk_package_rpm

		determine_aspnet_version_install_rpm
		download_aspnet_package_rpm

		determine_runtime_aspnet_install_rpm
		determine_runtime_sdk_install_rpm

		checkout_new_folder
		runtime_version="$runtime_aspnet"
		download_runtime_packages_rpm
		install_runtime_deps_package_zypper

		if [ "$runtime_aspnet" != "$runtime_sdk" ]; then
			runtime_version="$runtime_sdk"
			download_runtime_packages_rpm
			install_runtime_deps_package_zypper
		fi

		install_rpm_from_folder
		checkout_previous_folder

		install_aspnet_and_sdk_rpm

		dotnet --list-runtimes
		dotnet --list-sdks

		run_app
		test_result_install

	elif [[ "$2" == "uninstall" ]]; then
		uninstall_latest_sdk_warning
		check_if_sdk_is_installed_rpm

		determine_runtime_sdk_uninstall_rpm
		determine_aspnet_package_name_uninstall_rpm
		determine_runtime_aspnet_uninstall_rpm

		echo $runtime_sdk
		echo $runtime_aspnet

	fi

	if [[ "$3" == "uninstall" && "$success_install" == 1 || "$2" == "uninstall" ]]; then
		uninstall_dotnet_zypper
		test_result_uninstall
	fi
fi

if [ -e $log_file ]; then
	ch=$(printf "%-160s" "-")
	echo "${ch// /-} "
fi

