AzzySync
========

Command line tool to sync local folders with Azure blob storage. Right now this just does a one-way sync from a local folder into an Azure blob storage container.


Examples
========

These examples work in a PowerShell console, but you can run these with very little modification in plain windows console.

 1. Sync local dir, c:\static-content\images\ to Azure Development Storage Emulator (AzzySync defaults to storage emulator if no connection string is provided): 
    
    ```PowerShell
    .\AzzySync.exe s /containerName:images /localPath:"C:\static-content\images\"
    ```

 2. Sync c:\static-content\ to Azure blob storage: 
    
    ```PowerShell
    .\AzzySync.exe s /containerName:images /localPath:"C:\static-content\images\" /
    ```

Get command line help
========
```PowerShell
.\AzzySync.exe /?
```

Output: 
```
   sync|s: Performs a one way sync of files from a local folder to an Azure blob storage container.
        /c /containername           : Name of the blob storage container. (String)
        /l /localpath               : Path of local folder to sync to blob storage. (String)
        /s /storageconnectionstring : Connection string for the Azure blob storage account. (String) (Default = UseDevelopmentStorage=true)

   Global Parameters:
        /debug     :
        /debugmode :
        /help|h|?  : Help
```