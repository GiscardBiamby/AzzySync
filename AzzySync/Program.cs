using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using CLAP;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

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
            [Description("Path of local folder to sync to blob storage.")] string localPath
            , [Description("Connection string for the Azure blob storage account.")] string cloudConnectionString
            , [Description("Name of the blob storage container.")] string containerName
        ) {
            Console.WriteLine("Sync!");
            var connectionString = @"UseDevelopmentStorage=true";
            
            var blobClient = CloudStorageAccount.Parse(connectionString).CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            var container = blobClient.GetContainerReference(containerName);

            // Create the container if it does not already exist.
            container.CreateIfNotExists();

            // Output container URI to debug window.
            Console.WriteLine(container.Uri);
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
