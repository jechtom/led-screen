dotnet publish -r linux-arm --no-self-contained
scp -r .\bin\Debug\net6.0\linux-arm\publish\* pi@hehohub.lan:/home/pi/led-screen/