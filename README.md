[![Build Status](https://travis-ci.org/carlosjdelgado/MySql.LightServer.svg?branch=master)](https://travis-ci.org/carlosjdelgado/MySql.LightServer)
[![Build status](https://ci.appveyor.com/api/projects/status/9vbaofala03rch1m/branch/master?svg=true)](https://ci.appveyor.com/project/carlosjdelgado/mysql-lightserver/branch/master)
# MySql.LightServer 
A Light Server for C# tests, running on net core.

## Use
You cannot use this library until I publish the first version on nuget, I want to do some refactor before, so, be patient.

## Platforms supported
MySql.LightServe runs with net core, net framework is not yet supported.
You can run MySql on a Linux and a Windows machine, OSX is not supported, maybe in the future...

## How it works
Mysql.LightServer is simply running a minimal instance of MySql for tests. Server is created at run time and cleared at finish.

Mysql.LightServer makes it possible to create and run unit tests on a real MySql server without spending time on server setup.

## Examples
### Create server, table and data.

```c#
        //Get an instance
        MySqlLightServer dbServer = MySqlServer.Instance;
        
        //Start the server
        dbServer.StartServer();
        
        //Create a database and use it
        MySqlHelper.ExecuteNonQuery(dbServer.GetConnectionString(), "CREATE DATABASE testserver; USE testserver;");
        
        //Insert data
        MySqlHelper.ExecuteNonQuery(dbServer.GetConnectionString(), "INSERT INTO testTable (`id`, `value`) VALUES (2, 'test value')"); 
        
        //Shut down server
        dbServer.ShutDown();
```
## API
* **MySqlServer.Instance**: Retrieves an Instance of the server API.

* **MySqlServer.StartServer()**: Starts the server.

* **MySqlServer.StartServer(int serverPort)**: Starts the server at a specified port. Nice to have if you have a real MySql server running on the test machine.

* **MySqlServer.ShutDown()**: Shuts down the server.

* **MySqlServer.GetConnectionString()**: Returns a connection string to be used when connecting to the server.

* **MySqlServer.ServerPort**: Returns the server port of the instance.
