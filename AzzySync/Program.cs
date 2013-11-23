using System;
using System.IO;
using CLAP;
using Microsoft.WindowsAzure.Storage;

namespace AzzySync {
    class ConsoleAzzySync {
        public static void Main(string[] args) {
            Parser.RunConsole<AzzySync>(args);
        }
    }

    public class AzzySync {
        protected static bool _debugMode { get; set; }

        static AzzySync() {}

        [Verb(Description=@"Performs a one way sync of files from a local folder to an Azure blob storage container.")]
        public static void Sync(
            [Description("Path of local folder to sync to blob storage.")] 
            string localPath
            
            , 
            [Description("Name of the blob storage container.")] 
            string containerName
            
            , 
            [Description("Connection string for the Azure blob storage account."), DefaultValue(@"UseDevelopmentStorage=true")] 
            string storageConnectionString
            
            , 
            [Description("Forces re-upload all files from local dir to blob storage. This bypasses the hash calculation/check to see if a file has changed.")]
            [DefaultValue(false)] 
            bool forceReupload 
        ) {
            try {
                Console.WriteLine("Syncing localPath: '{0}', to container: '{1}'.", localPath, containerName);

                localPath = ResolveLocalDir(localPath);

                var blobClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(containerName);
                var syncer = new SyncToBlobStorage(blobClient, new DefaultMIMETypeMapper(), new SyncOptions{ ForceReupload = forceReupload });
                syncer.Sync(container, localPath);
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }


        private static string ResolveLocalDir(string localPath) {
            if (string.IsNullOrWhiteSpace(localPath)) {
                throw new ArgumentNullException("localPath");
            }

            if (!Path.IsPathRooted(localPath)) {
                localPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, localPath));
            }

            return localPath;
        }

        /// <summary>
        ///  DebugMode() 
        /// </summary>
        [Global]
        public static void DebugMode() {
            Console.WriteLine("DebugMode enabled");
            _debugMode = true;
        }


        /// <summary>
        /// Help() - displays program help info on the command line.
        /// </summary>
        /// <param name="help"></param>
        [Empty, Help(Aliases = "?,help,h")]
        public static void Help(string help) {
            Console.WriteLine(help);
        }
    }
}
