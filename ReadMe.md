To make the project work, you need to run four components: the front-end server, the routing server, the proxy/cache server, and ActiveMQ. Open four separate terminals and execute the following commands in each.

## Instructions

# Front-end
Go to waize-interface and start a html server with:
```bash
http-server
```
or
```bash
python -m http.server 8080
```

# Routing server
From waize-api create the files to launch the server in console mode by running:
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
replace win-x64 by your operating system equivalent

Then from the project's root launch the server:
```bash
./waize-api/WaizeRoutingServer/bin/Release/net8.0/win-x64/WaizeRoutingServer
```

# Proxy + cache server
From waize-proxy create the files to launch the server in console mode by running:
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
replace win-x64 by your operating system equivalent

Then from the project's root launch the server:
```bash
./waize-proxy/waize-proxy/bin/Release/net8.0/win-x64/waize-proxy
```

# ActiveMQ (5.18)
From the bin folder of your apache installation run:
```bash
activemq start
```
