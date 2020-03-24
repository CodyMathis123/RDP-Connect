# RDP Connect

This tool was created for the purposes of presenting users with their machines via Citrix. It relies on Ander's Client Health Database. More info on the script, and database can be found at http://andersrodland.com/configmgr-client-health-fix-broken-sccm-clients/

The username that launches the application is used to query the Clients table via stored procedure, and return the machines which they are the 'LastLoggedOnUser' for. It also supports regex replace, and wildcard lookup to identify additional usernames, and provide users a dropdown of their 'alternate' accounts, which also will have computer name lookup occur for. 

I will work to add more information, examples, and features as I have time. Feel free to make a pull request!

Right now, the config file would need edited either before compiling in Visual Studio, or after the install with a script. I'm creating an MSI installer using WIX Toolset. I hope to add MSI Properties to configure the XML config. 

![Imgur](https://i.imgur.com/wYdpJw1.png)
