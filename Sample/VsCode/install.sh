#!/bin/sh
npm i vscode-jsonrpc@6.0.0-next.5
npm i vscode-languageclient@7.0.0-next.9
npm i vscode-languageserver@7.0.0-next.7
npm i vscode-languageserver-protocol@3.16.0-next.7
npm i vscode-languageserver-types@3.16.0-next.3
cp -r ../Server/bin/Debug/net472 ./server/net472
npm install
npm run compile
#vsce package
