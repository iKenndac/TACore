using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KNFoundation;
using System.IO;
using DropNet;
using DropNet.Models;

namespace TACore {
    class DropboxSyncSource : SyncSource {

        public static string kConsumerKey = "o21wcwkodah76by";
        public static string kConsumerSecret = "8w2vcnqxa9lq2sx";

        public DropboxSyncSource() { }

        public DropboxSyncSource(string token, string secret, string user) {
            UserName = user;
            LoginToken = token;
            LoginSecret = secret;
            DropboxClient = new DropNetClient(kConsumerKey, kConsumerSecret, LoginToken, LoginSecret);
            //UpdateLockStatus();
        }

        public void SetupWithPlistRepresentation(Dictionary<string, object> plistRep) {
            UserName = (string)plistRep[Constants.kDropboxSyncSourceUserNameKey];
            LoginToken = (string)plistRep[Constants.kDropboxSyncSourceTokenKey];
            LoginSecret = (string)plistRep[Constants.kDropboxSyncSourceSecretKey];
            DropboxClient = new DropNetClient(kConsumerKey, kConsumerSecret, LoginToken, LoginSecret);
            //UpdateLockStatus();
        }

        public Dictionary<string, object> PlistRepresentation() {
            Dictionary<string, object> plist = new Dictionary<string, object>();
			plist[Constants.kDropboxSyncSourceUserNameKey] = UserName;
			plist[Constants.kDropboxSyncSourceTokenKey] = LoginToken;
			plist[Constants.kDropboxSyncSourceSecretKey] = LoginSecret;
            return plist;
        }

        public Boolean IsValid() {
            return (!String.IsNullOrWhiteSpace(LoginSecret)) && (!String.IsNullOrWhiteSpace(LoginToken));
        }

        public string DisplayName() {
            return KNBundleGlobalHelpers.KNLocalizedString("dropbox title", "");
        }

        public string LongDisplayName() {
            return String.Format(KNBundleGlobalHelpers.KNLocalizedString("dropbox account formatter", ""), UserName);
        }

        public string IconName() {
            return "Dropbox.png";
        }

        public bool LockSource() {

            Dictionary<string, object> plist = new Dictionary<string, object>();
            plist.Add(Constants.kSyncLockDateLockedKey, DateTime.Now);
            plist.Add(Constants.kSyncLockMachineIdentifierKey, System.Environment.MachineName);

            byte[] data = KNPropertyListSerialization.DataWithPropertyList(plist);

            if (data != null) {

                string lockPath = Constants.kDropBoxSyncStoreStoreDirectoryName + "/" +
                Constants.kDropBoxSyncStoreStoreFileName + "/";

				// MIGRATION: The call below used to return a bool. Loop up how this *actually* works now.
                return DropboxClient.UploadFile(lockPath, Constants.kSyncLockFileName, data) != null;

            } else {
                return false;
            }
        }

        public bool UnlockSource() {
           
            string lockPath = Constants.kDropBoxSyncStoreStoreDirectoryName + "/" +
                Constants.kDropBoxSyncStoreStoreFileName + "/" +
                Constants.kSyncLockFileName;

            if (FileOrDirectoryExistsAtDropboxPath(lockPath)) {

                // Exists 

                return DropboxClient.Delete(lockPath).Is_Deleted;

            } else {
                return true;
            }


        }

        public void UpdateLockStatus() {

            string lockPath = Constants.kDropBoxSyncStoreStoreDirectoryName + "/" +
                Constants.kDropBoxSyncStoreStoreFileName + "/" +
                Constants.kSyncLockFileName;

            if (FileOrDirectoryExistsAtDropboxPath(lockPath)) {

                Locked = true;

                byte[] data = DropboxClient.GetFile(lockPath);

                if (data == null) {
                    LockedByMe = false;
                } else {

                    Dictionary<string, object> plist = KNPropertyListSerialization.PropertyListWithData(data);
                    if (plist == null || plist[Constants.kSyncLockMachineIdentifierKey] == null) {
                        LockedByMe = false;
                    } else {
                        LockedByMe = (plist[Constants.kSyncLockMachineIdentifierKey].Equals(System.Environment.MachineName));
                    }
                }

            } else {
                Locked = false;
                LockedByMe = false;
            }
        }

        public WoWInstall UnpackSyncSourceToLocationInFileSystem(string path) {

            // We store the sync store in a zip file (kFileSyncStoreStoreFileName) inside our folder
            // Also assume that it's ok to dump our files directly into the given folder

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            if (Directory.Exists(Path.Combine(path, Constants.kWoWWTFFolderName))) {
                Directory.Delete(Path.Combine(path, Constants.kWoWWTFFolderName), true);
            }

            if (Directory.Exists(Path.Combine(path, Constants.kWoWInterfaceFolderName))) {
                Directory.Delete(Path.Combine(path, Constants.kWoWInterfaceFolderName), true);
            }

            string storePath = Constants.kDropBoxSyncStoreStoreDirectoryName + "/" +
                Constants.kDropBoxSyncStoreStoreFileName  + "/" +
                Constants.kFileSyncStoreStoreFileName;

            if (FileOrDirectoryExistsAtDropboxPath(storePath)) {

                byte[] fileContents = DropboxClient.GetFile(storePath);

                if (fileContents != null) {
                    Helpers.ExtractZipFileInStreamToDirectory(new MemoryStream(fileContents), path);
                }
            }

            return new WoWInstall(path);
        }

        public string PackAndStoreSyncSourceInFileSystem(WoWInstall install) {

            string wowSyncFolderPath = String.Concat(Constants.kDropBoxSyncStoreStoreDirectoryName, "/", Constants.kDropBoxSyncStoreStoreFileName);

            string temporaryZipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");

            Helpers.ArchiveInstallToZipFileAtDirectoryForBackup(install, temporaryZipPath);

            string storePath = String.Concat(wowSyncFolderPath, "/", Constants.kFileSyncStoreStoreFileName);
            string tempStorePath = String.Concat(storePath, "-incoming");
            string outgoingStorePath = String.Concat(storePath, "-outgoing");

            DropboxClient.UploadFile(wowSyncFolderPath, String.Concat(Constants.kFileSyncStoreStoreFileName, "-incoming"), File.ReadAllBytes(temporaryZipPath));

            // Remove existing outgoing store if present

            if (FileOrDirectoryExistsAtDropboxPath(outgoingStorePath)) {
                DropboxClient.Delete(outgoingStorePath);
            }

            // Move old store out of the way

            if (FileOrDirectoryExistsAtDropboxPath(storePath)) {
                DropboxClient.Move(storePath, outgoingStorePath);
            }

            //Move incoming store to proper place

            DropboxClient.Move(tempStorePath, storePath);

            // Remove outgoing store

            if (FileOrDirectoryExistsAtDropboxPath(outgoingStorePath)) {
                DropboxClient.Delete(outgoingStorePath);
            }

            try {
                File.Delete(temporaryZipPath);
            } catch (Exception) { }

            // Update UUID

            string storeSidecarPath = String.Concat(wowSyncFolderPath, "/", Constants.kFileSyncStoreStoreFileName, Constants.kSideCarFileSuffix);

            if (FileOrDirectoryExistsAtDropboxPath(storeSidecarPath)) {
                DropboxClient.Delete(storeSidecarPath);
            }

            string newId = System.Guid.NewGuid().ToString();

            Dictionary<string, object> plist = new Dictionary<string, object>();
			plist[Constants.kSideCarSyncSourceSyncIdKey] = newId;
			plist[Constants.kSideCarChecksumKey] = install.CalculateHashForInstallFiles();

            DropboxClient.UploadFile(
                wowSyncFolderPath, 
                String.Concat(Constants.kFileSyncStoreStoreFileName, Constants.kSideCarFileSuffix),
                KNPropertyListSerialization.DataWithPropertyList(plist)
                );

            return newId;

        }

        public string FetchSyncId() {

            string storeSideCarPath = Constants.kDropBoxSyncStoreStoreDirectoryName + "/" +
                Constants.kDropBoxSyncStoreStoreFileName + "/" +
                Constants.kFileSyncStoreStoreFileName + Constants.kSideCarFileSuffix;

            if (FileOrDirectoryExistsAtDropboxPath(storeSideCarPath)) {

                byte[] data = DropboxClient.GetFile(storeSideCarPath);

                if (data != null) {
                    Dictionary<string, object> plist = KNPropertyListSerialization.PropertyListWithData(data);
                    if (plist != null) {
                        return (string)plist[Constants.kSideCarSyncSourceSyncIdKey];
                    }
                }
            }
            return null;

        }

        public string FetchSyncDataHash() {

            string storeSideCarPath = Constants.kDropBoxSyncStoreStoreDirectoryName + "/" +
                Constants.kDropBoxSyncStoreStoreFileName + "/" +
                Constants.kFileSyncStoreStoreFileName + Constants.kSideCarFileSuffix;

            if (FileOrDirectoryExistsAtDropboxPath(storeSideCarPath)) {

                byte[] data = DropboxClient.GetFile(storeSideCarPath);

                if (data != null) {
                    Dictionary<string, object> plist = KNPropertyListSerialization.PropertyListWithData(data);
                    if (plist != null) {
                        return (string)plist[Constants.kSideCarChecksumKey];
                    }
                }
            }
            return null;
        }

        private Boolean FileOrDirectoryExistsAtDropboxPath(string path) {
            MetaData md = DropboxClient.GetMetaData(path);

            return (md != null && md.Path != null && md.Is_Deleted == false);
        }

        public string UserName { private set; get; }
        private string LoginToken { get; set; }
        private string LoginSecret { get; set; }
        public bool Locked { get; private set; }
        public bool LockedByMe { get; private set; }
        private DropNetClient DropboxClient { get; set; }
        
    }
}
