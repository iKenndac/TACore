using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeleportAddons {
    interface SyncSource {

        void SetupWithPlistRepresentation(Dictionary<string, object> plistRep);
        Dictionary<string, object> PlistRepresentation();

        string DisplayName();
        string LongDisplayName();
        string IconName();

        Boolean IsValid();

        Boolean LockSource();
        Boolean UnlockSource();
        void UpdateLockStatus();

        WoWInstall UnpackSyncSourceToLocationInFileSystem(string path);
        string PackAndStoreSyncSourceInFileSystem(WoWInstall install);

        string FetchSyncId();
        string FetchSyncDataHash();

        Boolean Locked {
            get;
        }

        Boolean LockedByMe {
            get;
        }
    }
}
