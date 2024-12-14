using CUE4Parse.UE4.Objects.Core.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UMapExporter
{
    public class PakArchiveEncryptionKey
    {
        public FGuid Guid;

        public string PakArchiveFileName;
        public string PakArchiveKey;

        public PakArchiveEncryptionKey()
        {
            Guid = new FGuid();
            PakArchiveKey = String.Empty;
        }

        public PakArchiveEncryptionKey(FGuid guid, string key)
        {
            Guid = guid;
            PakArchiveKey = key;
        }
    }
}
