For the project to be working you need to run 4 things, the front end server, the routing server, the proxy + cache server and the activemq. You need to run in 4 different terminal the following instructions

## Instructions

# Front-end
Go to waize-interface and start a html server with:
http-server
or
pyhton -m http.server 8080

# Routing server
From waize-api create the files to launch the server in console mode by running:
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
replace win-x64 by your operating system equivalent

Then from the project's root launch the server:
./waize-api/WaizeRoutingServer/bin/Release/net8.0/win-x64/WaizeRoutingServer

# Proxy + cache server
From waize-proxy create the files to launch the server in console mode by running:
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
replace win-x64 by your operating system equivalent

Then from the project's root launch the server:
./waize-proxy/waize-proxy/bin/Release/net8.0/win-x64/waize-proxy

# ActiveMQ (5.18)
From the bin folder of your apache installation run:
activemq start