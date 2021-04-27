#!/usr/bin/env bash

echo Installing basic Ubuntu \(VM\) XPlat environment

function Install-Packages {
	echo Installing Packages
	apt-get install clang -y
	apt-get install cmake -y
}

function Enable-Integration-Services {
	echo Checking for integration services
	res=$(grep -c "hv_vmbus" /etc/initramfs-tools/modules)
	if [ $res -eq 0 ]
	then
		echo Installing integration services
		echo hv_vmbus >> /etc/initramfs-tools/modules
		echo hv_storvsc >> /etc/initramfs-tools/modules
		echo hv_blkvsc >> /etc/initramfs-tools/modules
		echo hv_netvsc >> /etc/initramfs-tools/modules
	else
		echo Integration Services already installed
	fi
}

Install-Packages
Enable-Integration-Services
