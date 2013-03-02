using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using KNFoundation;

namespace TACore {

	public enum BackupFailureReason : int {
		BackupFileDoesntExist,
		BackupFileAlreadyExists,
		RestoreFailed,
		NothingToBackup
	}

	public class BackupException : Exception {

		public BackupException(BackupFailureReason reason) {
			FailureReason = reason;
		}
	
		public BackupFailureReason FailureReason { get; private set; }
	}

    public class Backup {

        public Backup(string path, string aDescription, DateTime aDate) {
            DateCreated = aDate;
            BackupDescription = aDescription;
            FullPathToBackupFile = path;
        }

        public Backup(Dictionary<string, object> plistRepresentation, string backupFilePath) {
            DateCreated = (DateTime)plistRepresentation[Constants.kBackupPlistDateCreatedKey];
            BackupDescription = (string)plistRepresentation[Constants.kBackupPlistDescriptionKey];
            FullPathToBackupFile = backupFilePath;

        }

        public Dictionary<string, object> PlistRepresentation() {
            Dictionary<string, object> plist = new Dictionary<string, object>();
			plist[Constants.kBackupPlistDateCreatedKey] = DateCreated;
			plist[Constants.kBackupPlistDescriptionKey] = BackupDescription;
			plist[Constants.kBackupPlistBackupPathKey] = FullPathToBackupFile;
            return plist;
        }

        public void RestoreToInstall(WoWInstall install) {

            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            
            Directory.CreateDirectory(path);

            if (File.Exists(FullPathToBackupFile)) {

                Helpers.ExtractZipFileAtPathToDirectory(FullPathToBackupFile, path);
                WoWInstall sourceInstall = new WoWInstall(path);
                WoWInstall newTarget = sourceInstall.InstallByDuplicatingInstallToDirectory(install.DirectoryPath);

				if (newTarget == null)
					throw new BackupException(BackupFailureReason.RestoreFailed);

                new DirectoryInfo(path).TryToDelete();

            } else {
				throw new BackupException(BackupFailureReason.BackupFileDoesntExist);
            }

        }

        public string IconName() {
            return "zip.png";
        }

        #region Properties

        public DateTime DateCreated {
            get;
            private set;
        }

        public string BackupDescription {
            get;
            private set;
        }

        public string FullPathToBackupFile {
            get;
            private set;
        }

        #endregion
    }
}
