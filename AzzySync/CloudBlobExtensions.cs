using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzzySync {
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
