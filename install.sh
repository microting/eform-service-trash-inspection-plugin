#!/bin/bash

if [ ! -d "/var/www/microting/eform-service-trashinspection-plugin" ]; then
  cd /var/www/microting
  su ubuntu -c \
  "git clone https://github.com/microting/eform-service-trashinspection-plugin.git -b stable"
fi

cd /var/www/microting/eform-service-trashinspection-plugin
su ubuntu -c \
"dotnet restore ServiceTrashInspectionPlugin.sln"

echo "################## START GITVERSION ##################"
export GITVERSION=`git describe --abbrev=0 --tags | cut -d "v" -f 2`
echo $GITVERSION
echo "################## END GITVERSION ##################"
su ubuntu -c \
"dotnet publish ServiceTrashInspectionPlugin.sln -o out /p:Version=$GITVERSION --runtime linux-x64 --configuration Release"

su ubuntu -c \
"mkdir -p /var/www/microting/eform-debian-service/MicrotingService/MicrotingService/out/Plugins/"

su ubuntu -c \
"cp -av /var/www/microting/eform-service-trashinspection-plugin/ServiceTrashInspectionPlugin/out /var/www/microting/eform-debian-service/MicrotingService/MicrotingService/out/Plugins/ServiceTrashInspectionPlugin"
./rabbitmqadmin declare queue name=eform-service-trashinspection-plugin durable=true
