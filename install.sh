#!/bin/bash

if [ ! -d "/var/www/microting/eform-service-trashinspection-plugin" ]; then
  cd /var/www/microting
  su ubuntu -c \
  "git clone https://github.com/microting/eform-service-trashinspection-plugin.git"
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
