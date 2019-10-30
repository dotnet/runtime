#!/usr/bin/env bash

OS=`cat /etc/os-release | grep "^ID=" | sed 's/ID=//g' | sed 's/["]//g' | awk '{print $1}'`
echo -e "Operating System: ${OS}\n"

realm="TEST.COREFX.NET"
domain="TEST"

principal1="TESTHOST/testfqdn.test.corefx.net"
principal2="TESTHTTP/localhost"
krb_user="krb_user"
krb_password="password"
ntlm_user="ntlm_user"
ntlm_password="ntlm_password"

kadmin="kadmin.local"
krb5kdc="krb5kdc"
kdb5_util="kdb5_util"

krb_conf="krb5.conf"
krb_conf_location="/etc/krb5.conf"
keytabfile="/etc/krb5.keytab"

# NTLM credentials file
ntlm_user_file="/var/tmp/ntlm_user_file"

PROGNAME=$(basename $0)
usage()
{
    echo "This script must be run with super-user privileges."
    echo "Usage: ${PROGNAME} [-h|--help] [-y|--yes] [-u|--uninstall]";
}

# Cleanup config files and uninstall KDC
clean_up()
{
    echo "Stopping KDC.."
    if pgrep krb5kdc 2> /dev/null; then pkill krb5kdc 2> /dev/null ; fi

    case ${OS} in
        "ubuntu" | "debian")
            kdc_conf_location="/etc/krb5kdc/kdc.conf"
            dpkg -s krb5-kdc >/dev/null 2>&1
            if [ $? -eq 0 ]; then
                echo "Uninstalling krb5-kdc"
                apt-get -y purge krb5-kdc
            fi
            ;;

        "centos" | "rhel" | "fedora")
            kdc_conf_location="/var/kerberos/krb5kdc/kdc.conf"
            yum list installed krb5-server >/dev/null 2>&1
            if [ $? -eq 0 ]; then
                echo "Uninstalling krb5-server"
                yum -y remove krb5-server
            fi
            ;;

        "opensuse")
            kdc_conf_location="/var/lib/kerberos/krb5kdc/kdc.conf"
            zypper search --installed-only krb5-server >/dev/null 2>&1
            if [ $? -eq 0 ]; then
               echo "Uninstalling krb5-server"
               zypper --non-interactive remove krb5-server >/dev/null 2>&1
            fi
            ;;
        *)
            echo "This is an unsupported operating system"
            exit 1
            ;;
    esac

    echo "Removing config files"
    if [ -f ${krb_conf_location} ]; then
        rm -f ${krb_conf_location}
    fi

    if [ -f ${kdc_conf_location} ]; then
        rm -f ${kdc_conf_location}
    fi

    echo "Removing KDC database"
    rm -f ${database_files}

    if [ -f ${keytabfile} ]; then
        rm -f ${keytabfile}
    fi

    echo "Removing NTLM credentials file"
    if [ -f ${ntlm_user_file} ]; then
        rm -f ${ntlm_user_file}
    fi

    echo "Cleanup completed"
}

error_exit()
{
    echo "${1:-"Unknown Error"}"
    echo "Aborting"
    clean_up
    exit 1
}

# Common function across linux distros to configure KDC post installation
configure_kdc()
{
    echo "Stopping KDC.."
    if pgrep krb5kdc 2> /dev/null; then pkill krb5kdc ; fi

    # Remove database files if exist
    rm -f ${database_files}

    add_principal_cmd="add_principal -pw ${krb_password}"
    # Create/copy krb5.conf and kdc.conf
    echo "Copying krb5.conf and kdc.conf.."
    cp ${krb_conf} ${krb_conf_location} || \
    error_exit "Cannot copy ${krb_conf} to ${krb_conf_location}"

    cp ${kdc_conf} ${kdc_conf_location} || \
    error_exit "Cannot copy ${kdc_conf} to ${kdc_conf_location}"

    echo "Creating KDC database for realm ${realm}.."
    ${kdb5_util} create -r ${realm} -P ${krb_password} -s || \
    error_exit "Cannot create KDC database for realm ${realm}"

    echo "Adding principal ${principal1}.."
    ${kadmin} -q "${add_principal_cmd} ${principal1}@${realm}" || \
    error_exit "Cannot add ${principal1}"

    echo "Adding principal ${principal2}.."
    ${kadmin} -q "${add_principal_cmd} ${principal2}@${realm}" || \
    error_exit "Cannot add ${principal2}"

    echo "Adding user ${krb_user}.."
    ${kadmin} -q "${add_principal_cmd} ${krb_user}@${realm}" || \
    error_exit "Cannot add ${krb_user}"

    echo "Exporting keytab for ${principal1}"
    ${kadmin} -q "ktadd -norandkey ${principal1}@${realm}" || \
    error_exit "Cannot export kytab for ${principal1}"

    echo "Exporting keytab for ${principal2}"
    ${kadmin} -q "ktadd -norandkey ${principal2}@${realm}" || \
    error_exit "Cannot export kytab for ${principal2}"

    echo "Exporting keytab for ${krb_user}"
    ${kadmin} -q "ktadd -norandkey ${krb_user}@${realm}" || \
    error_exit "Cannot export kytab for ${krb_user}"
}

# check the invoker of this script
if [ $EUID -ne 0 ]; then
    usage
    exit 1
fi

# Parse command-line arguments
TEMP=`getopt -o p:hyu --long password:,help,yes,uninstall -n 'test.sh' -- "$@"`
[ $? -eq 0 ] || {
    usage
    exit 1
}
eval set -- "$TEMP"
uninstall=0
force=0
while true; do
    case $1 in
        -h|--help) usage; exit 0;;
        -y|--yes) force=1; shift ;;
        -u|--uninstall) uninstall=1; shift;;
        -p|--password) shift; krb_password=$1; shift;;
        --) shift; break;;
        *) usage; exit 1;;
    esac
done

# Uninstallation
if [ $uninstall -eq 1 ]; then
    if [ $force -eq 0 ]; then
        echo "This will uninstall KDC from your machine and cleanup the related config files."
        read -p "Do you want to continue? ([Y]es/[N]o)? " choice
        case $(echo $choice | tr '[A-Z]' '[a-z]') in
            y|yes) clean_up;;
            *) echo "Skipping uninstallation";;
        esac
    else
        clean_up
    fi
    exit 0
fi

# Installation
if [ $force -eq 0 ]; then
    read -p "This will install KDC on your machine and create KDC principals. Do you want to continue? ([Y]es/[N]o)? " choice
    case $(echo $choice | tr '[A-Z]' '[a-z]') in
        y|yes) ;;
        *) echo "Skipping installation"; exit 0;;
    esac
fi

case ${OS} in
    "ubuntu" | "debian")
        kdc_conf="kdc.conf.ubuntu"
        kdc_conf_location="/etc/krb5kdc/kdc.conf"
        database_files="/var/lib/krb5kdc/principal*"

        dpkg -s krb5-kdc >/dev/null 2>&1
        if [ $? -ne 0 ]; then
            echo "Installing krb5-kdc.."
            export DEBIAN_FRONTEND=noninteractive
            apt-get -y install krb5-kdc krb5-admin-server
            if [ $? -ne 0 ]; then
                echo "Error occurred during installation, aborting"
                exit 1
            fi
        else
            echo "krb5-kdc already installed.."
            exit 2
        fi

        configure_kdc

        echo "Starting KDC.."
        ${krb5kdc}
        ;;

    "centos" | "rhel" | "fedora" )
        kdc_conf="kdc.conf.centos"
        kdc_conf_location="/var/kerberos/krb5kdc/kdc.conf"
        database_files="/var/kerberos/krb5kdc/principal*"

        yum list installed krb5-server >/dev/null 2>&1
        if [ $? -ne 0 ]; then
            echo "Installing krb5-server.."
            yum -y install krb5-server krb5-libs krb5-workstation
            if [ $? -ne 0 ]; then
                echo "Error occurred during installation, aborting"
                exit 1
            fi
        else
            echo "krb5-server already installed.."
            exit 2
        fi

        configure_kdc

        echo "Starting KDC.."
        systemctl start krb5kdc.service
        systemctl enable krb5kdc.service
        ;;

    "opensuse")
          # the following is a workaround for opensuse
          # details at https://groups.google.com/forum/#!topic/comp.protocols.kerberos/3itzZQ4fETA
          # and http://lists.opensuse.org/opensuse-factory/2013-10/msg00099.html
          export KRB5CCNAME=$PWD

          krb5kdc="/usr/lib/mit/sbin/krb5kdc"
          kadmin="/usr/lib/mit/sbin/kadmin.local"
          kdb5_util="/usr/lib/mit/sbin/kdb5_util"
          kdc_conf="kdc.conf.opensuse"
          kdc_conf_location="/var/lib/kerberos/krb5kdc/kdc.conf"
          database_files="/var/lib/kerberos/krb5kdc/principal*"

          zypper search --installed-only krb5-mini >/dev/null 2>&1
          if [ $? -eq 0 ]; then
              echo "removing krb5-mini which conflicts with krb5-server and krb5-devel"
              zypper --non-interactive remove krb5-mini
          fi

          zypper search --installed-only krb5-server >/dev/null 2>&1
          if [ $? -ne 0 ]; then
             echo "Installing krb5-server.."
             zypper --non-interactive install krb5-server krb5-client krb5-devel
             if [ $? -ne 0 ]; then
                 echo "Error occurred during installation, aborting"
                 exit 1
             fi
         else
             echo "krb5-server already installed.."
             exit 2
         fi

         configure_kdc

         echo "Starting KDC..${krb5kdc}"
         ${krb5kdc}
         ;;
    *)
        echo "This is an unsupported operating system"
        exit 1
        ;;
esac

# Create NTLM credentials file
grep -ir gssntlmssp.so /etc/gss/mech.d > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "$domain:$ntlm_user:$ntlm_password" > $ntlm_user_file
    echo "$realm:$krb_user:$krb_password" >> $ntlm_user_file
    chmod +r $ntlm_user_file
fi
chmod +r ${keytabfile}
