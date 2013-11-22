using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
