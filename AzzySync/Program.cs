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
            , [Description("Name of the blob storage container.")] string containerName
            , [Description("Connection string for the Azure blob storage account."), DefaultValue(@"UseDevelopmentStorage=true")] string storageConnectionString
        ) {
            Console.WriteLine("Sync!");

            var blobClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();
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
