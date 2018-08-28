#!/bin/sh
mkdir Plugins
cp ./SDK-Net35/bin/Release/* ./Plugins
zip -r SDK.zip Plugins
rm -r Plugins