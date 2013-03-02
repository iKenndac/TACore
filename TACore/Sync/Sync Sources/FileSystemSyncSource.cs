using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KNFoundation;
using System.IO;

namespace TACore {
    public class FileSystemSyncSource : SyncSource {

        public FileSystemSyncSource() { }

        public FileSystemSyncSource(string filePath) {
            SyncSourceLocation = filePath;
            UpdateLockStatus();
        }

        public void SetupWithPlistRepresentation(Dictionary<string, object> plistRep) {
            SyncSourceLocation = (string)plistRep[Constants.kFileSystemSyncSourceLocationKey];
            UpdateLockStatus();
        }

        public Dictionary<string, object> PlistRepresentation() {
            Dictionary<string, object> plist = new Dictionary<string, object>();
			plist[Constants.kFileSystemSyncSourceLocationKey] = SyncSourceLocation;
            return plist;
        }

        public Boolean IsValid() {
            return Directory.Exists(SyncSourceLocation);
        }

        public string DisplayName() {
            return String.Format(KNBundleGlobalHelpers.KNLocalizedString("file sync title formatter", ""),
                Path.GetFileNameWithoutExtension(SyncSourceLocation));
        }

        public string LongDisplayName() {
            return SyncSourceLocation;
        }

        public string IconName() {
            return null;
        }

        public Boolean LockSource() {

            Dictionary<string, object> plist = new Dictionary<string, object>();
            plist.Add(Constants.kSyncLockDateLockedKey, DateTime.Now);
            plist.Add(Constants.kSyncLockMachineIdentifierKey, System.Environment.MachineName);

            byte[] data = KNPropertyListSerialization.DataWithPropertyList(plist);

            if (data != null) {

                try {
                    string lockPath = Path.Combine(SyncSourceLocation, Constants.kSyncLockFileName);
                    File.WriteAllBytes(lockPath, data);
                    UpdateLockStatus();
                    return true;
                } catch (Exception) {
                    return false;
                }

            }

            return false;
        }


        public Boolean UnlockSource() {
            // This will override any lock, including those made by others

            string lockPath = Path.Combine(SyncSourceLocation, Constants.kSyncLockFileName);

            if (File.Exists(lockPath)) {

                try {
                    File.Delete(lockPath);
                    UpdateLockStatus();
                    return true;
                } catch (Exception) {
                    return false;
                }

            } else {
                return true;
            }
        }

        public void UpdateLockStatus() {

            string lockPath = Path.Combine(SyncSourceLocation, Constants.kSyncLockFileName);

            if (File.Exists(lockPath)) {

                Locked = true;

                byte[] plistData = File.ReadAllBytes(lockPath);
                if (plistData != null) {
                    Dictionary<string, object> plist = KNPropertyListSerialization.PropertyListWithData(plistData);

                    if (plist.ContainsKey(Constants.kSyncLockMachineIdentifierKey) &&
                        plist[Constants.kSyncLockMachineIdentifierKey].Equals(System.Environment.MachineName)) {
                        LockedByMe = true;
                    } else {
                        LockedByMe = false;
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

            string storePath = Path.Combine(SyncSourceLocation, Constants.kFileSyncStoreStoreFileName);

            if (File.Exists(storePath)) {

                Helpers.ExtractZipFileAtPathToDirectory(storePath, path);
                return new WoWInstall(path);

            } else {
                // An empty install
                return new WoWInstall(path);
            }

        }

        public string PackAndStoreSyncSourceInFileSystem(WoWInstall install) {

            if (!Directory.Exists(SyncSourceLocation)) {
                Directory.CreateDirectory(SyncSourceLocation);
            }

            string storePath = Path.Combine(SyncSourceLocation, Constants.kFileSyncStoreStoreFileName);
            string tempStorePath = String.Concat(storePath, "-incoming");
            string outgoingStorePath = String.Concat(storePath, "-outgoing");

            Helpers.ArchiveInstallToZipFileAtDirectoryForBackup(install, tempStorePath);

            // Remove existing outgoing store if present

            if (File.Exists(outgoingStorePath)) {
                File.Delete(outgoingStorePath);
            }

            // Move old store out of the way

            if (File.Exists(storePath)) {
                File.Move(storePath, outgoingStorePath);
            }

            //Move incoming store to proper place

            File.Move(tempStorePath, storePath);

            // Remove outgoing store

            if (File.Exists(outgoingStorePath)) {
                File.Delete(outgoingStorePath);
            }

            // Update UUID

            string storeSidecarPath = Path.Combine(
                SyncSourceLocation,
                String.Concat(Constants.kFileSyncStoreStoreFileName, Constants.kSideCarFileSuffix)
                );

            if (File.Exists(storeSidecarPath)) {
                File.Delete(storeSidecarPath);
            }

            string newId = System.Guid.NewGuid().ToString();

            Dictionary<string, object> plist = new Dictionary<string, object>();
			plist[Constants.kSideCarSyncSourceSyncIdKey] = newId;
			plist[Constants.kSideCarChecksumKey] = install.CalculateHashForInstallFiles();

            File.WriteAllBytes(storeSidecarPath, KNPropertyListSerialization.DataWithPropertyList(plist));

            return newId;
        }
        
        public string FetchSyncId() {

            string storeSideCarPath = Path.Combine(
                SyncSourceLocation,
                String.Concat(Constants.kFileSyncStoreStoreFileName, Constants.kSideCarFileSuffix)
                );

            if (File.Exists(storeSideCarPath)) {

                byte[] data = File.ReadAllBytes(storeSideCarPath);

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

            string storeSideCarPath = Path.Combine(
                SyncSourceLocation,
                String.Concat(Constants.kFileSyncStoreStoreFileName, Constants.kSideCarFileSuffix)
                );

            if (File.Exists(storeSideCarPath)) {

                byte[] data = File.ReadAllBytes(storeSideCarPath);

                if (data != null) {
                    Dictionary<string, object> plist = KNPropertyListSerialization.PropertyListWithData(data);
                    if (plist != null) {
                        return (string)plist[Constants.kSideCarChecksumKey];
                    }
                }
            }
            return null;
        }

        public string SyncSourceLocation {
            private set;
            get;
        }

        public Boolean Locked {
            private set;
            get;
        }

        public Boolean LockedByMe {
            private set;
            get;
        }


    }
}
