# RDP Connect

This tool was created for the purposes of presenting users with their machines via Citrix. It relies on Ander's Client Health Database. More info on the script, and database can be found at http://andersrodland.com/configmgr-client-health-fix-broken-sccm-clients/

The username that launches the application is used to query the Clients table via stored procedure, and return the machines which they are the 'LastLoggedOnUser' for. It also supports regex replace, and wildcard lookup to identify additional usernames, and provide users a dropdown of their 'alternate' accounts, which also will have computer name lookup occur for. 

I will work to add more information, examples, and features as I have time. Feel free to make a pull request!

Right now, the config file would need edited either before compiling in Visual Studio, or after the install with a script. I'm creating an MSI installer using WIX Toolset. I hope to add MSI Properties to configure the XML config. 

It is necessary to add two Stored Procedures to your Client Health database for this tool to work. They are provided in the [SQL-StoredProcedures.sql](https://github.com/CodyMathis123/RDP-Connect/blob/master/SQL-StoredProcedures.sql) file in this repo. 

### Currently, it has some feature support as listed below.
* Restrict connection to machines in a specified group as noted in XML
* Restrict connection from users in a specified group as noted in XML
* Settings menu to allow "MultiMonitor" and "Admin/Console" connection
* Regex replace, and wildcard lookup for additional username
    - The scenario here is for a user naming convention such as below. A regex replace / wildcard lookup can provide a user with a list of their accounts from the DB, as well as a list of endpoints for each account
         1. Standard Account: User
         2. Eleveated Account: User-A

![Imgur](https://i.imgur.com/wYdpJw1.png)
