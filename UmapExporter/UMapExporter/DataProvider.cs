using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

using Newtonsoft.Json;
using CUE4Parse.UE4.Vfs;

namespace UMapExporter
{
    public class DataProvider : DefaultFileProvider
    {
        //NOTE: encryptionKeys is a manually provided list of encryption keys for archives within the game that have different keys
        public DataProvider(string folder, VersionContainer version, List<PakArchiveEncryptionKey> encryptionKeys) : base(folder, SearchOption.AllDirectories, true, version)
        {
            Initialize();

            //build a dictionary of encryption keys that will be submitted to the CUE4Parse API
            //each entry will have it's coresponding GUID (i.e. actual file identifier), and AES key
            Dictionary<FGuid, FAesKey> encryptionKeysToSubmit = new Dictionary<FGuid, FAesKey>();

            //iterate through each of our own encryption key
            foreach (PakArchiveEncryptionKey entry in encryptionKeys)
            {
                FAesKey entryAesKey = new FAesKey(entry.PakArchiveKey);

                //make sure that each provided entry has a given pak file name
                if (string.IsNullOrEmpty(entry.PakArchiveFileName) == false)
                {
                    //NOTE: UnloadedVfs is the unloaded pak archives found within the given game directory
                    //iterate through the found game pak archives and find the matching pak archive file by the given file name we provided manually
                    IAesVfsReader? foundPakArchiveGUID = UnloadedVfs.FirstOrDefault(it => it.Name == entry.PakArchiveFileName);

                    if (foundPakArchiveGUID != null) //we found a matching pak archive!
                        encryptionKeysToSubmit[foundPakArchiveGUID.EncryptionKeyGuid] = entryAesKey;
                    else //we did not find a matching pak archive...
                        ConsoleWriter.WriteErrorLine(string.Format("The manually given pak file name '{0}' couldn't be found!", entry.PakArchiveFileName));
                }
                //if the provided entry doesn't have a pak file name...
                else
                {
                    //use the given GUID and assign it the given AES key
                    encryptionKeysToSubmit[entry.Guid] = entryAesKey;
                }
            }

            //submit our constructed dictionary list of pak archive GUIDs/AES keys to the CUE4Parse API to unencrypt and mount
            int mountedResult = SubmitKeys(encryptionKeysToSubmit);

            ConsoleWriter.WriteSuccessLine(string.Format("Mounted {0} containers", mountedResult));
        }
    }
}