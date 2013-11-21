using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Web;
using System.Security.Cryptography; 

namespace AzzySync {
    /// <summary>
    /// Maps file names to a MIME type. This is used to set the Content-Type header when uploading to blob storage. 
    /// </summary>
    public interface IMIMETypeMapper {
        /// <summary>
        /// Gets MIME type for the specified file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        string GetMIMETypeFromFileName(string fileName);
    }

    /// <summary>
    /// Implementation of IMIMETypeMapper that uses System.Web.MimeMapping. 
    /// Dunno if this would work on Mono, but it's OK for the moment. 
    /// </summary>
    public class DefaultMIMETypeMapper : IMIMETypeMapper {
        public string GetMIMETypeFromFileName(string fileName) {
            return System.Web.MimeMapping.GetMimeMapping(fileName);
        }
    }

    public class SyncStats {
        public uint New { get; set; }
        public uint Changed { get; set; }
        public uint Deleted { get; set; }
    }

    public class SyncedFile {
        public string LocalPath { get; set; }
        public CloudBlockBlob Blob { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SyncToBlobStorage {
        protected readonly CloudBlobClient _blobClient;
        protected readonly IMIMETypeMapper _mimeMapper;

        protected ConcurrentBag<SyncedFile> _new;
        protected ConcurrentBag<SyncedFile> _changed;
        protected ConcurrentBag<SyncedFile> _deleted;

        public SyncToBlobStorage(CloudBlobClient blobClient, IMIMETypeMapper mimeMapper) {
            _new = new ConcurrentBag<SyncedFile>();
            _changed = new ConcurrentBag<SyncedFile>();
            _deleted = new ConcurrentBag<SyncedFile>();
            _blobClient = blobClient;
            _mimeMapper = mimeMapper;
        }
        
        public void Sync(CloudBlobContainer container, string localPath) {
            Console.WriteLine("Syncing localPath: '{0}', to container: '{1}'.", localPath, container.Uri);
            container.CreateIfNotExists();
            container.SetPermissionsAsync(new BlobContainerPermissions {
                PublicAccess = BlobContainerPublicAccessType.Blob
            });

            SyncLocalToBlob(localPath, container);
            Console.WriteLine(
                @"Total new: {0}, updated: {1}, deleted {2}."
                , _new.Count
                , _changed.Count
                , _deleted.Count
            ); 
        }

        private void SyncLocalToBlob(string localPath, CloudBlobContainer container) {
            var blobs = container
                .ListBlobs(useFlatBlobListing: true, blobListingDetails: BlobListingDetails.Metadata)
                .OfType<CloudBlockBlob>()
                .ToList();
            var blobLookup = blobs.ToLookup(container, localPath);
            var localFiles = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories);

            var basePath = localPath;
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                basePath = basePath + Path.DirectorySeparatorChar;
            }
            
            Console.WriteLine("Total local files: {0}, Total remote blobs: {1}.", localFiles.Count(), blobs.Count());
            UploadNewOrChangedFiles(container, blobLookup, localFiles, basePath);
            DeleteBlobsThatAreNotInLocalDir(container, blobs, localFiles, basePath);

            // Get updated list of blobs so we can compare new local and remote counts: 
            blobs = container
                .ListBlobs(useFlatBlobListing: true, blobListingDetails: BlobListingDetails.Metadata)
                .OfType<CloudBlockBlob>()
                .ToList();
            Console.WriteLine("Total local files: {0}, Total remote blobs: {1}.", localFiles.Count(), blobs.Count());
        }

        /// <summary>
        /// Uploads files that are on localPath that aren't in blob storage, and also files that exist 
        /// both locally and in blob storage but where the md5 hashes don't match. 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobLookup"></param>
        /// <param name="localFiles"></param>
        /// <param name="basePath"></param>
        private void UploadNewOrChangedFiles(
            CloudBlobContainer container
            , Dictionary<string, CloudBlockBlob> blobLookup
            , string[] localFiles
            , string basePath
        ) {
            foreach (var localFile in localFiles) {
                var blobName = GetBlobName(basePath, localFile);
                var blob = blobLookup.ContainsKey(localFile) ? blobLookup[localFile] : null;
                var fileHash = GetFileHash(localFile); 
                
                if (blob == null) {
                    Console.WriteLine("New file. Uploading: {0}", localFile);
                    UploadToBlob(container, localFile, blobName, fileHash);
                    _new.Add(new SyncedFile { LocalPath = localFile, Blob = blob });
                }
                else {
                    if (fileHash != blob.Metadata["Hash"]) {
                        Console.WriteLine("Hashes differ, re-uploading: {0}", localFile);
                        UploadToBlob(container, localFile, blobName, fileHash);
                        _changed.Add(new SyncedFile { LocalPath = localFile, Blob = blob });
                    }
                    else {
                        Console.WriteLine("No change, skipping: {0}", localFile);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes files from blob storage if the same file doesn't also exist in the local dir
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobLookup"></param>
        /// <param name="localFiles"></param>
        /// <param name="basePath"></param>
        private void DeleteBlobsThatAreNotInLocalDir(
            CloudBlobContainer container
            , List<CloudBlockBlob> blobs
            , string[] localFiles
            , string basePath
        ) {
            var localFilesLookup = localFiles.ToLookup(f => f, StringComparer.OrdinalIgnoreCase);
            foreach (var blob in blobs.Where(b => !localFilesLookup.Contains(b.GetLocalPath(basePath)))) {
                Console.WriteLine("Blob doesn't exist in local dir, deleting blob: {0}.", blob.Uri);
                blob.Delete();
                _deleted.Add(new SyncedFile { LocalPath = blob.GetLocalPath(basePath), Blob = blob });
            }
        }

        /// <summary>
        /// Returns base64 encoded MD5 hash for the specified file. 
        /// </summary>
        /// <param name="localFile"></param>
        /// <returns></returns>
        private string GetFileHash(string localFile) {
            using (FileStream file = new FileStream(localFile, FileMode.Open)) {
                MD5 md5 = new MD5CryptoServiceProvider();
                string fileHash = Convert.ToBase64String(md5.ComputeHash(file));
                return fileHash;
            }
        }

        /// <summary>
        /// Uploads a file to blob storage. Right now this is pretty basic, and doesn't do anything special like 
        /// async, parallel, and haven't gone out of the way to support large files. Def. could improve 
        /// upon this in the future. 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="localFile"></param>
        /// <param name="blobName"></param>
        /// <param name="fileHash"></param>
        private void UploadToBlob(CloudBlobContainer container, string localFile, string blobName, string fileHash) {
            Console.WriteLine("Uploading Local file '{0}' to: '{1}'", localFile, blobName);
            var blob = container.GetBlockBlobReference(blobName);
            blob.Properties.ContentType = _mimeMapper.GetMIMETypeFromFileName(localFile);
            blob.Metadata["Hash"] = fileHash;

            using (var fileStream = File.OpenRead(localFile)) {
                blob.UploadFromStream(fileStream);
            }

            Console.WriteLine("Blob: {0}", blob.Uri);
        }

        /// <summary>
        /// Returns the name of the corresponding blob for a file in the local directory being synced.
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="localFile"></param>
        /// <returns></returns>
        private static string GetBlobName(string basePath, string localFile) {
            return localFile.Replace(basePath, "").Replace(@"\", @"/");
        }
    }

    public static class CloudBlobExtensions {
        /// <summary>
        /// Returns a Dictionary that is keyed off of the localPath of the specified blobs.
        /// </summary>
        /// <param name="blobs"></param>
        /// <param name="container"></param>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public static Dictionary<string, CloudBlockBlob> ToLookup(
            this List<CloudBlockBlob> blobs
            , CloudBlobContainer container
            , string localPath
        ) {
            var baseUri = container.Uri.ToString();
            return blobs.ToDictionary(b => b.GetLocalPath(localPath), b => b, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the path of the blob in the local directory (the "localPath" param) being synced.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public static string GetLocalPath(this CloudBlockBlob blob, string localPath) {
            var containerPath = blob.Container.Uri.LocalPath.ToString();
            var blobLocalPath = blob.Uri.LocalPath;
            string path = blobLocalPath.Remove(0, containerPath.Length).Replace('/', Path.DirectorySeparatorChar);

            if (path.StartsWith(Path.DirectorySeparatorChar.ToString())) {
                path = path.Substring(1);
            }

            path = Path.GetFullPath(Path.Combine(localPath, path));
            return path;
        }
    }
}
